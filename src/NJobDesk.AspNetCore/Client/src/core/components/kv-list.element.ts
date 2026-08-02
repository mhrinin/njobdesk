import { LitElement, css, html } from "lit";
import { customElement, property } from "lit/decorators.js";
import type { TemplateResult } from "lit";

export interface NJobDeskKvItem {
  name: string;
  value: string | TemplateResult;
  monospace?: boolean;
}

@customElement("njd-kv-list")
export class NJobDeskKvListElement extends LitElement {
  @property({ attribute: false })
  items: NJobDeskKvItem[] = [];

  render() {
    return html`
      <dl>
        ${this.items.map(
          (item) => html`
            <div class="property">
              <dt>${item.name}</dt>
              <dd class=${item.monospace ? "monospace" : ""}>${item.value}</dd>
            </div>
          `,
        )}
      </dl>
    `;
  }

  static styles = css`
    :host {
      display: block;
    }

    dl {
      margin: 0;
    }

    .property {
      display: flex;
      gap: var(--uui-size-space-5);
      padding: var(--uui-size-space-3) 0;
    }

    .property + .property {
      border-top: 1px solid var(--uui-color-divider);
    }

    dt {
      font-weight: 600;
      flex: 1 1 20ch;
    }

    dd {
      margin: 0;
      flex: 3 0 20ch;
      overflow-wrap: anywhere;
    }

    dd.monospace {
      font-family: monospace;
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-kv-list": NJobDeskKvListElement;
  }
}
