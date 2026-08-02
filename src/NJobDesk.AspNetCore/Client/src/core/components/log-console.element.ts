import { css, html, nothing } from "lit";
import { customElement, property, state } from "lit/decorators.js";
import { NJobDeskElement } from "../element.js";
import type { ExecutionLogLevel, ExecutionLogModel } from "../api/index.js";
import { logLevelTagColor } from "../utils/format.js";
import { searchInputIconStyles } from "../utils/shared-styles.js";
import "./code-block.element.js";
import "./empty-state.element.js";

const LevelOrder: ExecutionLogLevel[] = ["Trace", "Debug", "Information", "Warning", "Error", "Critical"];

@customElement("njd-log-console")
export class NJobDeskLogConsoleElement extends NJobDeskElement {
  @property({ attribute: false })
  entries: ExecutionLogModel[] = [];

  @property({ type: Boolean })
  loading = false;

  @state()
  private _levelFilter?: ExecutionLogLevel;

  @state()
  private _search = "";

  @state()
  private _copied = false;

  #copiedHandle?: number;

  disconnectedCallback() {
    super.disconnectedCallback();
    window.clearTimeout(this.#copiedHandle);
  }

  get #levelCounts(): Map<ExecutionLogLevel, number> {
    const counts = new Map<ExecutionLogLevel, number>();
    for (const entry of this.entries) {
      counts.set(entry.level, (counts.get(entry.level) ?? 0) + 1);
    }

    return counts;
  }

  get #filteredEntries(): ExecutionLogModel[] {
    const search = this._search.trim().toLowerCase();
    return this.entries.filter((entry) => {
      if (this._levelFilter && entry.level !== this._levelFilter) {
        return false;
      }

      return (
        !search ||
        entry.message.toLowerCase().includes(search) ||
        entry.category.toLowerCase().includes(search) ||
        (entry.exception?.toLowerCase().includes(search) ?? false) ||
        (entry.properties?.toLowerCase().includes(search) ?? false)
      );
    });
  }

  async #copyAll() {
    const text = this.#filteredEntries
      .map((entry) => {
        let line = `${this.#formatTime(entry.timestampUtc)} [${entry.level}] ${entry.category}: ${entry.message}`;
        if (entry.properties) {
          line += `\n${entry.properties}`;
        }

        return entry.exception ? `${line}\n${entry.exception}` : line;
      })
      .join("\n");
    await navigator.clipboard.writeText(text);
    this._copied = true;
    window.clearTimeout(this.#copiedHandle);
    this.#copiedHandle = window.setTimeout(() => (this._copied = false), 2000);
  }

  #formatTime(iso: string): string {
    const date = new Date(iso);
    const pad = (value: number, length = 2) => String(value).padStart(length, "0");
    return `${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}.${pad(date.getMilliseconds(), 3)}`;
  }

  #shortCategory(category: string): string {
    const lastDotIndex = category.lastIndexOf(".");
    return lastDotIndex >= 0 ? category.slice(lastDotIndex + 1) : category;
  }

  render() {
    if (this.loading) {
      return html`<uui-loader-bar></uui-loader-bar>`;
    }

    if (this.entries.length === 0) {
      return html`
        <njd-empty-state
          headline=${this.localize.term("njobdesk_logsEmpty")}
          message=${this.localize.term("njobdesk_logsEmptyHint")}></njd-empty-state>
      `;
    }

    return html`${this.#renderToolbar()}${this.#renderEntries()}`;
  }

  #renderToolbar() {
    const counts = this.#levelCounts;
    const levels = LevelOrder.filter((level) => counts.has(level));

    return html`
      <div class="toolbar">
        <div class="chips">
          <uui-button
            look=${this._levelFilter === undefined ? "primary" : "outline"}
            data-mark="njobdesk:log-chip:all"
            label=${this.localize.term("njobdesk_filterAll")}
            @click=${() => (this._levelFilter = undefined)}>
            ${this.localize.term("njobdesk_filterAll")}
            <span class="count">${this.entries.length}</span>
          </uui-button>
          ${levels.map(
            (level) => html`
              <uui-button
                look=${this._levelFilter === level ? "primary" : "outline"}
                data-mark="njobdesk:log-chip:${level}"
                label=${level}
                @click=${() => (this._levelFilter = this._levelFilter === level ? undefined : level)}>
                ${level}
                <span class="count">${counts.get(level)}</span>
              </uui-button>
            `,
          )}
        </div>
        <uui-input
          id="log-search"
          placeholder=${this.localize.term("njobdesk_logsSearchPlaceholder")}
          .value=${this._search}
          @input=${(event: InputEvent) => (this._search = (event.target as HTMLInputElement).value)}>
          <uui-icon slot="prepend" name="icon-search"></uui-icon>
        </uui-input>
        <uui-button
          compact
          look="secondary"
          data-mark="njobdesk:action:copy-logs"
          label=${this.localize.term("njobdesk_logsCopy")}
          @click=${() => this.#copyAll()}>
          <uui-icon name=${this._copied ? "icon-check" : "icon-documents"}></uui-icon>
        </uui-button>
      </div>
    `;
  }

  #renderEntries() {
    const filtered = this.#filteredEntries;
    if (filtered.length === 0) {
      return html`<p class="no-match">${this.localize.term("njobdesk_logsNoMatch")}</p>`;
    }

    return html`
      <ol class="console" data-mark="njobdesk:log-console">
        ${filtered.map((entry) => this.#renderEntry(entry))}
      </ol>
    `;
  }

  #renderEntry(entry: ExecutionLogModel) {
    const row = html`
      <span class="time">${this.#formatTime(entry.timestampUtc)}</span>
      <uui-tag class="level" look="secondary" color=${logLevelTagColor(entry.level)}>${entry.level}</uui-tag>
      <span class="message">${entry.message}</span>
      <span class="category" title=${entry.category}>${this.#shortCategory(entry.category)}</span>
    `;

    if (!entry.exception && !entry.properties) {
      return html`<li class="entry ${entry.level}"><div class="row">${row}</div></li>`;
    }

    return html`
      <li class="entry ${entry.level}">
        <details>
          <summary>
            <div class="row">${row}<uui-symbol-expand class="expand"></uui-symbol-expand></div>
          </summary>
          <div class="detail">
            ${entry.properties ? this.#renderProperties(entry.properties) : nothing}
            ${entry.exception ? html`<njd-code-block>${entry.exception}</njd-code-block>` : nothing}
          </div>
        </details>
      </li>
    `;
  }

  #renderProperties(propertiesJson: string) {
    let properties: Record<string, unknown>;
    try {
      properties = JSON.parse(propertiesJson);
    } catch {
      return html`<njd-code-block>${propertiesJson}</njd-code-block>`;
    }

    return html`
      <dl class="properties">
        ${Object.entries(properties).map(
          ([name, value]) => html`
            <div>
              <dt>${name}</dt>
              <dd>${typeof value === "string" ? value : JSON.stringify(value, null, 2)}</dd>
            </div>
          `,
        )}
      </dl>
    `;
  }

  static styles = [
    searchInputIconStyles,
    css`
    :host {
      display: grid;
      gap: var(--uui-size-space-4);
    }

    .toolbar {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-4);
      flex-wrap: wrap;
    }

    .chips {
      display: flex;
      gap: var(--uui-size-space-2);
      flex-wrap: wrap;
    }

    #log-search {
      flex: 1;
      min-width: 160px;
      max-width: 320px;
      margin-left: auto;
    }

    .count {
      margin-left: var(--uui-size-space-2);
      font-variant-numeric: tabular-nums;
      font-weight: 400;
      opacity: 0.7;
    }

    .console {
      list-style: none;
      margin: 0;
      padding: 0;
      border: 1px solid var(--uui-color-divider);
      border-radius: var(--uui-border-radius);
      overflow: hidden;
    }

    .entry + .entry {
      border-top: 1px solid var(--uui-color-divider);
    }

    .row {
      display: flex;
      align-items: baseline;
      gap: var(--uui-size-space-4);
      padding: var(--uui-size-space-2) var(--uui-size-space-4);
      border-left: 3px solid transparent;
    }

    .entry.Warning > .row,
    .entry.Warning > details > summary .row {
      border-left-color: var(--uui-color-warning);
    }

    .entry.Error > .row,
    .entry.Error > details > summary .row,
    .entry.Critical > .row,
    .entry.Critical > details > summary .row {
      border-left-color: var(--uui-color-danger);
    }

    .entry.Trace,
    .entry.Debug {
      opacity: 0.75;
    }

    .time {
      font-family: monospace;
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-text-alt);
      font-variant-numeric: tabular-nums;
      white-space: nowrap;
    }

    .level {
      min-width: 9ch;
      justify-content: center;
    }

    .message {
      font-family: monospace;
      font-size: var(--uui-type-small-size);
      white-space: pre-wrap;
      overflow-wrap: anywhere;
      flex: 1;
    }

    .category {
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-text-alt);
      white-space: nowrap;
    }

    details > summary {
      list-style: none;
      cursor: pointer;
    }

    details > summary::-webkit-details-marker {
      display: none;
    }

    details > summary .row:hover {
      background-color: var(--uui-color-surface-emphasis);
    }

    details[open] .expand {
      transform: rotate(90deg);
    }

    .detail {
      display: grid;
      grid-template-columns: minmax(0, 1fr);
      gap: var(--uui-size-space-3);
      padding: 0 var(--uui-size-space-4) var(--uui-size-space-3);
    }

    .detail njd-code-block {
      margin: 0;
    }

    .properties {
      margin: 0;
      border: 1px solid var(--uui-color-divider);
      border-radius: var(--uui-border-radius);
      font-size: var(--uui-type-small-size);
    }

    .properties > div {
      display: flex;
      gap: var(--uui-size-space-4);
      padding: var(--uui-size-space-2) var(--uui-size-space-4);
    }

    .properties > div + div {
      border-top: 1px solid var(--uui-color-divider);
    }

    .properties dt {
      flex: 0 0 20ch;
      font-family: monospace;
      color: var(--uui-color-text-alt);
      overflow-wrap: anywhere;
    }

    .properties dd {
      margin: 0;
      flex: 1;
      font-family: monospace;
      white-space: pre-wrap;
      overflow-wrap: anywhere;
    }

    .no-match {
      margin: 0;
      color: var(--uui-color-text-alt);
    }
  `,
  ];
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-log-console": NJobDeskLogConsoleElement;
  }
}
