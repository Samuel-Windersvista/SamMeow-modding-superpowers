// 检查器 golden 捕获器（C8 车道 A 步骤 0）
//
// 对 6 个目标跑指定版本的 check-mod-standard.ps1，输出存
// .scratch/c8-rules-registry/goldens/<phase>/<name>.txt，并写 _exits.json。
//
// 用法: node .scratch/c8-rules-registry/tools/capture-checker.mjs <phase> [checkerPath]
//   phase       = pre | post
//   checkerPath = 缺省 scripts/check-mod-standard.ps1（post 用）；
//                 pre 传 .scratch/c8-rules-registry/tools/check-mod-standard-pre.ps1
//
// 编码：PS 5.1 控制台缺省用 ANSI 代码页，CJK 会乱码。这里统一在子进程内先设
// [Console]::OutputEncoding = UTF8(no BOM)，再调用检查器，Node 侧按 utf8 解码，
// 写文件也按 utf8（无 BOM）。pre/post 用同一套捕获方式，diff 才有意义。
import { spawnSync } from "node:child_process";
import { mkdirSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";

const phase = process.argv[2] ?? "pre";
const checkerArg = process.argv[3] ?? "scripts/check-mod-standard.ps1";
const repoRoot = resolve(".");
const checker = resolve(repoRoot, checkerArg);
const outDir = resolve(repoRoot, `.scratch/c8-rules-registry/goldens/${phase}`);

/** 6 个目标：4 模板（默认 target 4.1.5）+ bridge（5.0.0, auto kind）+ radar（client） */
const targets = [
  { name: "server-mod", path: "templates/server-mod", args: ["-Kind", "server"] },
  { name: "client-mod", path: "templates/client-mod", args: ["-Kind", "client"] },
  { name: "paired-mod-Server", path: "templates/paired-mod/Server", args: ["-Kind", "server"] },
  { name: "paired-mod-Client", path: "templates/paired-mod/Client", args: ["-Kind", "client"] },
  {
    name: "tarkov-runtime-bridge",
    path: "tools/tarkov-runtime-bridge",
    args: ["-TargetSptVersion", "5.0.0"],
  },
  { name: "SPT5-AccurateCircularRadar", path: "mods/SPT5-AccurateCircularRadar", args: ["-Kind", "client"] },
];

mkdirSync(outDir, { recursive: true });

const summary = [];
const exits = {};
for (const target of targets) {
  const modPath = resolve(repoRoot, target.path);
  // 参数名（-Kind 等）必须保持裸 token，只有取值加引号；否则会被当成位置参数
  const argText = target.args.map((a) => (a.startsWith("-") ? a : `'${a}'`)).join(" ");
  const command =
    "[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false); " +
    `& '${checker}' -ModPath '${modPath}' ${argText}; exit $LASTEXITCODE`;

  const res = spawnSync(
    "powershell",
    ["-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", command],
    { encoding: "utf8", cwd: repoRoot, maxBuffer: 1 << 24 },
  );

  const text = (res.stdout ?? "") + (res.stderr ?? "");
  writeFileSync(resolve(outDir, `${target.name}.txt`), text, "utf8");
  exits[target.name] = res.status;

  const checksLine = text.split(/\r?\n/).find((l) => l.includes("checks: PASS=")) ?? "";
  summary.push({ name: target.name, exit: res.status, checks: checksLine.trim() });
}

writeFileSync(resolve(outDir, "_exits.json"), JSON.stringify(exits, null, 2) + "\n", "utf8");

for (const s of summary) {
  console.log(`${s.name}: exit=${s.exit}  ${s.checks}`);
}
