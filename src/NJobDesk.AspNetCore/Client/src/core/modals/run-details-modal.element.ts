import { css, html, nothing } from "lit";
import { customElement, state } from "lit/decorators.js";
import { UUITextStyles } from "@umbraco-ui/uui-css/lib";
import { NJobDeskModalBaseElement } from "./modal-base.element.js";
import { ExecutionsService, type ExecutionLogModel, type ExecutionModel } from "../api/index.js";
import { findProvider } from "../services/dashboard-state.js";
import { formatDateTime, formatDuration } from "../utils/format.js";
import type { NJobDeskKvItem } from "../components/kv-list.element.js";
import "../components/modal-layout.element.js";
import "../components/code-block.element.js";
import "../components/kv-list.element.js";
import "../components/log-console.element.js";
import "../components/state-tag.element.js";
import "../components/relative-time.element.js";

export interface NJobDeskRunDetailsModalData {
  execution: ExecutionModel;
}

@customElement("njd-run-details-modal")
export class NJobDeskRunDetailsModalElement extends NJobDeskModalBaseElement<NJobDeskRunDetailsModalData, never> {
  @state()
  private _logs: ExecutionLogModel[] = [];

  @state()
  private _loading = true;

  connectedCallback() {
    super.connectedCallback();
    this.#loadLogs();
  }

  async #loadLogs() {
    const execution = this.data?.execution;
    if (!execution) {
      this._loading = false;
      return;
    }

    const response = await ExecutionsService.getExecutionLogs({ path: { id: execution.id } });
    this._logs = response.data ?? [];
    this._loading = false;
  }

  get #detailItems(): NJobDeskKvItem[] {
    const execution = this.data!.execution;
    return [
      {
        name: this.localize.term("njobdesk_colJob"),
        value: execution.jobGroup ? `${execution.jobGroup}/${execution.jobName}` : execution.jobName,
        monospace: true,
      },
      ...(execution.triggerName
        ? [
            {
              name: this.localize.term("njobdesk_colTrigger"),
              value: execution.triggerName,
              monospace: true,
            },
          ]
        : []),
      {
        name: this.localize.term("njobdesk_colStarted"),
        value: html`${formatDateTime(execution.startedUtc)}
          (<njd-relative-time .date=${execution.startedUtc}></njd-relative-time>)`,
      },
      ...(execution.finishedUtc
        ? [{ name: this.localize.term("njobdesk_colFinished"), value: formatDateTime(execution.finishedUtc) }]
        : []),
      { name: this.localize.term("njobdesk_colDuration"), value: formatDuration(execution.durationMs) },
      { name: this.localize.term("njobdesk_colNode"), value: execution.schedulerInstanceId, monospace: true },
    ];
  }

  render() {
    const execution = this.data?.execution;
    if (!execution) {
      return nothing;
    }

    const runLogs = findProvider(execution.providerKey)?.capabilities.runLogs ?? true;
    return html`
      <njd-modal-layout
        headline=${execution.jobGroup ? `${execution.jobGroup}/${execution.jobName}` : execution.jobName}>
        <div class="uui-text layout">
          <div class="run-header" data-mark="njobdesk:run-header">
            <njd-state-tag kind="execution" .value=${execution.state}></njd-state-tag>
          </div>
          <uui-box headline=${this.localize.term("njobdesk_detailHeadline")}>
            <njd-kv-list .items=${this.#detailItems}></njd-kv-list>
          </uui-box>

          ${execution.errorMessage
            ? html`
                <uui-box headline=${this.localize.term("njobdesk_colError")}>
                  <njd-code-block>${execution.errorMessage}</njd-code-block>
                </uui-box>
              `
            : nothing}

          ${runLogs
            ? html`
                <uui-box headline=${this.localize.term("njobdesk_modalRunLogs")}>
                  ${execution.state === "Running"
                    ? html`<p class="running-note">${this.localize.term("njobdesk_logsPendingRun")}</p>`
                    : html`<njd-log-console .entries=${this._logs} ?loading=${this._loading}></njd-log-console>`}
                </uui-box>
              `
            : nothing}
        </div>

        <uui-button
          slot="actions"
          look="secondary"
          label=${this.localize.term("general_close")}
          @click=${() => this._rejectModal()}></uui-button>
      </njd-modal-layout>
    `;
  }

  static styles = [
    UUITextStyles,
    css`
      :host {
        display: block;
        height: 100%;
      }

      .layout > * + * {
        margin-top: var(--uui-size-layout-1);
      }

      .run-header {
        display: flex;
        align-items: center;
        gap: var(--uui-size-space-4);
      }

      .run-header h3 {
        margin: 0;
        font-size: var(--uui-type-h5-size);
        font-weight: 600;
        overflow-wrap: anywhere;
      }

      njd-code-block {
        margin: 0;
      }

      .running-note {
        margin: 0;
        color: var(--uui-color-text-alt);
      }
    `,
  ];
}

export default NJobDeskRunDetailsModalElement;

declare global {
  interface HTMLElementTagNameMap {
    "njd-run-details-modal": NJobDeskRunDetailsModalElement;
  }
}
