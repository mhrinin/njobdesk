import type { ExecutionModel } from "../api/index.js";
import { openModal } from "../services/modal.service.js";
import "../modals/run-details-modal.element.js";

export class NJobDeskRunOpenEvent extends CustomEvent<{ execution: ExecutionModel }> {
  public static readonly TYPE = "njd-run-open";

  public constructor(execution: ExecutionModel) {
    super(NJobDeskRunOpenEvent.TYPE, { detail: { execution }, bubbles: true, composed: true });
  }
}

export function attachRunOpenListener(host: EventTarget): void {
  host.addEventListener(NJobDeskRunOpenEvent.TYPE, ((event: Event) =>
    openModal(
      "njd-run-details-modal",
      { execution: (event as NJobDeskRunOpenEvent).detail.execution },
      { type: "sidebar", size: "large" },
    ).catch(() => undefined)) as EventListener);
}
