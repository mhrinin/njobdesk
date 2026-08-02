import { css, html } from "lit";
import { customElement, state } from "lit/decorators.js";
import { UUIIconRegistry } from "@umbraco-ui/uui-icon-registry/lib";
import type { UUIModalContainerElement } from "@umbraco-ui/uui-modal/lib";
import type { UUIToastNotificationContainerElement } from "@umbraco-ui/uui-toast-notification-container/lib";
import { UUITextStyles } from "@umbraco-ui/uui-css/lib";
import "@umbraco-ui/uui-modal/lib";
import "@umbraco-ui/uui-toast-notification/lib";
import "@umbraco-ui/uui-toast-notification-container/lib";
import "@umbraco-ui/uui-toast-notification-layout/lib";
import "@umbraco-ui/uui-scroll-container/lib";
import "@umbraco-ui/uui-dialog-layout/lib";
import { SchedulerService } from "./api/index.js";
import { NJobDeskElement } from "./element.js";
import { njdIcons } from "./icons/icons.js";
import { setReadOnly } from "./services/dashboard-state.js";
import { setModalContainer } from "./services/modal.service.js";
import { setToastContainer } from "./services/notification.service.js";
import { NJobDeskTileOpenEvent } from "./components/stat-tile.element.js";
import type { NJobDeskJobsFilterIntent } from "./views/jobs-view.element.js";
import type { NJobDeskHistoryFilterIntent } from "./views/history-view.element.js";
import "./modals/confirm-modal.element.js";
import "./views/overview-view.element.js";
import "./views/jobs-view.element.js";
import "./views/history-view.element.js";

type DashboardTab = "overview" | "jobs" | "history";

@customElement("njd-dashboard")
export class NJobDeskDashboardElement extends NJobDeskElement {
  @state()
  private _activeTab: DashboardTab = "overview";

  @state()
  private _jobsFilterIntent?: NJobDeskJobsFilterIntent;

  @state()
  private _historyFilterIntent?: NJobDeskHistoryFilterIntent;

  readonly #iconRegistry = new UUIIconRegistry();

  constructor() {
    super();
    for (const [name, svg] of Object.entries(njdIcons)) {
      this.#iconRegistry.defineIcon(name, svg);
    }
  }

  connectedCallback() {
    super.connectedCallback();
    this.#iconRegistry.attach(this);
    this.addEventListener(NJobDeskTileOpenEvent.TYPE, ((event: Event) =>
      this.#handleTileOpen((event as NJobDeskTileOpenEvent).detail.tile)) as EventListener);
    this.#resolveReadOnly();
  }

  // The standalone host injects readOnly synchronously; when it didn't (Umbraco), resolve it once
  // from the scheduler status API so mutating controls are gated in every host.
  async #resolveReadOnly() {
    if (window.__NJOBDESK__?.readOnly !== undefined) {
      return;
    }

    const response = await SchedulerService.getSchedulerStatus();
    if (response.data) {
      setReadOnly(response.data.readOnly);
    }
  }

  disconnectedCallback() {
    super.disconnectedCallback();
    this.#iconRegistry.detach(this);
  }

  firstUpdated() {
    setModalContainer(this.shadowRoot!.querySelector<UUIModalContainerElement>("uui-modal-container")!);
    setToastContainer(
      this.shadowRoot!.querySelector<UUIToastNotificationContainerElement>("uui-toast-notification-container")!,
    );
  }

  #handleTileOpen(tile: string) {
    const targets: Record<string, () => void> = {
      jobs: () => this.#openTab("jobs", {}),
      paused: () => this.#openTab("jobs", { state: "Paused" }),
      running: () => this.#openTab("history", { state: "Running" }),
      succeeded: () => this.#openTab("history", { state: "Succeeded", fromUtc: this.#last24h() }),
      failed: () => this.#openTab("history", { state: "Failed", fromUtc: this.#last24h() }),
    };
    targets[tile]?.();
  }

  #last24h(): string {
    return new Date(Date.now() - 86_400_000).toISOString();
  }

  #openTab(tab: DashboardTab, intent: NJobDeskJobsFilterIntent | NJobDeskHistoryFilterIntent) {
    if (tab === "jobs") {
      this._jobsFilterIntent = intent as NJobDeskJobsFilterIntent;
    } else if (tab === "history") {
      this._historyFilterIntent = intent as NJobDeskHistoryFilterIntent;
    }

    this._activeTab = tab;
  }

  #selectTab(tab: DashboardTab) {
    this._jobsFilterIntent = undefined;
    this._historyFilterIntent = undefined;
    this._activeTab = tab;
  }

  #renderTab(tab: DashboardTab, labelKey: string) {
    return html`
      <uui-tab
        label=${this.localize.term(labelKey)}
        data-mark="njobdesk:tab:${tab}"
        ?active=${this._activeTab === tab}
        @click=${() => this.#selectTab(tab)}></uui-tab>
    `;
  }

  render() {
    return html`
      <uui-tab-group>
        ${this.#renderTab("overview", "njobdesk_tabOverview")}
        ${this.#renderTab("jobs", "njobdesk_tabJobs")}
        ${this.#renderTab("history", "njobdesk_tabHistory")}
      </uui-tab-group>

      <div class="view">
        ${this._activeTab === "overview" ? html`<njd-overview-view></njd-overview-view>` : ""}
        ${this._activeTab === "jobs"
          ? html`<njd-jobs-view .filterIntent=${this._jobsFilterIntent}></njd-jobs-view>`
          : ""}
        ${this._activeTab === "history"
          ? html`<njd-history-view .filterIntent=${this._historyFilterIntent}></njd-history-view>`
          : ""}
      </div>

      <uui-modal-container></uui-modal-container>
      <uui-toast-notification-container auto-close="6000" bottom-up></uui-toast-notification-container>
    `;
  }

  static styles = [
    UUITextStyles,
    css`
      :host {
        display: block;
        font-family: inherit;
      }

      uui-tab-group {
        background-color: var(--uui-color-surface);
        border-bottom: 1px solid var(--uui-color-border);
        padding-left: var(--uui-size-layout-1);
      }

      .view {
        padding: var(--uui-size-layout-1);
      }

      uui-toast-notification-container {
        position: fixed;
        bottom: 0;
        right: 0;
        z-index: 6000;
        display: block;
        max-width: 400px;
        padding: var(--uui-size-space-5);
      }
    `,
  ];
}

export default NJobDeskDashboardElement;

declare global {
  interface HTMLElementTagNameMap {
    "njd-dashboard": NJobDeskDashboardElement;
  }
}
