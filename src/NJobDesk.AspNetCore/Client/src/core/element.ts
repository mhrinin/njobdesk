import { LitElement } from "lit";
import { localize, registerLocalization } from "./localization/localize.js";
import en from "./localization/en.js";

registerLocalization("en", en);

export class NJobDeskElement extends LitElement {
  readonly localize = localize;
}
