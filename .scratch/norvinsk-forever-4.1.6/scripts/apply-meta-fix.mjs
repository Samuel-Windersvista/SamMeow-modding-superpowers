// apply-meta-fix.mjs —— 修复版回填：notes 走 mo2_set_mod_notes（QSettings 转义），url 走 mo2_edit_meta（完整 slug 链接）
import { spawn } from "node:child_process";
import { readFileSync, writeFileSync, readdirSync, existsSync } from "node:fs";
import { join } from "node:path";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const MCP_DIR = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\tools\\mo2-mcp";
const MO2_ROOT = "E:\\Game\\EFT_Offline\\Norvinsk_Forever";
const PROFILE = "Default";
const MODS_DIR = join(MO2_ROOT, "mods");
const DETAIL_DIR = join(SCRATCH, "mod-detail");

const targets = JSON.parse(readFileSync(join(SCRATCH, "install-targets.json"), "utf8"));
const content = JSON.parse(readFileSync(join(SCRATCH, "meta-content.json"), "utf8"));
const log = (m) => console.log(`[${new Date().toISOString().slice(11, 19)}] ${m}`);

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

function request(method, params, timeoutMs = 180000) {
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

await request("initialize", { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "meta-fix", version: "1.0" } }, 60000);
proc.stdin.write(JSON.stringify({ jsonrpc: "2.0", method: "notifications/initialized", params: {} }) + "\n");
const sess = await call("mo2_session", { mo2Root: MO2_ROOT, profile: PROFILE });
log(`bind: ${JSON.stringify(sess.snapshot ?? sess.error)}`);

const results = [];
for (const t of targets) {
  const c = content[String(t.forgeId)];
  if (!c?.notes) { results.push({ id: t.forgeId, status: "missing-content" }); continue; }
  // url from mod-detail
  let url = "";
  try {
    const d = JSON.parse(readFileSync(join(DETAIL_DIR, `${t.forgeId}.json`), "utf8"));
    url = d.detail_url || (d.slug ? `https://sp-mod.com/mod/${t.forgeId}/${d.slug}` : "");
  } catch {}
  if (!url) url = `https://sp-mod.com/mod/${t.forgeId}`;

  const r1 = await planApply("mo2_set_mod_notes", { name: t.overlayName, notes: c.notes });
  const r2 = await planApply("mo2_edit_meta", { name: t.overlayName, updates: { General: { url } } });
  // 读回校验格式
  let fmtOk = false, urlOk = false, noteLines = 0;
  try {
    const text = readFileSync(join(MODS_DIR, t.overlayName, "meta.ini"), "utf8");
    const m = text.match(/^notes=(.*)$/m);
    if (m) {
      const v = m[1].trim();
      const unq = v.startsWith('"') && v.endsWith('"') ? v.slice(1, -1) : v;
      noteLines = unq.split("\\n").length;
      fmtOk = noteLines === c.notes.split("\n").length;
    }
    urlOk = text.includes(url.split("mod/")[1] ? new RegExp(`^url=.*mod/${t.forgeId}/`, "m").test(text) : false);
  } catch {}
  const ok = r1.ok && r2.ok && fmtOk;
  results.push({ id: t.forgeId, name: t.overlayName, notes: r1.ok, url: r2.ok, fmtOk, urlOk, noteLines });
  log(`${ok ? "OK " : "FAIL"} ${t.overlayName} (notes=${r1.ok} url=${r2.ok} fmt=${fmtOk} lines=${noteLines})${r1.ok ? "" : " " + JSON.stringify(r1.error).slice(0, 120)}${r2.ok ? "" : " " + JSON.stringify(r2.error).slice(0, 120)}`);
}
writeFileSync(join(SCRATCH, "meta-fix-results.json"), JSON.stringify(results, null, 1), "utf8");
const bad = results.filter((r) => !(r.notes && r.url && r.fmtOk));
log(`DONE total=${results.length} fail=${bad.length}`);
proc.kill();
