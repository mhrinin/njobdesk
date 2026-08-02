import { defineConfig } from "vite";

// Self-contained SPA embedded into the Dashboard assembly; everything (lit, uui, css) is bundled
// and asset paths are relative so it serves under any MapNJobDeskDashboard base path.
export default defineConfig({
  root: "src/standalone",
  base: "./",
  publicDir: false,
  build: {
    outDir: "../../../assets",
    emptyOutDir: true,
    sourcemap: false,
    rollupOptions: {
      external: (id: string) => {
        if (id.startsWith("@umbraco-cms")) {
          throw new Error(`Backoffice import leaked into the standalone bundle: ${id}`);
        }

        return false;
      },
    },
  },
});
