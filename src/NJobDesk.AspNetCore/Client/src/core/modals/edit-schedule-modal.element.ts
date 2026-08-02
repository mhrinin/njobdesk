import { css, html, nothing } from "lit";
import { customElement, state } from "lit/decorators.js";
import { NJobDeskModalBaseElement } from "./modal-base.element.js";
import { CronService, TriggersService, type CronValidationResultModel } from "../api/index.js";
import { formatDateTime } from "../utils/format.js";
import "../components/modal-layout.element.js";

export interface NJobDeskEditScheduleModalData {
  group: string;
  name: string;
  cronExpression: string;
  timeZoneId?: string | null;
}

const ValidationDebounceMs = 400;

@customElement("njd-edit-schedule-modal")
export class NJobDeskEditScheduleModalElement extends NJobDeskModalBaseElement<NJobDeskEditScheduleModalData, never> {
  @state()
  private _cronExpression = "";

  @state()
  private _validation?: CronValidationResultModel;

  @state()
  private _validating = false;

  @state()
  private _saving = false;

  @state()
  private _saveError?: string;

  #debounceHandle?: number;

  connectedCallback() {
    super.connectedCallback();
    this._cronExpression = this.data?.cronExpression ?? "";
    this.#validate();
  }

  disconnectedCallback() {
    super.disconnectedCallback();
    window.clearTimeout(this.#debounceHandle);
  }

  #onCronInput(event: InputEvent) {
    this._cronExpression = (event.target as HTMLInputElement).value;
    this._validation = undefined;
    this._saveError = undefined;
    this._validating = true;
    window.clearTimeout(this.#debounceHandle);
    this.#debounceHandle = window.setTimeout(() => this.#validate(), ValidationDebounceMs);
  }

  async #validate() {
    const expression = this._cronExpression.trim();
    if (!expression) {
      this._validation = undefined;
      this._validating = false;
      return;
    }

    this._validating = true;
    const response = await CronService.validateCron({
      body: { cronExpression: expression, nextFireTimeCount: 5, timeZoneId: this.data?.timeZoneId },
    });
    this._validation = response.data;
    this._validating = false;
  }

  get #canSave(): boolean {
    return !this._saving && !this._validating && this._validation?.isValid === true;
  }

  async #save() {
    if (!this.#canSave || !this.data) {
      return;
    }

    this._saving = true;
    this._saveError = undefined;
    const response = await TriggersService.rescheduleTrigger({
      path: { group: this.data.group, name: this.data.name },
      body: { cronExpression: this._cronExpression.trim(), timeZoneId: this.data.timeZoneId },
    });
    this._saving = false;

    if (response.error || !response.data) {
      const problem = response.error as { detail?: string } | undefined;
      this._saveError = problem?.detail ?? this.localize.term("njobdesk_loadError");
      return;
    }

    this._submitModal();
  }

  render() {
    return html`
      <njd-modal-layout headline=${this.localize.term("njobdesk_modalEditScheduleHeadline")}>
        <uui-box>
          <p class="trigger-name">${this.data?.group}/${this.data?.name}</p>
          <uui-form-layout-item>
            <uui-label slot="label" for="cron">${this.localize.term("njobdesk_colCron")}</uui-label>
            <uui-input
              id="cron"
              .value=${this._cronExpression}
              @input=${this.#onCronInput}></uui-input>
          </uui-form-layout-item>
          ${this.#renderValidation()}
          ${this._saveError ? html`<p class="error">${this._saveError}</p>` : nothing}
        </uui-box>

        <uui-button
          slot="actions"
          look="secondary"
          label=${this.localize.term("general_cancel")}
          @click=${() => this._rejectModal()}></uui-button>
        <uui-button
          slot="actions"
          look="primary"
          color="positive"
          .state=${this._saving ? "waiting" : undefined}
          ?disabled=${!this.#canSave}
          label=${this.localize.term("buttons_save")}
          @click=${() => this.#save()}></uui-button>
      </njd-modal-layout>
    `;
  }

  #renderValidation() {
    if (this._validating) {
      return html`<uui-loader-bar></uui-loader-bar>`;
    }

    if (!this._validation) {
      return nothing;
    }

    if (!this._validation.isValid) {
      return html`
        <uui-tag color="danger" look="secondary">${this.localize.term("njobdesk_modalInvalid")}</uui-tag>
        <p class="error">${this._validation.error}</p>
      `;
    }

    return html`
      <uui-tag color="positive" look="secondary">${this.localize.term("njobdesk_modalValid")}</uui-tag>
      <p class="summary">${this._validation.summary}</p>
      <p class="next-fires-label">${this.localize.term("njobdesk_modalNextFires")}</p>
      <ul>
        ${this._validation.nextFireTimesUtc?.map((fireTime) => html`<li>${formatDateTime(fireTime)}</li>`)}
      </ul>
    `;
  }

  static styles = css`
    uui-input {
      width: 100%;
    }

    .trigger-name {
      margin-top: 0;
      font-weight: 600;
    }

    .summary {
      white-space: pre-line;
      color: var(--uui-color-text-alt);
    }

    .next-fires-label {
      font-weight: 600;
      margin-bottom: var(--uui-size-space-1);
    }

    ul {
      margin: 0;
      padding-left: var(--uui-size-space-5);
    }

    .error {
      color: var(--uui-color-danger);
    }
  `;
}

export default NJobDeskEditScheduleModalElement;

declare global {
  interface HTMLElementTagNameMap {
    "njd-edit-schedule-modal": NJobDeskEditScheduleModalElement;
  }
}
