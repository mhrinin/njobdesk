type Dictionary = Record<string, Record<string, string>>;

const dictionaries = new Map<string, Dictionary>();
let culture = (document.documentElement.lang || navigator.language || "en").split("-")[0];

export function registerLocalization(language: string, dictionary: Dictionary): void {
  dictionaries.set(language, dictionary);
}

export function setCulture(language: string): void {
  culture = language.split("-")[0];
}

/** Umbraco-style "section_key" lookup with %0%, %1%, ... argument substitution. */
function term(key: string, ...args: unknown[]): string {
  const separator = key.indexOf("_");
  if (separator < 0) {
    return key;
  }

  const section = key.slice(0, separator);
  const entry = key.slice(separator + 1);
  const value = dictionaries.get(culture)?.[section]?.[entry] ?? dictionaries.get("en")?.[section]?.[entry];
  if (value === undefined) {
    return key;
  }

  return value.replace(/%(\d+)%/g, (match, index) => {
    const argument = args[Number(index)];
    return argument === undefined ? match : String(argument);
  });
}

function relativeTime(value: number, unit: Intl.RelativeTimeFormatUnit): string {
  return new Intl.RelativeTimeFormat(culture, { numeric: "auto" }).format(value, unit);
}

export const localize = { term, relativeTime };
