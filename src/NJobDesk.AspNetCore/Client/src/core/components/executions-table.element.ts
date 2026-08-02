import { css, html } from "lit";
import { customElement, property } from "lit/decorators.js";
import { NJobDeskElement } from "../element.js";
import type { ExecutionModel } from "../api/index.js";
import { elapsedSince, formatDuration } from "../utils/format.js";
import { NJobDeskRunOpenEvent } from "./run-open.event.js";
import "./provider-tag.element.js";
import "./state-tag.element.js";
import "./error-cell.element.js";
import "./relative-time.element.js";

@customElement("njd-executions-table")
export class NJobDeskExecutionsTableElement extends NJobDeskElement {
  @property({ attribute: false })
  executions: ExecutionModel[] = [];

  @property({ type: Boolean })
  showElapsed = false;

  #renderRow(execution: ExecutionModel) {
    return html`
      <uui-table-row>
        <uui-table-cell>
          <button
            type="button"
            class="job-link"
            style="all: unset; cursor: pointer; color: var(--uui-color-interactive); font-weight: 600"
            @click=${() => this.dispatchEvent(new NJobDeskRunOpenEvent(execution))}>
            ${execution.jobGroup ? `${execution.jobGroup}/${execution.jobName}` : execution.jobName}
          </button>
          <njd-provider-tag .providerKey=${execution.providerKey}></njd-provider-tag>
        </uui-table-cell>
        <uui-table-cell>
          <njd-state-tag kind="execution" .value=${execution.state}></njd-state-tag>
        </uui-table-cell>
        <uui-table-cell class="right">
          <njd-relative-time .date=${execution.startedUtc}></njd-relative-time>
        </uui-table-cell>
        <uui-table-cell class="right">
          ${this.showElapsed
            ? html`<span style="font-variant-numeric: tabular-nums">${elapsedSince(execution.startedUtc)}</span>`
            : html`<span style="font-variant-numeric: tabular-nums">${formatDuration(execution.durationMs)}</span>`}
        </uui-table-cell>
        <uui-table-cell>
          <span
            style="font-family: monospace; font-size: var(--uui-type-small-size); color: var(--uui-color-text-alt)"
            >${execution.schedulerInstanceId}</span
          >
        </uui-table-cell>
        <uui-table-cell>
          <njd-error-cell .value=${execution.errorMessage ?? ""}></njd-error-cell>
        </uui-table-cell>
        <uui-table-cell class="right">
          <uui-button
            compact
            look="secondary"
            data-mark="njobdesk:action:run-details"
            label=${this.localize.term("njobdesk_actionRunDetails")}
            @click=${() => this.dispatchEvent(new NJobDeskRunOpenEvent(execution))}>
            <uui-icon name="icon-zoom-in"></uui-icon>
          </uui-button>
        </uui-table-cell>
      </uui-table-row>
    `;
  }

  render() {
    return html`
      <uui-table>
        <uui-table-head>
          <uui-table-head-cell>${this.localize.term("njobdesk_colJob")}</uui-table-head-cell>
          <uui-table-head-cell>${this.localize.term("njobdesk_colState")}</uui-table-head-cell>
          <uui-table-head-cell class="right">${this.localize.term("njobdesk_colStarted")}</uui-table-head-cell>
          <uui-table-head-cell class="right">
            ${this.showElapsed
              ? this.localize.term("njobdesk_colElapsed")
              : this.localize.term("njobdesk_colDuration")}
          </uui-table-head-cell>
          <uui-table-head-cell>${this.localize.term("njobdesk_colNode")}</uui-table-head-cell>
          <uui-table-head-cell>${this.localize.term("njobdesk_colError")}</uui-table-head-cell>
          <uui-table-head-cell class="right"></uui-table-head-cell>
        </uui-table-head>
        ${this.executions.map((execution) => this.#renderRow(execution))}
      </uui-table>
    `;
  }

  static styles = css`
    :host {
      display: block;
    }

    uui-table-cell {
      height: var(--uui-size-16);
    }

    .right {
      text-align: right;
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-executions-table": NJobDeskExecutionsTableElement;
  }
}
