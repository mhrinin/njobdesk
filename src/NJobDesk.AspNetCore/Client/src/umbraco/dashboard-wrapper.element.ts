import { html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { umbHttpClient } from "@umbraco-cms/backoffice/http-client";
import { configureNJobDeskClient } from "../core/config.js";
import "../core/njd-app.element.js";

/**
 * The only file that touches @umbraco-cms/backoffice. It must be an UmbLitElement (controller host)
 * because the backoffice dashboard view provides contexts on the created element; it passes the
 * backoffice HTTP configuration (origin, credentials, bearer token) to the shared app and mounts it.
 */
@customElement("njobdesk-umbraco-dashboard")
export class NJobDeskUmbracoDashboardElement extends UmbLitElement {
  connectedCallback() {
    super.connectedCallback();
    const backOfficeConfig = umbHttpClient.getConfig();
    configureNJobDeskClient({
      ...backOfficeConfig,
      baseUrl: `${backOfficeConfig.baseUrl ?? ""}/njobdesk/api/v1`,
    });
  }

  render() {
    return html`<njd-dashboard></njd-dashboard>`;
  }
}

export default NJobDeskUmbracoDashboardElement;

declare global {
  interface HTMLElementTagNameMap {
    "njobdesk-umbraco-dashboard": NJobDeskUmbracoDashboardElement;
  }
}
