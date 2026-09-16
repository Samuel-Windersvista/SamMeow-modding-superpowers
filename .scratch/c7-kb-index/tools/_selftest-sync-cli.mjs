// sync-index.mjs 对抗性复验（C7 修复轮）
//
// 在临时 KB 副本上验证退出码与副作用语义（不触碰仓库数据）：
//   1. 非法条目 + 有新增 -> --write exit 1 且新增已写入
//   2. 非法条目 + 无新增 -> --write exit 1 不写盘
//   3. 合法 + 有新增     -> --write exit 0 写盘
//   4. 坏 JSON           -> exit 2
//   5. entries 非数组    -> exit 2（禁止静默当空）
//   6. 未知参数          -> exit 2
//   7. --index 缺值      -> exit 2
//   8. --kb=<path> 行内形式可用
//   9. 非法条目报告带 path 前缀（! <path>: <error>）
import { spawnSync } from "node:child_process";
import { mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const ROOT = "D:/Temp/opencode/c7-fix-round";
const SYNC = "scripts/spt-kb/sync-index.mjs";
rmSync(ROOT, { recursive: true, force: true });

const results = [];
function check(name, condition, detail) {
  results.push({ name, ok: Boolean(condition), detail });
}

function mkKb(name, indexRaw, files = {}) {
  const kb = join(ROOT, name);
  mkdirSync(kb, { recursive: true });
  writeFileSync(join(kb, "index.json"), indexRaw, "utf8");
  for (const [rel, content] of Object.entries(files)) {
    const full = join(kb, rel);
    mkdirSync(join(full, ".."), { recursive: true });
    writeFileSync(full, content, "utf8");
  }
  return kb;
}

function runSync(kb, extraArgs = [], useInlineKb = false) {
  const args = useInlineKb ? [`--kb=${kb}`, ...extraArgs] : ["--kb", kb, ...extraArgs];
  const res = spawnSync(process.execPath, [SYNC, ...args], { encoding: "utf8" });
  return { status: res.status, stdout: res.stdout ?? "", stderr: res.stderr ?? "" };
}

const VALID_ENTRY = {
  path: "curated/ok.md",
  title: "OK",
  version: ["4.1"],
  domain: "server",
  topic: "recipe",
  source: "curated",
};
const INVALID_ENTRY = {
  path: "curated/legacy.md",
  title: "Legacy",
  version: "4.1", // string：v2 契约下非法
  domain: "server",
  topic: "migration",
  source: "curated",
};
const NEW_FILE = "---\nversion: [5.0]\ndomain: client\ntopic: api\nsource: wiki\n---\n# 新页面\n";

// ---- 1. 非法 + 有新增 -> exit 1 且新增已写入 --------------------------------
{
  const kb = mkKb(
    "case1",
    JSON.stringify({ generated: "2026-01-01", schema_version: 2, entries: [INVALID_ENTRY] }, null, 1),
    { "curated/legacy.md": "# Legacy\n", "wiki/new.md": NEW_FILE },
  );
  const r = runSync(kb, ["--write"]);
  const after = JSON.parse(readFileSync(join(kb, "index.json"), "utf8"));
  check("1a 非法+有新增 --write exit 1", r.status === 1, `status=${r.status}`);
  check(
    "1b 新增已写入（entries 1 -> 2）",
    after.entries.length === 2 && after.entries.some((e) => e.path === "wiki/new.md"),
    `entries=${after.entries.length}`,
  );
  check(
    "1c 消息注明已写入 + 待修复",
    r.stdout.includes("已写入 1 条新增") && r.stdout.includes("1 条非法条目待修复"),
    r.stdout.split("\n").filter((l) => l.includes("已写入")).join(" | "),
  );
  check(
    "1d 非法条目报告带 path 前缀",
    r.stdout.includes("! curated/legacy.md:"),
    r.stdout.split("\n").find((l) => l.startsWith("  ! ")) ?? "(无)",
  );
  check(
    "1e 非法时省略统计行",
    !r.stdout.includes("[sync-index] 分布："),
    r.stdout.split("\n").find((l) => l.includes("统计")) ?? "(无)",
  );
}

// ---- 2. 非法 + 无新增 -> exit 1 不写盘 -------------------------------------
{
  const raw = JSON.stringify(
    { generated: "2026-01-01", schema_version: 2, entries: [INVALID_ENTRY] },
    null,
    1,
  );
  const kb = mkKb("case2", raw, { "curated/legacy.md": "# Legacy\n" });
  const r = runSync(kb, ["--write"]);
  const after = readFileSync(join(kb, "index.json"), "utf8");
  check("2a 非法+无新增 --write exit 1", r.status === 1, `status=${r.status}`);
  check("2b 未写盘（字节不变）", after === raw, after === raw ? "" : "文件被改写");
  check(
    "2c 消息注明未写盘",
    r.stdout.includes("非法条目待修复：未写盘"),
    r.stdout.split("\n").filter((l) => l.includes("未写盘")).join(" | "),
  );
}

// ---- 3. 合法 + 有新增 -> exit 0 写盘 ---------------------------------------
{
  const kb = mkKb(
    "case3",
    JSON.stringify({ generated: "2026-01-01", schema_version: 2, entries: [VALID_ENTRY] }, null, 1),
    { "curated/ok.md": "# OK\n", "wiki/new.md": NEW_FILE },
  );
  const r = runSync(kb, ["--write"]);
  const after = JSON.parse(readFileSync(join(kb, "index.json"), "utf8"));
  check("3a 合法+有新增 --write exit 0", r.status === 0, `status=${r.status}`);
  check("3b 写盘（entries 2 + generated 刷新）", after.entries.length === 2 && after.generated !== "2026-01-01", `entries=${after.entries.length} generated=${after.generated}`);
  check("3c 有统计行", r.stdout.includes("[sync-index] 分布："), "");
}

// ---- 4. 坏 JSON -> exit 2 ---------------------------------------------------
{
  const kb = mkKb("case4", "{ not json", {});
  const r = runSync(kb);
  check("4a 坏 JSON exit 2", r.status === 2, `status=${r.status}`);
  check("4b 错误信息明确", r.stderr.includes("不是合法 JSON"), r.stderr.trim().split("\n")[0]);
}

// ---- 5. entries 非数组 -> exit 2 -------------------------------------------
{
  const kb = mkKb("case5", JSON.stringify({ schema_version: 2, entries: { nope: true } }), {
    "wiki/new.md": NEW_FILE,
  });
  const r = runSync(kb, ["--write"]);
  check("5a entries 非数组 exit 2", r.status === 2, `status=${r.status}`);
  check("5b 明确拒绝按空索引处理", r.stderr.includes("entries 不是数组"), r.stderr.trim().split("\n")[0]);
}

// ---- 6. 未知参数 -> exit 2 -------------------------------------------------
{
  const kb = mkKb("case6", JSON.stringify({ schema_version: 2, entries: [] }), {});
  const r = runSync(kb, ["--bogus"]);
  check("6a 未知参数 exit 2", r.status === 2, `status=${r.status}`);
  check("6b 提示未知参数 + usage", r.stderr.includes("未知参数") && r.stderr.includes("用法:"), "");
}

// ---- 7. --index 缺值 -> exit 2 ---------------------------------------------
{
  const kb = mkKb("case7", JSON.stringify({ schema_version: 2, entries: [] }), {});
  const r = runSync(kb, ["--index"]);
  check("7a --index 缺值 exit 2", r.status === 2, `status=${r.status}`);
  check("7b 提示缺少取值", r.stderr.includes("参数缺少取值"), r.stderr.trim().split("\n")[0]);
}

// ---- 8. --kb=<path> 行内形式 -----------------------------------------------
{
  const kb = mkKb(
    "case8",
    JSON.stringify({ generated: "2026-01-01", schema_version: 2, entries: [VALID_ENTRY] }, null, 1),
    { "curated/ok.md": "# OK\n" },
  );
  const r = runSync(kb, [], true);
  check("8a --kb=<path> 行内形式 exit 0", r.status === 0, `status=${r.status}`);
  check("8b 命中同一 KB 根", r.stdout.includes(kb), "");
}

// ---- 9. validate-index CLI（N5） -------------------------------------------
{
  const validate = "scripts/spt-kb/validate-index.mjs";
  const bad = spawnSync(process.execPath, [validate, "--index"], { encoding: "utf8" });
  check("9a validate --index 缺值 exit 2", bad.status === 2, `status=${bad.status}`);
  const unknown = spawnSync(process.execPath, [validate, "--nope"], { encoding: "utf8" });
  check("9b validate 未知参数 exit 2", unknown.status === 2, `status=${unknown.status}`);
  const help = spawnSync(process.execPath, [validate, "--help"], { encoding: "utf8" });
  check("9c validate --help exit 0", help.status === 0 && help.stdout.includes("用法:"), `status=${help.status}`);
}

// ---- 汇总 -------------------------------------------------------------------
let failed = 0;
for (const r of results) {
  if (!r.ok) failed += 1;
  console.log(`${r.ok ? "PASS" : "FAIL"}  ${r.name}${r.detail ? "  [" + r.detail + "]" : ""}`);
}
console.log(`\n合计 ${results.length} 项，失败 ${failed} 项`);
process.exit(failed === 0 ? 0 : 1);
