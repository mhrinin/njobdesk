import { LitElement, css, html } from "lit";
import { customElement, property } from "lit/decorators.js";

export class NJobDeskJobOpenEvent extends CustomEvent<{ jobId: string }> {
  public static readonly TYPE = "njd-job-open";

  public constructor(jobId: string) {
    super(NJobDeskJobOpenEvent.TYPE, { detail: { jobId }, bubbles: true, composed: true });
  }
}

@customElement("njd-job-link-cell")
export class NJobDeskJobLinkCellElement extends LitElement {
  @property()
  jobId = "";

  @property()
  name = "";

  get #group(): string {
    const separatorIndex = this.jobId.indexOf("/");
    return separatorIndex > 0 ? this.jobId.slice(0, separatorIndex) : "";
  }

  render() {
    return html`
      <button type="button" @click=${() => this.dispatchEvent(new NJobDeskJobOpenEvent(this.jobId))}>
        <span class="name">${this.name}</span>
        ${this.#group ? html`<span class="group">${this.#group}</span>` : ""}
      </button>
    `;
  }

  static styles = css`
    button {
      all: unset;
      cursor: pointer;
      display: flex;
      flex-direction: column;
    }

    .name {
      color: var(--uui-color-interactive);
      font-weight: 600;
    }

    .group {
      font-size: var(--uui-type-small-size);
      color: var(--uui-color-text-alt);
    }

    button:hover .name {
      color: var(--uui-color-interactive-emphasis);
      text-decoration: underline;
    }
  `;
}

declare global {
  interface HTMLElementTagNameMap {
    "njd-job-link-cell": NJobDeskJobLinkCellElement;
  }
}
