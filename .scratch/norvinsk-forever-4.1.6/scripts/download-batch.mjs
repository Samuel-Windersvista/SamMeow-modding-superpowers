// download-batch.mjs —— 从 Forge 按批次下载安装包到实例 downloads 目录
// 用法: node download-batch.mjs B1 [B2 ...]
import { readFileSync, writeFileSync, mkdirSync, existsSync, statSync, renameSync, readdirSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { join } from "node:path";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const DLDIR = "E:\\Game\\EFT_Offline\\Norvinsk_Forever\\downloads";
const PROXY = process.env.SP_MOD_PROXY ?? "http://127.0.0.1:7890";
mkdirSync(DLDIR, { recursive: true });

const targets = JSON.parse(readFileSync(join(SCRATCH, "install-targets.json"), "utf8"));
const batches = process.argv.slice(2);
if (!batches.length) { console.log("usage: node download-batch.mjs B1 [B2 ...]"); process.exit(1); }
const list = targets.filter((t) => batches.includes(t.batch));

function magicExt(file) {
  const fd = readFileSync(file);
  if (fd.length < 4) return null;
  const b = [fd[0], fd[1], fd[2], fd[3]];
  if (b[0] === 0x50 && b[1] === 0x4b) return ".zip";
  if (b[0] === 0x37 && b[1] === 0x7a && b[2] === 0xbc && b[3] === 0xaf) return ".7z";
  if (b[0] === 0x52 && b[1] === 0x61 && b[2] === 0x72 && b[3] === 0x21) return ".rar";
  return ".bin";
}

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
let ok = 0, skip = 0, fail = 0;
for (const t of list) {
  const slug = (t.link ?? "").split("/").slice(-2, -1)[0] ?? `mod${t.forgeId}`;
  const base = `${t.forgeId}-${slug}-${t.version}`;
  // 已存在检查（任意扩展名）
  const existing = readdirSync(DLDIR).find((f) => f.startsWith(base + "."));
  if (existing) { console.log(`SKIP  ${existing}`); skip++; continue; }
  const tmp = join(DLDIR, base + ".part");
  const args = ["-g", "-sS", "-L", "-m", "900", "-A", "SamMeow-modpack-research/0.1", "-o", tmp, t.link];
  // 先行直连，失败再走代理
  let done = false;
  for (const proxy of [null, PROXY]) {
    const a2 = proxy ? [...args.slice(0, 6), "-x", proxy, ...args.slice(6)] : args;
    try {
      execFileSync("curl.exe", a2, { encoding: "utf8", maxBuffer: 16 * 1024 * 1024 });
      done = true;
      break;
    } catch (e) {
      console.log(`  retry(${proxy ? "proxy" : "direct"}) ${base}`);
      await sleep(2000);
    }
  }
  if (!done || !existsSync(tmp)) { console.log(`FAIL  ${base}`); fail++; continue; }
  const ext = magicExt(tmp);
  const final = join(DLDIR, base + ext);
  renameSync(tmp, final);
  const mb = (statSync(final).size / 1048576).toFixed(1);
  console.log(`OK    ${base}${ext} (${mb} MB)`);
  ok++;
  await sleep(400);
}
console.log(`done. ok=${ok} skip=${skip} fail=${fail}`);
