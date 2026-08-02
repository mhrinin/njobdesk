import { expect } from "@esm-bundle/chai";
import {
  elapsedSince,
  executionStateIcon,
  executionStateTagColor,
  formatDateTime,
  formatDuration,
  jobStateIcon,
  jobStateTagColor,
  logLevelTagColor,
  pickRelativeTimeUnit,
} from "./format.js";

describe("jobStateTagColor", () => {
  it("maps job states to tag colors", () => {
    expect(jobStateTagColor("Normal")).to.equal("positive");
    expect(jobStateTagColor("Paused")).to.equal("warning");
    expect(jobStateTagColor("Error")).to.equal("danger");
    expect(jobStateTagColor("Blocked")).to.equal("default");
    expect(jobStateTagColor("None")).to.equal("default");
  });
});

describe("executionStateTagColor", () => {
  it("maps execution states to tag colors", () => {
    expect(executionStateTagColor("Succeeded")).to.equal("positive");
    expect(executionStateTagColor("Failed")).to.equal("danger");
    expect(executionStateTagColor("Running")).to.equal("warning");
    expect(executionStateTagColor("Vetoed")).to.equal("default");
  });
});

describe("logLevelTagColor", () => {
  it("maps log levels to tag colors", () => {
    expect(logLevelTagColor("Trace")).to.equal("default");
    expect(logLevelTagColor("Debug")).to.equal("default");
    expect(logLevelTagColor("Information")).to.equal("default");
    expect(logLevelTagColor("Warning")).to.equal("warning");
    expect(logLevelTagColor("Error")).to.equal("danger");
    expect(logLevelTagColor("Critical")).to.equal("danger");
  });
});

describe("formatDuration", () => {
  it("formats milliseconds below one second", () => {
    expect(formatDuration(250)).to.equal("250 ms");
  });

  it("formats seconds below one minute", () => {
    expect(formatDuration(45_013)).to.equal("45.0 s");
  });

  it("formats minutes and seconds", () => {
    expect(formatDuration(125_000)).to.equal("2m 5s");
  });

  it("falls back to a dash for missing values", () => {
    expect(formatDuration(null)).to.equal("—");
    expect(formatDuration(undefined)).to.equal("—");
  });
});

describe("pickRelativeTimeUnit", () => {
  it("picks hours for multi-hour deltas", () => {
    expect(pickRelativeTimeUnit(2 * 3_600_000)).to.deep.equal({ value: 2, unit: "hour" });
  });

  it("picks minutes for sub-hour deltas", () => {
    expect(pickRelativeTimeUnit(-120_000)).to.deep.equal({ value: -2, unit: "minute" });
  });

  it("falls back to seconds for tiny deltas", () => {
    expect(pickRelativeTimeUnit(500)).to.deep.equal({ value: 1, unit: "second" });
  });
});

describe("state icons", () => {
  it("maps job states to icons", () => {
    expect(jobStateIcon("Normal")).to.equal("icon-check");
    expect(jobStateIcon("Paused")).to.equal("icon-pause");
    expect(jobStateIcon("Error")).to.equal("icon-alert");
    expect(jobStateIcon("Blocked")).to.equal("icon-time");
    expect(jobStateIcon("None")).to.equal("icon-block");
  });

  it("maps execution states to icons", () => {
    expect(executionStateIcon("Succeeded")).to.equal("icon-check");
    expect(executionStateIcon("Failed")).to.equal("icon-alert");
    expect(executionStateIcon("Running")).to.equal("icon-time");
    expect(executionStateIcon("Vetoed")).to.equal("icon-block");
  });
});

describe("elapsedSince", () => {
  const now = Date.UTC(2026, 6, 11, 12, 0, 0);

  it("returns the elapsed duration", () => {
    expect(elapsedSince(new Date(now - 45_000).toISOString(), now)).to.equal("45.0 s");
  });

  it("never goes negative", () => {
    expect(elapsedSince(new Date(now + 60_000).toISOString(), now)).to.equal("0 ms");
  });
});

describe("formatDateTime", () => {
  it("falls back to a dash for missing values", () => {
    expect(formatDateTime(null)).to.equal("—");
    expect(formatDateTime(undefined)).to.equal("—");
  });

  it("formats an ISO timestamp", () => {
    expect(formatDateTime("2026-07-11T12:00:00Z")).to.not.equal("—");
  });
});
