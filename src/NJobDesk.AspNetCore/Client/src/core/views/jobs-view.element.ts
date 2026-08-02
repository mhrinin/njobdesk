import { css, html, nothing } from "lit";
import { customElement, property, state } from "lit/decorators.js";
import { UUITextStyles } from "@umbraco-ui/uui-css/lib";
import type { UUIPaginationEvent } from "@umbraco-ui/uui";
import { NJobDeskElement } from "../element.js";
import { confirm, openModal } from "../services/modal.service.js";
import { notify, type NotificationColor } from "../services/notification.service.js";
import {
  CronService,
  JobsService,
  TriggersService,
  type JobDetailModel,
  type JobState,
  type JobSummaryModel,
  type TriggerModel,
} from "../api/index.js";
import { formatDateTime } from "../utils/format.js";
import { searchInputIconStyles } from "../utils/shared-styles.js";
import { NJobDeskJobOpenEvent } from "../components/job-link-cell.element.js";
import { NJobDeskJobActionEvent, type NJobDeskJobAction } from "../components/job-actions-cell.element.js";
import {
  NJobDeskTriggerActionEvent,
  type NJobDeskTriggerAction,
} from "../components/trigger-actions-cell.element.js";
import type { NJobDeskCronCellValue } from "../components/cron-cell.element.js";
import { attachRunOpenListener } from "../components/run-open.event.js";
import type { NJobDeskEditScheduleModalData } from "../modals/edit-schedule-modal.element.js";
import { NJobDeskJobDetailCloseEvent } from "./job-detail.element.js";
import "../modals/edit-schedule-modal.element.js";
import "../components/job-link-cell.element.js";
import "../components/job-actions-cell.element.js";
import "../components/state-tag.element.js";
import "../components/cron-cell.element.js";
import "../components/relative-time.element.js";
import "../components/empty-state.element.js";
import "./job-detail.element.js";

const PageSize = 20;
const StateChips: Array<JobState | undefined> = [undefined, "Normal", "Paused", "Error"];

function splitId(id: string): { group: string; name: string } {
  const separatorIndex = id.indexOf("/");
  return { group: id.slice(0, separatorIndex), name: id.slice(separatorIndex + 1) };
}

type NJobDeskKeyPath = ReturnType<typeof splitId>;
type SimpleAction = [(path: NJobDeskKeyPath) => Promise<{ error?: unknown }>, string];

const SimpleJobActions: Partial<Record<NJobDeskJobAction, SimpleAction>> = {
  trigger: [(path) => JobsService.triggerJob({ path }), "njobdesk_toastTriggered"],
  pause: [(path) => JobsService.pauseJob({ path }), "njobdesk_toastPaused"],
  resume: [(path) => JobsService.resumeJob({ path }), "njobdesk_toastResumed"],
};

const SimpleTriggerActions: Partial<Record<NJobDeskTriggerAction, SimpleAction>> = {
  pause: [(path) => TriggersService.pauseTrigger({ path }), "njobdesk_toastPaused"],
  resume: [(path) => TriggersService.resumeTrigger({ path }), "njobdesk_toastResumed"],
  "reset-error": [(path) => TriggersService.resetTriggerError({ path }), "njobdesk_toastResetError"],
};

export interface NJobDeskJobsFilterIntent {
  state?: JobState;
}

@customElement("njd-jobs-view")
export class NJobDeskJobsViewElement extends NJobDeskElement {
  @property({ attribute: false })
  set filterIntent(intent: NJobDeskJobsFilterIntent | undefined) {
    if (intent?.state) {
      this._stateFilter = intent.state;
    }
  }

  @state()
  private _stateFilter?: JobState;

  @state()
  private _jobs: JobSummaryModel[] = [];

  @state()
  private _filter = "";

  @state()
  private _showSystemJobs = false;

  @state()
  private _page = 1;

  @state()
  private _selectedJob?: JobDetailModel;

  @state()
  private _loading = true;

  connectedCallback() {
    super.connectedCallback();
    this.#loadJobs();
    attachRunOpenListener(this);
    this.addEventListener(NJobDeskJobOpenEvent.TYPE, ((event: Event) =>
      this.#openJob((event as NJobDeskJobOpenEvent).detail.jobId)) as EventListener);
    this.addEventListener(NJobDeskJobActionEvent.TYPE, ((event: Event) => {
      const { action, jobId } = (event as NJobDeskJobActionEvent).detail;
      this.#handleJobAction(action, jobId);
    }) as EventListener);
    this.addEventListener(NJobDeskTriggerActionEvent.TYPE, ((event: Event) => {
      const { action, triggerId } = (event as NJobDeskTriggerActionEvent).detail;
      this.#handleTriggerAction(action, triggerId);
    }) as EventListener);
    this.addEventListener(NJobDeskJobDetailCloseEvent.TYPE, () => (this._selectedJob = undefined));
  }

  #notify(color: NotificationColor, messageKey: string) {
    notify(color, this.localize.term(messageKey));
  }

  async #loadJobs() {
    this._loading = true;
    const response = await JobsService.getJobs({ query: { take: 500 } });
    this._jobs = response.data?.items ?? [];
    this._loading = false;
  }

  async #openJob(jobId: string) {
    const response = await JobsService.getJob({ path: splitId(jobId) });
    if (response.data) {
      this._selectedJob = response.data;
    }
  }

  async #refresh() {
    await this.#loadJobs();
    if (this._selectedJob) {
      await this.#openJob(`${this._selectedJob.job.group}/${this._selectedJob.job.name}`);
    }
  }

  async #runSimpleAction([call, toastKey]: SimpleAction, path: NJobDeskKeyPath) {
    this.#reportResult(!(await call(path)).error, toastKey);
  }

  async #handleJobAction(action: NJobDeskJobAction, jobId: string) {
    const path = splitId(jobId);
    try {
      const simple = SimpleJobActions[action];
      if (simple) {
        await this.#runSimpleAction(simple, path);
      } else if (action === "delete") {
        await confirm({
          headline: this.localize.term("njobdesk_confirmDeleteHeadline"),
          content: this.localize.term("njobdesk_confirmDeleteMessage", jobId),
          color: "danger",
          confirmLabel: this.localize.term("njobdesk_actionDelete"),
        });
        const response = await JobsService.deleteJob({ path });
        this.#reportResult(!response.error, "njobdesk_toastDeleted");
        this._selectedJob = undefined;
      }

      await this.#refresh();
    } catch {
      return;
    }
  }

  async #handleTriggerAction(action: NJobDeskTriggerAction, triggerId: string) {
    const path = splitId(triggerId);
    try {
      const simple = SimpleTriggerActions[action];
      if (simple) {
        await this.#runSimpleAction(simple, path);
        await this.#refresh();
        return;
      }

      switch (action) {
        case "unschedule": {
          await confirm({
            headline: this.localize.term("njobdesk_confirmUnscheduleHeadline"),
            content: this.localize.term("njobdesk_confirmUnscheduleMessage", triggerId),
            color: "danger",
            confirmLabel: this.localize.term("njobdesk_actionUnschedule"),
          });
          const response = await TriggersService.unscheduleTrigger({ path });
          this.#reportResult(!response.error, "njobdesk_toastUnscheduled");
          break;
        }
        case "edit": {
          const trigger = this.#findTrigger(triggerId);
          if (!trigger?.cronExpression) {
            return;
          }

          await openModal<NJobDeskEditScheduleModalData, never>(
            "njd-edit-schedule-modal",
            {
              group: trigger.group,
              name: trigger.name,
              cronExpression: trigger.cronExpression,
              timeZoneId: trigger.timeZoneId,
            },
            { type: "dialog", size: "small" },
          );
          this.#notify("positive", "njobdesk_toastRescheduled");
          break;
        }
        case "preview": {
          await this.#previewNextFires(triggerId);
          return;
        }
      }

      await this.#refresh();
    } catch {
      return;
    }
  }

  #findTrigger(triggerId: string): TriggerModel | undefined {
    return this._selectedJob?.triggers.find((candidate) => `${candidate.group}/${candidate.name}` === triggerId);
  }

  async #previewNextFires(triggerId: string) {
    const trigger = this.#findTrigger(triggerId);
    if (!trigger?.cronExpression) {
      return;
    }

    const response = await CronService.validateCron({
      body: { cronExpression: trigger.cronExpression, nextFireTimeCount: 5, timeZoneId: trigger.timeZoneId },
    });
    const fireTimes = response.data?.nextFireTimesUtc ?? [];
    try {
      await confirm({
        headline: this.localize.term("njobdesk_modalNextFires"),
        content: html`
          <p><code>${trigger.cronExpression}</code> — ${trigger.cronSummary ?? ""}</p>
          <ul>
            ${fireTimes.map((fireTime) => html`<li>${formatDateTime(fireTime)}</li>`)}
          </ul>
        `,
        confirmLabel: this.localize.term("general_close"),
      });
    } catch {
      return;
    }
  }

  #reportResult(success: boolean, successKey: string) {
    if (success) {
      this.#notify("positive", successKey);
    } else {
      this.#notify("danger", "njobdesk_toastActionFailed");
    }
  }

  get #filteredJobs(): JobSummaryModel[] {
    const filter = this._filter.trim().toLowerCase();
    return this._jobs.filter((job) => {
      if (!this._showSystemJobs && job.isSystemJob) {
        return false;
      }

      if (this._stateFilter && job.state !== this._stateFilter) {
        return false;
      }

      return !filter || job.name.toLowerCase().includes(filter) || job.group.toLowerCase().includes(filter);
    });
  }

  render() {
    if (this._selectedJob) {
      return html`<njd-job-detail .detail=${this._selectedJob}></njd-job-detail>`;
    }

    if (this._loading) {
      return html`<uui-loader-bar></uui-loader-bar>`;
    }

    const filtered = this.#filteredJobs;
    const pageCount = Math.max(1, Math.ceil(filtered.length / PageSize));
    const page = Math.min(this._page, pageCount);
    const visible = filtered.slice((page - 1) * PageSize, page * PageSize);

    return html`
      <div class="uui-text layout">
        ${this.#renderToolbar()}
        ${filtered.length === 0
          ? html`<njd-empty-state
              headline=${this.localize.term("njobdesk_jobsEmpty")}
              message=${this._jobs.length === 0 ? this.localize.term("njobdesk_jobsOnboarding") : ""}></njd-empty-state>`
          : html`
              <uui-box class="flush">
                ${this.#renderJobsTable(visible)}
              </uui-box>
              ${pageCount > 1
                ? html`<uui-pagination
                    .current=${page}
                    .total=${pageCount}
                    @change=${(event: UUIPaginationEvent) => (this._page = event.target.current)}></uui-pagination>`
                : nothing}
            `}
      </div>
    `;
  }

  #renderJobsTable(jobs: JobSummaryModel[]) {
    return html`
      <uui-table data-mark="njobdesk:table:jobs">
        <uui-table-head>
          <uui-table-head-cell>${this.localize.term("njobdesk_colJob")}</uui-table-head-cell>
          <uui-table-head-cell>${this.localize.term("njobdesk_colState")}</uui-table-head-cell>
          <uui-table-head-cell>${this.localize.term("njobdesk_colSchedule")}</uui-table-head-cell>
          <uui-table-head-cell class="right">${this.localize.term("njobdesk_colNextFire")}</uui-table-head-cell>
          <uui-table-head-cell class="right">${this.localize.term("njobdesk_colLastFire")}</uui-table-head-cell>
          <uui-table-head-cell class="center">${this.localize.term("njobdesk_colTriggers")}</uui-table-head-cell>
          <uui-table-head-cell class="right"></uui-table-head-cell>
        </uui-table-head>
        ${jobs.map((job) => {
          const jobId = `${job.group}/${job.name}`;
          return html`
            <uui-table-row>
              <uui-table-cell>
                <njd-job-link-cell .jobId=${jobId} .name=${job.name}></njd-job-link-cell>
              </uui-table-cell>
              <uui-table-cell>
                <njd-state-tag .value=${job.state}></njd-state-tag>
              </uui-table-cell>
              <uui-table-cell>
                <njd-cron-cell
                  .value=${{ summary: job.scheduleSummary } satisfies NJobDeskCronCellValue}></njd-cron-cell>
              </uui-table-cell>
              <uui-table-cell class="right">
                ${job.nextFireTimeUtc
                  ? html`<njd-relative-time .date=${job.nextFireTimeUtc}></njd-relative-time>`
                  : "—"}
              </uui-table-cell>
              <uui-table-cell class="right">
                ${job.previousFireTimeUtc
                  ? html`<njd-relative-time .date=${job.previousFireTimeUtc}></njd-relative-time>`
                  : "—"}
              </uui-table-cell>
              <uui-table-cell class="center">${job.triggerCount}</uui-table-cell>
              <uui-table-cell class="right">
                <njd-job-actions-cell .jobId=${jobId} .state=${job.state}></njd-job-actions-cell>
              </uui-table-cell>
            </uui-table-row>
          `;
        })}
      </uui-table>
    `;
  }

  #renderToolbar() {
    const countableJobs = this._jobs.filter((job) => this._showSystemJobs || !job.isSystemJob);
    const chipCount = (chip: JobState | undefined) =>
      chip ? countableJobs.filter((job) => job.state === chip).length : countableJobs.length;

    return html`
      <div class="toolbar">
        <uui-input
          id="search"
          placeholder=${this.localize.term("njobdesk_filterPlaceholder")}
          .value=${this._filter}
          @input=${(event: InputEvent) => {
            this._filter = (event.target as HTMLInputElement).value;
            this._page = 1;
          }}>
          <uui-icon slot="prepend" name="icon-search"></uui-icon>
        </uui-input>
        <div class="chips">
          ${StateChips.map((chip) => {
            const label = chip ?? this.localize.term("njobdesk_filterAll");
            return html`
              <uui-button
                look=${this._stateFilter === chip ? "primary" : "outline"}
                data-mark="njobdesk:chip:${chip ?? "all"}"
                label=${label}
                @click=${() => {
                  this._stateFilter = chip;
                  this._page = 1;
                }}>${label}<span class="count">${chipCount(chip)}</span></uui-button>
            `;
          })}
        </div>
        <uui-toggle
          label=${this.localize.term("njobdesk_showSystemJobs")}
          .checked=${this._showSystemJobs}
          @change=${(event: Event) => {
            this._showSystemJobs = (event.target as HTMLInputElement).checked;
            this._page = 1;
          }}>${this.localize.term("njobdesk_showSystemJobs")}</uui-toggle>
      </div>
    `;
  }

  static styles = [
    UUITextStyles,
    searchInputIconStyles,
    css`
      :host {
        display: block;
      }

      .layout > * + * {
        margin-top: var(--uui-size-layout-1);
      }

      .toolbar {
        display: flex;
        align-items: center;
        gap: var(--uui-size-space-5);
        flex-wrap: wrap;
      }

      #search {
        flex: 1;
        min-width: 200px;
        max-width: 320px;
      }

      .chips {
        display: flex;
        gap: var(--uui-size-space-2);
      }

      .chips .count {
        margin-left: var(--uui-size-space-2);
        font-variant-numeric: tabular-nums;
        font-weight: 400;
        opacity: 0.7;
      }

      .toolbar > uui-toggle {
        margin-left: auto;
      }

      uui-table-cell {
        height: var(--uui-size-16);
      }

      .right {
        text-align: right;
      }

      .center {
        text-align: center;
      }

      uui-pagination {
        display: block;
        margin-top: var(--uui-size-layout-1);
      }

      uui-box.flush {
        --uui-box-default-padding: 0;
      }
    `,
  ];
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-jobs-view": NJobDeskJobsViewElement;
  }
}
