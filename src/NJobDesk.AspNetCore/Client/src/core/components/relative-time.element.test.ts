import { expect } from "@esm-bundle/chai";
import type { NJobDeskRelativeTimeElement } from "./relative-time.element.js";
import "./relative-time.element.js";

describe("njd-relative-time", () => {
  let element: NJobDeskRelativeTimeElement;

  beforeEach(() => {
    element = document.createElement("njd-relative-time");
    document.body.appendChild(element);
  });

  afterEach(() => {
    element.remove();
  });

  it("renders a dash without a date", async () => {
    await element.updateComplete;
    expect(element.shadowRoot!.textContent).to.contain("—");
  });

  it("renders a time element carrying the source date", async () => {
    const iso = new Date(Date.now() + 2 * 3_600_000).toISOString();
    element.date = iso;
    await element.updateComplete;

    const time = element.shadowRoot!.querySelector("time")!;
    expect(time.getAttribute("datetime")).to.equal(iso);
    expect(time.textContent!.trim()).to.not.be.empty;
  });
});
