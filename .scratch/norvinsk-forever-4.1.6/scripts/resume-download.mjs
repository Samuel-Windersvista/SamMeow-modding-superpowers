// resume-download.mjs —— 针对剩余文件的续传下载器（支持 .part 断点续传 + 校验尺寸）
import { readFileSync, writeFileSync, mkdirSync, existsSync, statSync, renameSync, unlinkSync, openSync, readSync, closeSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { join } from "node:path";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const DLDIR = "E:\\Game\\EFT_Offline\\Norvinsk_Forever\\downloads";
const PROXY = process.env.SP_MOD_PROXY ?? "http://127.0.0.1:7890";
const targets = JSON.parse(readFileSync(join(SCRATCH, "install-targets.json"), "utf8"));
const ids = process.argv.slice(2).map(Number);

function magicExt(file) {
  const fd = openSync(file, "r");
  const b = Buffer.alloc(8);
  readSync(fd, b, 0, 8, 0);
  closeSync(fd);
  if (b[0] === 0x50 && b[1] === 0x4b) return ".zip";
  if (b[0] === 0x37 && b[1] === 0x7a && b[2] === 0xbc && b[3] === 0xaf) return ".7z";
  if (b[0] === 0x52 && b[1] === 0x61 && b[2] === 0x72 && b[3] === 0x21) return ".rar";
  return ".bin";
}

for (const id of ids) {
  const t = targets.find((x) => x.forgeId === id);
  if (!t) { console.log(`no target for ${id}`); continue; }
  const slug = (t.link ?? "").split("/").slice(-2, -1)[0] ?? `mod${id}`;
  const base = `${id}-${slug}-${t.version}`;
  // 已有成品？
  const { readdirSync } = await import("node:fs");
  const done = readdirSync(DLDIR).find((f) => f.startsWith(base + ".") && !f.endsWith(".part"));
  if (done) { console.log(`SKIP ${done}`); continue; }
  const part = join(DLDIR, base + ".part");
  if (existsSync(part)) {
    const cur = statSync(part).size;
    const expect = t.size || 0;
    if (expect && cur === expect) { renameSync(part, join(DLDIR, base + magicExt(part))); console.log(`COMPLETE ${base}`); continue; }
    console.log(`RESUME ${base} from ${(cur / 1048576).toFixed(1)}MB (expect ${(expect / 1048576).toFixed(1)}MB)`);
  } else {
    console.log(`FRESH  ${base}`);
  }
  let ok = false;
  for (let attempt = 0; attempt < 5 && !ok; attempt++) {
    const useProxy = attempt % 2 === 1;
    const args = ["-g", "-sS", "-L", "-m", "1800", "-A", "SamMeow-modpack-research/0.1", "-C", "-", "-o", part];
    if (useProxy) args.push("-x", PROXY);
    args.push(t.link);
    try {
      execFileSync("curl.exe", args, { encoding: "utf8", maxBuffer: 16 * 1024 * 1024 });
      ok = true;
    } catch (e) {
      console.log(`  attempt ${attempt + 1} failed (${useProxy ? "proxy" : "direct"}), part=${(statSync(part).size / 1048576).toFixed(1)}MB`);
    }
  }
  if (ok) {
    const size = statSync(part).size;
    const expect = t.size || 0;
    if (expect && size !== expect) { console.log(`WARN size mismatch ${size} != ${expect}`); }
    renameSync(part, join(DLDIR, base + magicExt(part)));
    console.log(`OK ${base} (${(size / 1048576).toFixed(1)}MB)`);
  } else {
    console.log(`FAIL ${base}`);
  }
}
console.log("resume-download done");
