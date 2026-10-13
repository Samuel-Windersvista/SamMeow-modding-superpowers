// repo-scan.mjs —— 清点 E:\云文件\GitHub 下的 mod 分支仓库（SamMeow/Moew/LIN 等），提取目标 SPT 版本
import { readFileSync, readdirSync, existsSync, statSync } from "node:fs";
import { join, basename } from "node:path";

const ROOT = "E:\\云文件\\GitHub";
const PAT = /^(SamMeow-|SamMeow_|Moew-|LIN-|JoshMate-|WhiteBoxFix|True-GL|SPT-Modpack|SamMeow--)/i;
const repos = readdirSync(ROOT, { withFileTypes: true }).filter((e) => e.isDirectory() && PAT.test(e.name)).map((e) => e.name);
console.log("repos:", repos.length);
for (const repo of repos) {
  const dir = join(ROOT, repo);
  const info = { repo, packages: [], csproj: [], hasBin: false, readme: null };
  const walk = (d, depth) => {
    if (depth > 3) return;
    let ents = [];
    try { ents = readdirSync(d, { withFileTypes: true }); } catch { return; }
    for (const e of ents) {
      const p = join(d, e.name);
      if (e.isDirectory()) {
        if (/^(\.git|node_modules|\.vs|obj)$/i.test(e.name)) continue;
        if (/^bin$/i.test(e.name)) info.hasBin = true;
        walk(p, depth + 1);
      } else if (e.name === "package.json" && info.packages.length < 3) {
        try {
          const j = JSON.parse(readFileSync(p, "utf8").replace(/^\uFEFF/, ""));
          info.packages.push({ path: p.replace(ROOT + "\\", ""), name: j.name, version: j.version, sptVersion: j.sptVersion ?? null });
        } catch {}
      } else if (e.name.endsWith(".csproj") && info.csproj.length < 2) {
        try {
          const t = readFileSync(p, "utf8");
          const tf = t.match(/<TargetFramework>([^<]+)</)?.[1] ?? "?";
          const ver = t.match(/<Version>([^<]+)</)?.[1] ?? "";
          info.csproj.push({ path: p.replace(ROOT + "\\", ""), tf, ver });
        } catch {}
      }
    }
  };
  walk(dir, 0);
  const rj = join(dir, "README.md");
  if (existsSync(rj)) info.readme = readFileSync(rj, "utf8").split(/\r?\n/).slice(0, 2).join(" / ").slice(0, 100);
  console.log("\n## " + repo + (info.hasBin ? " [has bin]" : ""));
  for (const p of info.packages) console.log(`   pkg: ${p.name}@${p.version} spt=${p.sptVersion} (${p.path})`);
  for (const c of info.csproj) console.log(`   csproj: ${c.path} tf=${c.tf} v=${c.ver}`);
  if (info.readme) console.log(`   readme: ${info.readme}`);
}
