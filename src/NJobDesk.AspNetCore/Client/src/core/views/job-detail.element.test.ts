import { expect } from "@esm-bundle/chai";
import type { JobDetailModel } from "../api/index.js";
import { NJobDeskJobActionEvent } from "../components/job-actions-cell.element.js";
import { NJobDeskJobDetailCloseEvent, type NJobDeskJobDetailElement } from "./job-detail.element.js";
import "./job-detail.element.js";

function createDetail(): JobDetailModel {
  return {
    job: {
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
    },
    triggers: [
      {
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

describe("njd-job-detail", () => {
  let element: NJobDeskJobDetailElement;

  beforeEach(async () => {
    element = document.createElement("njd-job-detail");
    element.detail = createDetail();
    document.body.appendChild(element);
    await element.updateComplete;
  });

  afterEach(() => {
    element.remove();
  });

  it("renders the job id in the header", () => {
    const header = element.shadowRoot!.querySelector(".detail-header h3")!;
    expect(header.textContent).to.equal("site/cleanup");
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

  it("dispatches a job action event from the trigger button", () => {
    let received: { action: string; jobId: string } | undefined;
    element.addEventListener(NJobDeskJobActionEvent.TYPE, ((event: Event) => {
      received = (event as NJobDeskJobActionEvent).detail;
    }) as EventListener);

    element.shadowRoot!.querySelector<HTMLElement>("[data-mark='njobdesk:action:trigger']")!.click();

    expect(received).to.deep.equal({ action: "trigger", jobId: "site/cleanup" });
  });

  it("renders a loader without a detail model", async () => {
    const empty = document.createElement("njd-job-detail");
    document.body.appendChild(empty);
    await empty.updateComplete;

    expect(empty.shadowRoot!.querySelector("uui-loader-bar")).to.not.be.null;
    empty.remove();
  });
});
