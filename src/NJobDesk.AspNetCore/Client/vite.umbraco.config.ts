import { defineConfig } from "vite";

// Backoffice extension bundle: lit + the uui subpackages the app structurally needs are bundled
// (uui's defineElement skips tags the backoffice already registered), only the backoffice import-map
// packages stay external.
export default defineConfig({
  build: {
    lib: {
      entry: "src/umbraco/manifests.ts",
      formats: ["es"],
      fileName: "njobdesk",
    },
    outDir: "../../NJobDesk.Umbraco/wwwroot/App_Plugins/NJobDesk",
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      external: [/^@umbraco-cms/],
    },
  },
});
