import { css, html, nothing } from "lit";
import { customElement, property } from "lit/decorators.js";
import { NJobDeskElement } from "../element.js";
import type { JobState, SchedulerCapabilities, TriggerType } from "../api/index.js";
import { ReadOnlyController } from "../services/dashboard-state.js";
import { popoverMenuStyles } from "../utils/shared-styles.js";

export type NJobDeskTriggerAction = "pause" | "resume" | "unschedule" | "reset-error" | "edit" | "preview";

export class NJobDeskTriggerActionEvent extends CustomEvent<{ action: NJobDeskTriggerAction; triggerId: string }> {
  public static readonly TYPE = "njd-trigger-action";

  public constructor(action: NJobDeskTriggerAction, triggerId: string) {
    super(NJobDeskTriggerActionEvent.TYPE, { detail: { action, triggerId }, bubbles: true, composed: true });
  }
}

@customElement("njd-trigger-actions-cell")
export class NJobDeskTriggerActionsCellElement extends NJobDeskElement {
  @property()
  triggerId = "";

  @property()
  state: JobState | undefined;

  @property()
  type: TriggerType | undefined;

  @property({ attribute: false })
  capabilities: SchedulerCapabilities | undefined;

  #readOnly = new ReadOnlyController(this);

  #dispatch(action: NJobDeskTriggerAction) {
    this.shadowRoot?.querySelector<HTMLElement & { hidePopover(): void }>("uui-popover-container")?.hidePopover();
    this.dispatchEvent(new NJobDeskTriggerActionEvent(action, this.triggerId));
  }

  render() {
    const capabilities = this.capabilities;
    if (!this.state || !this.type || this.#readOnly.readOnly || !capabilities) {
      return nothing;
    }

    const isCron = this.type === "Cron";
    if (!capabilities.pause && !capabilities.scheduleEditing && !capabilities.delete && !isCron) {
      return nothing;
    }

    return html`
      <uui-action-bar>
        ${this.#renderPauseResume(capabilities)}
        ${capabilities.pause && this.state === "Error"
          ? html`
              <uui-button
                compact
                look="secondary"
                data-mark="njobdesk:action:reset-error"
                label=${this.localize.term("njobdesk_actionResetError")}
                @click=${() => this.#dispatch("reset-error")}>
                <uui-icon name="icon-undo"></uui-icon>
              </uui-button>
            `
          : nothing}
        ${capabilities.scheduleEditing
          ? html`
              <uui-button
                compact
                look="secondary"
                ?disabled=${!isCron}
                title=${!isCron ? this.localize.term("njobdesk_editOnlyCron") : ""}
                data-mark="njobdesk:action:edit"
                label=${this.localize.term("njobdesk_actionEdit")}
                @click=${() => this.#dispatch("edit")}>
                <uui-icon name="icon-edit"></uui-icon>
              </uui-button>
            `
          : nothing}
        ${isCron
          ? html`
              <uui-button
                compact
                look="secondary"
                data-mark="njobdesk:action:preview"
                label=${this.localize.term("njobdesk_actionPreview")}
                @click=${() => this.#dispatch("preview")}>
                <uui-icon name="icon-calendar"></uui-icon>
              </uui-button>
            `
          : nothing}
        ${capabilities.delete
          ? html`
              <uui-button
                compact
                look="secondary"
                popovertarget="more"
                label=${this.localize.term("njobdesk_actionMore")}>
                <uui-symbol-more></uui-symbol-more>
              </uui-button>
            `
          : nothing}
      </uui-action-bar>
      ${capabilities.delete
        ? html`
            <uui-popover-container id="more" placement="bottom-end">
              <div class="menu">
                <uui-button
                  look="default"
                  color="danger"
                  data-mark="njobdesk:action:unschedule"
                  label=${this.localize.term("njobdesk_actionUnschedule")}
                  @click=${() => this.#dispatch("unschedule")}>
                  <uui-icon name="icon-trash"></uui-icon>
                  ${this.localize.term("njobdesk_actionUnschedule")}
                </uui-button>
              </div>
            </uui-popover-container>
          `
        : nothing}
    `;
  }

  #renderPauseResume(capabilities: SchedulerCapabilities) {
    if (!capabilities.pause) {
      return nothing;
    }

    return this.state === "Paused"
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
    "njd-trigger-actions-cell": NJobDeskTriggerActionsCellElement;
  }
}
