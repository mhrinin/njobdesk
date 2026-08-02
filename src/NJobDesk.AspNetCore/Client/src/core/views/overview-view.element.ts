import { css, html, nothing } from "lit";
import { customElement, property, state } from "lit/decorators.js";
import { UUITextStyles } from "@umbraco-ui/uui-css/lib";
import { NJobDeskElement } from "../element.js";
import { setProviders, setReadOnly } from "../services/dashboard-state.js";
import { confirm } from "../services/modal.service.js";
import { notify } from "../services/notification.service.js";
import {
  ExecutionsService,
  SchedulerService,
  type DashboardStatusModel,
  type ExecutionModel,
  type ProviderStatusModel,
  type SchedulerStatisticsModel,
} from "../api/index.js";
import { formatDateTime } from "../utils/format.js";
import "../components/stat-tile.element.js";
import "../components/trend-strip.element.js";
import "../components/kv-list.element.js";
import "../components/empty-state.element.js";
import "../components/executions-table.element.js";
import type { NJobDeskKvItem } from "../components/kv-list.element.js";
import { attachRunOpenListener } from "../components/run-open.event.js";

const RefreshIntervalMs = 5000;

@customElement("njd-overview-view")
export class NJobDeskOverviewViewElement extends NJobDeskElement {
  @property({ type: Boolean })
  live = true;

  @state()
  private _status?: DashboardStatusModel;

  @state()
  private _statistics?: SchedulerStatisticsModel;

  @state()
  private _running: ExecutionModel[] = [];

  @state()
  private _loadFailed = false;

  #refreshHandle?: number;

  connectedCallback() {
    super.connectedCallback();
    this.#refresh();
    attachRunOpenListener(this);
    this.#refreshHandle = window.setInterval(() => {
      if (this.live) {
        this.#refresh();
      }
    }, RefreshIntervalMs);
  }

  disconnectedCallback() {
    super.disconnectedCallback();
    window.clearInterval(this.#refreshHandle);
  }

  async #refresh() {
    const status = await SchedulerService.getSchedulerStatus();
    if (status.error || typeof status.data !== "object") {
      this._loadFailed = true;
      return;
    }

    this._loadFailed = false;
    this._status = status.data;
    setProviders(status.data.providers);
    if (window.__NJOBDESK__?.readOnly === undefined) {
      setReadOnly(status.data.readOnly);
    }

    if (status.data.providers.length === 0) {
      return;
    }

    const [statistics, running] = await Promise.all([
      SchedulerService.getSchedulerStatistics(),
      ExecutionsService.getRunningExecutions(),
    ]);
    if (statistics.data && typeof statistics.data === "object") {
      this._statistics = statistics.data;
    }

    if (Array.isArray(running.data)) {
      this._running = running.data;
    }
  }

  async #pauseAll() {
    try {
      await confirm({
        headline: this.localize.term("njobdesk_confirmPauseAllHeadline"),
        content: this.localize.term("njobdesk_confirmPauseAllMessage"),
        color: "danger",
        confirmLabel: this.localize.term("njobdesk_pauseAll"),
      });
    } catch {
      return;
    }

    const response = await SchedulerService.pauseAll();
    this.#notifyResult(!response.error, "njobdesk_toastPausedAll");
    await this.#refresh();
  }

  async #resumeAll() {
    const response = await SchedulerService.resumeAll();
    this.#notifyResult(!response.error, "njobdesk_toastResumedAll");
    await this.#refresh();
  }

  #notifyResult(success: boolean, successKey: string) {
    notify(
      success ? "positive" : "danger",
      this.localize.term(success ? successKey : "njobdesk_toastActionFailed"),
    );
  }

  #toggleLive(enabled: boolean) {
    this.live = enabled;
    this.dispatchEvent(new CustomEvent("njd-live-changed", { detail: { live: enabled }, bubbles: true, composed: true }));
    if (enabled) {
      this.#refresh();
    }
  }

  render() {
    if (this._loadFailed) {
      return html`<njd-empty-state
        headline=${this.localize.term("njobdesk_loadError")}></njd-empty-state>`;
    }

    if (!this._status) {
      return html`<uui-loader-bar></uui-loader-bar>`;
    }

    if (this._status.providers.length === 0) {
      return html`
        <njd-empty-state
          headline=${this.localize.term("njobdesk_notConfiguredHeadline")}
          message=${this.localize.term("njobdesk_notConfigured")}></njd-empty-state>
      `;
    }

    return html`
      <div class="uui-text layout">
        ${this.#renderTiles()}
        ${this.#renderTrend()}
        ${this.#renderRunning()}
        ${this.#renderProviders()}
      </div>
    `;
  }

  #renderTiles() {
    const statistics = this._statistics;
    return html`
      <div class="tiles" data-mark="njobdesk:tiles">
        <njd-stat-tile
          tile="jobs"
          label=${this.localize.term("njobdesk_statJobs")}
          .value=${statistics?.jobsTotal}></njd-stat-tile>
        <njd-stat-tile
          tile="running"
          label=${this.localize.term("njobdesk_statRunning")}
          .value=${statistics?.runningCount}
          accent=${(statistics?.runningCount ?? 0) > 0 ? "positive" : "default"}
          pulse></njd-stat-tile>
        <njd-stat-tile
          tile="paused"
          label=${this.localize.term("njobdesk_statPaused")}
          .value=${statistics?.jobsPaused}
          accent=${(statistics?.jobsPaused ?? 0) > 0 ? "warning" : "default"}></njd-stat-tile>
        <njd-stat-tile
          tile="succeeded"
          label=${this.localize.term("njobdesk_statSucceeded24h")}
          .value=${statistics?.succeeded24h}
          accent="positive"></njd-stat-tile>
        <njd-stat-tile
          tile="failed"
          label=${this.localize.term("njobdesk_statFailures")}
          .value=${statistics?.failed24h}
          accent=${(statistics?.failed24h ?? 0) > 0 ? "danger" : "default"}></njd-stat-tile>
      </div>
    `;
  }

  #renderTrend() {
    return html`
      <uui-box headline=${this.localize.term("njobdesk_trendHeadline")}>
        <njd-trend-strip .buckets=${this._statistics?.buckets ?? []}></njd-trend-strip>
      </uui-box>
    `;
  }

  #renderRunning() {
    return html`
      <uui-box class=${this._running.length > 0 ? "flush" : ""}>
        <div slot="header" class="box-header">
          <h2>${this.localize.term("njobdesk_statRunning")}</h2>
        </div>
        <div slot="header-actions" class="live-controls">
          <uui-toggle
            label=${this.localize.term("njobdesk_live")}
            .checked=${this.live}
            @change=${(event: Event) => this.#toggleLive((event.target as HTMLInputElement).checked)}>
            ${this.localize.term("njobdesk_live")}
          </uui-toggle>
          <uui-button
            compact
            look="secondary"
            label=${this.localize.term("njobdesk_refresh")}
            @click=${() => this.#refresh()}>
            <uui-icon name="icon-refresh"></uui-icon>
          </uui-button>
        </div>
        ${this._running.length === 0
          ? html`<njd-empty-state
              variant="positive"
              headline=${this.localize.term("njobdesk_runningEmpty")}></njd-empty-state>`
          : html`<njd-executions-table .executions=${this._running} showElapsed></njd-executions-table>`}
      </uui-box>
    `;
  }

  #renderProviders() {
    const status = this._status;
    if (!status) {
      return nothing;
    }

    const showPauseAll = !status.readOnly && status.providers.some((provider) => provider.capabilities.pause);
    return html`
      <div class="providers-section" data-mark="njobdesk:providers">
        <div class="providers-header">
          <h2>${this.localize.term("njobdesk_statScheduler")}</h2>
          ${showPauseAll
            ? html`
                <div class="scheduler-actions">
                  <uui-button
                    look="secondary"
                    color="warning"
                    compact
                    data-mark="njobdesk:action:pause-all"
                    label=${this.localize.term("njobdesk_pauseAll")}
                    @click=${() => this.#pauseAll()}>${this.localize.term("njobdesk_pauseAll")}</uui-button>
                  <uui-button
                    look="secondary"
                    color="positive"
                    compact
                    data-mark="njobdesk:action:resume-all"
                    label=${this.localize.term("njobdesk_resumeAll")}
                    @click=${() => this.#resumeAll()}>${this.localize.term("njobdesk_resumeAll")}</uui-button>
                </div>
              `
            : nothing}
        </div>
        <div class="providers">
          ${status.providers.map((provider) => this.#renderProviderCard(provider))}
        </div>
      </div>
    `;
  }

  #renderProviderCard(provider: ProviderStatusModel) {
    const yesNo = (value: boolean) => this.localize.term(value ? "njobdesk_yes" : "njobdesk_no");
    const detail = provider.degraded ? undefined : provider.status;
    const items: NJobDeskKvItem[] = provider.degraded
      ? [{ name: this.localize.term("njobdesk_colError"), value: provider.error ?? "—" }]
      : [
          { name: this.localize.term("njobdesk_metaSchedulerName"), value: detail?.schedulerName ?? "—" },
          {
            name: this.localize.term("njobdesk_metaInstanceId"),
            value: detail?.schedulerInstanceId ?? "—",
            monospace: true,
          },
          { name: this.localize.term("njobdesk_statClustered"), value: yesNo(detail?.clustered ?? false) },
          { name: this.localize.term("njobdesk_metaStoreType"), value: detail?.storeType ?? "—" },
          {
            name: this.localize.term("njobdesk_metaThreadPool"),
            value: detail?.threadPoolSize != null ? `${detail.threadPoolSize}` : "—",
          },
          {
            name: this.localize.term("njobdesk_metaRunningSince"),
            value: detail?.runningSinceUtc ? formatDateTime(detail.runningSinceUtc) : "—",
          },
          { name: this.localize.term("njobdesk_metaProviderVersion"), value: provider.providerVersion ?? "—" },
          {
            name: this.localize.term("njobdesk_metaHistoryEnabled"),
            value: this.localize.term(provider.capabilities.history ? "njobdesk_enabled" : "njobdesk_disabled"),
          },
        ];

    return html`
      <uui-box data-mark="njobdesk:provider:${provider.key}">
        <div slot="header" class="box-header">
          <h2>${provider.displayName}</h2>
          ${provider.degraded
            ? html`<uui-tag look="secondary" color="danger">
                ${this.localize.term("njobdesk_providerUnavailable")}
              </uui-tag>`
            : html`<uui-tag look="secondary" color=${detail?.state === "Started" ? "positive" : "warning"}>
                ${detail?.state ?? "—"}
              </uui-tag>`}
        </div>
        <njd-kv-list .items=${items}></njd-kv-list>
      </uui-box>
    `;
  }

  static styles = [
    UUITextStyles,
    css`
      :host {
        display: block;
      }

      .layout > * + * {
        margin-top: var(--uui-size-space-5);
      }

      .tiles {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
        gap: var(--uui-size-space-4);
      }

      .box-header {
        display: flex;
        align-items: center;
        gap: var(--uui-size-space-3);
      }

      .box-header h2 {
        font-size: var(--uui-type-h5-size);
        margin: 0;
      }

      .live-controls {
        display: flex;
        align-items: center;
        gap: var(--uui-size-space-4);
      }

      .scheduler-actions {
        display: flex;
        gap: var(--uui-size-space-3);
      }

      .providers-section > * + * {
        margin-top: var(--uui-size-space-3);
      }

      .providers-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--uui-size-space-4);
      }

      .providers-header h2 {
        font-size: var(--uui-type-h5-size);
        margin: 0;
      }

      .providers {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
        gap: var(--uui-size-space-4);
        align-items: start;
      }

      uui-box.flush {
        --uui-box-default-padding: 0;
      }
    `,
  ];
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-overview-view": NJobDeskOverviewViewElement;
  }
}
