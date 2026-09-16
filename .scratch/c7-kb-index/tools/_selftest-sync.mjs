// sync-index 生成器端到端自测（临时 KB，不触碰仓库数据）
//
// 验证：frontmatter title / H1 回退 / 文件名回退、source 前缀推导、version 标量归一、
// 缺省值、已有条目绝不改写、--write 追加 + generated 更新、格式保持、重跑零新增。
import { execFileSync } from "node:child_process";
import { mkdirSync, rmSync, writeFileSync, readFileSync, existsSync } from "node:fs";
import { join } from "node:path";

const KB = "D:/Temp/opencode/c7-sync-selftest/kb";
const INDEX = join(KB, "index.json");
const SYNC = "scripts/spt-kb/sync-index.mjs";

rmSync("D:/Temp/opencode/c7-sync-selftest", { recursive: true, force: true });

const write = (rel, content) => {
  const full = join(KB, rel);
  mkdirSync(join(full, ".."), { recursive: true });
  writeFileSync(full, content, "utf8");
};

// 既有条目（title/keywords/summary 为手工资产，必须保持原样）
const existing = {
  path: "curated/existing.md",
  title: "手工标题（不得改写）",
  version: ["4.1"],
  domain: "server",
  topic: "recipe",
  source: "curated",
  keywords: ["hand", "asset"],
  summary: "手工摘要（不得改写）",
};
mkdirSync(KB, { recursive: true });
writeFileSync(
  INDEX,
  JSON.stringify({ generated: "2026-01-01", schema_version: 2, entries: [existing] }, null, 1),
  "utf8",
);
write("curated/existing.md", "---\nversion: [4.1]\ndomain: server\ntopic: recipe\nsource: curated\n---\n# 源文件标题（与条目不同）\n");

// 新增 1：frontmatter.title 优先
write("wiki/new-with-title.md", "---\ntitle: FM 标题\nversion: [4.1]\ndomain: client\ntopic: testing\nsource: wiki\n---\n# 另一个 H1\n");
// 新增 2：无 frontmatter title -> 首个 H1；缺省 version/domain/topic
write("wiki/new-h1-only.md", "# 仅 H1 的标题\n\n正文。\n");
// 新增 3：无 frontmatter、无 H1 -> 文件名；source 按前缀 wiki-tushonka -> wiki
write("wiki-tushonka/new-filename-only.md", "没有标题也没有 frontmatter。\n");
// 新增 4：version 标量 -> 归一为数组
write("curated/new-version-scalar.md", "---\nversion: 5.0\ndomain: both\ntopic: api\nsource: curated\n---\n# 标量版本\n");

const run = (args) => {
  const out = execFileSync(process.execPath, [SYNC, "--kb", KB, "--index", INDEX, ...args], {
    encoding: "utf8",
  });
  return out;
};

console.log("=== dry-run ===");
const dry = run([]);
for (const line of dry.split(/\r?\n/)) {
  if (/新增|孤儿|非法|dry-run|分布/.test(line) || line.trim().startsWith("+") || line.trim().startsWith("-")) {
    console.log(line);
  }
}

console.log("\n=== --write ===");
const written = run(["--write"]);
console.log(written.split(/\r?\n/).filter((l) => /已写盘|未写盘/.test(l)).join("\n"));

const after = JSON.parse(readFileSync(INDEX, "utf8"));
console.log("\n=== 写后状态 ===");
console.log("generated=" + after.generated + " entries=" + after.entries.length);
const kept = after.entries.find((e) => e.path === "curated/existing.md");
console.log(
  "既有条目保持原样: " +
    (kept.title === existing.title &&
      JSON.stringify(kept.keywords) === JSON.stringify(existing.keywords) &&
      kept.summary === existing.summary),
);
for (const e of after.entries.slice(1)) {
  console.log(
    `  + ${e.path}  title="${e.title}" version=${JSON.stringify(e.version)} domain=${e.domain} topic=${e.topic} source=${e.source}`,
  );
}

console.log("\n=== 重跑 dry（应零新增） ===");
const rerun = run([]);
console.log(rerun.split(/\r?\n/).filter((l) => /新增|孤儿|非法/.test(l)).join("\n"));

console.log("\n=== 格式保持 ===");
const text = readFileSync(INDEX, "utf8");
console.log("CRLF=" + (text.match(/\r\n/g) || []).length + " LF-only=" + ((text.match(/(?<!\r)\n/g) || []).length));
console.log("BOM=" + (text.charCodeAt(0) === 0xfeff) + " trailingNewline=" + /\r?\n$/.test(text));
console.log("indent sample=" + JSON.stringify(text.split("\r\n")[1]));
console.log("dist 存在=" + existsSync("tools/spt-mcp/dist/kb/index.js"));
