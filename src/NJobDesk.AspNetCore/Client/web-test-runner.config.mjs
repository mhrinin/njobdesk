import { fileURLToPath } from "node:url";
import { esbuildPlugin } from "@web/dev-server-esbuild";

export default {
  files: "src/**/*.test.ts",
  nodeResolve: true,
  plugins: [
    esbuildPlugin({
      ts: true,
      target: "es2022",
      tsconfig: fileURLToPath(new URL("./tsconfig.json", import.meta.url)),
    }),
  ],
};
