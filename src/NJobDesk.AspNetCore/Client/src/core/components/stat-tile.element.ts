import { LitElement, css, html, nothing } from "lit";
import { customElement, property } from "lit/decorators.js";

export class NJobDeskTileOpenEvent extends CustomEvent<{ tile: string }> {
  public static readonly TYPE = "njd-tile-open";

  public constructor(tile: string) {
    super(NJobDeskTileOpenEvent.TYPE, { detail: { tile }, bubbles: true, composed: true });
  }
}

@customElement("njd-stat-tile")
export class NJobDeskStatTileElement extends LitElement {
  @property()
  tile = "";

  @property()
  label = "";

  @property({ attribute: false })
  value?: number;

  @property()
  accent: "default" | "positive" | "warning" | "danger" = "default";

  @property({ type: Boolean })
  pulse = false;

  render() {
    return html`
      <uui-box class=${this.accent}>
        <button type="button" data-mark="njobdesk:tile:${this.tile}" @click=${() => this.dispatchEvent(new NJobDeskTileOpenEvent(this.tile))}>
          <span class="value">
            ${this.value === undefined ? html`<uui-loader></uui-loader>` : this.value}
            ${this.pulse && (this.value ?? 0) > 0 ? html`<span class="pulse" aria-hidden="true"></span>` : nothing}
          </span>
          <span class="label">${this.label}</span>
        </button>
      </uui-box>
    `;
  }

  static styles = css`
    :host {
      display: block;
    }

    uui-box {
      --uui-box-default-padding: 0;
      height: 100%;
      border-top: 3px solid transparent;
    }

    uui-box.positive {
      border-top-color: var(--uui-color-positive);
    }

    uui-box.warning {
      border-top-color: var(--uui-color-warning);
    }

    uui-box.danger {
      border-top-color: var(--uui-color-danger);
    }

    button {
      all: unset;
      cursor: pointer;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: var(--uui-size-space-2);
      width: 100%;
      box-sizing: border-box;
      padding: var(--uui-size-space-5) var(--uui-size-space-4);
    }

    button:focus-visible {
      outline: 2px solid var(--uui-color-focus);
      outline-offset: -2px;
    }

    button:hover .label {
      color: var(--uui-color-interactive-emphasis);
    }

    .value {
      display: inline-flex;
      align-items: center;
      gap: var(--uui-size-space-3);
      font-size: 3rem;
      font-weight: 300;
      line-height: 1;
      font-variant-numeric: tabular-nums;
    }

    uui-box.danger .value {
      color: var(--uui-color-danger);
    }

    uui-box.positive .value {
      color: var(--uui-color-positive);
    }

    uui-box.warning .value {
      color: var(--uui-color-warning-standalone);
    }

    .label {
      font-size: var(--uui-type-small-size);
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--uui-color-text-alt);
    }

    .pulse {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      background-color: var(--uui-color-positive);
      animation: njd-pulse 1.5s ease-in-out infinite;
    }

    @keyframes njd-pulse {
      0%,
      100% {
        opacity: 1;
        transform: scale(1);
      }
      50% {
        opacity: 0.4;
        transform: scale(0.75);
      }
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-stat-tile": NJobDeskStatTileElement;
  }
}
