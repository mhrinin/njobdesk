import { css, html } from "lit";
import { customElement, property } from "lit/decorators.js";
import { NJobDeskElement } from "../element.js";
import { pickRelativeTimeUnit } from "../utils/format.js";

@customElement("njd-relative-time")
export class NJobDeskRelativeTimeElement extends NJobDeskElement {
  @property()
  date?: string | null;

  render() {
    if (!this.date) {
      return html`—`;
    }

    const target = new Date(this.date);
    const { value, unit } = pickRelativeTimeUnit(target.getTime() - Date.now());
    return html`
      <time datetime=${this.date} title=${target.toLocaleString()}>
        ${this.localize.relativeTime(value, unit)}
      </time>
    `;
  }

  static styles = css`
    :host {
      display: inline;
      font-variant-numeric: tabular-nums;
    }

    time {
      text-decoration: underline dotted var(--uui-color-border-emphasis);
      text-underline-offset: 3px;
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-relative-time": NJobDeskRelativeTimeElement;
  }
}
