import { css, html, nothing } from "lit";
import { customElement } from "lit/decorators.js";
import { NJobDeskElement } from "../element.js";
import { ProvidersController } from "../services/dashboard-state.js";

@customElement("njd-provider-banner")
export class NJobDeskProviderBannerElement extends NJobDeskElement {
  #providers = new ProvidersController(this);

  render() {
    const degraded = this.#providers.degraded;
    if (degraded.length === 0) {
      return nothing;
    }

    return html`
      <div class="banner" role="alert" data-mark="njobdesk:provider-banner">
        <uui-icon name="icon-alert"></uui-icon>
        <div class="messages">
          ${degraded.map(
            (provider) => html`
              <p>
                <strong>${this.localize.term("njobdesk_providerDegraded", provider.displayName)}</strong>
                ${provider.error ? html`<span class="reason">${provider.error}</span>` : nothing}
              </p>
            `,
          )}
        </div>
      </div>
    `;
  }

  static styles = css`
    :host {
      display: block;
    }

    .banner {
      display: flex;
      align-items: flex-start;
      gap: var(--uui-size-space-4);
      padding: var(--uui-size-space-4) var(--uui-size-layout-1);
      background-color: var(--uui-color-warning);
      color: var(--uui-color-warning-contrast);
      border-bottom: 1px solid var(--uui-color-border);
    }

    uui-icon {
      flex-shrink: 0;
      margin-top: 2px;
    }

    .messages p {
      margin: 0;
    }

    .messages p + p {
      margin-top: var(--uui-size-space-2);
    }

    .reason {
      opacity: 0.8;
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-provider-banner": NJobDeskProviderBannerElement;
  }
}
