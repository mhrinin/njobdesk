import type { ReactiveController, ReactiveControllerHost } from "lit";
import type { ProviderStatusModel } from "../api/index.js";

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

// The registered scheduler providers (with capabilities and degraded state) come from the same
// status endpoint. The shell fetches them on boot and the overview refreshes them while polling;
// components read them through ProvidersController to gate actions and render badges.
let providers: ProviderStatusModel[] = [];

export function setProviders(next: ProviderStatusModel[]): void {
  providers = next;
  target.dispatchEvent(new Event("change"));
}

export function getProviders(): ProviderStatusModel[] {
  return providers;
}

export function findProvider(key: string | undefined): ProviderStatusModel | undefined {
  return providers.find((provider) => provider.key === key);
}

export class ProvidersController implements ReactiveController {
  #host: ReactiveControllerHost;
  #onChange = () => this.#host.requestUpdate();

  constructor(host: ReactiveControllerHost) {
    this.#host = host;
    host.addController(this);
  }

  get providers(): ProviderStatusModel[] {
    return providers;
  }

  get multiProvider(): boolean {
    return providers.length > 1;
  }

  get degraded(): ProviderStatusModel[] {
    return providers.filter((provider) => provider.degraded);
  }

  find(key: string | undefined): ProviderStatusModel | undefined {
    return findProvider(key);
  }

  hostConnected(): void {
    target.addEventListener("change", this.#onChange);
  }

  hostDisconnected(): void {
    target.removeEventListener("change", this.#onChange);
  }
}
