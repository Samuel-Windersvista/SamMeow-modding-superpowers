/**
 * INI editing helpers — section/key upsert preserving other content.
 *
 * Per PLAN-PATCH P-F2: shared by S4 metadata tools.
 */

/** Upsert a value into [section] key= in INI text. Appends section if missing. */
export function upsertIniValue(
  text: string,
  section: string,
  key: string,
  value: string,
): string {
  const lines = text.split(/\r?\n/);
  let inSection = false;
  let sectionFoundIdx = -1;
  for (let i = 0; i < lines.length; i++) {
    const trimmed = lines[i].trim();
    if (trimmed === `[${section}]`) {
      inSection = true;
      sectionFoundIdx = i;
      continue;
    }
    if (inSection && trimmed.startsWith("[")) {
      inSection = false;
    }
    if (inSection && trimmed.startsWith(`${key}=`)) {
      lines[i] = `${key}=${value}`;
      return lines.join("\n");
    }
  }
  if (sectionFoundIdx >= 0) {
    lines.splice(sectionFoundIdx + 1, 0, `${key}=${value}`);
    return lines.join("\n");
  }
  return text + (text.endsWith("\n") ? "" : "\n") + `[${section}]\n${key}=${value}\n`;
}

/**
 * Quote + escape a value for QSettings-compatible INI output (meta.ini).
 *
 * MO2 reads meta.ini through QSettings (Qt IniFormat). Values with special
 * characters are written quoted, with `\` -> `\\`, `"` -> `\"` and newlines
 * escaped as literal `\n` sequences (verified against real MO2-written
 * `notes=` fields). Always quoting also keeps empty strings and
 * whitespace-padded values intact on read-back.
 */
export function qsQuote(value: string): string {
  const escaped = value
    .replace(/\\/g, "\\\\")
    .replace(/"/g, '\\"')
    .replace(/\r\n|\r|\n/g, "\\n");
  return `"${escaped}"`;
}
