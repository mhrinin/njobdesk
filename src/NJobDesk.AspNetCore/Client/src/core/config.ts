import { client } from "./api/client.gen.js";
import type { Config } from "./api/client/types.gen.js";

export interface NJobDeskDashboardBootConfig {
  apiBase?: string;
  basePath?: string;
  readOnly?: boolean;
}

declare global {
  interface Window {
    __NJOBDESK__?: NJobDeskDashboardBootConfig;
  }
}

/** Merge transport configuration (baseUrl, credentials, auth headers) into the generated API client. */
export function configureNJobDeskClient(config: Partial<Config>): void {
  client.setConfig({ ...client.getConfig(), ...config });
}
