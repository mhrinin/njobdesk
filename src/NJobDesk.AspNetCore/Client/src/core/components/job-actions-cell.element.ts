import { css, html, nothing } from "lit";
import { customElement, property } from "lit/decorators.js";
import { NJobDeskElement } from "../element.js";
import type { JobState } from "../api/index.js";
import { ReadOnlyController } from "../services/dashboard-state.js";
import { popoverMenuStyles } from "../utils/shared-styles.js";

export type NJobDeskJobAction = "trigger" | "pause" | "resume" | "delete";

export class NJobDeskJobActionEvent extends CustomEvent<{ action: NJobDeskJobAction; jobId: string }> {
  public static readonly TYPE = "njd-job-action";

  public constructor(action: NJobDeskJobAction, jobId: string) {
    super(NJobDeskJobActionEvent.TYPE, { detail: { action, jobId }, bubbles: true, composed: true });
  }
}

@customElement("njd-job-actions-cell")
export class NJobDeskJobActionsCellElement extends NJobDeskElement {
  @property()
  jobId = "";

  @property()
  state: JobState | undefined;

  #readOnly = new ReadOnlyController(this);

  #dispatch(action: NJobDeskJobAction) {
    this.shadowRoot?.querySelector<HTMLElement & { hidePopover(): void }>("uui-popover-container")?.hidePopover();
    this.dispatchEvent(new NJobDeskJobActionEvent(action, this.jobId));
  }

  render() {
    if (this.#readOnly.readOnly) {
      return nothing;
    }

    return html`
      <uui-action-bar>
        <uui-button
          compact
          look="secondary"
          data-mark="njobdesk:action:trigger"
          label=${this.localize.term("njobdesk_actionTrigger")}
          @click=${() => this.#dispatch("trigger")}>
          <uui-icon name="icon-play"></uui-icon>
        </uui-button>
        ${this.state === "Paused"
          ? html`
              <uui-button
                compact
                look="secondary"
                data-mark="njobdesk:action:resume"
                label=${this.localize.term("njobdesk_actionResume")}
                @click=${() => this.#dispatch("resume")}>
                <uui-icon name="icon-refresh"></uui-icon>
              </uui-button>
            `
          : html`
              <uui-button
                compact
                look="secondary"
                data-mark="njobdesk:action:pause"
                label=${this.localize.term("njobdesk_actionPause")}
                @click=${() => this.#dispatch("pause")}>
                <uui-icon name="icon-pause"></uui-icon>
              </uui-button>
            `}
        <uui-button
          compact
          look="secondary"
          popovertarget="more"
          label=${this.localize.term("njobdesk_actionMore")}>
          <uui-symbol-more></uui-symbol-more>
        </uui-button>
      </uui-action-bar>
      <uui-popover-container id="more" placement="bottom-end">
        <div class="menu">
          <uui-button
            look="default"
            color="danger"
            data-mark="njobdesk:action:delete"
            label=${this.localize.term("njobdesk_actionDelete")}
            @click=${() => this.#dispatch("delete")}>
            <uui-icon name="icon-trash"></uui-icon>
            ${this.localize.term("njobdesk_actionDelete")}
          </uui-button>
        </div>
      </uui-popover-container>
    `;
  }

  static styles = [
    popoverMenuStyles,
    css`
      :host {
        display: inline-block;
      }
    `,
  ];
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-job-actions-cell": NJobDeskJobActionsCellElement;
  }
}
