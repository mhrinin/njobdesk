import { LitElement, css, html, nothing } from "lit";
import { customElement, property } from "lit/decorators.js";

@customElement("njd-empty-state")
export class NJobDeskEmptyStateElement extends LitElement {
  @property()
  variant: "info" | "positive" = "info";

  @property()
  headline = "";

  @property()
  message = "";

  render() {
    return html`
      <div class="uui-text" id="empty-state">
        ${this.variant === "positive"
          ? html`<uui-icon name="icon-check" class="positive"></uui-icon>`
          : html`<uui-icon name="icon-info"></uui-icon>`}
        <h4>${this.headline}</h4>
        ${this.message ? html`<p>${this.message}</p>` : nothing}
        <slot></slot>
      </div>
    `;
  }

  static styles = css`
    :host {
      display: block;
    }

    #empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--uui-size-space-2);
      padding: var(--uui-size-layout-1);
      text-align: center;
      opacity: 0;
      animation: njd-fade-in 200ms 200ms forwards;
    }

    uui-icon {
      font-size: 1.8rem;
      color: var(--uui-color-text-alt);
    }

    uui-icon.positive {
      color: var(--uui-color-positive);
    }

    h4 {
      margin: 0;
    }

    p {
      margin: 0;
      color: var(--uui-color-text-alt);
    }

    @keyframes njd-fade-in {
      to {
        opacity: 1;
      }
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-empty-state": NJobDeskEmptyStateElement;
  }
}
