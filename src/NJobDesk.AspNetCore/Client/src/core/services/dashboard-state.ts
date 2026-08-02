import type { ReactiveController, ReactiveControllerHost } from "lit";

// Read-only is a static server config for the app's lifetime. The standalone host injects it
// synchronously via window.__NJOBDESK__.readOnly; the Umbraco host does not, so the shell
// resolves it once from the scheduler status API. Components read it through ReadOnlyController.
const target = new EventTarget();
let readOnly = Boolean(globalThis.window?.__NJOBDESK__?.readOnly);

export function isReadOnly(): boolean {
  return readOnly;
}

export function setReadOnly(value: boolean): void {
  if (value !== readOnly) {
    readOnly = value;
    target.dispatchEvent(new Event("change"));
  }
}

export class ReadOnlyController implements ReactiveController {
  #host: ReactiveControllerHost;
  #onChange = () => this.#host.requestUpdate();

  constructor(host: ReactiveControllerHost) {
    this.#host = host;
    host.addController(this);
  }

  get readOnly(): boolean {
    return readOnly;
  }

  hostConnected(): void {
    target.addEventListener("change", this.#onChange);
  }

  hostDisconnected(): void {
    target.removeEventListener("change", this.#onChange);
  }
}
