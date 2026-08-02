import { css, html, nothing } from "lit";
import { customElement, property } from "lit/decorators.js";
import { UUITextStyles } from "@umbraco-ui/uui-css/lib";
import { NJobDeskElement } from "../element.js";
import { ReadOnlyController } from "../services/dashboard-state.js";
import type { JobDetailModel, TriggerModel } from "../api/index.js";
import { popoverMenuStyles } from "../utils/shared-styles.js";
import { NJobDeskJobActionEvent } from "../components/job-actions-cell.element.js";
import type { NJobDeskCronCellValue } from "../components/cron-cell.element.js";
import "../components/trigger-actions-cell.element.js";
import "../components/provider-tag.element.js";
import "../components/state-tag.element.js";
import "../components/cron-cell.element.js";
import "../components/kv-list.element.js";
import "../components/run-timeline.element.js";
import "../components/relative-time.element.js";
import type { NJobDeskKvItem } from "../components/kv-list.element.js";

export class NJobDeskJobDetailCloseEvent extends Event {
  public static readonly TYPE = "njd-job-detail-close";

  public constructor() {
    super(NJobDeskJobDetailCloseEvent.TYPE, { bubbles: true, composed: true });
  }
}

@customElement("njd-job-detail")
export class NJobDeskJobDetailElement extends NJobDeskElement {
  #readOnly = new ReadOnlyController(this);

  @property({ attribute: false })
  detail?: JobDetailModel;

  render() {
    const detail = this.detail;
    if (!detail) {
      return html`<uui-loader-bar></uui-loader-bar>`;
    }

    const jobId = detail.job.id;
    const capabilities = detail.job.capabilities;
    const yesNo = (value: boolean) => this.localize.term(value ? "njobdesk_yes" : "njobdesk_no");
    const detailItems: NJobDeskKvItem[] = [
      { name: this.localize.term("njobdesk_detailJobType"), value: detail.job.jobType ?? "—", monospace: true },
      ...(detail.job.description
        ? [{ name: this.localize.term("njobdesk_detailDescription"), value: detail.job.description }]
        : []),
      ...(detail.job.durable != null
        ? [{ name: this.localize.term("njobdesk_detailDurable"), value: yesNo(detail.job.durable) }]
        : []),
      ...(detail.job.concurrentExecutionDisallowed != null
        ? [
            {
              name: this.localize.term("njobdesk_detailConcurrent"),
              value: yesNo(detail.job.concurrentExecutionDisallowed),
            },
          ]
        : []),
      { name: this.localize.term("njobdesk_detailSystemJob"), value: yesNo(detail.job.isSystemJob) },
    ];

    return html`
      <div class="uui-text layout">
        <div class="detail-header" data-mark="njobdesk:detail-header">
          <uui-button
            compact
            look="default"
            data-mark="njobdesk:action:back"
            label=${this.localize.term("njobdesk_detailBack")}
            @click=${() => this.dispatchEvent(new NJobDeskJobDetailCloseEvent())}>
            <uui-icon name="icon-arrow-left"></uui-icon>
          </uui-button>
          <h3>${detail.job.group ? html`<span class="group">${detail.job.group} /</span> ` : nothing}${detail.job.name}</h3>
          <njd-provider-tag .providerKey=${detail.job.providerKey}></njd-provider-tag>
          <njd-state-tag .value=${detail.job.state}></njd-state-tag>
          <div class="detail-actions" ?hidden=${this.#readOnly.readOnly}>
            ${capabilities.triggerNow
              ? html`
                  <uui-button
                    look="primary"
                    color="positive"
                    compact
                    data-mark="njobdesk:action:trigger"
                    label=${this.localize.term("njobdesk_actionTrigger")}
                    @click=${() => this.dispatchEvent(new NJobDeskJobActionEvent("trigger", jobId))}>
                    <uui-icon name="icon-play"></uui-icon>
                    ${this.localize.term("njobdesk_actionTrigger")}
                  </uui-button>
                `
              : nothing}
            ${this.#renderPauseResume(detail, jobId)}
            ${capabilities.delete
              ? html`
                  <uui-button
                    compact
                    look="secondary"
                    popovertarget="detail-more"
                    label=${this.localize.term("njobdesk_actionMore")}>
                    <uui-symbol-more></uui-symbol-more>
                  </uui-button>
                  <uui-popover-container id="detail-more" placement="bottom-end">
                    <div class="menu">
                      <uui-button
                        look="default"
                        color="danger"
                        label=${this.localize.term("njobdesk_actionDelete")}
                        @click=${() => this.dispatchEvent(new NJobDeskJobActionEvent("delete", jobId))}>
                        <uui-icon name="icon-trash"></uui-icon>
                        ${this.localize.term("njobdesk_actionDelete")}
                      </uui-button>
                    </div>
                  </uui-popover-container>
                `
              : nothing}
          </div>
        </div>

        <uui-box headline=${this.localize.term("njobdesk_detailHeadline")}>
          <njd-kv-list .items=${detailItems}></njd-kv-list>
        </uui-box>

        <uui-box class="flush" headline=${this.localize.term("njobdesk_detailTriggers")}>
          ${this.#renderTriggersTable(detail)}
        </uui-box>

        <uui-box headline=${this.localize.term("njobdesk_detailTimeline")}>
          <njd-run-timeline .executions=${detail.recentExecutions}></njd-run-timeline>
        </uui-box>
      </div>
    `;
  }

  #renderPauseResume(detail: JobDetailModel, jobId: string) {
    if (!detail.job.capabilities.pause) {
      return nothing;
    }

    return detail.job.state === "Paused"
      ? html`
          <uui-button
            look="secondary"
            compact
            label=${this.localize.term("njobdesk_actionResume")}
            @click=${() => this.dispatchEvent(new NJobDeskJobActionEvent("resume", jobId))}>
            ${this.localize.term("njobdesk_actionResume")}
          </uui-button>
        `
      : html`
          <uui-button
            look="secondary"
            compact
            label=${this.localize.term("njobdesk_actionPause")}
            @click=${() => this.dispatchEvent(new NJobDeskJobActionEvent("pause", jobId))}>
            ${this.localize.term("njobdesk_actionPause")}
          </uui-button>
        `;
  }

  #renderTriggersTable(detail: JobDetailModel) {
    return html`
      <uui-table data-mark="njobdesk:table:triggers">
        <uui-table-head>
          <uui-table-head-cell>${this.localize.term("njobdesk_colTrigger")}</uui-table-head-cell>
          <uui-table-head-cell>${this.localize.term("njobdesk_colType")}</uui-table-head-cell>
          <uui-table-head-cell>${this.localize.term("njobdesk_colState")}</uui-table-head-cell>
          <uui-table-head-cell>${this.localize.term("njobdesk_colSchedule")}</uui-table-head-cell>
          <uui-table-head-cell class="right">${this.localize.term("njobdesk_colNextFire")}</uui-table-head-cell>
          <uui-table-head-cell class="right">${this.localize.term("njobdesk_colLastFire")}</uui-table-head-cell>
          <uui-table-head-cell>${this.localize.term("njobdesk_colMisfire")}</uui-table-head-cell>
          <uui-table-head-cell class="right"></uui-table-head-cell>
        </uui-table-head>
        ${detail.triggers.map(
          (trigger: TriggerModel) => html`
            <uui-table-row>
              <uui-table-cell>${trigger.group ? `${trigger.group}/${trigger.name}` : trigger.name}</uui-table-cell>
              <uui-table-cell>${trigger.type}</uui-table-cell>
              <uui-table-cell>
                <njd-state-tag .value=${trigger.state}></njd-state-tag>
              </uui-table-cell>
              <uui-table-cell>
                <njd-cron-cell
                  .value=${{
                    summary: trigger.cronSummary,
                    cronExpression: trigger.cronExpression,
                  } satisfies NJobDeskCronCellValue}></njd-cron-cell>
              </uui-table-cell>
              <uui-table-cell class="right">
                ${trigger.nextFireTimeUtc
                  ? html`<njd-relative-time .date=${trigger.nextFireTimeUtc}></njd-relative-time>`
                  : "—"}
              </uui-table-cell>
              <uui-table-cell class="right">
                ${trigger.previousFireTimeUtc
                  ? html`<njd-relative-time .date=${trigger.previousFireTimeUtc}></njd-relative-time>`
                  : "—"}
              </uui-table-cell>
              <uui-table-cell>${trigger.misfireInstruction ?? "—"}</uui-table-cell>
              <uui-table-cell class="right">
                <njd-trigger-actions-cell
                  .triggerId=${trigger.id}
                  .state=${trigger.state}
                  .type=${trigger.type}
                  .capabilities=${detail.job.capabilities}></njd-trigger-actions-cell>
              </uui-table-cell>
            </uui-table-row>
          `,
        )}
      </uui-table>
    `;
  }

  static styles = [
    UUITextStyles,
    popoverMenuStyles,
    css`
      :host {
        display: block;
      }

      .layout > * + * {
        margin-top: var(--uui-size-layout-1);
      }

      uui-table-cell {
        height: var(--uui-size-16);
      }

      .right {
        text-align: right;
      }

      .detail-header {
        display: flex;
        align-items: center;
        gap: var(--uui-size-space-4);
        background-color: var(--uui-color-surface);
        border-radius: var(--uui-border-radius);
        box-shadow: var(--uui-shadow-depth-1);
        padding: var(--uui-size-space-4) var(--uui-size-space-5);
      }

      .detail-header h3 {
        margin: 0;
        font-size: var(--uui-type-h4-size);
        font-weight: 400;
      }

      .detail-header h3 .group {
        color: var(--uui-color-text-alt);
      }

      .detail-actions {
        display: flex;
        align-items: center;
        gap: var(--uui-size-space-3);
        margin-left: auto;
      }

      .detail-actions[hidden] {
        display: none;
      }

      uui-box.flush {
        --uui-box-default-padding: 0;
      }
    `,
  ];
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-job-detail": NJobDeskJobDetailElement;
  }
}
