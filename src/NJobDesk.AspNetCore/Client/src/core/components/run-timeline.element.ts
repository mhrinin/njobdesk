import { css, html } from "lit";
import { customElement, property } from "lit/decorators.js";
import { NJobDeskElement } from "../element.js";
import type { ExecutionModel } from "../api/index.js";
import { executionStateTagColor, formatDuration } from "../utils/format.js";
import { NJobDeskRunOpenEvent } from "./run-open.event.js";
import "./state-tag.element.js";
import "./relative-time.element.js";

@customElement("njd-run-timeline")
export class NJobDeskRunTimelineElement extends NJobDeskElement {
  @property({ attribute: false })
  executions: ExecutionModel[] = [];

  render() {
    if (this.executions.length === 0) {
      return html`<p class="empty">${this.localize.term("njobdesk_timelineEmpty")}</p>`;
    }

    return html`
      <ol>
        ${this.executions.map((execution) => this.#renderItem(execution))}
      </ol>
    `;
  }

  #renderItem(execution: ExecutionModel) {
    const dotColor = executionStateTagColor(execution.state);

    return html`
      <li>
        <button
          type="button"
          class="row"
          data-mark="njobdesk:timeline:run"
          title=${this.localize.term("njobdesk_actionRunDetails")}
          @click=${() => this.dispatchEvent(new NJobDeskRunOpenEvent(execution))}>
          <span class="dot ${dotColor}" aria-hidden="true"></span>
          <uui-tag look="secondary" color=${dotColor}>${execution.state}</uui-tag>
          <njd-relative-time .date=${execution.startedUtc}></njd-relative-time>
          <span class="duration">${formatDuration(execution.durationMs)}</span>
          <span class="node">${execution.schedulerInstanceId}</span>
          ${execution.errorMessage
            ? html`<span class="error" title=${execution.errorMessage}>${execution.errorMessage}</span>`
            : html`<span class="trigger">${execution.triggerName ?? ""}</span>`}
          <uui-symbol-expand class="expand"></uui-symbol-expand>
        </button>
      </li>
    `;
  }

  static styles = css`
    :host {
      display: block;
    }

    ol {
      list-style: none;
      margin: 0;
      padding: 0 0 0 var(--uui-size-space-4);
      border-left: 2px solid var(--uui-color-divider-standalone);
    }

    li + li {
      margin-top: var(--uui-size-space-2);
    }

    .row {
      all: unset;
      box-sizing: border-box;
      width: 100%;
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-4);
      padding: var(--uui-size-space-2) var(--uui-size-space-3);
      border-radius: var(--uui-border-radius);
      position: relative;
      cursor: pointer;
    }

    .row:hover {
      background-color: var(--uui-color-surface-emphasis);
    }

    .row:focus-visible {
      outline: 2px solid var(--uui-color-focus);
    }

    .dot {
      position: absolute;
      left: calc(-1 * var(--uui-size-space-4) - var(--uui-size-space-3) - 4px);
      width: 10px;
      height: 10px;
      border-radius: 50%;
      border: 2px solid var(--uui-color-surface);
    }

    .dot.positive {
      background-color: var(--uui-color-positive);
    }

    .dot.danger {
      background-color: var(--uui-color-danger);
    }

    .dot.warning {
      background-color: var(--uui-color-warning);
    }

    .dot.default {
      background-color: var(--uui-color-border-emphasis);
    }

    .duration {
      font-variant-numeric: tabular-nums;
      min-width: 7ch;
      text-align: right;
    }

    .node,
    .trigger {
      font-family: monospace;
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-text-alt);
    }

    .trigger {
      margin-left: auto;
    }

    .error {
      margin-left: auto;
      max-width: 40ch;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-danger-standalone);
    }

    .expand {
      margin-left: var(--uui-size-space-2);
    }

    .empty {
      margin: 0;
      color: var(--uui-color-text-alt);
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-run-timeline": NJobDeskRunTimelineElement;
  }
}
