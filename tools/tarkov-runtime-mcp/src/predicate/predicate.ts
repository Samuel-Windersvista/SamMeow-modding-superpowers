// =============================================================================
// 谓词引擎（最小语法）
//
// 语法：`<字段路径> <运算符> <值>`，例如：
//   version.core equals 5.0.0
//   capabilities.sections contains server_status
//   version.raw matches ^SPT 5\.0
//   connection.port > 6000
//
// 字段路径为点分路径，支持数组下标段（如 sections.0）。
// 运算符仅含 contains / equals / matches 与数值比较（> >= < <=）；
// 不做通用表达式语言。解析失败返回结构化结果而非抛异常。
// =============================================================================

/** 支持的运算符（最小集） */
export type PredicateOperator = "contains" | "equals" | "matches" | ">" | ">=" | "<" | "<=";

const OPERATORS: readonly PredicateOperator[] = ["contains", "equals", "matches", ">", ">=", "<", "<="];

const NUMERIC_OPERATORS: readonly PredicateOperator[] = [">", ">=", "<", "<="];

/** 字段路径段：非空且不含空白/点号 */
const PATH_SEGMENT = /^[^\s.]+$/;

export interface ParsedPredicate {
  /** 原始表达式 */
  raw: string;
  /** 点分字段路径 */
  path: string;
  op: PredicateOperator;
  /** 运算符后的剩余文本（可含空格） */
  value: string;
  /** matches 预编译正则（仅 op=matches） */
  regex?: RegExp;
  /** 数值比较右值（仅数值运算符） */
  numericValue?: number;
}

export type PredicateParseResult =
  | { ok: true; predicate: ParsedPredicate }
  | { ok: false; reason: string };

function isValidPath(path: string): boolean {
  const segments = path.split(".");
  return segments.length > 0 && segments.every((segment) => PATH_SEGMENT.test(segment));
}

/** 解析谓词表达式；非法语法返回 { ok:false, reason }。 */
export function parsePredicate(raw: string): PredicateParseResult {
  const trimmed = raw.trim();
  const firstSpace = trimmed.search(/\s/);
  if (firstSpace < 0) {
    return { ok: false, reason: `谓词缺少运算符与值：${JSON.stringify(raw)}` };
  }
  const path = trimmed.slice(0, firstSpace);
  const rest = trimmed.slice(firstSpace).trim();
  const secondSpace = rest.search(/\s/);
  if (secondSpace < 0) {
    return { ok: false, reason: `谓词缺少值：${JSON.stringify(raw)}` };
  }
  const op = rest.slice(0, secondSpace) as PredicateOperator;
  const value = rest.slice(secondSpace).trim();

  if (!isValidPath(path)) {
    return { ok: false, reason: `非法字段路径：${JSON.stringify(path)}` };
  }
  if (!OPERATORS.includes(op)) {
    return { ok: false, reason: `未知运算符：${JSON.stringify(op)}` };
  }
  if (!value) {
    return { ok: false, reason: `谓词值为空：${JSON.stringify(raw)}` };
  }

  const predicate: ParsedPredicate = { raw, path, op, value };

  if (op === "matches") {
    try {
      predicate.regex = new RegExp(value);
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      return { ok: false, reason: `非法正则：${message}` };
    }
  }

  if (NUMERIC_OPERATORS.includes(op)) {
    const numericValue = Number(value);
    if (!Number.isFinite(numericValue)) {
      return { ok: false, reason: `数值比较的右值非数值：${JSON.stringify(value)}` };
    }
    predicate.numericValue = numericValue;
  }

  return { ok: true, predicate };
}

// -----------------------------------------------------------------------------
// 求值
// -----------------------------------------------------------------------------

export interface PathResolution {
  found: boolean;
  value: unknown;
}

/** 按点分路径取值；数组段按数字下标索引。路径缺失时 found=false。 */
export function resolvePath(root: unknown, path: string): PathResolution {
  let current: unknown = root;
  for (const segment of path.split(".")) {
    if (current === null || current === undefined) {
      return { found: false, value: undefined };
    }
    if (Array.isArray(current)) {
      const index = Number(segment);
      if (!Number.isInteger(index) || index < 0 || index >= current.length) {
        return { found: false, value: undefined };
      }
      current = current[index];
    } else if (typeof current === "object") {
      if (!Object.prototype.hasOwnProperty.call(current, segment)) {
        return { found: false, value: undefined };
      }
      current = (current as Record<string, unknown>)[segment];
    } else {
      return { found: false, value: undefined };
    }
  }
  return { found: true, value: current };
}

function scalarToString(value: unknown): string {
  if (typeof value === "string") return value;
  if (typeof value === "number" || typeof value === "boolean") return String(value);
  try {
    return JSON.stringify(value) ?? String(value);
  } catch {
    return String(value);
  }
}

function containsValue(actual: unknown, expected: string): boolean {
  if (typeof actual === "string") {
    return actual.includes(expected);
  }
  if (Array.isArray(actual)) {
    return actual.some((item) => scalarToString(item) === expected);
  }
  return false;
}

function equalsValue(actual: unknown, expected: string): boolean {
  if (typeof actual === "number") {
    const numeric = Number(expected);
    return Number.isFinite(numeric) && numeric === actual;
  }
  if (typeof actual === "boolean") {
    return expected === String(actual);
  }
  if (typeof actual === "string") {
    return actual === expected;
  }
  if (actual === null) {
    return expected === "null";
  }
  if (actual === undefined) {
    return expected === "undefined";
  }
  return scalarToString(actual) === expected;
}

function numericValueOf(actual: unknown): number | null {
  if (typeof actual === "number") {
    return Number.isFinite(actual) ? actual : null;
  }
  if (typeof actual === "string" && actual.trim() !== "") {
    const numeric = Number(actual);
    return Number.isFinite(numeric) ? numeric : null;
  }
  return null;
}

function compareNumbers(left: number, op: PredicateOperator, right: number): boolean {
  switch (op) {
    case ">":
      return left > right;
    case ">=":
      return left >= right;
    case "<":
      return left < right;
    case "<=":
      return left <= right;
    default:
      return false;
  }
}

export interface PredicateEvaluation {
  satisfied: boolean;
  /** 字段路径是否解析成功 */
  pathFound: boolean;
  /** 实际观察到的字段值（路径缺失时为 undefined） */
  actual: unknown;
}

/** 对任意对象求值谓词；不抛异常。 */
export function evaluatePredicate(predicate: ParsedPredicate, root: unknown): PredicateEvaluation {
  const { found, value } = resolvePath(root, predicate.path);
  if (!found) {
    return { satisfied: false, pathFound: false, actual: undefined };
  }

  let satisfied: boolean;
  switch (predicate.op) {
    case "contains":
      satisfied = containsValue(value, predicate.value);
      break;
    case "equals":
      satisfied = equalsValue(value, predicate.value);
      break;
    case "matches":
      satisfied = predicate.regex ? predicate.regex.test(scalarToString(value)) : false;
      break;
    default: {
      const left = numericValueOf(value);
      satisfied = left !== null && compareNumbers(left, predicate.op, predicate.numericValue ?? Number.NaN);
      break;
    }
  }
  return { satisfied, pathFound: true, actual: value };
}
