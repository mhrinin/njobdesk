import { property } from "lit/decorators.js";
import { NJobDeskElement } from "../element.js";
import type { NJobDeskModalHandle } from "../services/modal.service.js";

export abstract class NJobDeskModalBaseElement<TData, TResult = never> extends NJobDeskElement {
  @property({ attribute: false })
  data?: TData;

  @property({ attribute: false })
  modalHandle?: NJobDeskModalHandle<TResult>;

  protected _submitModal(result?: TResult) {
    this.modalHandle?.submit(result);
  }

  protected _rejectModal(reason?: unknown) {
    this.modalHandle?.reject(reason);
  }
}
