import { css, html } from "lit";
import { customElement, property } from "lit/decorators.js";
import { NJobDeskElement } from "../element.js";

/** Modal chrome (header, scrollable body, actions footer) replacing umb-body-layout. */
@customElement("njd-modal-layout")
export class NJobDeskModalLayoutElement extends NJobDeskElement {
  @property()
  headline = "";

  render() {
    return html`
      <div class="header"><h3>${this.headline}</h3></div>
      <uui-scroll-container class="main">
        <slot></slot>
      </uui-scroll-container>
      <div class="actions"><slot name="actions"></slot></div>
    `;
  }

  static styles = css`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      width: 100%;
      background-color: var(--uui-color-surface);
      box-sizing: border-box;
    }

    .header {
      flex-shrink: 0;
      display: flex;
      align-items: center;
      min-height: var(--uui-size-16);
      padding: 0 var(--uui-size-layout-1);
      border-bottom: 1px solid var(--uui-color-divider-standalone);
    }

    .header h3 {
      margin: 0;
      font-size: 15px;
    }

    .main {
      flex: 1;
      min-height: 0;
      padding: var(--uui-size-layout-1);
      box-sizing: border-box;
    }

    .actions {
      flex-shrink: 0;
      display: flex;
      justify-content: flex-end;
      gap: var(--uui-size-space-3);
      padding: var(--uui-size-space-4) var(--uui-size-layout-1);
      border-top: 1px solid var(--uui-color-divider-standalone);
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-modal-layout": NJobDeskModalLayoutElement;
  }
}
