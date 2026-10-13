// gen-install-list.mjs —— 生成《永远的诺文斯克》安装目标清单（68 项）+ 对照表文档
import { readFileSync, writeFileSync, existsSync } from "node:fs";
import { join } from "node:path";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const VDIR = join(SCRATCH, "forge-versions");

const rows = readFileSync(join(SCRATCH, "final-selected.tsv"), "utf8").split(/\r?\n/).map((l) => l.split("\t"));
const byIdx = new Map();
for (const c of rows) if (c[0]) byIdx.set(+c[0], c);

// 批次映射
const BATCH = { 4: "B1", 5: "B1", 97: "B1", 98: "B1", 147: "B1",
  90: "B2", 93: "B2", 94: "B2", 95: "B2", 96: "B2",
  7: "B3", 8: "B3", 10: "B3", 11: "B3", 22: "B3", 23: "B3", 24: "B3", 25: "B3", 28: "B3", 29: "B3", 32: "B3", 34: "B3", 84: "B3",
  17: "B4", 18: "B4", 19: "B4", 20: "B4", 36: "B4", 37: "B4", 44: "B4", 46: "B4", 54: "B4", 56: "B4", 57: "B4", 58: "B4", 59: "B4", 62: "B4", 63: "B4", 64: "B4", 65: "B4", 78: "B4",
  112: "B5", 113: "B5", 114: "B5", 115: "B5", 116: "B5", 117: "B5", 118: "B5", 135: "B5", 136: "B5", 138: "B5", 139: "B5", 141: "B5", 144: "B5", 146: "B5",
  66: "B6", 67: "B6", 70: "B6", 73: "B6", 81: "B6", 82: "B6", 88: "B6", 119: "B6", 122: "B6", 126: "B6", 131: "B6",
  16: "B7", 48: "B7", 89: "B7", 120: "B7" };
const MERGE = new Set([84, 71, 140, 145, 128, 132]); // 与主条目同档合并的重复条目

function normName(name) {
  return name
    .replace(/(客户端文件|服务器文件|服务器端文件)$/, "")
    .replace(/\s*[-–]?\s*已AI优化\s*$/, "")
    .replace(/\s*[-–]?\s*AI优化版\s*$/, "")
    .replace(/\s*[-–]?\s*已AI增强版本\s*$/, "")
    .replace(/\s*[-–]?\s*AI优化版\s*$/, "")
    .trim();
}
const OVERRIDE_NAME = {
  10: "[7]面具修复-KnightMaskFix(4.1.3+)",
  84: "[7]面具修复-KnightMaskFix(4.1.3+)",
  90: "[6]友方AI系统-PITFireteam",
  122: "[3]武器整合-Epic's All in One",
  128: "[3]武器整合-Epic's All in One",
  141: "[2]新酒水-MoreEnergyDrinks(近似替代)",
  146: "[2]新物品-藏身处猫咪-Tarkin'sHideoutCat",
};

const targets = [];
const seen = new Set();
for (const [idx, batch] of Object.entries(BATCH).map(([k, v]) => [+k, v]).sort((a, b) => a[0] - b[0])) {
  if (MERGE.has(idx)) continue;
  const c = byIdx.get(idx);
  if (!c || !c[8] || c[8] === "UNRESOLVED") {
    // 钉死项（未在 tsv 中选中）：从 forge-versions 缓存取最新版链接
    if ([16, 48, 89, 120].includes(idx)) {
      const idMap = { 16: 1100, 48: 2543, 89: 2248, 120: 2882 };
      const id = idMap[idx];
      let arr = [];
      try { arr = JSON.parse(readFileSync(join(VDIR, `${id}.json`), "utf8"))?.data ?? []; } catch {}
      const sorted = [...arr].sort((a, b) => String(b.published_at ?? "").localeCompare(String(a.published_at ?? "")));
      const v = sorted[0];
      const key = `f${id}@${v?.version}`;
      if (seen.has(key)) continue;
      seen.add(key);
      targets.push({
        batch, idxs: [idx], overlayName: OVERRIDE_NAME[idx] ?? normName(c?.[1] ?? `idx-${idx}`),
        forgeId: id, forgeName: "?", version: v?.version, constraint: v?.spt_version_constraint,
        link: v?.link, size: v?.content_length, pinned: true, note: "钉死小版本(实验批)",
      });
    }
    continue;
  }
  const key = `${c[2]}@${c[4]}`;
  if (seen.has(key)) { const t = targets.find((x) => x.forgeId === +c[2] && x.version === c[4]); if (t) t.idxs.push(idx); continue; }
  seen.add(key);
  targets.push({
    batch, idxs: [idx], overlayName: OVERRIDE_NAME[idx] ?? normName(c[1]),
    forgeId: +c[2], forgeName: c[3], version: c[4], constraint: c[5],
    link: c[8], size: +c[7] || 0, pinned: false, note: c[9] ?? "",
  });
}
targets.sort((a, b) => a.batch.localeCompare(b.batch) || a.idxs[0] - b.idxs[0]);

// priority: B1 低 -> B7 高
const batchNum = { B1: 1, B2: 2, B3: 3, B4: 4, B5: 5, B6: 6, B7: 7 };
let cnt = {};
for (const t of targets) {
  cnt[t.batch] = (cnt[t.batch] ?? 0) + 1;
  t.priority = 100 + batchNum[t.batch] * 100 + cnt[t.batch];
}
writeFileSync(join(SCRATCH, "install-targets.json"), JSON.stringify(targets, null, 1), "utf8");

// 统计
const sum = targets.reduce((a, t) => a + (t.size || 0), 0);
console.log("targets:", targets.length, "batches:", JSON.stringify(cnt), "sizeGB:", (sum / 1073741824).toFixed(2));
for (const t of targets) console.log(`${t.batch} p${t.priority} #${t.idxs.join(",")} ${t.overlayName}  <- forge#${t.forgeId} ${t.forgeName}@${t.version} (${t.constraint})${t.pinned ? " [PINNED]" : ""}${t.note ? " {" + t.note + "}" : ""}`);
