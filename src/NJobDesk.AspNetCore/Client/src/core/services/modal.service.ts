import type { UUIModalContainerElement, UUIModalElement } from "@umbraco-ui/uui-modal/lib";

let container: UUIModalContainerElement | undefined;

export function setModalContainer(element: UUIModalContainerElement): void {
  container = element;
}

export interface NJobDeskModalHandle<TResult> {
  submit(result?: TResult): void;
  reject(reason?: unknown): void;
}

export interface NJobDeskModalOptions {
  type?: "dialog" | "sidebar";
  size?: "small" | "medium" | "large" | "full";
}

interface ModalContentElement<TData, TResult> extends HTMLElement {
  data?: TData;
  modalHandle?: NJobDeskModalHandle<TResult>;
}

/**
 * Opens `tag` inside a uui-modal in the app's modal container, passing `data` to the element and
 * a handle it submits or rejects through. The promise resolves on submit and rejects on dismiss
 * (backdrop, escape) or explicit rejection — the same contract as the Umbraco modal manager.
 */
export function openModal<TData, TResult = void>(
  tag: string,
  data: TData,
  options?: NJobDeskModalOptions,
): Promise<TResult> {
  if (!container) {
    return Promise.reject(new Error("NJobDesk modal container is not mounted."));
  }

  const host = container;
  return new Promise<TResult>((resolve, reject) => {
    const modal = document.createElement(
      options?.type === "sidebar" ? "uui-modal-sidebar" : "uui-modal-dialog",
    ) as UUIModalElement & { size?: string };
    if (options?.type === "sidebar" && options.size) {
      modal.size = options.size;
    }

    const content = document.createElement(tag) as ModalContentElement<TData, TResult>;
    content.data = data;

    let settled = false;
    content.modalHandle = {
      submit: (result?: TResult) => {
        settled = true;
        resolve(result as TResult);
        modal.close();
      },
      reject: (reason?: unknown) => {
        settled = true;
        reject(reason);
        modal.close();
      },
    };

    modal.addEventListener("uui:modal-close-end", () => {
      if (!settled) {
        settled = true;
        reject(new Error("Modal was dismissed."));
      }

      modal.remove();
    });

    modal.appendChild(content);
    host.appendChild(modal);
  });
}

export interface NJobDeskConfirmArgs {
  headline: string;
  content: unknown;
  color?: "danger" | "positive";
  confirmLabel?: string;
}

/** Resolves when confirmed, rejects when cancelled — the same contract as umbConfirmModal. */
export function confirm(args: NJobDeskConfirmArgs): Promise<void> {
  return openModal<NJobDeskConfirmArgs, void>("njd-confirm-modal", args);
}
