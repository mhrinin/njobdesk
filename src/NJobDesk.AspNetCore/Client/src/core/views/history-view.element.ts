import { css, html, nothing } from "lit";
import { customElement, property, state } from "lit/decorators.js";
import { NJobDeskElement } from "../element.js";
import type { UUIPaginationEvent } from "@umbraco-ui/uui";
import { ExecutionsService, type ExecutionModel, type ExecutionStatus } from "../api/index.js";
import { attachRunOpenListener } from "../components/run-open.event.js";
import { searchInputIconStyles } from "../utils/shared-styles.js";
import "../components/executions-table.element.js";
import "../components/empty-state.element.js";

const PageSize = 20;
const StateOptions: ExecutionStatus[] = ["Running", "Succeeded", "Failed", "Vetoed"];

export interface NJobDeskHistoryFilterIntent {
  state?: ExecutionStatus;
  fromUtc?: string;
}

@customElement("njd-history-view")
export class NJobDeskHistoryViewElement extends NJobDeskElement {
  @property({ attribute: false })
  set filterIntent(intent: NJobDeskHistoryFilterIntent | undefined) {
    if (!intent) {
      return;
    }

    if (intent.state) {
      this._stateFilter = intent.state;
    }

    if (intent.fromUtc) {
      this._fromDate = intent.fromUtc.slice(0, 10);
    }

    if (this.isConnected) {
      this._page = 1;
      this.#load();
    }
  }

  @state()
  private _executions: ExecutionModel[] = [];

  @state()
  private _total = 0;

  @state()
  private _page = 1;

  @state()
  private _stateFilter?: ExecutionStatus;

  @state()
  private _jobNameFilter = "";

  @state()
  private _fromDate = "";

  @state()
  private _toDate = "";

  @state()
  private _loading = true;

  @state()
  private _live = false;

  #refreshHandle?: number;

  connectedCallback() {
    super.connectedCallback();
    this.#load();
    attachRunOpenListener(this);
    this.#refreshHandle = window.setInterval(() => {
      if (this._live && this._page === 1) {
        this.#load();
      }
    }, 5000);
  }

  disconnectedCallback() {
    super.disconnectedCallback();
    window.clearInterval(this.#refreshHandle);
  }

  async #load() {
    this._loading = this._executions.length === 0;
    const response = await ExecutionsService.getExecutions({
      query: {
        skip: (this._page - 1) * PageSize,
        take: PageSize,
        state: this._stateFilter,
        jobName: this._jobNameFilter.trim() || undefined,
        fromUtc: this._fromDate ? new Date(this._fromDate).toISOString() : undefined,
        toUtc: this._toDate ? new Date(`${this._toDate}T23:59:59`).toISOString() : undefined,
      },
    });
    this._executions = response.data?.items ?? [];
    this._total = Number(response.data?.total ?? 0);
    this._loading = false;
  }

  #applyFilter(update: () => void) {
    update();
    this._page = 1;
    this.#load();
  }

  render() {
    const pageCount = Math.max(1, Math.ceil(this._total / PageSize));

    return html`
      <div class="toolbar">
        <uui-input
          placeholder=${this.localize.term("njobdesk_filterPlaceholder")}
          .value=${this._jobNameFilter}
          @change=${(event: Event) =>
            this.#applyFilter(() => (this._jobNameFilter = (event.target as HTMLInputElement).value))}>
          <uui-icon slot="prepend" name="icon-search"></uui-icon>
        </uui-input>
        <div class="chips">
          <uui-button
            look=${this._stateFilter === undefined ? "primary" : "outline"}
            data-mark="njobdesk:history-chip:all"
            label=${this.localize.term("njobdesk_filterAll")}
            @click=${() => this.#applyFilter(() => (this._stateFilter = undefined))}>
            ${this.localize.term("njobdesk_filterAll")}
          </uui-button>
          ${StateOptions.map(
            (option) => html`
              <uui-button
                look=${this._stateFilter === option ? "primary" : "outline"}
                data-mark="njobdesk:history-chip:${option}"
                label=${option}
                @click=${() =>
                  this.#applyFilter(() => (this._stateFilter = this._stateFilter === option ? undefined : option))}>
                ${option}
              </uui-button>
            `,
          )}
        </div>
        <label class="date-filter">
          <span>${this.localize.term("njobdesk_historyFromDate")}</span>
          <uui-input
            type="date"
            label=${this.localize.term("njobdesk_historyFromDate")}
            .value=${this._fromDate}
            @change=${(event: Event) =>
              this.#applyFilter(() => (this._fromDate = (event.target as HTMLInputElement).value))}></uui-input>
        </label>
        <label class="date-filter">
          <span>${this.localize.term("njobdesk_historyToDate")}</span>
          <uui-input
            type="date"
            label=${this.localize.term("njobdesk_historyToDate")}
            .value=${this._toDate}
            @change=${(event: Event) =>
              this.#applyFilter(() => (this._toDate = (event.target as HTMLInputElement).value))}></uui-input>
        </label>
        <div class="live-controls">
          <uui-toggle
            label=${this.localize.term("njobdesk_live")}
            .checked=${this._live}
            @change=${(event: Event) => (this._live = (event.target as HTMLInputElement).checked)}>
            ${this.localize.term("njobdesk_live")}
          </uui-toggle>
          <uui-button
            compact
            look="secondary"
            label=${this.localize.term("njobdesk_refresh")}
            @click=${() => this.#load()}>
            <uui-icon name="icon-refresh"></uui-icon>
          </uui-button>
        </div>
      </div>

      ${this._loading
        ? html`<uui-loader-bar></uui-loader-bar>`
        : this._executions.length === 0
          ? this._stateFilter === "Failed"
            ? html`<njd-empty-state
                variant="positive"
                headline=${this.localize.term("njobdesk_historyAllClear")}></njd-empty-state>`
            : html`<njd-empty-state
                headline=${this.localize.term("njobdesk_historyEmpty")}></njd-empty-state>`
          : html`
              <uui-box class="flush">
                <njd-executions-table .executions=${this._executions}></njd-executions-table>
              </uui-box>
              ${pageCount > 1
                ? html`<uui-pagination
                    .current=${this._page}
                    .total=${pageCount}
                    @change=${(event: UUIPaginationEvent) => {
                      this._page = event.target.current;
                      this.#load();
                    }}></uui-pagination>`
                : nothing}
            `}
    `;
  }

  static styles = [
    searchInputIconStyles,
    css`
    :host {
      display: grid;
      gap: var(--uui-size-layout-1);
      align-content: start;
    }

    uui-box.flush {
      --uui-box-default-padding: 0;
    }

    .toolbar {
      display: flex;
      gap: var(--uui-size-space-4);
      align-items: center;
      flex-wrap: wrap;
    }

    .toolbar > uui-input {
      flex: 1;
      min-width: 200px;
      max-width: 320px;
    }

    .chips {
      display: flex;
      gap: var(--uui-size-space-2);
    }

    .date-filter {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-2);
    }

    .date-filter > span {
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-text-alt);
    }

    .live-controls {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-4);
      margin-left: auto;
    }

    uui-pagination {
      justify-self: center;
    }
  `,
  ];
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-history-view": NJobDeskHistoryViewElement;
  }
}
