import { css, html } from "lit";
import { customElement, property } from "lit/decorators.js";
import { NJobDeskElement } from "../element.js";
import type { ExecutionBucketModel } from "../api/index.js";

@customElement("njd-trend-strip")
export class NJobDeskTrendStripElement extends NJobDeskElement {
  @property({ attribute: false })
  buckets: ExecutionBucketModel[] = [];

  get #isEmpty(): boolean {
    return this.buckets.every((bucket) => bucket.succeeded === 0 && bucket.failed === 0);
  }

  #bucketTitle(bucket: ExecutionBucketModel): string {
    const start = new Date(bucket.hourStartUtc);
    const end = new Date(start.getTime() + 3_600_000);
    return `${this.#hour(start)}–${this.#hour(end)} · ${bucket.succeeded} ${this.localize.term("njobdesk_trendSucceeded")} · ${bucket.failed} ${this.localize.term("njobdesk_trendFailed")}`;
  }

  #hour(date: Date): string {
    return date.toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" });
  }

  render() {
    if (this.#isEmpty) {
      return html`<p class="empty">${this.localize.term("njobdesk_trendEmpty")}</p>`;
    }

    const max = Math.max(...this.buckets.map((bucket) => bucket.succeeded + bucket.failed), 1);
    return html`
      <div class="strip" role="img" aria-label=${this.localize.term("njobdesk_trendLabel")}>
        ${this.buckets.map((bucket, index) => {
          const succeededPct = (bucket.succeeded / max) * 100;
          const failedPct = (bucket.failed / max) * 100;
          return html`
            <div class="hour" title=${this.#bucketTitle(bucket)}>
              <div class="bar">
                <div class="failed" style="height: ${failedPct}%"></div>
                <div class="succeeded" style="height: ${succeededPct}%"></div>
              </div>
              <span class="hour-label">${index % 6 === 0 ? this.#hour(new Date(bucket.hourStartUtc)) : ""}</span>
            </div>
          `;
        })}
      </div>
    `;
  }

  static styles = css`
    :host {
      display: block;
    }

    .strip {
      display: flex;
      gap: var(--uui-size-space-1);
      align-items: stretch;
    }

    .hour {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-1);
      min-width: 0;
    }

    .bar {
      display: flex;
      flex-direction: column;
      justify-content: flex-end;
      height: 72px;
      border-radius: var(--uui-border-radius);
      background-color: var(--uui-color-surface-alt);
      overflow: hidden;
    }

    .succeeded {
      background-color: var(--uui-color-positive);
    }

    .failed {
      background-color: var(--uui-color-danger);
    }

    .succeeded:not([style="height: 0%"]),
    .failed:not([style="height: 0%"]) {
      min-height: 2px;
    }

    .succeeded[style="height: 0%"],
    .failed[style="height: 0%"] {
      min-height: 0;
      height: 0 !important;
    }

    .hour-label {
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-text-alt);
      white-space: nowrap;
      height: 18px;
    }

    .empty {
      margin: 0;
      color: var(--uui-color-text-alt);
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-trend-strip": NJobDeskTrendStripElement;
  }
}
