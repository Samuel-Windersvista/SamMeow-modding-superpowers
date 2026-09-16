// =============================================================================
// mcp-kit/schema.ts — MCP inputSchema 规范化 + canonical schema 管道
//
// 来源（C2 共享内核抽取）：
//   - normalizeMcpInputSchema 自 tools/mo2-mcp/src/schema-normalizer.ts 逐字搬移
//     （C3 抽取产物 → C2 上移到共享内核；逻辑一字未改，仅 import 面变化）。
//   - mo2 侧原文件在 wave2 车道 C 删除；本模块成为唯一权威实现。
//
// 背景（BUG-13 / BUG-27）：
//   这段逻辑是全仓库 bug 密度最高的部分，且是唯一直接上 MCP wire（tools/list）
//   的逻辑——形状错误会让 Anthropic 系客户端的整场工具注册崩溃。原始逐字
//   注释（含 BUG-13 Lane A 与 BUG-27 的回归上下文）随代码一并搬移，见下方
//   函数级文档注释。
//
// canonical 管道（决策 D3）：
//   zodToJsonSchema(schema, { target: "jsonSchema7" }) → 去 `$schema`
//   → normalizeMcpInputSchema
//   单管道一处；mo2 从 openApi3 迁移到本管道，golden 对比验证。
// =============================================================================

import type { ZodTypeAny } from "zod";
import { zodToJsonSchema } from "zod-to-json-schema";

/**
 * Ensure the inputSchema returned via tools/list is a clean top-level
 * `type: "object"` schema with NO top-level `anyOf` / `oneOf` / `allOf`.
 *
 * Background:
 *   - Zod `discriminatedUnion("mode", ...)` and `z.union([...])` convert to
 *     a top-level `{ anyOf: [...] }` (or `oneOf`/`allOf`) shape via
 *     zodToJsonSchema. That is valid JSON Schema but NOT a valid MCP
 *     `inputSchema` once we put it on the wire to Anthropic.
 *   - Anthropic's tool-use API explicitly rejects top-level
 *     `oneOf`/`allOf`/`anyOf` in `input_schema`, even when `type: "object"`
 *     is also declared. The error surface is:
 *         "tools.<N>.custom.input_schema: input_schema does not support
 *          oneOf, allOf, or anyOf at the top level"
 *     This crashes tool registration entirely on every Anthropic-backed
 *     OpenCode session (claude-opus-4-7 / claude-sonnet-4-* etc.). OpenAI's
 *     strict validator happens to accept the `{type:"object", ..., anyOf:[...]}`
 *     wrapped shape, which is why the bug looks "Anthropic-only" from the
 *     outside.
 *   - The established workaround is to hand-write object schemas with all
 *     modes' properties merged at top level: top-level
 *     oneOf/anyOf/allOf/enum/not is forbidden by OpenAI-style strict
 *     tool-schema backends. The real branch-by-branch validation lives in
 *     the Zod `safeParse` inside `dispatch.ts`, NOT in the wire schema.
 *
 * What this function does:
 *   1. If `schema` is already `type: "object"` AND carries no top-level
 *      union keyword, pass it through unchanged.
 *   2. Otherwise collect all union variants from `anyOf` / `oneOf` / `allOf`.
 *   3. Hoist shared discriminant properties (a property that appears in
 *      every branch with `const` or single-value `enum`) into top-level
 *      `properties` with a unioned `enum`. Mark them `required` only if
 *      every branch requires them. This is the BUG-13 Lane A hoist that
 *      gives OpenAI tool-callers an anchor for argument decoding.
 *   4. ALSO merge each branch's other properties into top-level `properties`
 *      (first-wins). This gives LLMs visibility into the per-branch field
 *      shapes — they can see that `apply` mode wants `plan_id`, etc. —
 *      without leaking the union keyword onto the wire.
 *   5. Set `additionalProperties: true` so branch-specific fields not
 *      named here are still accepted by clients that enforce the wire
 *      schema strictly.
 *   6. DROP the original `anyOf` / `oneOf` / `allOf` keyword entirely.
 *
 * !!! DO NOT REINTRODUCE `[kw]: branches` AT THE TOP LEVEL !!!
 *   That was the v1.2-pre shape (BUG-13 Lane A pre-fix) and was the cause
 *   of the recurring Anthropic-side
 *     "input_schema does not support oneOf, allOf, or anyOf at the top level"
 *   crash. The real per-branch validator is Zod inside `dispatch.ts`; the
 *   wire schema must stay anyOf-free. If a future change needs the branch
 *   shape preserved for some external consumer, add a SEPARATE field
 *   (e.g. `x-branches`) outside the JSON Schema standard keyword set; do
 *   NOT use anyOf/oneOf/allOf at the top level.
 *   See also: D:\awesome-bgs-mod-master\AGENTS.md — "MCP inputSchema
 *   Anthropic Compatibility (2026-06-17)".
 */
export function normalizeMcpInputSchema(
  schema: Record<string, unknown>,
): Record<string, unknown> {
  const fallback: Record<string, unknown> = {
    type: "object",
    properties: {},
    additionalProperties: true,
  };
  if (!schema || typeof schema !== "object") return fallback;

  const unionKeywords = ["anyOf", "oneOf", "allOf"] as const;
  const hasUnion = unionKeywords.some((kw) => Array.isArray(schema[kw]));

  // Clean object schema with no union keyword: pass through.
  if (!hasUnion && schema.type === "object") {
    return schema;
  }

  // Collect branches across whichever union keyword(s) appear at top level.
  const topBranches: unknown[] = [];
  for (const kw of unionKeywords) {
    const v = schema[kw];
    if (Array.isArray(v)) topBranches.push(...v);
  }

  // BUG-27 fix: recursively flatten nested unions before discriminant
  // extraction and property merge. Zod
  //   z.union([z.discriminatedUnion("action", [v1, v2, v3]), applyShape])
  // serializes via zodToJsonSchema to a top-level
  //   { anyOf: [ { oneOf: [v1, v2, v3] }, applyShape ] }
  // The inner {oneOf:[...]} envelope has no .properties of its own, so the
  // pre-BUG-27 merge loop only saw applyShape's fields and the LLM never
  // got visibility into v1/v2/v3's action / entry / title / updates. By
  // recursively flattening to [v1, v2, v3, applyShape], every concrete
  // branch reaches the discriminant + merge passes. This is empirically
  // hit by gpt-5.x family on B.5.1 (mo2_configure_executable plan call).
  const branches = _flattenUnionBranches(topBranches);

  // Discriminant hoist (preserves BUG-13 Lane A semantics for OpenAI).
  // Also handles partial discriminants — properties that carry a const /
  // single-value enum in SOME but not all branches (e.g. `action` is a
  // literal in the three plan variants but absent from the apply variant).
  // Those are hoisted with a unioned enum but NOT added to top-level
  // `required`, so OpenAI's strict tool-schema validator no longer locks
  // such properties to whichever branch happened to merge first.
  const { properties: discriminantProps, required: discriminantRequired } =
    extractDiscriminants(branches);

  // Merge all non-discriminant branch properties so LLMs see every field.
  const mergedProperties: Record<string, unknown> = { ...discriminantProps };
  const topProps = schema.properties;
  if (topProps && typeof topProps === "object") {
    for (const [k, val] of Object.entries(topProps as Record<string, unknown>)) {
      if (!(k in mergedProperties)) mergedProperties[k] = val;
    }
  }
  for (const branch of branches) {
    if (!branch || typeof branch !== "object") continue;
    const bp = (branch as Record<string, unknown>).properties;
    if (bp && typeof bp === "object") {
      for (const [k, val] of Object.entries(bp as Record<string, unknown>)) {
        if (!(k in mergedProperties)) mergedProperties[k] = val;
      }
    }
  }

  const result: Record<string, unknown> = {
    type: "object",
    properties: mergedProperties,
    additionalProperties: true,
  };
  if (discriminantRequired.length > 0) {
    result.required = discriminantRequired;
  }
  // NOTE: do NOT add `anyOf`/`oneOf`/`allOf` here. See the function-level
  // doc comment for why; Anthropic's tool-use API rejects the resulting
  // schema and the whole MCP fails to register.
  return result;
}

/**
 * canonical schema 管道：Zod schema -> MCP 可上线的 inputSchema。
 *
 * 步骤（决策 D3）：
 *   1. zodToJsonSchema(schema, { target: "jsonSchema7" }) — 统一目标，避免
 *      openApi3/jsonSchema7 在 nullable、$schema、literal 编码上的方言漂移。
 *   2. 剥离顶层 `$schema` 键——严格 schema 后端不接受该元键。
 *   3. normalizeMcpInputSchema — 消除顶层 anyOf/oneOf/allOf（Anthropic 硬约束）。
 *
 * 对无顶层 union 的 schema，第 3 步是恒等 pass（原样返回）。
 */
export function schemaFor(schema: ZodTypeAny): Record<string, unknown> {
  const json = zodToJsonSchema(schema, {
    target: "jsonSchema7",
  }) as Record<string, unknown>;
  delete json.$schema;
  return normalizeMcpInputSchema(json);
}

interface HoistedDiscriminants {
  properties: Record<string, unknown>;
  required: string[];
}

/**
 * Scan union branches for discriminant properties — those that carry either
 * `const` or a single-value `enum` in ONE OR MORE branches. Hoist each such
 * property into the parent `properties` map with a unioned `enum` across
 * branches.
 *
 * Full discriminant (appears in EVERY branch): hoisted; added to top-level
 * `required` iff every branch also requires it (BUG-13 Lane A semantics).
 *
 * Partial discriminant (appears in 2+ branches but not all, OR appears in
 * only one branch): hoisted with the unioned enum but NEVER added to
 * top-level `required`. Without this case, a property like `action` that is
 * `const:'add'` in variant 1, `const:'edit'` in variant 2, `const:'remove'`
 * in variant 3 but absent from the apply variant would fall through to the
 * merge loop, get first-wins `{const:'add'}`, and lock OpenAI strict
 * tool-schema validators to that single value (BUG-27 secondary symptom).
 *
 * Returns empty properties + required when no const / single-enum property
 * is found in any branch.
 */
function extractDiscriminants(branches: unknown): HoistedDiscriminants {
  if (!Array.isArray(branches) || branches.length === 0) {
    return { properties: {}, required: [] };
  }

  type Candidate = { values: unknown[]; type: string | undefined };
  const perBranch: Array<Map<string, Candidate>> = [];
  const requiredPerBranch: Array<Set<string>> = [];
  // allPropNames is a Set so we union across branches but preserve the
  // first-seen insertion order — matters for the `required` array's order
  // (existing tests assert ['mode', 'action']).
  const allPropNames = new Set<string>();

  for (const branch of branches) {
    if (!branch || typeof branch !== "object") {
      return { properties: {}, required: [] };
    }
    const b = branch as Record<string, unknown>;
    const props =
      b.properties && typeof b.properties === "object"
        ? (b.properties as Record<string, unknown>)
        : {};
    const req = Array.isArray(b.required) ? (b.required as string[]) : [];
    const map = new Map<string, Candidate>();
    for (const [propName, propSchema] of Object.entries(props)) {
      if (!propSchema || typeof propSchema !== "object") continue;
      const ps = propSchema as Record<string, unknown>;
      let values: unknown[] | undefined;
      if ("const" in ps) {
        values = [ps.const];
      } else if (Array.isArray(ps.enum) && ps.enum.length === 1) {
        values = [ps.enum[0]];
      }
      if (values === undefined) continue;
      map.set(propName, {
        values,
        type: typeof ps.type === "string" ? ps.type : undefined,
      });
      allPropNames.add(propName);
    }
    perBranch.push(map);
    requiredPerBranch.push(new Set(req));
  }

  if (allPropNames.size === 0) {
    return { properties: {}, required: [] };
  }

  const hoistedProperties: Record<string, unknown> = {};
  const hoistedRequired: string[] = [];
  for (const propName of allPropNames) {
    const allValues: unknown[] = [];
    const types = new Set<string>();
    let inEveryBranch = true;
    let requiredInEvery = true;
    for (let i = 0; i < branches.length; i++) {
      const cand = perBranch[i].get(propName);
      if (cand === undefined) {
        inEveryBranch = false;
        requiredInEvery = false;
        continue;
      }
      for (const v of cand.values) {
        if (!allValues.includes(v)) allValues.push(v);
      }
      if (cand.type !== undefined) types.add(cand.type);
      if (!requiredPerBranch[i].has(propName)) requiredInEvery = false;
    }
    const hoistedProp: Record<string, unknown> = {};
    if (types.size === 1) {
      hoistedProp.type = [...types][0];
    }
    hoistedProp.enum = allValues;
    hoistedProperties[propName] = hoistedProp;
    if (inEveryBranch && requiredInEvery) {
      hoistedRequired.push(propName);
    }
  }

  return { properties: hoistedProperties, required: hoistedRequired };
}

/**
 * Recursively flatten union branches. If a branch is itself a JSON Schema
 * union envelope ({anyOf:[...]} / {oneOf:[...]} / {allOf:[...]}), expand
 * its children into the flat branch list. Used by normalizeMcpInputSchema
 * to handle Zod shapes like
 *   z.union([z.discriminatedUnion(...), applyShape])
 * which serialize to nested {anyOf:[{oneOf:[v1,v2,v3]}, applyShape]} via
 * zodToJsonSchema.
 *
 * Recursion is depth-first. The first union keyword found on a branch wins
 * (the loop breaks); JSON Schema is not supposed to mix anyOf/oneOf/allOf
 * on the same node, but if it does we only expand one of them.
 *
 * Edge cases:
 *  - Non-object branches (rare; defensive): preserved as-is.
 *  - Empty `oneOf: []` / `anyOf: []`: contributes nothing to the flat list.
 *  - Branch with BOTH a union keyword AND its own .properties (exotic;
 *    not produced by current Zod-derived shapes in this repo): the union
 *    is expanded and the parent's own properties are dropped. This matches
 *    the BUG-27 fix prompt's policy and is locked in by a regression test
 *    so a future change has to decide deliberately.
 *  - Self-referencing $ref schemas: not followed; we only inspect direct
 *    `anyOf`/`oneOf`/`allOf` arrays on the branch object.
 */
function _flattenUnionBranches(branches: unknown[]): unknown[] {
  const flat: unknown[] = [];
  for (const branch of branches) {
    if (!branch || typeof branch !== "object") {
      flat.push(branch);
      continue;
    }
    const b = branch as Record<string, unknown>;
    let nested: unknown[] | undefined;
    for (const kw of ["anyOf", "oneOf", "allOf"] as const) {
      if (Array.isArray(b[kw])) {
        nested = b[kw] as unknown[];
        break;
      }
    }
    if (nested !== undefined) {
      flat.push(..._flattenUnionBranches(nested));
    } else {
      flat.push(branch);
    }
  }
  return flat;
}
