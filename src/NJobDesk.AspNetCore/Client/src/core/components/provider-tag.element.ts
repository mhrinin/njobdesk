import { css, html, nothing } from "lit";
import { customElement, property } from "lit/decorators.js";
import { NJobDeskElement } from "../element.js";
import { ProvidersController } from "../services/dashboard-state.js";

/** Badge naming the provider a job or run belongs to. Hidden while only one provider is registered. */
@customElement("njd-provider-tag")
export class NJobDeskProviderTagElement extends NJobDeskElement {
  @property()
  providerKey = "";

  #providers = new ProvidersController(this);

  render() {
    if (!this.providerKey || !this.#providers.multiProvider) {
      return nothing;
    }

    const provider = this.#providers.find(this.providerKey);
    return html`
      <uui-tag look="secondary" title=${provider?.displayName ?? this.providerKey}>
        ${provider?.displayName ?? this.providerKey}
      </uui-tag>
    `;
  }

  static styles = css`
    :host {
      display: inline-block;
    }

    uui-tag {
      white-space: nowrap;
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-provider-tag": NJobDeskProviderTagElement;
  }
}
