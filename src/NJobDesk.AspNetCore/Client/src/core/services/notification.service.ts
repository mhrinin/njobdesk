import type { UUIToastNotificationContainerElement } from "@umbraco-ui/uui-toast-notification-container/lib";

let container: UUIToastNotificationContainerElement | undefined;

export function setToastContainer(element: UUIToastNotificationContainerElement): void {
  container = element;
}

export type NotificationColor = "positive" | "danger" | "warning" | "default";

export function notify(color: NotificationColor, message: string): void {
  if (!container) {
    return;
  }

  const toast = document.createElement("uui-toast-notification");
  toast.color = color === "default" ? "" : color;

  const layout = document.createElement("uui-toast-notification-layout");
  layout.appendChild(document.createTextNode(message));
  toast.appendChild(layout);
  container.appendChild(toast);
}
