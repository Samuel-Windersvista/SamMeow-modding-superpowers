import type { DependencyContainer } from "tsyringe";
import type { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import type { IDatabaseTables } from "@spt/models/spt/server/IDatabaseTables";
import type { ILogger } from "@spt/models/spt/utils/ILogger";
import type { DatabaseServer } from "@spt/servers/DatabaseServer";
import * as fs from "node:fs";
import * as path from "node:path";

/**
 * 3.11 行为验证工具 mod：dump 关键数据库表到 JSON 文件。
 * 作为 3.11->4.1 迁移的行为预期基准（与 4.1 的 SptDbDump mod 对应）。
 *
 * 输出：<SPT>/user/mods/SptDbDump311/dump/<table>.json
 * 用法：
 *   1. 干净 3.11 启动一次 -> baseline
 *   2. 带业务 mod 启动一次 -> with-mod
 *   3. 与 4.1 侧 diff 对照，判断迁移后行为是否等价
 */
class SptDbDump311 implements IPostDBLoadMod {
    private modName = "SptDbDump311";

    public postDBLoad(container: DependencyContainer): void {
        const logger = container.resolve<ILogger>("WinstonLogger");
        const databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
        const database: IDatabaseTables = databaseServer.getTables();

        const dumpDir = path.join(process.cwd(), "user", "mods", this.modName, "dump");
        fs.mkdirSync(dumpDir, { recursive: true });

        logger.info("SptDbDump311: dumping key tables...");

        const tables: Record<string, unknown> = {
            templateItems: database.templates?.items,
            templateQuests: database.templates?.quests,
            templateHandbook: database.templates?.handbook,
            traders: database.traders,
            globalsConfig: database.globals?.config,
            locales: database.locales?.global,
        };

        for (const [name, data] of Object.entries(tables)) {
            try {
                const json = JSON.stringify(data, null, 2);
                const filePath = path.join(dumpDir, `${name}.json`);
                fs.writeFileSync(filePath, json, "utf8");
                logger.info(`  dumped ${name}.json (${Math.round(json.length / 1024)} KB)`);
            } catch (err) {
                logger.error(`  FAILED ${name}: ${err}`);
            }
        }

        logger.info("SptDbDump311: dump complete -> " + dumpDir);
    }
}

module.exports = { mod: new SptDbDump311() };
