/* eslint-disable @typescript-eslint/naming-convention */

import fs, { truncate } from "node:fs";
import path from "node:path";
import type { DependencyContainer } from "tsyringe";
import type { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import type { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";
import { LogTextColor } from "@spt/models/spt/logging/LogTextColor";
import { WTTInstanceManager } from "./Services/WTTInstanceManager";
import { CustomItemService } from "./Services/CustomItemService";
import { CustomAssortSchemeService } from "./Services/CustomAssortSchemeService";
import { QuestAPI } from "./Services/QuestAPI";
import { CustomLootspawnService } from "./Services/CustomLootspawnService";
import { CustomBotLoadoutService } from "./Services/CustomBotLoadoutService";
import { ArmoryQuestHelper } from "./Services/ArmoryQuestHelper";
import { WTTBot } from "./ChatBot/WTTBot";
import { DialogueController } from "@spt/controllers/DialogueController";
import { ConfigServer } from "@spt/servers/ConfigServer";
import { ICoreConfig } from "@spt/models/spt/config/ICoreConfig";
import { ConfigTypes } from "@spt/models/enums/ConfigTypes";

class WTTArmory
implements IPreSptLoadMod, IPostDBLoadMod
{
    private instanceManager: WTTInstanceManager = new WTTInstanceManager();
    private modName = "WTT-Armory";
    private customItemService: CustomItemService = new CustomItemService();
    private customAssortSchemeService: CustomAssortSchemeService = new CustomAssortSchemeService();
    private questAPI: QuestAPI = new QuestAPI();
    private customLootspawnService: CustomLootspawnService = new CustomLootspawnService();
    private customBotLoadoutService: CustomBotLoadoutService = new CustomBotLoadoutService();
    private armoryQuestHelper: ArmoryQuestHelper = new ArmoryQuestHelper();
    debug = false;

    // Anything that needs done on preSptLoad, place here.
    public preSptLoad(container: DependencyContainer): void 
    {
        this.instanceManager.preSptLoad(container, this.modName);
        this.instanceManager.debug = this.debug;
    
        this.displayCreditBanner();
    
        this.customItemService.preSptLoad(this.instanceManager);
        this.customAssortSchemeService.preSptLoad(this.instanceManager);
        this.questAPI.preSptLoad(this.instanceManager);
        this.customLootspawnService.preSptLoad(this.instanceManager);
        this.customBotLoadoutService.preSptLoad(this.instanceManager);
        this.armoryQuestHelper.preSptLoad(this.instanceManager);
    
        // Chatbot
        container.register<WTTBot>("WTTBot", WTTBot);
        const wttBot = container.resolve<WTTBot>("WTTBot");
    
        // Hook up to friend/list route to send welcome gift
        this.instanceManager.staticRouter.registerStaticRouter(
            "WTTBotWelcomeGiftPeeking",
            [
                {
                    url: "/client/friend/list",
                    action: async (url, info, sessionId, output) =>
                    {
                        const profile = this.instanceManager.profileHelper.getProfileByPmcId(sessionId);
                        if (profile["WTT"] == null)
                        {
                            profile["WTT"] = {};
                            profile["WTT"]["HasReceivedWelcomeGift"] = false;
                            wttBot.sendWelcomeGift(sessionId, this.instanceManager);
                            profile["WTT"]["HasReceivedWelcomeGift"] = true;
                        }
                        else
                        {
                            if (!profile["WTT"]["HasReceivedWelcomeGift"])
                            {
                                wttBot.sendWelcomeGift(sessionId, this.instanceManager); 
                                profile["WTT"]["HasReceivedWelcomeGift"] = true;
                            }
                        }
                        return output;
                    }
                }
            ],
            "spt"
        );
    }
    

    // Anything that needs done on postDBLoad, place here.
    public postDBLoad(container: DependencyContainer): void 
    {
        this.instanceManager.postDBLoad(container);
    
        // Chatbot
        const myChatBot = container.resolve<WTTBot>("WTTBot");
        container.resolve<DialogueController>("DialogueController").registerChatBot(myChatBot);
        const coreConfig = container.resolve<ConfigServer>("ConfigServer").getConfig<ICoreConfig>(ConfigTypes.CORE);
        const myChatBotInfo = myChatBot.getChatBot();
        coreConfig.features.chatbotFeatures.ids[myChatBotInfo.Info.Nickname] = myChatBotInfo._id;
        coreConfig.features.chatbotFeatures.enabledBots[myChatBotInfo._id] = true;
    

        // Services
        this.customItemService.postDBLoad();
        this.customAssortSchemeService.postDBLoad();
        this.questAPI.postDBLoad();
        this.customLootspawnService.postDBLoad();
        this.customBotLoadoutService.postDBLoad();
        this.armoryQuestHelper.postDBLoad();
    

        // Locales
        this.handleLocales();
        
    }

    private handleLocales(): void 
    {

        const locales = this.instanceManager.database.locales.global;
        const WTTLocalesDir = path.join(__dirname, "..", "db", "locales");
        
        const WTTLocales: Record<string, Record<string, string>> = {};
        const WTTlocaleFiles = fs.readdirSync(WTTLocalesDir);
        
        for (const file of WTTlocaleFiles) {
            if (!file.endsWith(".json")) continue;
        
            const localeCode = path.basename(file, ".json");
            const filePath = path.join(WTTLocalesDir, file);
        
            try {
                const data = JSON.parse(fs.readFileSync(filePath, "utf-8"));
                WTTLocales[localeCode] = data;
            } catch (err) {
                console.warn(`Failed to parse ${file}:`, err);
            }
        }
        
        const fallback = WTTLocales["en"] ?? {};
        
        for (const locale of Object.keys(locales)) {
            const customLocale = WTTLocales[locale] ?? fallback;
        
            for (const [key, value] of Object.entries(customLocale)) {
                locales[locale][key] = value;
            }
        }
    }
    


    private displayCreditBanner(): void 
    {
        this.instanceManager.colorLog
        (`[${this.modName}] Forged by the creators of WTT & allies — arm yourself accordingly.`, "green");
    }   
}

module.exports = { mod: new WTTArmory() };
