import type { CreateClientConfig } from "./api/client.gen";

/**
 * Pre-configure the generated client (called by the generated code via runtimeConfigPath).
 * The standalone host injects window.__NJOBDESK__ before the app module loads; an empty
 * baseUrl means same-origin relative requests. Hosts can override later via configureNJobDeskClient.
 */
export const createClientConfig: CreateClientConfig = (config) => ({
  ...config,
  baseUrl: globalThis.window?.__NJOBDESK__?.apiBase ?? "",
  credentials: "same-origin",
});
