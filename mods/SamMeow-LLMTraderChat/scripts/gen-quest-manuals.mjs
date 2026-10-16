#!/usr/bin/env node
// gen-quest-manuals.mjs
// ---------------------------------------------------------------------------
// 生成每位商人的「任务手册」静态知识文档（knowledge/traders/<id>/05-quests.md）。
//
// 数据来源（均以本机默认路径为准，可经 CLI 覆盖）：
//   原版任务表 : <SptData>/database/templates/quests.json
//   原版中文   : <SptData>/database/locales/global/ch.json
//   地图表     : <SptData>/database/locations/*/base.json
//   mod 任务   : 见下方 SOURCES（Scorpion / Artem / Painter / Ref / WTT-Armory /
//                Epic's All in One / Vagabond，逐一可选；缺失即跳过并记录）
//
// 关键规则（工单 17）：
//   - 以任务表为准（ground truth），绝不反向从 locale 生成（否则产生幽灵条目，
//     例如孤儿键 664ca9f577af670dad0ad339 不得出现）。
//   - 任务名/描述：locale[quest.name] / locale[quest.description]（字段值即 locale key，
//     原版为 "<id> name"/"<id> description"，Painter 等 mod 为 "painter_1 name"）。
//   - 目标：conditions.AvailableForFinish[].id（或 ._props.id）作为裸 key 查 locale；
//     无法解析的目标跳过并计数。
//   - 幂等：每次运行先删除目标目录内旧的 05-quests*.md 再全量重写。
//   - UTF-8 无 BOM，LF 行尾（与既有 knowledge/traders/*.md 一致）。
//   - 控制台输出仅 ASCII（计数与相对路径），避免 GBK 控制台乱码。
//
// 用法：
//   node scripts/gen-quest-manuals.mjs
//   node scripts/gen-quest-manuals.mjs --spt-data "D:\SPT\SPT_Runtime\SPT_Data" --mods-root "D:\MO2\mods"
//   node scripts/gen-quest-manuals.mjs --no-mods        # 只生成原版
//   node scripts/gen-quest-manuals.mjs --split-bytes 204800
// ---------------------------------------------------------------------------

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// 本机默认值（SPT 41x + 《永远的诺文斯克》MO2 包）。
const DEFAULT_SPT_DATA = 'E:\\Game\\EFT_Offline\\SPT_41x\\SPT_Runtime\\SPT_Data';
const DEFAULT_MODS_ROOT = 'E:\\Game\\EFT_Offline\\Norvinsk_Forever\\mods';
const DEFAULT_OUT = path.resolve(__dirname, '..', 'knowledge', 'traders');

// 19 位已接入商人（id -> 英文标签，仅用于控制台报告；中文名见 SharedConstants.cs 注释）。
const KNOWN_TRADERS = new Map([
  ['54cb50c76803fa8b248b4571', 'Prapor'],
  ['54cb57776803fa99248b456e', 'Therapist'],
  ['579dc571d53a0658a154fbec', 'Fence'],
  ['58330581ace78e27b8b10cee', 'Skier'],
  ['5935c25fb3acc3127c3d8cd9', 'Peacekeeper'],
  ['5a7c2eca46aef81a7ca2145d', 'Mechanic'],
  ['5ac3b934156ae10c4430e83c', 'Ragman'],
  ['5c0647fdd443bc2504c2d371', 'Jaeger'],
  ['638f541a29ffd1183d187f57', 'Lightkeeper'],
  ['656f0f98d80a697f855d34b1', 'BTRDriver'],
  ['6617beeaa9cfa777ca915b7c', 'Ref'],
  ['6864e812f9fe664cb8b8e152', 'Storyteller'],
  ['668aaff35fd574b6dcc4a686', 'Painter'],
  ['66bf757f27d0b097db0acea5', 'Artem'],
  ['6688d464bc40c867f60e7d7e', 'Scorpion'],
  ['1337bb0dd843363fcd1be869', 'Croupier'],
  ['67419e9d0d4541ce671543bb', 'HideoutHarry'],
  ['bd3a8b28356d9c6509966546', 'Badger'],
  ['67d3a28a3d6f4f7dbd09ed13', 'Courier'],
]);

// mod 任务源（相对 <mods-root>）。每个源可选；缺文件即跳过。
const SOURCES = {
  scorpion: {
    label: 'Scorpion',
    quests: ['[4]新商人-毒蝎-acidphantasm-scorpion', 'SPT_Runtime', 'user', 'mods', 'acidphantasm-scorpion', 'data', 'quests', 'Scorpion_quests.json'],
    locales: [['[4]新商人-毒蝎-acidphantasm-scorpion', 'SPT_Runtime', 'user', 'mods', 'acidphantasm-scorpion', 'data', 'locales', 'zh-CN', 'ScorpionLocale.json']],
  },
  artem: {
    label: 'Artem',
    quests: ['[4]新商人-Artem', 'SPT_Runtime', 'user', 'mods', 'WTT-Artem', 'db', 'CustomQuests', '66bf757f27d0b097db0acea5', 'Quests', 'ArtemQuests.json'],
    locales: [['[4]新商人-Artem', 'SPT_Runtime', 'user', 'mods', 'WTT-Artem', 'db', 'CustomQuests', '66bf757f27d0b097db0acea5', 'Locales', 'artemenglish.json']],
  },
  painter: {
    label: 'Painter',
    quests: ['[4]新商人-油漆匠-Painter', 'SPT_Runtime', 'user', 'mods', 'Painter-4.0', 'db', 'CustomQuests', '668aaff35fd574b6dcc4a686', 'Quests', 'painter.json'],
    locales: [['[4]新商人-油漆匠-Painter', 'SPT_Runtime', 'user', 'mods', 'Painter-4.0', 'db', 'CustomQuests', '668aaff35fd574b6dcc4a686', 'Locales', 'ch.json']],
  },
  // Ref（sptFriendlyQuests）：db/quests.json 无 traderId，且其 6 个 id 均已存在于原版任务表
  // （To Great Heights!/Against the Conscience [PVE ZONE]），实为对既有 Ref 任务的
  // AvailableForFinish 覆写补丁，并非新增任务。按补丁处理（覆写目标，不产生重复条目）。
  ref: {
    label: 'Ref(sptFriendlyQuests patches)',
    patches: ['[4]新商人-竞技场商人-ref-sptfriendly-quests', 'SPT_Runtime', 'user', 'mods', 'acidphantasm-reffriendlyquests', 'db', 'quests.json'],
  },
  wttArmory: {
    label: 'WTT-Armory',
    questsDirs: ['[3]新武器-WTT武器库-WTT - Armory客户端', 'SPT_Runtime', 'user', 'mods', 'WTT-Armory', 'db', 'CustomQuests'],
    locales: [
      ['[3]新武器-WTT武器库-WTT - Armory客户端', 'SPT_Runtime', 'user', 'mods', 'WTT-Armory', 'db', 'CustomLocales', 'ch.json'],
    ],
    localesDirs: ['[3]新武器-WTT武器库-WTT - Armory客户端', 'SPT_Runtime', 'user', 'mods', 'WTT-Armory', 'db', 'CustomQuests'],
  },
  epicAio: {
    label: "Epic's All in One",
    quests: ['[3]武器整合-Epic\'s All in One', 'SPT_Runtime', 'user', 'mods', "EpicRangeTime-Epic's All In One", 'db', 'CustomQuests', 'Badger', 'Quests', 'BadgerQuests.json'],
    locales: [['[3]武器整合-Epic\'s All in One', 'SPT_Runtime', 'user', 'mods', "EpicRangeTime-Epic's All In One", 'db', 'CustomQuests', 'Badger', 'Locales', 'ch.json']],
  },
  // Vagabond：config/quests/*.json 为 JSONC（含注释），且任务文本内嵌于 locales.en；
  // newQuest.traderId 明确存在（无需猜测），文件名与商人一一对应。
  vagabond: {
    label: 'Vagabond',
    questsGlobDir: ['[5]开放世界大修-Vagabond', 'SPT_Runtime', 'user', 'mods', 'Vagabond', 'config', 'quests'],
  },
};

// ------------------------------- CLI 解析 ---------------------------------

function parseArgs(argv) {
  const opts = {
    sptData: DEFAULT_SPT_DATA,
    modsRoot: DEFAULT_MODS_ROOT,
    out: DEFAULT_OUT,
    noMods: false,
    splitBytes: 200 * 1024,
  };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    const next = () => argv[++i];
    if (a === '--spt-data') opts.sptData = next();
    else if (a === '--mods-root') opts.modsRoot = next();
    else if (a === '--out') opts.out = path.resolve(next());
    else if (a === '--no-mods') opts.noMods = true;
    else if (a === '--split-bytes') opts.splitBytes = Number(next());
    else if (a === '--help' || a === '-h') { printHelp(); process.exit(0); }
    else throw new Error(`unknown argument: ${a}`);
  }
  return opts;
}

function printHelp() {
  console.log(
    [
      'Usage: node scripts/gen-quest-manuals.mjs [options]',
      '  --spt-data <dir>      SPT_Data 根 (default: ' + DEFAULT_SPT_DATA + ')',
      '  --mods-root <dir>     MO2 mods 根 (default: ' + DEFAULT_MODS_ROOT + ')',
      '  --out <dir>           输出 traders 目录 (default: mods/SamMeow-LLMTraderChat/knowledge/traders)',
      '  --no-mods             仅生成原版',
      '  --split-bytes <n>     单文件字节上限触发拆分 (default: 204800)',
    ].join('\n'),
  );
}

// ------------------------------- 工具函数 ---------------------------------

function readJson(file) {
  let raw = fs.readFileSync(file, 'utf8');
  if (raw.charCodeAt(0) === 0xfeff) raw = raw.slice(1); // 去 BOM
  return JSON.parse(raw);
}

// JSONC（带 // 与 /* */ 注释）宽松解析：仅用于 Vagabond config。
function readJsonc(file) {
  let raw = fs.readFileSync(file, 'utf8').replace(/^\uFEFF/, '');
  // 粗略剥离注释（本目录内字符串值不含 "//" 序列，足够安全）。
  const stripped = raw
    .replace(/\/\*[\s\S]*?\*\//g, '')
    .replace(/(^|[^:"])\/\/[^\n\r]*/g, '$1');
  return JSON.parse(stripped);
}

function listFilesInDirs(baseDirs, recursePredicate) {
  // 返回 baseDirs 下（可含一层子目录）满足谓词的文件绝对路径列表。
  const out = [];
  const walk = (dir) => {
    if (!fs.existsSync(dir)) return;
    for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
      const p = path.join(dir, e.name);
      if (e.isDirectory()) walk(p);
      else if (recursePredicate(p)) out.push(p);
    }
  };
  for (const d of baseDirs) walk(d);
  return out;
}

function mergeLocale(dst, src) {
  if (!src || typeof src !== 'object') return;
  for (const [k, v] of Object.entries(src)) dst[k] = v;
}

// 仅补齐缺失 key 的合并（供英文回退：中文优先）。
function mergeLocaleFallback(dst, src) {
  if (!src || typeof src !== 'object') return;
  for (const [k, v] of Object.entries(src)) if (!(k in dst)) dst[k] = v;
}

function conditionKey(c) {
  if (c == null || typeof c !== 'object') return null;
  if (c._props && typeof c._props === 'object' && typeof c._props.id === 'string') return c._props.id;
  if (typeof c.id === 'string') return c.id;
  return null;
}

function truncate(text, n) {
  const arr = Array.from(String(text).replace(/\s+/g, ' ').trim());
  return arr.length <= n ? arr.join('') : arr.slice(0, n).join('') + '…';
}

const SIDE_LABEL = { Pmc: 'PMC 专属', Bear: 'BEAR 专属', Usec: 'USEC 专属', Savage: 'SCAV', SavagePlayer: 'SCAV' };

// ------------------------------- 主流程 -----------------------------------

function main() {
  const opts = parseArgs(process.argv.slice(2));
  const stats = [];   // 逐源统计
  const quests = new Map();   // qid -> quest 对象（合并后的任务表，ground truth）
  const locale = {};          // 合并后的本地化表（中文优先，英文回退仅补缺）
  const englishFallback = {}; // 英文回退池

  // 1. 原版任务表 + 中文 locale
  const vanQuestsPath = path.join(opts.sptData, 'database', 'templates', 'quests.json');
  const vanChPath = path.join(opts.sptData, 'database', 'locales', 'global', 'ch.json');
  const vanQuests = readJson(vanQuestsPath);
  const vanCh = readJson(vanChPath);
  mergeLocale(locale, vanCh);
  let vanillaCount = 0;
  for (const [qid, q] of Object.entries(vanQuests)) { quests.set(qid, q); vanillaCount++; }
  stats.push({ source: 'vanilla', kind: 'quests', quests: vanillaCount, applied: vanillaCount, unresolvedName: 0, unresolvedObjectives: 0 });

  // 2. 地图名映射（id -> 中文名；也支持目录 key 别名）
  const locById = new Map();
  const locByKey = new Map();
  const locDir = path.join(opts.sptData, 'database', 'locations');
  if (fs.existsSync(locDir)) {
    for (const d of fs.readdirSync(locDir, { withFileTypes: true })) {
      if (!d.isDirectory()) continue;
      const bp = path.join(locDir, d.name, 'base.json');
      if (!fs.existsSync(bp)) continue;
      try {
        const b = readJson(bp);
        const name = locale[b._Id + ' Name'];
        if (name) { locById.set(b._Id, name); locByKey.set(d.name.toLowerCase(), name); }
      } catch { /* 忽略个别坏文件 */ }
    }
  }

  // 3. 读取各 mod 源（每源独立 try；缺文件即跳过并记录）
  const resolve = (parts) => path.join(opts.modsRoot, ...parts);

  const modSources = opts.noMods ? {} : SOURCES;

  // 3a. 单文件任务源（Scorpion / Artem / Painter / WTT / Epic）
  for (const key of ['scorpion', 'artem', 'painter', 'wttArmory', 'epicAio']) {
    const src = modSources[key];
    if (!src) continue;
    const questFiles = [];
    if (src.quests) questFiles.push(resolve(src.quests));
    if (src.questsDirs) {
      const base = resolve(src.questsDirs);
      questFiles.push(...listFilesInDirs([base], (p) => /\.json$/i.test(p) && /[\\/]Quests[\\/]/i.test(p)));
    }
    // locale 文件
    const locFiles = [];
    for (const l of src.locales || []) locFiles.push(resolve(l));
    if (src.localesDirs) {
      const base = resolve(src.localesDirs);
      locFiles.push(...listFilesInDirs([base], (p) => /\.json$/i.test(p) && /[\\/]Locales[\\/]/i.test(p)));
    }

    let added = 0;
    let presentQuestFiles = 0;
    for (const f of questFiles) {
      if (!fs.existsSync(f)) continue;
      presentQuestFiles++;
      const j = readJson(f);
      for (const q of Object.values(j)) {
        if (!q || typeof q !== 'object' || typeof q._id !== 'string') continue;
        quests.set(q._id, q);
        added++;
      }
    }
    // 合并 locale：中文类直并（覆盖），英文类进回退池（最后仅补缺 key）。
    let localeMerged = 0;
    for (const f of locFiles) {
      if (!fs.existsSync(f)) continue;
      const j = readJson(f);
      if (/en\.json$/i.test(f) || /english/i.test(f)) { mergeLocaleFallback(englishFallback, j); }
      else { mergeLocale(locale, j); localeMerged += Object.keys(j).length; }
    }
    stats.push({
      source: src.label, kind: 'quest-source',
      quests: added, applied: added, files: presentQuestFiles,
      localeKeys: localeMerged, missing: presentQuestFiles === 0,
    });
  }

  // 3b. Ref 补丁（覆写既有任务的 AvailableForFinish）
  if (modSources.ref) {
    const f = resolve(modSources.ref.patches);
    let patched = 0, orphan = 0;
    if (fs.existsSync(f)) {
      const j = readJson(f);
      for (const [qid, patch] of Object.entries(j)) {
        const target = quests.get(qid);
        if (!target) { orphan++; continue; }
        if (patch && Array.isArray(patch.AvailableForFinish)) {
          target.conditions = target.conditions || {};
          target.conditions.AvailableForFinish = patch.AvailableForFinish;
          patched++;
        }
      }
    }
    stats.push({ source: modSources.ref.label, kind: 'patch', quests: 0, applied: patched, orphan });
  }

  // 3c. Vagabond（JSONC + 内嵌 en locale + newQuest.traderId）
  if (modSources.vagabond) {
    const base = resolve(modSources.vagabond.questsGlobDir);
    let added = 0;
    if (fs.existsSync(base)) {
      for (const f of fs.readdirSync(base)) {
        if (!/\.json$/i.test(f)) continue;
        let j;
        try { j = readJsonc(path.join(base, f)); } catch (e) { stats.push({ source: 'Vagabond:' + f, kind: 'parse-error', quests: 0, applied: 0, error: e.message }); continue; }
        const nq = j && j.newQuest;
        if (nq && typeof nq._id === 'string') {
          quests.set(nq._id, nq);
          added++;
          if (j.locales && typeof j.locales === 'object') {
            for (const loc of Object.values(j.locales)) {
              if (loc && typeof loc === 'object') mergeLocaleFallback(englishFallback, loc);
            }
          }
        }
      }
    }
    stats.push({ source: modSources.vagabond.label, kind: 'quest-source', quests: added, applied: added });
  }

  // 3d. 处理假设记录（工单 17 要求「记录你假设了什么」）
  const notes = [
    'Ref: sptFriendlyQuests db/quests.json has no traderId and its 6 ids already exist in the vanilla table',
    '     (To Great Heights! 1-5 + Against the Conscience 2, traderId=Ref) -> treated as AvailableForFinish',
    '     overrides of existing quests, not as net-new quests (0 added).',
    'Vagabond: each config file carries newQuest.traderId -> attributed directly (no per-ticket-table guessing).',
    'Text language: Artem (artemenglish.json) and Vagabond (embedded locales.en) are English-only;',
    '     WTT-Armory quest NAMES fall back to English (QuestName/en.json) while objectives resolve from CustomLocales/ch.json.',
  ];

  // 4. 英文回退仅补缺失 key（中文优先）
  mergeLocaleFallback(locale, englishFallback);

  // 5. 按 traderId 分组
  const byTrader = new Map();
  let droppedNoTrader = 0;
  const unknownTraders = new Set();
  for (const [qid, q] of quests) {
    const tid = q.traderId;
    if (!tid) { droppedNoTrader++; continue; }
    if (!KNOWN_TRADERS.has(tid)) unknownTraders.add(tid);
    if (!byTrader.has(tid)) byTrader.set(tid, []);
    byTrader.get(tid).push({ qid, q });
  }

  // 6. 生成文档（先清旧档，再全量写；按字节上限拆分）
  const report = [];
  let filesWritten = 0;
  const orderedTraders = [...byTrader.keys()].sort((a, b) => (KNOWN_TRADERS.get(a) || a).localeCompare(KNOWN_TRADERS.get(b) || b));

  for (const tid of orderedTraders) {
    const list = byTrader.get(tid);
    list.sort((a, b) => (a.qid < b.qid ? -1 : a.qid > b.qid ? 1 : 0));

    const dir = path.join(opts.out, tid);
    const sections = [];
    let unresolvedObjectives = 0;
    let unresolvedName = 0;

    for (const { q } of list) {
      const name = locale[q.name] || q.QuestName || null;
      if (!name) unresolvedName++;
      const heading = name || q._id;

      const sideLabel = SIDE_LABEL[q.side] || q.side || '未知';
      const typeLabel = q.type || '未知';
      const locName = resolveLocation(q.location, locale, locById, locByKey);

      const objectives = [];
      const aff = q.conditions && q.conditions.AvailableForFinish;
      if (Array.isArray(aff)) {
        for (const c of aff) {
          const k = conditionKey(c);
          if (!k) { if (c && typeof c === 'object') unresolvedObjectives++; continue; }
          const text = locale[k];
          if (text === undefined || text === null || text === '') { unresolvedObjectives++; continue; }
          objectives.push(String(text));
        }
      }

      const descRaw = locale[q.description];
      const desc = descRaw ? truncate(descRaw, 200) : null;

      const lines = [`## ${heading}`];
      lines.push(`- 类型：${sideLabel}（${typeLabel}）`);
      if (locName) lines.push(`- 地点：${locName}`);
      if (objectives.length) {
        lines.push('- 目标：');
        for (const o of objectives) lines.push(`  - ${o}`);
      } else {
        lines.push('- 目标：（无可用目标文本）');
      }
      if (desc) lines.push(`- 委托人原话（摘要）：${desc}`);
      sections.push(lines.join('\n'));
    }

    if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
    // 幂等：清理旧的 05-quests*.md
    for (const f of fs.readdirSync(dir)) {
      if (/^05-quests(-\d+)?\.md$/.test(f)) fs.rmSync(path.join(dir, f), { force: true });
    }

    const header = '# 委托任务\n\n来源：任务数据自动生成（scripts/gen-quest-manuals.mjs），请勿手动编辑。\n';
    const chunks = chunkSections(header, sections, opts.splitBytes);
    chunks.forEach((chunk, i) => {
      const fname = chunks.length === 1 ? '05-quests.md' : `05-quests-${i + 1}.md`;
      fs.writeFileSync(path.join(dir, fname), chunk, { encoding: 'utf8' });
      filesWritten++;
    });

    report.push({
      traderId: tid, label: KNOWN_TRADERS.get(tid) || tid, quests: list.length,
      unresolvedName, unresolvedObjectives, files: chunks.length,
    });
  }

  // 7. 控制台报告（仅 ASCII）
  console.log('=== gen-quest-manuals: source stats ===');
  for (const s of stats) console.log('  ' + JSON.stringify(s));
  console.log('=== gen-quest-manuals: assumptions/notes ===');
  for (const n of notes) console.log('  ' + n);
  console.log('=== gen-quest-manuals: per-trader stats ===');
  let totalQuests = 0, totalUnresolvedObj = 0, totalUnresolvedName = 0;
  for (const r of report) {
    totalQuests += r.quests; totalUnresolvedObj += r.unresolvedObjectives; totalUnresolvedName += r.unresolvedName;
    console.log(`  ${r.traderId} ${r.label} quests=${r.quests} files=${r.files} unresolvedName=${r.unresolvedName} unresolvedObjectives=${r.unresolvedObjectives}`);
  }
  console.log('=== gen-quest-manuals: totals ===');
  console.log(`  traders=${report.length} quests=${totalQuests} files=${filesWritten} unresolvedName=${totalUnresolvedName} unresolvedObjectives=${totalUnresolvedObj} droppedNoTrader=${droppedNoTrader}`);
  if (unknownTraders.size) console.log('  unknownTraders=' + [...unknownTraders].join(','));
}

function resolveLocation(loc, locale, locById, locByKey) {
  if (!loc || typeof loc !== 'string') return null;
  if (loc.toLowerCase() === 'any') return null;
  if (locale[loc + ' Name']) return locale[loc + ' Name'];
  if (locById.has(loc)) return locById.get(loc);
  if (locByKey.has(loc.toLowerCase())) return locByKey.get(loc.toLowerCase());
  return null;
}

function chunkSections(header, sections, maxBytes) {
  const chunks = [];
  let cur = header;
  let curHas = false;
  for (const sec of sections) {
    const add = (curHas ? '\n' : '') + sec + '\n';
    if (curHas && Buffer.byteLength(cur + add, 'utf8') > maxBytes) {
      chunks.push(cur);
      cur = header;
      curHas = false;
    }
    cur += (curHas ? '\n' : '') + sec + '\n';
    curHas = true;
  }
  chunks.push(cur);
  return chunks;
}

main();
