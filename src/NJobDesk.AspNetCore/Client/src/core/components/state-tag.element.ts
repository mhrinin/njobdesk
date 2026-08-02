import { LitElement, css, html } from "lit";
import { customElement, property } from "lit/decorators.js";
import type { ExecutionStatus, JobState } from "../api/index.js";
import {
  executionStateIcon,
  executionStateTagColor,
  jobStateIcon,
  jobStateTagColor,
  type TagColor,
} from "../utils/format.js";

@customElement("njd-state-tag")
export class NJobDeskStateTagElement extends LitElement {
  @property()
  value: JobState | ExecutionStatus | undefined;

  @property()
  kind?: "job" | "execution";

  get #isExecutionState(): boolean {
    return this.kind === "execution";
  }

  #color(): TagColor {
    if (!this.value) {
      return "default";
    }

    return this.#isExecutionState
      ? executionStateTagColor(this.value as ExecutionStatus)
      : jobStateTagColor(this.value as JobState);
  }

  #icon(): string {
    if (!this.value) {
      return "icon-block";
    }

    return this.#isExecutionState
      ? executionStateIcon(this.value as ExecutionStatus)
      : jobStateIcon(this.value as JobState);
  }

  render() {
    if (!this.value) {
      return html`—`;
    }

    return html`
      <uui-tag color=${this.#color()} look="secondary">
        <uui-icon name=${this.#icon()}></uui-icon>
        ${this.value}
        ${this.value === "Running" || this.value === "Blocked" ? html`<span class="pulse" aria-hidden="true"></span>` : ""}
      </uui-tag>
    `;
  }

  static styles = css`
    :host {
      display: inline-block;
    }

    uui-tag {
      display: inline-flex;
      align-items: center;
      gap: var(--uui-size-space-1);
      white-space: nowrap;
    }

    .pulse {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background-color: currentColor;
      animation: njd-pulse 1.5s ease-in-out infinite;
    }

    @keyframes njd-pulse {
      0%,
      100% {
        opacity: 1;
      }
      50% {
        opacity: 0.3;
      }
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-state-tag": NJobDeskStateTagElement;
  }
}
