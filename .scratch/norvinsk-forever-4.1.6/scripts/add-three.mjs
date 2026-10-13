// add-three.mjs —— 增量补装 3 项（IOF/BRNVG/HDR）：下载 + 安装(comments) + notes(专用转义写入) + url + 读回校验
import { spawn, execFileSync } from "node:child_process";
import { readFileSync, writeFileSync, readdirSync, existsSync, renameSync, openSync, readSync, closeSync } from "node:fs";
import { join } from "node:path";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const MCP_DIR = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\tools\\mo2-mcp";
const MO2_ROOT = "E:\\Game\\EFT_Offline\\Norvinsk_Forever";
const PROFILE = "Default";
const DLDIR = join(MO2_ROOT, "downloads");
const MODS_DIR = join(MO2_ROOT, "mods");
const PROXY = process.env.SP_MOD_PROXY ?? "http://127.0.0.1:7890";
const log = (m) => console.log(`[${new Date().toISOString().slice(11, 19)}] ${m}`);

const ITEMS = [
  { forgeId: 954, overlayName: "[7]平衡的夜视装备视效-BRNVG", category: "B6-武器/配件/视觉", ver: "3.0.4",
    slug: "borkels-realistic-night-vision-goggles-nvgs-and-t-7",
    comments: "以真实感着色器重做夜视装备的成像效果",
    notes: "- 以真实 NVG 像管模拟替换原版夜视着色器\n- 支持自动增益与自动门控（AUTOGATING/AUTOGAIN）\n- 夜视仪外圈可见自然环境光（类似 GAMMA 效果）\n- 新增手动增益调节按键（默认未绑定）\n- 覆盖 NVG 与 T-7 热成像设备\n- 内置多款兼容补丁（CAG / WTT Black Division / ISB 等）\n- 含服务端组件，与客户端成对生效" },
  { forgeId: 2960, overlayName: "[10]物品自动分类功能-IOF", category: "B3-修复/价格/容器 UI", ver: "1.9.0",
    slug: "iof-inventory-organizing-feature",
    comments: "在仓库与容器中提供自动整理与排序功能",
    notes: "- 通过物品标签（@ 标签+参数）定义整理规则\n- 支持仓库与各类容器的自动整理\n- 支持递归容器排序\n- 可自定义分类与排序行为\n- 社区接力移植维护（4.1.x 由 flir 维护）" },
  { forgeId: 2953, overlayName: "[6]AI锁头重定向-HeadshotDamageRedirection", category: "B4-功能扩展", ver: "1.8.0",
    slug: "headshot-damage-redirection",
    comments: "将头部/胸部致命伤害分摊到其他部位以增加生存机会",
    notes: "- 将爆头/胸部伤害重定向分摊到多个身体部位\n- 可配置重定向比例、阈值与目标部位\n- 支持最小伤害阈值、伤害上限与概率触发\n- 内置老爹模式（全局减伤）选项\n- 游戏内 F12 配置界面" },
];

function magicExt(file) {
  const fd = openSync(file, "r"); const b = Buffer.alloc(8); readSync(fd, b, 0, 8, 0); closeSync(fd);
  if (b[0] === 0x50 && b[1] === 0x4b) return ".zip";
  if (b[0] === 0x37 && b[1] === 0x7a && b[2] === 0xbc && b[3] === 0xaf) return ".7z";
  if (b[0] === 0x52 && b[1] === 0x61 && b[2] === 0x72 && b[3] === 0x21) return ".rar";
  return ".bin";
}

// ---------- 1) download ----------
const localFiles = {};
for (const it of ITEMS) {
  const base = `${it.forgeId}-${it.slug}-${it.ver}`;
  const existing = readdirSync(DLDIR).find((f) => f.startsWith(base + ".") && !f.endsWith(".part"));
  if (existing) { log(`SKIP-DL ${existing}`); localFiles[it.forgeId] = join(DLDIR, existing); continue; }
  const tmp = join(DLDIR, base + ".part");
  const url = `https://sp-mod.com/mod/download/${it.forgeId}/${it.slug}/${it.ver}`;
  let ok = false;
  for (const proxy of [null, PROXY, null, PROXY]) {
    const args = ["-g", "-sS", "-L", "-m", "900", "-A", "SamMeow-modpack-research/0.1", "-o", tmp];
    if (proxy) args.push("-x", proxy);
    args.push(url);
    try { execFileSync("curl.exe", args, { encoding: "utf8", maxBuffer: 16 * 1024 * 1024 }); ok = true; break; } catch { log(`  retry(${proxy ? "proxy" : "direct"}) ${base}`); }
  }
  if (!ok || !existsSync(tmp)) { log(`FAIL-DL ${base}`); continue; }
  const ext = magicExt(tmp);
  const final = join(DLDIR, base + ext);
  renameSync(tmp, final);
  localFiles[it.forgeId] = final;
  log(`OK-DL ${base}${ext}`);
}

// ---------- 2) MCP driver ----------
const proc = spawn("node", ["dist/index.js"], {
  cwd: MCP_DIR,
  env: { ...process.env, MO2_ROOT, MO2_PROFILE: PROFILE, MO2_PERMISSION_CEILING: "full-control" },
  stdio: ["pipe", "pipe", "pipe"],
});
let nextId = 1, buf = "", stderrBuf = "";
const pending = new Map();
proc.stderr.on("data", (c) => { stderrBuf += c.toString("utf8"); if (stderrBuf.length > 20000) stderrBuf = stderrBuf.slice(-10000); });
proc.stdout.on("data", (c) => {
  buf += c.toString("utf8");
  let i;
  while ((i = buf.indexOf("\n")) >= 0) {
    const line = buf.slice(0, i).trim(); buf = buf.slice(i + 1);
    if (!line) continue;
    try { const msg = JSON.parse(line); if (typeof msg.id === "number" && pending.has(msg.id)) { const e = pending.get(msg.id); pending.delete(msg.id); clearTimeout(e.timer); e.resolve(msg); } } catch {}
  }
});
proc.once("exit", (code) => { for (const [, e] of pending) { clearTimeout(e.timer); e.reject(new Error(`mcp exited code=${code}`)); } pending.clear(); });

function request(method, params, timeoutMs = 900000) {
  const id = nextId++;
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => { pending.delete(id); reject(new Error(`timeout ${method}`)); }, timeoutMs);
    pending.set(id, { resolve, reject, timer });
    proc.stdin.write(JSON.stringify({ jsonrpc: "2.0", id, method, params }) + "\n");
  });
}
async function call(name, args) {
  const resp = await request("tools/call", { name, arguments: args });
  const text = resp.result?.content?.[0]?.text;
  if (typeof text !== "string") return { ok: false, error: { code: "malformed", resp } };
  try { return JSON.parse(text); } catch { return { ok: false, error: { code: "bad_json", text: text.slice(0, 300) } }; }
}
async function planApply(name, args) {
  const plan = await call(name, { mode: "plan", ...args });
  if (!plan.ok) return { ok: false, phase: "plan", error: plan.error };
  const apply = await call(name, { mode: "apply", plan_id: plan.result.planId, lease_token: plan.result.lease_token });
  if (!apply.ok) return { ok: false, phase: "apply", error: apply.error };
  return { ok: true };
}

await request("initialize", { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "add-three", version: "1.0" } }, 60000);
proc.stdin.write(JSON.stringify({ jsonrpc: "2.0", method: "notifications/initialized", params: {} }) + "\n");
const sess = await call("mo2_session", { mo2Root: MO2_ROOT, profile: PROFILE });
log(`bind: ${JSON.stringify(sess.snapshot ?? sess.error)}`);

const results = [];
for (const it of ITEMS) {
  const file = localFiles[it.forgeId];
  if (!file || !existsSync(file)) { log(`SKIP-INSTALL(no archive) ${it.overlayName}`); results.push({ id: it.forgeId, status: "no-archive" }); continue; }
  const r1 = await planApply("mo2_install", { archive_path: file, mod_name: it.overlayName, profile: PROFILE, category: it.category, comments: it.comments });
  log(`install ${r1.ok ? "OK" : "FAIL"} ${it.overlayName}${r1.ok ? "" : " " + JSON.stringify(r1.error).slice(0, 160)}`);
  if (!r1.ok) { results.push({ id: it.forgeId, status: "install-fail", error: r1.error }); continue; }
  const r2 = await planApply("mo2_set_mod_notes", { name: it.overlayName, notes: it.notes });
  const r3 = await planApply("mo2_edit_meta", { name: it.overlayName, updates: { General: { url: `https://sp-mod.com/mod/${it.forgeId}/${it.slug}` } } });
  // 读回校验
  let fmtOk = false, urlOk = false, noteLines = 0, commentOk = false;
  try {
    const text = readFileSync(join(MODS_DIR, it.overlayName, "meta.ini"), "utf8");
    const m = text.match(/^notes=(.*)$/m);
    if (m) {
      const v = m[1].trim();
      const unq = v.startsWith('"') && v.endsWith('"') ? v.slice(1, -1) : v;
      noteLines = unq.split("\\n").length;
      fmtOk = noteLines === it.notes.split("\n").length;
    }
    urlOk = new RegExp(`^url=.*mod/${it.forgeId}/${it.slug}`, "m").test(text);
    commentOk = /^comments=.+/m.test(text);
  } catch {}
  const ok = r2.ok && r3.ok && fmtOk && urlOk && commentOk;
  log(`${ok ? "OK" : "CHECK"} ${it.overlayName} (notes=${r2.ok} url=${r3.ok} fmt=${fmtOk} lines=${noteLines} comments=${commentOk})`);
  results.push({ id: it.forgeId, name: it.overlayName, status: ok ? "ok" : "check", notes: r2.ok, url: r3.ok, fmtOk, urlOk, commentOk, noteLines });
}
writeFileSync(join(SCRATCH, "add-three-results.json"), JSON.stringify(results, null, 1), "utf8");
log(`DONE total=${results.length} ok=${results.filter((r) => r.status === "ok").length}`);
proc.kill();
