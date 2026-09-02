"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
var __param = (this && this.__param) || function (paramIndex, decorator) {
    return function (target, key) { decorator(target, key, paramIndex); }
};
var _a, _b, _c, _d;
Object.defineProperty(exports, "__esModule", { value: true });
exports.WTTBot = void 0;
const tsyringe_1 = require("C:/snapshot/project/node_modules/tsyringe");
const MemberCategory_1 = require("C:/snapshot/project/obj/models/enums/MemberCategory");
const MailSendService_1 = require("C:/snapshot/project/obj/services/MailSendService");
const MessageType_1 = require("C:/snapshot/project/obj/models/enums/MessageType");
const HashUtil_1 = require("C:/snapshot/project/obj/utils/HashUtil");
const ItemHelper_1 = require("C:/snapshot/project/obj/helpers/ItemHelper");
const DialogueHelper_1 = require("C:/snapshot/project/obj/helpers/DialogueHelper");
let WTTBot = class WTTBot {
    mailSendService;
    itemHelper;
    hashUtil;
    dialogueHelper;
    constructor(mailSendService, itemHelper, hashUtil, dialogueHelper) {
        this.mailSendService = mailSendService;
        this.itemHelper = itemHelper;
        this.hashUtil = hashUtil;
        this.dialogueHelper = dialogueHelper;
    }
    getChatBot() {
        return {
            _id: "686eea254af06fe1f89b528a",
            aid: 2742069,
            Info: {
                Level: 69,
                MemberCategory: MemberCategory_1.MemberCategory.SYSTEM,
                SelectedMemberCategory: MemberCategory_1.MemberCategory.SYSTEM,
                Nickname: "WTTBot",
                Side: "Usec",
            },
        };
    }
    sendWelcomeGift(sessionId, instanceManager) {
        const hipoint_id = this.hashUtil.generate();
        const items = [
            {
                _id: hipoint_id,
                _tpl: "679f2453d1970258c1df3fce",
                upd: {
                    Repairable: {
                        MaxDurability: 100,
                        Durability: 100
                    },
                    FireMode: {
                        FireMode: "single"
                    },
                    StackObjectsCount: 1
                }
            },
            {
                _id: "679f2637fb1f262b630c4ae6",
                _tpl: "679f24d15ff75ca48f6462a2",
                parentId: hipoint_id,
                slotId: "mod_barrel"
            },
            {
                _id: "679f263450509cd453be31ae",
                _tpl: "679f24e723451339c041f28d",
                parentId: hipoint_id,
                slotId: "mod_reciever"
            },
            {
                _id: "679f263a88a4534ea2c0b400",
                _tpl: "679f24e0434b197ed7a10468",
                parentId: hipoint_id,
                slotId: "mod_pistolgrip"
            },
            {
                _id: "679f26324e0bf19f0e7a6afc",
                _tpl: "679f24edc0aba3ffa139b03f",
                parentId: hipoint_id,
                slotId: "mod_magazine"
            },
            {
                _id: this.hashUtil.generate(),
                _tpl: "679f24edc0aba3ffa139b03f"
            },
            {
                _id: this.hashUtil.generate(),
                _tpl: "679f24edc0aba3ffa139b03f"
            },
            {
                _id: this.hashUtil.generate(),
                _tpl: "657025a4bfc87b3a34093250"
            }
        ];
        const message = `
        [SYSTEM NOTICE]
        -> Status: ACTIVE
            WTT Armory Installation: DETECTED
            Initializing Welcome Protocol...
        -> Status: DEPLOYING...
            Dispensing Complimentary Item...
        -> Status: COMPLETE
            User Input: NOT REQUIRED`;
        const details = {
            recipientId: sessionId,
            sender: MessageType_1.MessageType.MESSAGE_WITH_ITEMS,
            dialogType: MessageType_1.MessageType.USER_MESSAGE,
            senderDetails: this.getChatBot(),
            messageText: message,
            items: items,
            itemsMaxStorageLifetimeSeconds: 172800
        };
        const dialogsInProfile = this.dialogueHelper.getDialogsForProfile(sessionId);
        const senderId = this.getChatBot()._id;
        let senderDialog = dialogsInProfile[senderId];
        if (!senderDialog) {
            dialogsInProfile[senderId] = {
                _id: senderId,
                type: MessageType_1.MessageType.USER_MESSAGE,
                messages: [],
                pinned: false,
                new: 0,
                attachmentsNew: 0,
                Users: [this.getChatBot()],
            };
            senderDialog = dialogsInProfile[senderId];
        }
        this.mailSendService.sendMessageToPlayer(details);
    }
    handleMessage(sessionId, request) {
        const responses = [
            "This is an automated response from WTTBot: WTT is currently delayed due to quantum instability. Estimated release: Thursday. No, not *that* Thursday.",
            "You've reached WTTBot. Unfortunately, the dev team is currently trapped in the Hideout. Please try again next Thursday.",
            "WTT has not yet released. It *will* release Thursday. Trust the process. Fear the Thursday.",
            "WTTBot here. The prophecy foretells of a great release on a Thursday. Which one? That’s classified.",
            "WTT is currently buffering… [==>-------------] 12% complete. Estimated finish: Thursday.",
            "Beep boop. WTTBot online. WTT is offline. Rebooting next Thursday.",
            "Sorry! WTT is locked behind a level 69 questline. You can accept it Thursday.",
            "You’ve messaged WTTBot. Please wait patiently in the Tarkov queue. Estimated wait time: Between 1 and 100 Thursdays.",
            "Thank you for contacting WTTBot Support. All agents are currently AFK waiting for Thursday. A representative will be with you shortly.",
            "This is an automated transmission. WTT is not available right now. It has gone to a farm upstate. It will return Thursday.",
            "WTTBot detected high levels of enthusiasm. Unfortunately, enthusiasm does not correlate with a release date. Check back Thursday.",
            "This is WTTBot. I have consulted the stars, tea leaves, a VHS copy of Back to the Future, and Nikita. WTT releases on a Thursday. Trust me, bro.",
            "You've unlocked a secret message! Too bad it’s just WTTBot telling you WTT is NOT out yet. Come back Thursday.",
            "WTTBot Error: ‘mod.status’ is undefined. Retry again on Thursday.",
            "This is WTTBot. The WTT devs are busy duct taping guns together. Check back Thursday. Any Thursday. Pick one.",
            "WTT is in a Schrödinger's Thursday state. It has both released and not released. You won't know until Thursday.",
            "According to 9 out of 10 PMCs, WTT will release on Thursday. The 10th PMC was shot before answering.",
            "We considered releasing today, but the *very expensive* algorithm we use said 'Try Again Later'. New calculation scheduled: Thursday.",
            "Welcome to WTTBot. The mod is undergoing final extraction. Estimated exfil: Thursday.",
            "All WTT roads lead to Thursday. Don’t ask why. I don't have that clearance........ yet.",
            "This is WTTBot. Please keep all limbs inside the queue until Thursday.",
            "We tried releasing WTT on a Tuesday once. The servers caught fire. Never again... Thursday only.",
            "WTT is currently stuck behind a locked door. We lost the key. Locksmith ETA: Thursday.",
            "WTTBot has received your messages. Your hype level has been noted. Please remain calm until Thursday.",
            "WTTBot processing request… ERROR 404: Release Not Found. Please return Thursday.",
            "You’ve reached the WTT Hotline. All agents are busy pretending it's not Thursday yet.",
            "WTT is currently experiencing a temporal anomaly. Time will stabilize by Thursday.",
        ];
        const randomMessage = responses[Math.floor(Math.random() * responses.length)];
        this.mailSendService.sendUserMessageToPlayer(sessionId, this.getChatBot(), randomMessage);
        return request.dialogId;
    }
};
exports.WTTBot = WTTBot;
exports.WTTBot = WTTBot = __decorate([
    (0, tsyringe_1.injectable)(),
    __param(0, (0, tsyringe_1.inject)("MailSendService")),
    __param(1, (0, tsyringe_1.inject)("ItemHelper")),
    __param(2, (0, tsyringe_1.inject)("HashUtil")),
    __param(3, (0, tsyringe_1.inject)("DialogueHelper")),
    __metadata("design:paramtypes", [typeof (_a = typeof MailSendService_1.MailSendService !== "undefined" && MailSendService_1.MailSendService) === "function" ? _a : Object, typeof (_b = typeof ItemHelper_1.ItemHelper !== "undefined" && ItemHelper_1.ItemHelper) === "function" ? _b : Object, typeof (_c = typeof HashUtil_1.HashUtil !== "undefined" && HashUtil_1.HashUtil) === "function" ? _c : Object, typeof (_d = typeof DialogueHelper_1.DialogueHelper !== "undefined" && DialogueHelper_1.DialogueHelper) === "function" ? _d : Object])
], WTTBot);
//# sourceMappingURL=WTTBot.js.map