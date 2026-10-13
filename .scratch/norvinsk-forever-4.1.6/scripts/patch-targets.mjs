import { readFileSync, writeFileSync } from "node:fs";
const p = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6\\install-targets.json";
const t = JSON.parse(readFileSync(p, "utf8"));
for (const x of t) {
  if (x.forgeId === 791) x.overlayName = "[6]AI增强-SAIN";
  if (x.forgeId === 1100) x.forgeName = "ACCURATE CIRCULAR RADAR";
  if (x.forgeId === 2543) x.forgeName = "BossNotifier";
  if (x.forgeId === 2248) x.forgeName = "Separate Hostility";
  if (x.forgeId === 2882) x.forgeName = "Eco's Attachment Emporium";
}
writeFileSync(p, JSON.stringify(t, null, 1), "utf8");
console.log("patched targets:", t.length);
