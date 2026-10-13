// meta-scan.mjs —— 扫描 Life 包全部 mod 的 meta.ini，提取来源信息
import { readFileSync, writeFileSync, readdirSync, existsSync } from "node:fs";
import { join } from "node:path";

const MODS = "E:\\Game\\EFT_Offline\\Life_in_Norvinsk_v0.3.2\\mods";
const OUT = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6\\life-meta.tsv";

const dirs = readdirSync(MODS, { withFileTypes: true }).filter((e) => e.isDirectory()).map((e) => e.name);
const rows = [];
for (const name of dirs) {
  const p = join(MODS, name, "meta.ini");
  if (!existsSync(p)) { rows.push({ name, missing: true }); continue; }
  const t = readFileSync(p, "utf8").replace(/^\uFEFF/, "");
  const get = (k) => {
    const m = t.match(new RegExp(`^${k}=(.*)$`, "m"));
    return m ? m[1].trim() : "";
  };
  rows.push({
    name,
    version: get("version"),
    file: get("installationFile"),
    url: get("url"),
    modid: get("modid"),
    comments: get("comments"),
  });
}
writeFileSync(OUT, rows.map((r) => [r.name, r.version, r.file, r.url, r.modid, r.comments].join("\t")).join("\n"), "utf8");

const hub = rows.filter((r) => (r.url ?? "").includes("hub.sp-tarkov.com"));
console.log("total:", rows.length, "with-hub-url:", hub.length);
for (const h of hub) console.log("HUB:", h.name, "=>", h.url);
