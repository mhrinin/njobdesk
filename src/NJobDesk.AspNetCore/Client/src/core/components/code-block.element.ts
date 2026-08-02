import { css, html } from "lit";
import { customElement, state } from "lit/decorators.js";
import { NJobDeskElement } from "../element.js";

@customElement("njd-code-block")
export class NJobDeskCodeBlockElement extends NJobDeskElement {
  @state()
  private _copied = false;

  #copiedHandle?: number;

  disconnectedCallback() {
    super.disconnectedCallback();
    window.clearTimeout(this.#copiedHandle);
  }

  async #copy() {
    await navigator.clipboard.writeText(this.textContent?.trim() ?? "");
    this._copied = true;
    window.clearTimeout(this.#copiedHandle);
    this.#copiedHandle = window.setTimeout(() => (this._copied = false), 2000);
  }

  render() {
    return html`
      <div id="header">
        <uui-button
          compact
          label=${this.localize.term("general_copy")}
          @click=${() => this.#copy()}>
          <uui-icon name=${this._copied ? "icon-check" : "icon-documents"}></uui-icon>
          ${this.localize.term("general_copy")}
        </uui-button>
      </div>
      <pre><code><slot></slot></code></pre>
    `;
  }

  static styles = css`
    :host {
      display: block;
      border: 1px solid var(--uui-color-divider-emphasis);
      border-radius: var(--uui-border-radius);
      overflow: hidden;
    }

    #header {
      display: flex;
      justify-content: flex-end;
      background-color: var(--uui-color-surface-alt);
      border-bottom: 1px solid var(--uui-color-divider-emphasis);
    }

    pre {
      font-family: monospace;
      background-color: var(--uui-color-surface-alt);
      color: var(--uui-color-text);
      display: block;
      margin: 0;
      padding: var(--uui-size-space-3) var(--uui-size-space-4);
      max-height: 400px;
      overflow-y: auto;
    }

    pre,
    code {
      white-space: pre-wrap;
      overflow-wrap: anywhere;
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-code-block": NJobDeskCodeBlockElement;
  }
}
