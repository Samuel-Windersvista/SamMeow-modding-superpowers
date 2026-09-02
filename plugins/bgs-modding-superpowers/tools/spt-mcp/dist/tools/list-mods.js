import { z } from "zod";
import { listClientMods, listServerMods } from "../mod-reader.js";
import { errEnv, okEnv, SPT_ERROR_CODES } from "../types.js";
export const ListModsInput = z
    .object({
    path: z.string().min(1).describe("要扫描的目录绝对路径"),
    type: z.enum(["server", "client"]).describe("server = 扫描含 package.json 的子目录；client = 递归扫描 .dll 文件"),
})
    .strict();
export function runListMods(args) {
    const parsed = ListModsInput.safeParse(args);
    if (!parsed.success) {
        return errEnv("spt_list_mods", "无效输入", SPT_ERROR_CODES.INVALID_INPUT, parsed.error.message);
    }
    const { path, type } = parsed.data;
    try {
        const mods = type === "server" ? listServerMods(path) : listClientMods(path);
        return okEnv("spt_list_mods", `找到 ${mods.length} 个 ${type} mod`, { mods });
    }
    catch (error) {
        return errEnv("spt_list_mods", `扫描失败：${error instanceof Error ? error.message : String(error)}`, SPT_ERROR_CODES.INTERNAL_ERROR);
    }
}
//# sourceMappingURL=list-mods.js.map