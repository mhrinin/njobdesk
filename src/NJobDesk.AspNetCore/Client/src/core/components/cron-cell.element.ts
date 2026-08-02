import { LitElement, css, html, nothing } from "lit";
import { customElement, property } from "lit/decorators.js";

export interface NJobDeskCronCellValue {
  summary?: string | null;
  cronExpression?: string | null;
}

@customElement("njd-cron-cell")
export class NJobDeskCronCellElement extends LitElement {
  @property({ type: Object })
  value: NJobDeskCronCellValue | undefined;

  render() {
    if (!this.value || (!this.value.summary && !this.value.cronExpression)) {
      return html`—`;
    }

    return html`
      <span class="summary">${this.value.summary ?? this.value.cronExpression}</span>
      ${this.value.summary && this.value.cronExpression
        ? html`<span class="cron">${this.value.cronExpression}</span>`
        : nothing}
    `;
  }

  static styles = css`
    :host {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-1);
    }

    .cron {
      font-family: monospace;
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-text-alt);
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-cron-cell": NJobDeskCronCellElement;
  }
}
