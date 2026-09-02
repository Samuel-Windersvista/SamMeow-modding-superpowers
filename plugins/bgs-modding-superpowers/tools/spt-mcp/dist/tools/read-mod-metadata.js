import { z } from "zod";
import { readModMetadata } from "../mod-reader.js";
import { errEnv, okEnv, SPT_ERROR_CODES } from "../types.js";
export const ReadModMetadataInput = z
    .object({
    modPath: z.string().min(1).describe("mod 目录绝对路径（server）或 DLL 路径（client）；client 传目录时取第一个 DLL"),
    type: z.enum(["server", "client"]).describe("server = 读 package.json；client = DLL 文件名派生"),
})
    .strict();
export function runReadModMetadata(args) {
    const parsed = ReadModMetadataInput.safeParse(args);
    if (!parsed.success) {
        return errEnv("spt_read_mod_metadata", "无效输入", SPT_ERROR_CODES.INVALID_INPUT, parsed.error.message);
    }
    const { modPath, type } = parsed.data;
    try {
        const meta = readModMetadata(modPath, type);
        if (!meta) {
            return errEnv("spt_read_mod_metadata", `未找到 ${type} mod 元数据：${modPath}`, SPT_ERROR_CODES.NOT_FOUND, type === "server"
                ? "目录下缺少可解析的 package.json"
                : "路径不是 .dll 文件且目录内没有 .dll");
        }
        return okEnv("spt_read_mod_metadata", `读取 ${meta.name} 元数据成功`, { mod: meta });
    }
    catch (error) {
        return errEnv("spt_read_mod_metadata", `读取失败：${error instanceof Error ? error.message : String(error)}`, SPT_ERROR_CODES.INTERNAL_ERROR);
    }
}
//# sourceMappingURL=read-mod-metadata.js.map