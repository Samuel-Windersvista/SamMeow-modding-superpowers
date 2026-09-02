"use strict";
/**
 * 3.11 行为验证工具 mod：dump 关键数据库表到 JSON 文件。
 * 直接 JS 版（无 TS 依赖，3.11 运行时即 CommonJS JS）。
 * 作为 3.11->4.1 迁移的行为预期基准。
 *
 * 输出：<SPT>/user/mods/SptDbDump311/dump/<table>.json
 */
const fs = require("node:fs");
const path = require("node:path");

class SptDbDump311 {
    constructor() {
        this.modName = "SptDbDump311";
    }

    postDBLoad(container) {
        const logger = container.resolve("WinstonLogger");
        const databaseServer = container.resolve("DatabaseServer");
        const database = databaseServer.getTables();

        const dumpDir = path.join(process.cwd(), "user", "mods", this.modName, "dump");
        fs.mkdirSync(dumpDir, { recursive: true });

        logger.info("SptDbDump311: dumping key tables...");

        const tables = {
            templateItems: database?.templates?.items,
            templateQuests: database?.templates?.quests,
            templateHandbook: database?.templates?.handbook,
            traders: database?.traders,
            globalsConfig: database?.globals?.config,
            locales: database?.locales?.global,
        };

        for (const [name, data] of Object.entries(tables)) {
            try {
                const json = JSON.stringify(data, null, 2);
                const filePath = path.join(dumpDir, `${name}.json`);
                fs.writeFileSync(filePath, json, "utf8");
                logger.info(`  dumped ${name}.json (${Math.round(json.length / 1024)} KB)`);
            } catch (err) {
                logger.error(`  FAILED ${name}: ${err.message}`);
            }
        }

        logger.info("SptDbDump311: dump complete -> " + dumpDir);
    }
}

module.exports = { mod: new SptDbDump311() };
