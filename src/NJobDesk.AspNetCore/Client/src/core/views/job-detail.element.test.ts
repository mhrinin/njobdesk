import { expect } from "@esm-bundle/chai";
import type { JobDetailModel, SchedulerCapabilities } from "../api/index.js";
import { NJobDeskJobActionEvent } from "../components/job-actions-cell.element.js";
import { NJobDeskJobDetailCloseEvent, type NJobDeskJobDetailElement } from "./job-detail.element.js";
import "./job-detail.element.js";

const fullCapabilities: SchedulerCapabilities = {
  triggerNow: true,
  pause: true,
  scheduleEditing: true,
  delete: true,
  groups: true,
  triggers: true,
  history: true,
  runLogs: true,
  interrupt: true,
};

const noCapabilities: SchedulerCapabilities = {
  triggerNow: false,
  pause: false,
  scheduleEditing: false,
  delete: false,
  groups: false,
  triggers: false,
  history: false,
  runLogs: false,
  interrupt: false,
};

function createDetail(capabilities: SchedulerCapabilities = fullCapabilities): JobDetailModel {
  return {
    job: {
      id: "demo:site.cleanup",
      providerKey: "demo",
      group: "site",
      name: "cleanup",
      description: null,
      jobType: "Demo.CleanupJob",
      durable: true,
      concurrentExecutionDisallowed: false,
      triggerCount: 1,
      scheduleSummary: null,
      state: "Normal",
      nextFireTimeUtc: null,
      previousFireTimeUtc: null,
      isSystemJob: false,
      capabilities,
    },
    triggers: [
      {
        id: "demo:site.cleanup-trigger",
        group: "site",
        name: "cleanup-trigger",
        description: null,
        type: "Cron",
        cronExpression: "0 0 3 * * ?",
        cronSummary: "At 03:00",
        timeZoneId: null,
        state: "Normal",
        nextFireTimeUtc: null,
        previousFireTimeUtc: null,
        startTimeUtc: "2026-01-01T00:00:00Z",
        endTimeUtc: null,
        misfireInstruction: "SmartPolicy",
        priority: 5,
      },
    ],
    recentExecutions: [],
  };
}

async function createElement(detail: JobDetailModel): Promise<NJobDeskJobDetailElement> {
  const element = document.createElement("njd-job-detail");
  element.detail = detail;
  document.body.appendChild(element);
  await element.updateComplete;
  return element;
}

describe("njd-job-detail", () => {
  let element: NJobDeskJobDetailElement;

  beforeEach(async () => {
    element = await createElement(createDetail());
  });

  afterEach(() => {
    element.remove();
  });

  it("renders the job group and name in the header", () => {
    const header = element.shadowRoot!.querySelector(".detail-header h3")!;
    expect(header.textContent).to.contain("site /");
    expect(header.textContent).to.contain("cleanup");
  });

  it("renders one row per trigger", () => {
    const rows = element.shadowRoot!.querySelectorAll<HTMLElement>("uui-table-row");
    expect(rows.length).to.equal(1);
    expect(rows[0].textContent).to.contain("site/cleanup-trigger");
  });

  it("dispatches a close event from the back button", () => {
    let closed = false;
    element.addEventListener(NJobDeskJobDetailCloseEvent.TYPE, () => (closed = true));

    element.shadowRoot!.querySelector<HTMLElement>("[data-mark='njobdesk:action:back']")!.click();

    expect(closed).to.be.true;
  });

  it("dispatches a job action event carrying the opaque job id", () => {
    let received: { action: string; jobId: string } | undefined;
    element.addEventListener(NJobDeskJobActionEvent.TYPE, ((event: Event) => {
      received = (event as NJobDeskJobActionEvent).detail;
    }) as EventListener);

    element.shadowRoot!.querySelector<HTMLElement>("[data-mark='njobdesk:action:trigger']")!.click();

    expect(received).to.deep.equal({ action: "trigger", jobId: "demo:site.cleanup" });
  });

  it("hides management actions when the provider lacks the capabilities", async () => {
    const limited = await createElement(createDetail(noCapabilities));

    expect(limited.shadowRoot!.querySelector("[data-mark='njobdesk:action:trigger']")).to.be.null;
    expect(limited.shadowRoot!.querySelector("[popovertarget='detail-more']")).to.be.null;
    const triggerActions = limited.shadowRoot!.querySelector("njd-trigger-actions-cell")!;
    await triggerActions.updateComplete;
    expect(triggerActions.shadowRoot!.querySelector("[data-mark='njobdesk:action:edit']")).to.be.null;
    expect(triggerActions.shadowRoot!.querySelector("[data-mark='njobdesk:action:pause']")).to.be.null;
    limited.remove();
  });

  it("keeps the cron preview but hides editing without the schedule-editing capability", async () => {
    const limited = await createElement(createDetail({ ...noCapabilities, triggers: true }));

    const triggerActions = limited.shadowRoot!.querySelector("njd-trigger-actions-cell")!;
    await triggerActions.updateComplete;
    expect(triggerActions.shadowRoot!.querySelector("[data-mark='njobdesk:action:preview']")).to.not.be.null;
    expect(triggerActions.shadowRoot!.querySelector("[data-mark='njobdesk:action:edit']")).to.be.null;
    limited.remove();
  });

  it("renders a loader without a detail model", async () => {
    const empty = document.createElement("njd-job-detail");
    document.body.appendChild(empty);
    await empty.updateComplete;

    expect(empty.shadowRoot!.querySelector("uui-loader-bar")).to.not.be.null;
    empty.remove();
  });
});
