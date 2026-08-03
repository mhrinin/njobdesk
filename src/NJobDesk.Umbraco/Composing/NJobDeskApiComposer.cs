using Asp.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NJobDesk.AspNetCore.Controllers;
using NJobDesk.AspNetCore.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.Authorization;
#if NET10_0_OR_GREATER
using Microsoft.OpenApi;
#else
using Microsoft.OpenApi.Models;
#endif

namespace NJobDesk.Umbraco.Composing;

public sealed class NJobDeskApiComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // The API serves at the same default prefix (/njobdesk/api/v1) as standalone hosts, so one
        // generated client works everywhere. The dashboard policy authenticates the backoffice
        // bearer scheme and delegates to the backoffice Settings-section policy, so backoffice
        // authentication guards both the API and the UI.
        builder.Services.AddNJobDeskControllers(options =>
        {
            options.AuthorizationPolicy = AuthorizationPolicies.SectionAccessSettings;
            options.AuthenticationSchemes = [OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme];
        });

        builder.Services.AddSingleton<IOperationIdHandler, NJobDeskOperationIdHandler>();
        builder.Services.AddSingleton<ISchemaIdHandler, NJobDeskSchemaIdHandler>();

        // PostConfigure so this reliably runs after Umbraco's swagger setup, which sets its own
        // DocInclusionPredicate; the controllers don't carry [MapToApi], so route them into our
        // swagger document (and out of every other document) explicitly, preserving Umbraco's
        // predicate for the rest.
        builder.Services.PostConfigure<SwaggerGenOptions>(options =>
        {
            options.SwaggerDoc(Constants.ApiName, new OpenApiInfo
            {
                Title = "NJobDesk Backoffice API",
                Version = "1.0",
            });

            // Umbraco 16 already registers this scheme id globally; 17 does not.
            if (!options.SwaggerGeneratorOptions.SecuritySchemes.ContainsKey(BackOfficeSchemeId))
            {
                options.AddSecurityDefinition(BackOfficeSchemeId, new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                });
            }
            options.OperationFilter<NJobDeskOperationSecurityFilter>();
            options.DocumentFilter<NJobDeskSecurityDocumentFilter>();

            var previous = options.SwaggerGeneratorOptions.DocInclusionPredicate;
            options.DocInclusionPredicate((documentName, apiDescription) =>
            {
                var isNJobDesk = apiDescription.ActionDescriptor is ControllerActionDescriptor controllerAction
                    && typeof(NJobDeskApiControllerBase).IsAssignableFrom(controllerAction.ControllerTypeInfo);
                return documentName == Constants.ApiName ? isNJobDesk : !isNJobDesk && previous(documentName, apiDescription);
            });
        });
    }

    // Umbraco's default schema-id handler doesn't expand generic arguments for non-Cms types, so our
    // PagedResult<T> closed generics would collide. Match Umbraco's Paged{T} naming.
    public sealed class NJobDeskSchemaIdHandler : ISchemaIdHandler
    {
        public bool CanHandle(Type type) =>
            type.Namespace?.StartsWith("NJobDesk", StringComparison.Ordinal) is true;

        public string Handle(Type type) => NJobDeskSchemaId(type);
    }

    private static string NJobDeskSchemaId(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var name = type.Name[..type.Name.IndexOf('`')];
        if (name.EndsWith("Result", StringComparison.Ordinal))
        {
            name = name[..^"Result".Length];
        }

        return name + string.Concat(type.GetGenericArguments().Select(NJobDeskSchemaId));
    }

    internal const string BackOfficeSchemeId = "Backoffice User";

    // Umbraco's BackOfficeSecurityRequirementsOperationFilterBase keys off [MapToApi], which the
    // neutral controllers don't carry, so declare the security requirement + 401 ourselves.
    public sealed class NJobDeskOperationSecurityFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor controllerAction
                || !typeof(NJobDeskApiControllerBase).IsAssignableFrom(controllerAction.ControllerTypeInfo))
            {
                return;
            }

            operation.Responses ??= [];
            operation.Responses.TryAdd("401", new OpenApiResponse
            {
                Description = "The resource is protected and requires an authentication token",
            });
        }
    }

    // Security requirements must reference the scheme through the host document or they are omitted
    // during serialization, so they are attached document-wide after generation.
    public sealed class NJobDeskSecurityDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument document, DocumentFilterContext context)
        {
            if (context.DocumentName != Constants.ApiName)
            {
                return;
            }

            foreach (var path in document.Paths.Values)
            {
                foreach (var operation in path.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>())
                {
#if NET10_0_OR_GREATER
                    operation.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(BackOfficeSchemeId, document)] = [] }];
#else
                    operation.Security =
                    [
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = BackOfficeSchemeId },
                            }] = [],
                        },
                    ];
#endif
                }
            }
        }
    }

    public sealed class NJobDeskOperationIdHandler(IOptions<ApiVersioningOptions> apiVersioningOptions)
        : OperationIdHandler(apiVersioningOptions)
    {
        protected override bool CanHandle(ApiDescription apiDescription, ControllerActionDescriptor controllerActionDescriptor) =>
            typeof(NJobDeskApiControllerBase).IsAssignableFrom(controllerActionDescriptor.ControllerTypeInfo);

        public override string Handle(ApiDescription apiDescription) =>
            $"{apiDescription.ActionDescriptor.RouteValues["action"]}";
    }
}
