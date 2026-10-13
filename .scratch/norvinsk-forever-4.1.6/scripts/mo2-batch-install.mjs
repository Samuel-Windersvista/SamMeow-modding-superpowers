// mo2-batch-install.mjs —— 通过 stdio JSON-RPC 驱动 mo2-mcp，批量安装剩余批次
// 用法: node mo2-batch-install.mjs [B6 B5 B4 B3 B2 B7] [--reorder-b1] [--only-b1-reorder]
import { spawn } from "node:child_process";
import { readFileSync, writeFileSync, readdirSync, mkdirSync, existsSync } from "node:fs";
import { join } from "node:path";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const MCP_DIR = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\tools\\mo2-mcp";
const MO2_ROOT = "E:\\Game\\EFT_Offline\\Norvinsk_Forever";
const PROFILE = "Default";
const MODS_DIR = join(MO2_ROOT, "mods");
const DLDIR = join(MO2_ROOT, "downloads");
const ORDER = ["B6", "B5", "B4", "B3", "B2", "B7"];

const argv = process.argv.slice(2);
const flags = argv.filter((a) => a.startsWith("--"));
const batches = argv.filter((a) => !a.startsWith("--"));
const runBatches = batches.length ? batches : ORDER;
const reorderB1 = flags.includes("--reorder-b1") || flags.includes("--only-b1-reorder");

const targets = JSON.parse(readFileSync(join(SCRATCH, "install-targets.json"), "utf8"));
const list = ORDER.filter((b) => runBatches.includes(b)).flatMap((b) => targets.filter((t) => t.batch === b));

const log = (m) => console.log(`[${new Date().toISOString().slice(11, 19)}] ${m}`);

// ---------- spawn MCP ----------
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

function request(method, params, timeoutMs = 1800000) {
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

await request("initialize", { protocolVersion: "2024-11-05", capabilities: {}, clientInfo: { name: "batch-install", version: "1.0" } }, 60000);
proc.stdin.write(JSON.stringify({ jsonrpc: "2.0", method: "notifications/initialized", params: {} }) + "\n");
const sess = await call("mo2_session", { mo2Root: MO2_ROOT, profile: PROFILE });
log(`bind: ${JSON.stringify(sess.snapshot ?? sess.error)}`);

// ---------- install loop ----------
const results = [];
if (!flags.includes("--only-b1-reorder")) {
  for (const t of list) {
    const overlay = t.overlayName;
    if (existsSync(join(MODS_DIR, overlay))) { log(`SKIP(exists) ${overlay}`); results.push({ id: t.forgeId, name: overlay, status: "skip-exists" }); continue; }
    const file = readdirSync(DLDIR).find((f) => f.startsWith(`${t.forgeId}-`) && !f.endsWith(".part"));
    if (!file) { log(`MISSING-ARCHIVE ${overlay}`); results.push({ id: t.forgeId, name: overlay, status: "missing-archive" }); continue; }
    const comments = `Forge #${t.forgeId} ${t.forgeName} ${t.version}（约束 ${t.constraint}）| 参照 Life_in_Norvinsk ${t.idxs && t.idxs[0] ? "条目#" + t.idxs.join("/") : ""}`;
    const notes = `来源: ${t.link} | SPT约束: ${t.constraint} | 批次: ${t.batch} | 《永远的诺文斯克》v0.1.x | 安装: ${new Date().toISOString().slice(0, 10)}${t.note ? " | 备注: " + t.note : ""}`;
    const planArgs = { mode: "plan", archive_path: join(DLDIR, file), mod_name: overlay, profile: PROFILE, target_priority: t.priority, category: t.batch, comments, notes };
    const choicesFile = join(SCRATCH, "fomod-choices", `${t.forgeId}.json`);
    if (existsSync(choicesFile)) {
      try { planArgs.fomod_choices = JSON.parse(readFileSync(choicesFile, "utf8")); } catch (e) { log(`choices parse fail ${t.forgeId}: ${e}`); }
    }
    const plan = await call("mo2_install", planArgs);
    if (!plan.ok) {
      log(`PLAN-FAIL ${overlay}: ${JSON.stringify(plan.error).slice(0, 200)}`);
      results.push({ id: t.forgeId, name: overlay, status: "plan-fail", error: plan.error });
      continue;
    }
    const apply = await call("mo2_install", { mode: "apply", plan_id: plan.result.planId, lease_token: plan.result.lease_token });
    if (!apply.ok) {
      log(`APPLY-FAIL ${overlay}: ${JSON.stringify(apply.error).slice(0, 300)}`);
      results.push({ id: t.forgeId, name: overlay, status: "apply-fail", error: apply.error });
    } else {
      log(`OK ${overlay} (${file})`);
      results.push({ id: t.forgeId, name: overlay, status: "ok", file });
    }
  }
}

// ---------- B1 reorder to bottom ----------
if (reorderB1) {
  const b1 = targets.filter((t) => t.batch === "B1");
  for (const t of b1) {
    const plan = await call("mo2_send_mod_to", { mode: "plan", name: t.overlayName, target_mode: "bottom", profile: PROFILE });
    if (!plan.ok) { log(`REORDER-PLAN-FAIL ${t.overlayName}: ${JSON.stringify(plan.error).slice(0, 200)}`); continue; }
    const apply = await call("mo2_send_mod_to", { mode: "apply", plan_id: plan.result.planId, lease_token: plan.result.lease_token });
    log(apply.ok ? `MOVED-BOTTOM ${t.overlayName}` : `REORDER-APPLY-FAIL ${t.overlayName}: ${JSON.stringify(apply.error).slice(0, 200)}`);
  }
}

writeFileSync(join(SCRATCH, "install-batch-results.json"), JSON.stringify(results, null, 1), "utf8");
const ok = results.filter((r) => r.status === "ok").length, fail = results.filter((r) => r.status.includes("fail") || r.status === "missing-archive").length, skip = results.filter((r) => r.status === "skip-exists").length;
log(`DONE ok=${ok} fail=${fail} skip=${skip}`);
proc.kill();
