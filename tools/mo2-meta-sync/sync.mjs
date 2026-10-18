#!/usr/bin/env node
// mo2-meta-sync: sync a mod repo's description fields into an MO2 overlay meta.ini.
//
// Node ESM, zero external dependencies. The source of truth lives in the mod repo
// (fragment.json); this tool pushes it into the MO2 overlay's meta.ini at deploy
// time (the deploy flow deliberately protects meta.ini from being overwritten, so
// its descriptive fields would otherwise drift forever).
//
// Only the four description keys are ever touched, and only inside [General]:
//   comments, notes, version, installationFile
// Every other key and every other section survives byte-for-byte.

import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';

const TOOL = 'mo2-meta-sync';
const ALLOWED_KEYS = ['comments', 'notes', 'version', 'installationFile'];

class SyncError extends Error {}

// ---------------------------------------------------------------------------
// Console safety: Windows GBK consoles must not be corrupted by non-ASCII output.
// Everything printed to stdout/stderr goes through here.
// ---------------------------------------------------------------------------
function asciiSafe(value, maxLen) {
  let out = '';
  for (const ch of String(value)) {
    const cp = ch.codePointAt(0);
    if (ch === '\n') out += '\\n';
    else if (ch === '\r') out += '\\r';
    else if (ch === '\t') out += '\\t';
    else if (cp >= 0x20 && cp <= 0x7e) out += ch;
    else if (cp <= 0xffff) out += '\\u' + cp.toString(16).padStart(4, '0');
    else out += '\\U' + cp.toString(16).padStart(8, '0');
  }
  if (maxLen && out.length > maxLen) out = out.slice(0, maxLen) + '...';
  return out;
}

function log(msg) {
  process.stdout.write(msg + '\n');
}

// ---------------------------------------------------------------------------
// File-level encoding: preserve BOM presence and line-ending style.
// ---------------------------------------------------------------------------
function readBytes(filePath, label) {
  try {
    return fs.readFileSync(filePath);
  } catch (err) {
    throw new SyncError(`cannot read ${label} file: ${filePath} (${err.code || err.message})`);
  }
}

function decodeUtf8(buf) {
  let bom = false;
  let body = buf;
  if (buf.length >= 3 && buf[0] === 0xef && buf[1] === 0xbb && buf[2] === 0xbf) {
    bom = true;
    body = buf.subarray(3);
  }
  return { bom, text: body.toString('utf8') };
}

function encodeUtf8(text, bom) {
  return Buffer.from((bom ? '\ufeff' : '') + text, 'utf8');
}

// Split into records { text, eol } so untouched lines round-trip byte-for-byte.
function toRecords(text) {
  const records = [];
  let start = 0;
  for (let i = 0; i < text.length; i += 1) {
    const ch = text[i];
    if (ch === '\n') {
      records.push({ text: text.slice(start, i), eol: '\n' });
      start = i + 1;
    } else if (ch === '\r') {
      if (text[i + 1] === '\n') {
        records.push({ text: text.slice(start, i), eol: '\r\n' });
        i += 1;
      } else {
        records.push({ text: text.slice(start, i), eol: '\r' });
      }
      start = i + 1;
    }
  }
  if (start < text.length) {
    records.push({ text: text.slice(start), eol: '' });
  } else if (text.length === 0) {
    records.push({ text: '', eol: '' });
  }
  return records;
}

function serialize(records) {
  return records.map((r) => r.text + r.eol).join('');
}

function detectEol(records) {
  for (const r of records) {
    if (r.eol) return r.eol;
  }
  return '\n';
}

// ---------------------------------------------------------------------------
// meta.ini grammar (QSettings INI dialect).
// ---------------------------------------------------------------------------
function sectionName(text) {
  const m = /^\s*\[([^\]]+)\]\s*$/.exec(text);
  return m ? m[1] : null;
}

function keyOf(text) {
  const eq = text.indexOf('=');
  if (eq === -1) return null;
  return text.slice(0, eq).trim();
}

function rawValueOf(text) {
  const eq = text.indexOf('=');
  return eq === -1 ? '' : text.slice(eq + 1);
}

function valueIsQuoted(text) {
  const v = rawValueOf(text).trim();
  return v.length >= 2 && v.startsWith('"') && v.endsWith('"');
}

function unescapeValue(inner) {
  let out = '';
  let i = 0;
  while (i < inner.length) {
    const ch = inner[i];
    if (ch !== '\\') {
      out += ch;
      i += 1;
      continue;
    }
    const next = inner[i + 1];
    if (next === undefined) {
      out += '\\';
      i += 1;
      continue;
    }
    switch (next) {
      case '\\': out += '\\'; i += 2; break;
      case '"': out += '"'; i += 2; break;
      case 'n': out += '\n'; i += 2; break;
      case 'r': out += '\r'; i += 2; break;
      case 't': out += '\t'; i += 2; break;
      case 'a': out += '\x07'; i += 2; break;
      case 'b': out += '\b'; i += 2; break;
      case 'f': out += '\f'; i += 2; break;
      case 'v': out += '\v'; i += 2; break;
      case 'x': {
        const hex = inner.slice(i + 2, i + 6);
        if (/^[0-9a-fA-F]{1,4}$/.test(hex)) {
          out += String.fromCharCode(parseInt(hex, 16));
          i += 2 + hex.length;
        } else {
          out += 'x';
          i += 2;
        }
        break;
      }
      default:
        out += next;
        i += 2;
    }
  }
  return out;
}

function decodeIniValue(text) {
  const v = rawValueOf(text).trim();
  if (v.length >= 2 && v.startsWith('"') && v.endsWith('"')) {
    return unescapeValue(v.slice(1, -1));
  }
  return v;
}

function needsQuotes(value) {
  if (value === '') return false;
  if (/^\s|\s$/.test(value)) return true;
  return /[\r\n"\\]/.test(value);
}

function escapeQuoted(value) {
  let out = '';
  for (const ch of value) {
    if (ch === '\\') out += '\\\\';
    else if (ch === '"') out += '\\"';
    else if (ch === '\n') out += '\\n';
    else if (ch === '\r') out += '\\r';
    else out += ch;
  }
  return out;
}

// preferQuoted mirrors the file's existing style for an already-present key.
function formatValue(value, preferQuoted) {
  if (needsQuotes(value) || preferQuoted) {
    return '"' + escapeQuoted(value) + '"';
  }
  return value;
}

// ---------------------------------------------------------------------------
// Fragment validation.
// ---------------------------------------------------------------------------
function parseFragment(filePath) {
  const buf = readBytes(filePath, 'fragment');
  // Tolerate a UTF-8 BOM (Windows editors / Set-Content -Encoding utf8 add one).
  const { text } = decodeUtf8(buf);
  let parsed;
  try {
    parsed = JSON.parse(text);
  } catch (err) {
    throw new SyncError(`invalid fragment JSON: ${filePath} (${err.message})`);
  }
  if (parsed === null || typeof parsed !== 'object' || Array.isArray(parsed)) {
    throw new SyncError('fragment must be a JSON object');
  }
  const fragment = {};
  for (const [key, value] of Object.entries(parsed)) {
    if (!ALLOWED_KEYS.includes(key)) {
      throw new SyncError(`fragment key not allowed: "${key}" (allowed: ${ALLOWED_KEYS.join(', ')})`);
    }
    if (typeof value !== 'string') {
      throw new SyncError(`fragment value for "${key}" must be a string`);
    }
    fragment[key] = value;
  }
  return fragment;
}

// ---------------------------------------------------------------------------
// Locate the [General] section and validate the file is parseable.
// ---------------------------------------------------------------------------
function findGeneral(records) {
  let genIdx = -1;
  for (let i = 0; i < records.length; i += 1) {
    const name = sectionName(records[i].text);
    if (name !== null && name !== 'General') {
      // A section header is fine anywhere.
      continue;
    }
    if (name === 'General') {
      genIdx = i;
      break;
    }
  }
  return genIdx;
}

function validateParseable(records) {
  for (let i = 0; i < records.length; i += 1) {
    const text = records[i].text;
    if (text.trim() === '') continue;
    if (sectionName(text) !== null) continue;
    if (/^\s*[;#]/.test(text)) continue; // comment line
    if (text.indexOf('=') === -1) {
      throw new SyncError(`unparsable meta.ini at line ${i + 1}: "${asciiSafe(text, 80)}"`);
    }
  }
}

// ---------------------------------------------------------------------------
// Push: fragment -> meta.ini records.
// ---------------------------------------------------------------------------
function planPush(records, fragment) {
  const changes = [];
  const eol = detectEol(records);
  let genIdx = findGeneral(records);
  let start;
  let end;

  if (genIdx === -1) {
    if (records.length && records[records.length - 1].eol === '') {
      records[records.length - 1].eol = eol;
    }
    if (records.length && records[records.length - 1].text !== '') {
      records.push({ text: '', eol });
    }
    records.push({ text: '[General]', eol });
    genIdx = records.length - 1;
    start = records.length;
    end = records.length;
  } else {
    start = genIdx + 1;
    end = records.length;
    for (let i = start; i < records.length; i += 1) {
      if (sectionName(records[i].text) !== null) {
        end = i;
        break;
      }
    }
  }

  const missing = [];
  let lastKeyIdx = -1;
  for (let i = start; i < end; i += 1) {
    if (keyOf(records[i].text) !== null) lastKeyIdx = i;
  }

  for (const key of ALLOWED_KEYS) {
    if (!Object.prototype.hasOwnProperty.call(fragment, key)) continue;
    const value = fragment[key];
    let found = -1;
    for (let i = start; i < end; i += 1) {
      if (keyOf(records[i].text) === key) {
        found = i;
        break;
      }
    }
    if (found === -1) {
      missing.push({ key, value });
      continue;
    }
    const oldText = records[found].text;
    const newText = key + '=' + formatValue(value, valueIsQuoted(oldText));
    if (newText !== oldText) {
      records[found].text = newText;
      changes.push({ key, oldValue: decodeIniValue(oldText), newValue: value, added: false });
    }
  }

  if (missing.length > 0) {
    let insertAt = lastKeyIdx === -1 ? start : lastKeyIdx + 1;
    if (insertAt > 0 && records[insertAt - 1] && records[insertAt - 1].eol === '') {
      records[insertAt - 1].eol = eol;
    }
    const newRecords = [];
    for (const { key, value } of missing) {
      newRecords.push({ text: key + '=' + formatValue(value, false), eol });
      changes.push({ key, oldValue: undefined, newValue: value, added: true });
    }
    records.splice(insertAt, 0, ...newRecords);
  }

  return { changes };
}

function writeAtomically(filePath, buffer) {
  const dir = path.dirname(filePath);
  const tmp = path.join(dir, `.${path.basename(filePath)}.tmp-${process.pid}-${Date.now()}`);
  try {
    fs.writeFileSync(tmp, buffer);
    fs.renameSync(tmp, filePath);
  } catch (err) {
    try {
      if (fs.existsSync(tmp)) fs.unlinkSync(tmp);
    } catch { /* best effort cleanup */ }
    throw new SyncError(`atomic write failed: ${filePath} (${err.code || err.message})`);
  }
}

function runPush(fragmentPath, metaPath, dryRun) {
  const fragment = parseFragment(fragmentPath);
  const { bom, text } = decodeUtf8(readBytes(metaPath, 'meta'));
  const records = toRecords(text);
  validateParseable(records);

  const original = serialize(records);
  const { changes } = planPush(records, fragment);
  const updated = serialize(records);

  if (original === updated) {
    log(`[${TOOL}] no changes (${Object.keys(fragment).length} key(s) already in sync)`);
    return { changed: false, changes, dryRun };
  }

  if (dryRun) {
    log(`[${TOOL}] DRY-RUN (${changes.length} planned change(s), nothing written):`);
    for (const c of changes) {
      if (c.added) {
        log(`  + ${c.key}: (absent) -> "${asciiSafe(c.newValue, 80)}"`);
      } else {
        log(`  ~ ${c.key}: "${asciiSafe(c.oldValue, 80)}" -> "${asciiSafe(c.newValue, 80)}"`);
      }
    }
    return { changed: true, changes, dryRun };
  }

  writeAtomically(metaPath, encodeUtf8(updated, bom));
  log(`[${TOOL}] applied ${changes.length} change(s) to ${asciiSafe(metaPath, 200)}`);
  for (const c of changes) {
    if (c.added) {
      log(`  + ${c.key}: (absent) -> "${asciiSafe(c.newValue, 80)}"`);
    } else {
      log(`  ~ ${c.key}: "${asciiSafe(c.oldValue, 80)}" -> "${asciiSafe(c.newValue, 80)}"`);
    }
  }
  return { changed: true, changes, dryRun };
}

// ---------------------------------------------------------------------------
// Pull: meta.ini -> fragment.json (bootstrap repo-side truth).
// ---------------------------------------------------------------------------
function runPull(metaPath, outPath) {
  const { text } = decodeUtf8(readBytes(metaPath, 'meta'));
  const records = toRecords(text);
  validateParseable(records);

  const genIdx = findGeneral(records);
  const result = {};
  if (genIdx !== -1) {
    let end = records.length;
    for (let i = genIdx + 1; i < records.length; i += 1) {
      if (sectionName(records[i].text) !== null) {
        end = i;
        break;
      }
    }
    for (let i = genIdx + 1; i < end; i += 1) {
      const key = keyOf(records[i].text);
      if (key !== null && ALLOWED_KEYS.includes(key)) {
        result[key] = decodeIniValue(records[i].text);
      }
    }
  }

  try {
    fs.writeFileSync(outPath, JSON.stringify(result, null, 2) + '\n', 'utf8');
  } catch (err) {
    throw new SyncError(`cannot write out file: ${outPath} (${err.code || err.message})`);
  }
  log(`[${TOOL}] pulled ${Object.keys(result).length} key(s) -> ${asciiSafe(outPath, 200)}`);
  for (const key of ALLOWED_KEYS) {
    if (Object.prototype.hasOwnProperty.call(result, key)) {
      log(`  ${key}: "${asciiSafe(result[key], 80)}"`);
    }
  }
  return result;
}

// ---------------------------------------------------------------------------
// Self-test: synthetic tricky meta.ini, full push + read-back assertions.
// ---------------------------------------------------------------------------
function runSelfTest() {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'mo2-meta-sync-'));
  try {
    const metaPath = path.join(dir, 'meta.ini');
    const fragPath = path.join(dir, 'fragment.json');
    const outPath = path.join(dir, 'pulled.json');

    const L = (s) => s + '\r\n';
    const originalText =
      L('[General]') +
      L('gameName=SPT4.1') +
      L('version=0.0.1') +
      L('category="0"') +
      L('comments="old comment"') +
      L('notes="old note"') +
      L('validated=true') +
      L('color=@Variant(\\0\\0\\0\\x43\\0\\xff\\xff\\0\\0\\0\\0\\0\\0\\0\\0)') +
      L('tracked=0') +
      L('') +
      L('[installedFiles]') +
      L('1\\modid=0') +
      L('size=1');

    const noteValue = 'line one\nline two "quoted" and \\ backslash';
    const frag = {
      comments: 'new comment',
      notes: noteValue,
      version: '0.2.0',
      installationFile: 'new.zip',
    };

    fs.writeFileSync(metaPath, Buffer.from(originalText, 'utf8'));
    fs.writeFileSync(fragPath, JSON.stringify(frag), 'utf8');

    runPush(fragPath, metaPath, false);

    const afterBuf = fs.readFileSync(metaPath);
    const after = afterBuf.toString('utf8');

    // (b) expected exact output: only target keys change, new key appended.
    const expected =
      L('[General]') +
      L('gameName=SPT4.1') +
      L('version=0.2.0') +
      L('category="0"') +
      L('comments="new comment"') +
      L('notes="line one\\nline two \\"quoted\\" and \\\\ backslash"') +
      L('validated=true') +
      L('color=@Variant(\\0\\0\\0\\x43\\0\\xff\\xff\\0\\0\\0\\0\\0\\0\\0\\0)') +
      L('tracked=0') +
      L('installationFile=new.zip') +
      L('') +
      L('[installedFiles]') +
      L('1\\modid=0') +
      L('size=1');

    if (after !== expected) {
      return fail(
        'push bytes mismatch\n--- expected ---\n' + asciiSafe(expected, 2000) +
        '\n--- actual ---\n' + asciiSafe(after, 2000)
      );
    }

    // CRLF preserved, no BOM introduced.
    if (afterBuf.length >= 3 && afterBuf[0] === 0xef && afterBuf[1] === 0xbb && afterBuf[2] === 0xbf) {
      return fail('BOM was introduced');
    }
    if (after.split('\r\n').length - 1 !== 14) {
      return fail('CRLF count changed');
    }

    // (c) escaping round-trips: read back and compare.
    const { text: back } = decodeUtf8(afterBuf);
    const backRecords = toRecords(back);
    const backGen = findGeneral(backRecords);
    const backMap = {};
    {
      let end = backRecords.length;
      for (let i = backGen + 1; i < backRecords.length; i += 1) {
        if (sectionName(backRecords[i].text) !== null) {
          end = i;
          break;
        }
      }
      for (let i = backGen + 1; i < end; i += 1) {
        const k = keyOf(backRecords[i].text);
        if (k !== null && ALLOWED_KEYS.includes(k)) backMap[k] = decodeIniValue(backRecords[i].text);
      }
    }
    for (const key of ALLOWED_KEYS) {
      if (backMap[key] !== frag[key]) {
        return fail(`round-trip mismatch for "${key}": "${asciiSafe(backMap[key], 80)}" != "${asciiSafe(frag[key], 80)}"`);
      }
    }

    // (d) pull returns the same values.
    const pulled = runPull(metaPath, outPath);
    const pulledRaw = fs.readFileSync(outPath);
    if (pulledRaw.length >= 3 && pulledRaw[0] === 0xef && pulledRaw[1] === 0xbb && pulledRaw[2] === 0xbf) {
      return fail('pull wrote a BOM');
    }
    for (const key of ALLOWED_KEYS) {
      if (pulled[key] !== frag[key]) {
        return fail(`pull mismatch for "${key}": "${asciiSafe(pulled[key], 80)}" != "${asciiSafe(frag[key], 80)}"`);
      }
    }

    log('SELF-TEST PASS');
    return 0;
  } catch (err) {
    return fail(err && err.message ? err.message : String(err));
  } finally {
    try {
      fs.rmSync(dir, { recursive: true, force: true });
    } catch { /* best effort */ }
  }
}

function fail(reason) {
  log('SELF-TEST FAIL: ' + asciiSafe(reason, 2000));
  return 1;
}

// ---------------------------------------------------------------------------
// CLI.
// ---------------------------------------------------------------------------
function parseArgs(argv) {
  const args = argv.slice(2);
  const opts = { mode: null, fragment: null, meta: null, out: null, dryRun: false };
  for (let i = 0; i < args.length; i += 1) {
    const token = args[i];
    if (token === '--self-test') opts.mode = 'self-test';
    else if (token === '--pull') opts.mode = 'pull';
    else if (token === '--dry-run') opts.dryRun = true;
    else if (token === '--fragment') opts.fragment = args[++i];
    else if (token === '--meta') opts.meta = args[++i];
    else if (token === '--out') opts.out = args[++i];
    else throw new SyncError(`unknown argument: ${token}`);
  }
  return opts;
}

function main() {
  const opts = parseArgs(process.argv);

  if (opts.mode === 'self-test') {
    process.exitCode = runSelfTest();
    return;
  }

  if (opts.mode === 'pull') {
    if (!opts.meta || !opts.out) {
      throw new SyncError('pull mode requires --meta <path> --out <fragment.json>');
    }
    runPull(opts.meta, opts.out);
    return;
  }

  if (!opts.fragment || !opts.meta) {
    throw new SyncError('push mode requires --fragment <fragment.json> --meta <path/to/meta.ini> [--dry-run]');
  }
  runPush(opts.fragment, opts.meta, opts.dryRun);
}

try {
  main();
} catch (err) {
  if (err instanceof SyncError) {
    process.stderr.write(`[${TOOL}] ERROR: ${asciiSafe(err.message)}\n`);
  } else {
    process.stderr.write(`[${TOOL}] ERROR: ${asciiSafe(err && err.stack ? err.stack : String(err))}\n`);
  }
  process.exitCode = 1;
}
