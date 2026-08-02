import { LitElement, css, html } from "lit";
import { customElement, property, state } from "lit/decorators.js";
import "./code-block.element.js";

@customElement("njd-error-cell")
export class NJobDeskErrorCellElement extends LitElement {
  @property()
  value: string | undefined;

  @state()
  private _expanded = false;

  render() {
    if (!this.value) {
      return html`—`;
    }

    if (this._expanded) {
      return html`
        <njd-code-block @click=${(event: Event) => event.stopPropagation()}>${this.value}</njd-code-block>
        <button type="button" class="collapse" @click=${() => (this._expanded = false)}>
          <uui-symbol-expand open></uui-symbol-expand>
        </button>
      `;
    }

    return html`
      <button type="button" class="preview" title=${this.value} @click=${() => (this._expanded = true)}>
        ${this.value}
      </button>
    `;
  }

  static styles = css`
    :host {
      display: block;
      max-width: 420px;
    }

    button {
      all: unset;
      cursor: pointer;
    }

    button:focus-visible {
      outline: 2px solid var(--uui-color-focus);
    }

    .preview {
      color: var(--uui-color-danger-standalone);
      display: block;
      max-width: 100%;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .collapse {
      display: block;
      margin-top: var(--uui-size-space-1);
      color: var(--uui-color-text-alt);
    }

    njd-code-block {
      margin: var(--uui-size-space-2) 0;
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-error-cell": NJobDeskErrorCellElement;
  }
}
