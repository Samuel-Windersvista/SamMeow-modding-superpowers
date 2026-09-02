import { inject, injectable } from "tsyringe";

import { IDialogueChatBot } from "@spt/helpers/Dialogue/IDialogueChatBot";
import { ISendMessageRequest } from "@spt/models/eft/dialog/ISendMessageRequest";
import { IUserDialogInfo } from "@spt/models/eft/profile/IUserDialogInfo";
import { MemberCategory } from "@spt/models/enums/MemberCategory";
import { MailSendService } from "@spt/services/MailSendService";
import { ISendMessageDetails } from "@spt/models/spt/dialog/ISendMessageDetails";
import { MessageType } from "@spt/models/enums/MessageType";
import { HashUtil } from "@spt/utils/HashUtil";
import { ItemHelper } from "@spt/helpers/ItemHelper";
import { IItem } from "@spt/models/eft/common/tables/IItem";
import { WTTInstanceManager } from "../Services/WTTInstanceManager";
import { IDialogue } from "@spt/models/eft/profile/ISptProfile";
import { DialogueHelper } from "@spt/helpers/DialogueHelper";

@injectable()
export class WTTBot implements IDialogueChatBot
{
    constructor(
        @inject("MailSendService") protected mailSendService: MailSendService,
        @inject("ItemHelper") protected itemHelper: ItemHelper,
        @inject("HashUtil") protected hashUtil: HashUtil,
        @inject("DialogueHelper") protected dialogueHelper: DialogueHelper,
    )
    {}

    public getChatBot(): IUserDialogInfo
    {
        return {
            _id: "686eea254af06fe1f89b528a",
            aid: 2742069,
            Info: {
                Level: 69,
                MemberCategory: MemberCategory.SYSTEM,
                SelectedMemberCategory: MemberCategory.SYSTEM,
                Nickname: "WTTBot",
                Side: "Usec",
            },
        };
    }

    public sendWelcomeGift(sessionId: string, instanceManager: WTTInstanceManager): void {

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
        ]
    

        const message = 
        `
        [SYSTEM NOTICE]
        -> Status: ACTIVE
            WTT Armory Installation: DETECTED
            Initializing Welcome Protocol...
        -> Status: DEPLOYING...
            Dispensing Complimentary Item...
        -> Status: COMPLETE
            User Input: NOT REQUIRED`;


        const details: ISendMessageDetails = {
            recipientId: sessionId,
            sender: MessageType.MESSAGE_WITH_ITEMS,
            dialogType: MessageType.USER_MESSAGE,
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
                type: MessageType.USER_MESSAGE,
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

    public handleMessage(sessionId: string, request: ISendMessageRequest): string
    {
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
    
        this.mailSendService.sendUserMessageToPlayer(
            sessionId,
            this.getChatBot(),
            randomMessage
        );
    
        return request.dialogId;
    }
    
}
