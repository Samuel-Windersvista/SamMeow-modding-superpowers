// apply-meta-content.mjs —— 通过 stdio JSON-RPC 驱动 mo2-mcp，批量回填 meta.ini（comments/notes/url）
import { spawn } from "node:child_process";
import { readFileSync, writeFileSync, existsSync } from "node:fs";
import { join } from "node:path";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const MCP_DIR = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\tools\\mo2-mcp";
const MO2_ROOT = "E:\\Game\\EFT_Offline\\Norvinsk_Forever";
const PROFILE = "Default";

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

function request(method, params, timeoutMs = 120000) {
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

await request("initialize", { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "meta-apply", version: "1.0" } }, 60000);
proc.stdin.write(JSON.stringify({ jsonrpc: "2.0", method: "notifications/initialized", params: {} }) + "\n");
const sess = await call("mo2_session", { mo2Root: MO2_ROOT, profile: PROFILE });
log(`bind: ${JSON.stringify(sess.snapshot ?? sess.error)}`);

const results = [];
for (const t of targets) {
  const c = content[String(t.forgeId)];
  if (!c || !c.comments || !c.notes) { log(`MISSING-CONTENT #${t.forgeId} ${t.overlayName}`); results.push({ id: t.forgeId, name: t.overlayName, status: "missing-content" }); continue; }
  const updates = { General: { comments: c.comments, notes: c.notes, url: `https://sp-mod.com/mod/${t.forgeId}` } };
  const plan = await call("mo2_edit_meta", { mode: "plan", name: t.overlayName, updates });
  if (!plan.ok) { log(`PLAN-FAIL ${t.overlayName}: ${JSON.stringify(plan.error).slice(0, 200)}`); results.push({ id: t.forgeId, name: t.overlayName, status: "plan-fail", error: plan.error }); continue; }
  const apply = await call("mo2_edit_meta", { mode: "apply", plan_id: plan.result.planId, lease_token: plan.result.lease_token });
  if (!apply.ok) { log(`APPLY-FAIL ${t.overlayName}: ${JSON.stringify(apply.error).slice(0, 200)}`); results.push({ id: t.forgeId, name: t.overlayName, status: "apply-fail", error: apply.error }); }
  else { log(`OK ${t.overlayName}`); results.push({ id: t.forgeId, name: t.overlayName, status: "ok" }); }
}
writeFileSync(join(SCRATCH, "meta-apply-results.json"), JSON.stringify(results, null, 1), "utf8");
const ok = results.filter((r) => r.status === "ok").length;
log(`DONE ok=${ok} fail=${results.length - ok}`);
proc.kill();
