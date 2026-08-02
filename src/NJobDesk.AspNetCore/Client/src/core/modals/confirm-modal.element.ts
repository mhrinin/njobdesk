import { css, html } from "lit";
import { customElement } from "lit/decorators.js";
import { NJobDeskModalBaseElement } from "./modal-base.element.js";
import type { NJobDeskConfirmArgs } from "../services/modal.service.js";

@customElement("njd-confirm-modal")
export class NJobDeskConfirmModalElement extends NJobDeskModalBaseElement<NJobDeskConfirmArgs, void> {
  render() {
    return html`
      <uui-dialog-layout headline=${this.data?.headline ?? ""}>
        <div class="content">${this.data?.content}</div>
        <uui-button
          slot="actions"
          label=${this.localize.term("general_cancel")}
          @click=${() => this._rejectModal()}></uui-button>
        <uui-button
          slot="actions"
          look="primary"
          color=${this.data?.color ?? "positive"}
          label=${this.data?.confirmLabel ?? this.localize.term("general_confirm")}
          @click=${() => this._submitModal()}></uui-button>
      </uui-dialog-layout>
    `;
  }

  static styles = css`
    .content {
      max-width: 480px;
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-confirm-modal": NJobDeskConfirmModalElement;
  }
}
