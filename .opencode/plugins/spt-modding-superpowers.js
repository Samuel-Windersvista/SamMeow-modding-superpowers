// spt-modding-superpowers — OpenCode plugin entrypoint
//
// Wires three things:
//   1. config.skills.paths: append <plugin>/skills so OpenCode discovers our SKILL.md files
//   2. config.mcp.mo2 + config.mcp.spt + config.mcp.tarkov:
//                           register the bundled MCP stdio servers
//                           (node tools/<server>/dist/index.js)
//   3. first-user-message bootstrap: inject the using-spt-modding-superpowers SKILL body
//                                    into the first user message so the host agent loads
//                                    the bootstrap on every session start
//
// Pattern adapted from obra/superpowers/.opencode/plugins/superpowers.js (for skills + bootstrap
// injection) and alvinunreal/oh-my-opencode-slim (for the local-MCP config.mcp surface).

import path from 'path';
import fs from 'fs';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// .opencode/plugins/<file>.js lives two dirs below the plugin root
const PLUGIN_ROOT = path.resolve(__dirname, '..', '..');

const SKILLS_DIR = path.join(PLUGIN_ROOT, 'skills');
const MO2_MCP_ENTRY = path.join(PLUGIN_ROOT, 'tools', 'mo2-mcp', 'dist', 'index.js');
const SPT_MCP_ENTRY = path.join(PLUGIN_ROOT, 'tools', 'spt-mcp', 'dist', 'index.js');
const TARKOV_MCP_ENTRY = path.join(PLUGIN_ROOT, 'tools', 'tarkov-runtime-mcp', 'dist', 'index.js');
const BOOTSTRAP_SKILL = path.join(SKILLS_DIR, 'using-spt-modding-superpowers', 'SKILL.md');

// Sentinel used to detect already-injected bootstrap so we don't double-inject across reloads.
const BOOTSTRAP_MARKER = 'EXTREMELY_IMPORTANT_SPT_MODDING_SUPERPOWERS';

function readBootstrap() {
  try {
    return fs.readFileSync(BOOTSTRAP_SKILL, 'utf8');
  } catch (err) {
    // Bootstrap skill not present yet (e.g., before P2 in the reshape plan).
    // Plugin still loads; first-user-message injection is just a no-op.
    return null;
  }
}

export const SptModdingSuperpowersPlugin = async () => {
  const bootstrap = readBootstrap();

  // Runtime-layout health check. The single source of truth for KB-root /
  // archive / helper paths is shared/runtime-layout.mjs, imported by both this
  // plugin and spt-mcp; the plugin no longer sets SPT_KB_ROOT / SPT_MCP_HELPER /
  // SPT_IL_HELPER (the parent env passes through and the resolver validates it).
  // Dynamic import keeps a missing module from breaking plugin loading.
  try {
    const layoutModule = await import('../../shared/runtime-layout.mjs').catch(() => null);
    if (layoutModule) {
      const layout = layoutModule.resolveRuntimeLayout(PLUGIN_ROOT);
      if (layout.warnings.length > 0) {
        console.error(layoutModule.formatLayoutWarnings(layout));
      }
    } else {
      console.error('[spt-modding-superpowers] runtime-layout 模块缺失，跳过健康检查');
    }
  } catch (err) {
    console.error(`[spt-modding-superpowers] 运行时布局检查失败：${err?.message ?? err}`);
  }

  return {
    // NOTE: `mcp:` on the plugin return is NOT a documented Hook key in
    // @opencode-ai/plugin's Hooks interface and is silently ignored by
    // current OpenCode. The `config:` hook below is the only canonical
    // surface for registering plugin-bundled MCP servers. (Verified against
    // anomalyco/opencode upstream Hooks definition + real precedents like
    // vercel-labs/coding-agent-template, glommer/memelord.)

    config: async (config) => {
      // (a) Make our skills discoverable by appending to config.skills.paths.
      config.skills ??= {};
      config.skills.paths ??= [];
      if (!config.skills.paths.includes(SKILLS_DIR)) {
        config.skills.paths.push(SKILLS_DIR);
      }

      // (b) Register the bundled MCP servers via the documented opencode
      //     config.mcp surface. Use ??= so user overrides in opencode.json win.
      //     `enabled: true` is explicit per every real-world LOCAL-stdio MCP
      //     precedent observed in public opencode plugins.
      config.mcp ??= {};
      // mo2-mcp: lazy-bind MCP server (v1.2-pre refactor landed). Server starts
      // clean with no MO2 root env requirement; agent binds to an MO2 instance
      // by calling mo2_session({ mo2Root, profile? }). If the MO2 root env var
      // is set in the parent env, main() does an eager auto-bind before writing
      // the ready log. environment: {} passes through the parent env
      // transparently (no hardcoded paths in the plugin file).
      config.mcp.mo2 ??= {
        type: 'local',
        command: ['node', MO2_MCP_ENTRY],
        enabled: true,
        environment: {},
        timeout: 240000,
      };
      // spt-mcp: file-based MCP server (no daemon, all tools synchronous).
      // No path env vars are injected here: spt-mcp resolves KB root, Forge
      // archive, and the .NET helper artifacts through the shared resolver
      // (shared/runtime-layout.mjs) and validates each one. `environment: {}`
      // passes the parent env through transparently, so a user-supplied
      // SPT_KB_ROOT / SPT_MCP_HELPER / SPT_IL_HELPER still wins — and an
      // explicitly-set-but-invalid value now surfaces as an error instead of
      // silently falling back.
      config.mcp.spt ??= {
        type: 'local',
        command: ['node', SPT_MCP_ENTRY],
        enabled: true,
        environment: {},
        timeout: 60000,
      };
      // tarkov-mcp: runtime-state MCP server for a live SPT 5.x server
      // (handshake/version gate + in-raid tools). timeout is 360000 because the
      // server's wait_for tool caps at MAX_WAIT_TIMEOUT_MS = 300_000; the extra
      // 60s is handshake + transport margin, otherwise the client aborts a
      // legitimately long wait. `environment: {}` passes the parent env through
      // transparently (same override semantics as mo2/spt above).
      config.mcp.tarkov ??= {
        type: 'local',
        command: ['node', TARKOV_MCP_ENTRY],
        enabled: true,
        environment: {},
        timeout: 360000,
      };
    },

    // (d) Inject the bootstrap skill body into the first user message of each session.
    //     Using a user message (not system) avoids:
    //       1. Token bloat from system messages repeated every turn
    //       2. Multiple system messages breaking some non-Anthropic models
    //     Matches the pattern in obra/superpowers/.opencode/plugins/superpowers.js.
    'experimental.chat.messages.transform': async (_input, output) => {
      if (!bootstrap || !output?.messages?.length) return;
      const firstUser = output.messages.find((m) => m?.info?.role === 'user');
      if (!firstUser?.parts?.length) return;
      // Idempotency: skip if any part already carries the marker.
      if (firstUser.parts.some((p) => p?.type === 'text' && p?.text?.includes(BOOTSTRAP_MARKER))) {
        return;
      }
      const ref = firstUser.parts[0];
      firstUser.parts.unshift({ ...ref, type: 'text', text: bootstrap });
    },
  };
};

export default SptModdingSuperpowersPlugin;
