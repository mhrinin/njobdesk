import type { ExecutionLogLevel, ExecutionStatus, JobState } from "../api/index.js";

export type TagColor = "default" | "positive" | "warning" | "danger";

const jobStateIcons: Partial<Record<JobState, string>> = {
  Normal: "icon-check",
  Paused: "icon-pause",
  Error: "icon-alert",
  Blocked: "icon-time",
};

const executionStateIcons: Partial<Record<ExecutionStatus, string>> = {
  Succeeded: "icon-check",
  Failed: "icon-alert",
  Running: "icon-time",
};

const jobStateTagColors: Partial<Record<JobState, TagColor>> = {
  Normal: "positive",
  Paused: "warning",
  Error: "danger",
};

const executionStateTagColors: Partial<Record<ExecutionStatus, TagColor>> = {
  Succeeded: "positive",
  Failed: "danger",
  Running: "warning",
};

export const jobStateIcon = (state: JobState): string => jobStateIcons[state] ?? "icon-block";

export const executionStateIcon = (state: ExecutionStatus): string => executionStateIcons[state] ?? "icon-block";

export const jobStateTagColor = (state: JobState): TagColor => jobStateTagColors[state] ?? "default";

export const executionStateTagColor = (state: ExecutionStatus): TagColor => executionStateTagColors[state] ?? "default";

const logLevelTagColors: Partial<Record<ExecutionLogLevel, TagColor>> = {
  Warning: "warning",
  Error: "danger",
  Critical: "danger",
};

export const logLevelTagColor = (level: ExecutionLogLevel): TagColor => logLevelTagColors[level] ?? "default";

export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) {
    return "—";
  }

  return new Date(iso).toLocaleString(undefined, {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

const relativeUnits: Array<[Intl.RelativeTimeFormatUnit, number]> = [
  ["day", 86_400_000],
  ["hour", 3_600_000],
  ["minute", 60_000],
  ["second", 1_000],
];

export function pickRelativeTimeUnit(deltaMs: number): { value: number; unit: Intl.RelativeTimeFormatUnit } {
  for (const [unit, unitMs] of relativeUnits) {
    if (Math.abs(deltaMs) >= unitMs || unit === "second") {
      return { value: Math.round(deltaMs / unitMs), unit };
    }
  }

  return { value: 0, unit: "second" };
}

export function formatDuration(durationMs: number | null | undefined): string {
  if (durationMs === null || durationMs === undefined) {
    return "—";
  }

  if (durationMs < 1_000) {
    return `${durationMs} ms`;
  }

  if (durationMs < 60_000) {
    return `${(durationMs / 1_000).toFixed(1)} s`;
  }

  const minutes = Math.floor(durationMs / 60_000);
  const seconds = Math.round((durationMs % 60_000) / 1_000);
  return `${minutes}m ${seconds}s`;
}

export function elapsedSince(iso: string | null | undefined, nowMs = Date.now()): string {
  if (!iso) {
    return "—";
  }

  return formatDuration(Math.max(0, nowMs - new Date(iso).getTime()));
}
