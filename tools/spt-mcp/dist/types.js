// =============================================================================
// spt-mcp 类型定义
// =============================================================================
export const SPT_ERROR_CODES = {
    INVALID_INPUT: "invalid_input",
    NOT_FOUND: "not_found",
    INTERNAL_ERROR: "internal_error",
    INVALID_REQUEST: "invalid_request",
};
export function okEnv(tool, summary, data) {
    return { ok: true, tool, summary, data };
}
export function errEnv(tool, summary, code, hint) {
    return { ok: false, tool, summary, code, hint };
}
//# sourceMappingURL=types.js.map