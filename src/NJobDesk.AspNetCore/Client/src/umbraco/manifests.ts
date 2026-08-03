export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "NJobDesk Dashboard",
    alias: "NJobDesk.Dashboard",
    type: "dashboard",
    js: () => import("./dashboard-wrapper.element.js"),
    weight: -10,
    meta: {
      label: "Jobs",
      pathname: "njobdesk",
    },
    conditions: [
      {
        alias: "Umb.Condition.SectionAlias",
        match: "Umb.Section.Settings",
      },
    ],
  },
];
