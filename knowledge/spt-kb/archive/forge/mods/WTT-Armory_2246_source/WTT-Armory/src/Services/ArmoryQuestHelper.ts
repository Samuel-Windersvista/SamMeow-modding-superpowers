/* eslint-disable @typescript-eslint/naming-convention */
import type { WTTInstanceManager } from "./WTTInstanceManager";

export class ArmoryQuestHelper {
    private instanceManager: WTTInstanceManager;

    public preSptLoad(instanceManager: WTTInstanceManager): void {
        this.instanceManager = instanceManager;
    }

    public postDBLoad(): void {
        this.modifyQuests();
    }

    private modifyQuests(): void {
        const quests = this.instanceManager.database.templates.quests;

        // Define weapon IDs
        const BROWNING_AUTO5 = "669fca7f9ed4916116c76d5e";
        const BROWNING_AUTO5_BOSS = "6840ebf5b8687ba34f8dfbca";
        const SERBU_SUPER_SHORTY = "6761b213607f9a6f79017d23";
        const SVD_DRAGUNOV = "6657bc8faeddd6b0a9b40224";
        const SVD_DRAGUNOV_GREEN = "6657bd4d3a4d6e7c33fd2fdc";
        const BERETTA_92FS = "6868d249cdee524f8c0ba45f";
        const CZ75B = "6661012d16fbd2fb75408f87";
        const CZ75B_OMEGA = "67748ec2e6a045dc97e2f978";
        const HI_POINT_C9 = "679f2453d1970258c1df3fce";
        const HK_UCP = "68433b58a8f9a618b11082d4";
        const LAR_GRIZZLY_MK5 = "677c9a47baecf3c4b2453365";
        const LAR_GRIZZLY_MK5_GOLD = "677ca3e62e9e964a11a55d8e";
        const LAR_GRIZZLY_MK5_STAINLESS = "677ca334da2787e0538c882d";
        const ARMORY_PRODIGY = "665fe0e865683281eb8e7ed6";
        const SR2_UDAV = "68677d09339b397ed3d37522";
        const STACCATO_XC = "68452c3da87156b67d9ec538";
        const WILSON_COMBAT_EDC_X9 = "6761b213607f9a6f79017d52";
        const CZ_SCORPION_EVO = "687afda52dc9fd6c0e14c602";
        const MINEBEA_PM9 = "6761b213607f9a6f79017cd8";
        const AEK971 = "0cb4a36dd2e587b46e813dbe";
        const AEK973 = "686093d590c3dce07984c38a";
        const BOFORS_AK5C = "6671ebcdd32bd95eb398e920";
        const BOFORS_AK5D = "682d3dd16900cb35564c8825";
        const AN94 = "678fe4a4906c7bd23722c71f";
        const AN94_KIROV = "679a6a534f3d279c99b135b9";
        const HK417 = "664a5b945636ce820472f225";
        const HK_G3 = "664274a4d2e5fe0439d545a6";
        const MK23 = "68b7f4060a4536984f82cf4b";
        const AG042 = "68c23b960f579b9b5fe021e7";
        const AG043 = "68cf56067ff6ceab0c2fd49e";
        const HK_XM8 = "66b1770c5f8b2271bb5887dc";
        const IWI_CARMEL = "66ba249b102a9dd6040a6e7e";
        const IWI_CARMEL_FDE = "66ba26a6925f9921573224c9";
        const IWI_TAVOR_X95 = "66a47e98c486ec9d1af3a4da";
        const IWI_TAVOR_X95_FDE = "66a544c956621d3364f6085e";
        const KNIGHTS_ARMAMENT_PDW = "6761b213607f9a6f79017c7e";
        const PATRIOT = "66839591f4d0cba7b041b2af";
        const REMINGTON_ACR = "67a01e4ea2b82626b73d10a3";
        const REMINGTON_ACR_FDE = "67a01e4ea2b82626b73d10a4";
        const WAGES_OF_SIN_AR15 = "676b4e2ff185a450a0b300b4";
        const WAGES_OF_SIN_AR15_RED = "6761b213607f9a6f79017d40";
        const WAGES_OF_SIN_AR15_FDE = "676c3cac01023283e5f6a562";
        const ZASTAVA_M76 = "66e88596febdcf9daade16a8";
        const REMINGTON_MSR = "684e32eaec9f5eb3cacc7ca7";
        const M700_XL_ACTION = "deee28079e76d537f532021c";
        const M700_XL_ACTION_NORMA = "1bf618e47cce6d69bec01e9f";
        const AXMC_300_WINMAG = "68a3836826dffa87b5767c04";


        // ====================== PRAPOR QUESTS ======================

        // Punisher Part 4 (59ca264786f77445a80ed044)
        const punisher4 = quests["59ca264786f77445a80ed044"];
        if (punisher4?.conditions?.AvailableForFinish) {
            for (const condition of punisher4.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(BROWNING_AUTO5)) counterCond.weapon.push(BROWNING_AUTO5);
                            if (!counterCond.weapon.includes(BROWNING_AUTO5_BOSS)) counterCond.weapon.push(BROWNING_AUTO5_BOSS);
                            if (!counterCond.weapon.includes(SERBU_SUPER_SHORTY)) counterCond.weapon.push(SERBU_SUPER_SHORTY);
                        }
                    }
                }
            }
        }

        // Punisher Part 6 (59ca2eb686f77445a80ed049)
        const punisher6 = quests["59ca2eb686f77445a80ed049"];
        if (punisher6?.conditions?.AvailableForFinish) {
            for (const condition of punisher6.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(SVD_DRAGUNOV)) counterCond.weapon.push(SVD_DRAGUNOV);
                            if (!counterCond.weapon.includes(SVD_DRAGUNOV_GREEN)) counterCond.weapon.push(SVD_DRAGUNOV_GREEN);
                        }
                    }
                }
            }
        }

        // Mall Cop (64e7b99017ab941a6f7bf9d7)
        const mallCop = quests["64e7b99017ab941a6f7bf9d7"];
        if (mallCop?.conditions?.AvailableForFinish) {
            for (const condition of mallCop.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(BERETTA_92FS)) counterCond.weapon.push(BERETTA_92FS);
                            if (!counterCond.weapon.includes(CZ75B)) counterCond.weapon.push(CZ75B);
                            if (!counterCond.weapon.includes(CZ75B_OMEGA)) counterCond.weapon.push(CZ75B_OMEGA);
                            if (!counterCond.weapon.includes(HI_POINT_C9)) counterCond.weapon.push(HI_POINT_C9);
                            if (!counterCond.weapon.includes(HK_UCP)) counterCond.weapon.push(HK_UCP);
                            if (!counterCond.weapon.includes(LAR_GRIZZLY_MK5)) counterCond.weapon.push(LAR_GRIZZLY_MK5);
                            if (!counterCond.weapon.includes(LAR_GRIZZLY_MK5_GOLD)) counterCond.weapon.push(LAR_GRIZZLY_MK5_GOLD);
                            if (!counterCond.weapon.includes(LAR_GRIZZLY_MK5_STAINLESS)) counterCond.weapon.push(LAR_GRIZZLY_MK5_STAINLESS);
                            if (!counterCond.weapon.includes(ARMORY_PRODIGY)) counterCond.weapon.push(ARMORY_PRODIGY);
                            if (!counterCond.weapon.includes(SR2_UDAV)) counterCond.weapon.push(SR2_UDAV);
                            if (!counterCond.weapon.includes(STACCATO_XC)) counterCond.weapon.push(STACCATO_XC);
                            if (!counterCond.weapon.includes(WILSON_COMBAT_EDC_X9)) counterCond.weapon.push(WILSON_COMBAT_EDC_X9);
                            if (!counterCond.weapon.includes(MK23)) counterCond.weapon.push(MK23);
                        }
                    }
                }
            }
        }

        // Tickets, Please (64e7b9a4aac4cd0a726562cb)
        const ticketsPlease = quests["64e7b9a4aac4cd0a726562cb"];
        if (ticketsPlease?.conditions?.AvailableForFinish) {
            for (const condition of ticketsPlease.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(CZ_SCORPION_EVO)) counterCond.weapon.push(CZ_SCORPION_EVO);
                            if (!counterCond.weapon.includes(MINEBEA_PM9)) counterCond.weapon.push(MINEBEA_PM9);
                        }
                    }
                }
            }
        }

        // District Patrol (64e7b9bffd30422ed03dad38)
        const districtPatrol = quests["64e7b9bffd30422ed03dad38"];
        if (districtPatrol?.conditions?.AvailableForFinish) {
            for (const condition of districtPatrol.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(AEK971)) counterCond.weapon.push(AEK971);
                            if (!counterCond.weapon.includes(AEK973)) counterCond.weapon.push(AEK973);
                            if (!counterCond.weapon.includes(BOFORS_AK5C)) counterCond.weapon.push(BOFORS_AK5C);
                            if (!counterCond.weapon.includes(BOFORS_AK5D)) counterCond.weapon.push(BOFORS_AK5D);
                            if (!counterCond.weapon.includes(AN94)) counterCond.weapon.push(AN94);
                            if (!counterCond.weapon.includes(AN94_KIROV)) counterCond.weapon.push(AN94_KIROV);
                            if (!counterCond.weapon.includes(HK417)) counterCond.weapon.push(HK417);
                            if (!counterCond.weapon.includes(HK_G3)) counterCond.weapon.push(HK_G3);
                            if (!counterCond.weapon.includes(HK_XM8)) counterCond.weapon.push(HK_XM8);
                            if (!counterCond.weapon.includes(IWI_CARMEL)) counterCond.weapon.push(IWI_CARMEL);
                            if (!counterCond.weapon.includes(IWI_CARMEL_FDE)) counterCond.weapon.push(IWI_CARMEL_FDE);
                            if (!counterCond.weapon.includes(IWI_TAVOR_X95)) counterCond.weapon.push(IWI_TAVOR_X95);
                            if (!counterCond.weapon.includes(IWI_TAVOR_X95_FDE)) counterCond.weapon.push(IWI_TAVOR_X95_FDE);
                            if (!counterCond.weapon.includes(KNIGHTS_ARMAMENT_PDW)) counterCond.weapon.push(KNIGHTS_ARMAMENT_PDW);
                            if (!counterCond.weapon.includes(PATRIOT)) counterCond.weapon.push(PATRIOT);
                            if (!counterCond.weapon.includes(REMINGTON_ACR)) counterCond.weapon.push(REMINGTON_ACR);
                            if (!counterCond.weapon.includes(REMINGTON_ACR_FDE)) counterCond.weapon.push(REMINGTON_ACR_FDE);
                            if (!counterCond.weapon.includes(WAGES_OF_SIN_AR15)) counterCond.weapon.push(WAGES_OF_SIN_AR15);
                            if (!counterCond.weapon.includes(WAGES_OF_SIN_AR15_FDE)) counterCond.weapon.push(WAGES_OF_SIN_AR15_FDE);
                            if (!counterCond.weapon.includes(WAGES_OF_SIN_AR15_RED)) counterCond.weapon.push(WAGES_OF_SIN_AR15_RED);
                            if (!counterCond.weapon.includes(ZASTAVA_M76)) counterCond.weapon.push(ZASTAVA_M76);
                            if (!counterCond.weapon.includes(SVD_DRAGUNOV)) counterCond.weapon.push(SVD_DRAGUNOV);
                            if (!counterCond.weapon.includes(SVD_DRAGUNOV_GREEN)) counterCond.weapon.push(SVD_DRAGUNOV_GREEN);
                            if (!counterCond.weapon.includes(AG042)) counterCond.weapon.push(AG042);
                            if (!counterCond.weapon.includes(AG043)) counterCond.weapon.push(AG043);
                        }
                    }
                }
            }
        }

        // ====================== SKIER QUESTS ======================

        // Stirrup (596b455186f77457cb50eccb)
        const stirrup = quests["596b455186f77457cb50eccb"];
        if (stirrup?.conditions?.AvailableForFinish) {
            for (const condition of stirrup.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(BERETTA_92FS)) counterCond.weapon.push(BERETTA_92FS);
                            if (!counterCond.weapon.includes(CZ75B)) counterCond.weapon.push(CZ75B);
                            if (!counterCond.weapon.includes(CZ75B_OMEGA)) counterCond.weapon.push(CZ75B_OMEGA);
                            if (!counterCond.weapon.includes(HI_POINT_C9)) counterCond.weapon.push(HI_POINT_C9);
                            if (!counterCond.weapon.includes(HK_UCP)) counterCond.weapon.push(HK_UCP);
                            if (!counterCond.weapon.includes(LAR_GRIZZLY_MK5)) counterCond.weapon.push(LAR_GRIZZLY_MK5);
                            if (!counterCond.weapon.includes(LAR_GRIZZLY_MK5_GOLD)) counterCond.weapon.push(LAR_GRIZZLY_MK5_GOLD);
                            if (!counterCond.weapon.includes(LAR_GRIZZLY_MK5_STAINLESS)) counterCond.weapon.push(LAR_GRIZZLY_MK5_STAINLESS);
                            if (!counterCond.weapon.includes(ARMORY_PRODIGY)) counterCond.weapon.push(ARMORY_PRODIGY);
                            if (!counterCond.weapon.includes(SR2_UDAV)) counterCond.weapon.push(SR2_UDAV);
                            if (!counterCond.weapon.includes(STACCATO_XC)) counterCond.weapon.push(STACCATO_XC);
                            if (!counterCond.weapon.includes(WILSON_COMBAT_EDC_X9)) counterCond.weapon.push(WILSON_COMBAT_EDC_X9);
                            if (!counterCond.weapon.includes(MK23)) counterCond.weapon.push(MK23);
                        }
                    }
                }
            }
        }

        // Silent Caliber (5c0bc91486f7746ab41857a2)
        const silentCaliber = quests["5c0bc91486f7746ab41857a2"];
        if (silentCaliber?.conditions?.AvailableForFinish) {
            for (const condition of silentCaliber.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(BROWNING_AUTO5)) counterCond.weapon.push(BROWNING_AUTO5);
                            if (!counterCond.weapon.includes(BROWNING_AUTO5_BOSS)) counterCond.weapon.push(BROWNING_AUTO5_BOSS);
                            if (!counterCond.weapon.includes(SERBU_SUPER_SHORTY)) counterCond.weapon.push(SERBU_SUPER_SHORTY);
                        }
                    }
                }
            }
        }

        // Setup (5c1234c286f77406fa13baeb)
        const setup = quests["5c1234c286f77406fa13baeb"];
        if (setup?.conditions?.AvailableForFinish) {
            for (const condition of setup.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(BROWNING_AUTO5)) counterCond.weapon.push(BROWNING_AUTO5);
                            if (!counterCond.weapon.includes(BROWNING_AUTO5_BOSS)) counterCond.weapon.push(BROWNING_AUTO5_BOSS);
                            if (!counterCond.weapon.includes(SERBU_SUPER_SHORTY)) counterCond.weapon.push(SERBU_SUPER_SHORTY);
                        }
                    }
                }
            }
        }

        // Connections Up North (6764174c86addd02bc033d68)
        const connections = quests["6764174c86addd02bc033d68"];
        if (connections?.conditions?.AvailableForFinish) {
            for (const condition of connections.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(REMINGTON_MSR)) counterCond.weapon.push(REMINGTON_MSR);
                            if (!counterCond.weapon.includes(M700_XL_ACTION)) counterCond.weapon.push(M700_XL_ACTION);
                            if (!counterCond.weapon.includes(M700_XL_ACTION_NORMA)) counterCond.weapon.push(M700_XL_ACTION_NORMA);
                            if (!counterCond.weapon.includes(AXMC_300_WINMAG)) counterCond.weapon.push(AXMC_300_WINMAG);

                        }
                    }
                }
            }
        }

        // ====================== PEACEKEEPER QUESTS ======================

        // Spa Tour Part 1 (5a03153686f77442d90e2171)
        const spaTour = quests["5a03153686f77442d90e2171"];
        if (spaTour?.conditions?.AvailableForFinish) {
            for (const condition of spaTour.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(BROWNING_AUTO5)) counterCond.weapon.push(BROWNING_AUTO5);
                            if (!counterCond.weapon.includes(BROWNING_AUTO5_BOSS)) counterCond.weapon.push(BROWNING_AUTO5_BOSS);
                            if (!counterCond.weapon.includes(SERBU_SUPER_SHORTY)) counterCond.weapon.push(SERBU_SUPER_SHORTY);
                        }
                    }
                }
            }
        }

        // Worst Job (63a9b229813bba58a50c9ee5)
        const worstJob = quests["63a9b229813bba58a50c9ee5"];
        if (worstJob?.conditions?.AvailableForFinish) {
            for (const condition of worstJob.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(WAGES_OF_SIN_AR15)) counterCond.weapon.push(WAGES_OF_SIN_AR15);
                            if (!counterCond.weapon.includes(WAGES_OF_SIN_AR15_RED)) counterCond.weapon.push(WAGES_OF_SIN_AR15_RED);
                            if (!counterCond.weapon.includes(WAGES_OF_SIN_AR15_FDE)) counterCond.weapon.push(WAGES_OF_SIN_AR15_FDE);
                        }
                    }
                }
            }
        }

        // ====================== JAEGER QUESTS ======================

        // Tarkov Shooter Part 1 (5bc4776586f774512d07cf05)
        const tsPart1 = quests["5bc4776586f774512d07cf05"];
        if (tsPart1?.conditions?.AvailableForFinish) {
            for (const condition of tsPart1.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(REMINGTON_MSR)) counterCond.weapon.push(REMINGTON_MSR);
                            if (!counterCond.weapon.includes(M700_XL_ACTION)) counterCond.weapon.push(M700_XL_ACTION);
                            if (!counterCond.weapon.includes(M700_XL_ACTION_NORMA)) counterCond.weapon.push(M700_XL_ACTION_NORMA);
                            if (!counterCond.weapon.includes(AXMC_300_WINMAG)) counterCond.weapon.push(AXMC_300_WINMAG);


                        }
                    }
                }
            }
        }
        // Tarkov Shooter Part 2 (5bc479e586f7747f376c7da3)
        const tsPart2 = quests["5bc479e586f7747f376c7da3"];
        if (tsPart2?.conditions?.AvailableForFinish) {
            for (const condition of tsPart2.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(REMINGTON_MSR)) counterCond.weapon.push(REMINGTON_MSR);
                            if (!counterCond.weapon.includes(M700_XL_ACTION)) counterCond.weapon.push(M700_XL_ACTION);
                            if (!counterCond.weapon.includes(M700_XL_ACTION_NORMA)) counterCond.weapon.push(M700_XL_ACTION_NORMA);
                            if (!counterCond.weapon.includes(AXMC_300_WINMAG)) counterCond.weapon.push(AXMC_300_WINMAG);


                        }
                    }
                }
            }
        }
        // Tarkov Shooter Part 3 (5bc47dbf86f7741ee74e93b9)
        const tsPart3 = quests["5bc47dbf86f7741ee74e93b9"];
        if (tsPart3?.conditions?.AvailableForFinish) {
            for (const condition of tsPart3.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(REMINGTON_MSR)) counterCond.weapon.push(REMINGTON_MSR);
                            if (!counterCond.weapon.includes(M700_XL_ACTION)) counterCond.weapon.push(M700_XL_ACTION);
                            if (!counterCond.weapon.includes(M700_XL_ACTION_NORMA)) counterCond.weapon.push(M700_XL_ACTION_NORMA);
                            if (!counterCond.weapon.includes(AXMC_300_WINMAG)) counterCond.weapon.push(AXMC_300_WINMAG);


                        }
                    }
                }
            }
        }
        // Tarkov Shooter Part 4 (5bc480a686f7741af0342e29)
        const tsPart4 = quests["5bc480a686f7741af0342e29"];
        if (tsPart4?.conditions?.AvailableForFinish) {
            for (const condition of tsPart4.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(REMINGTON_MSR)) counterCond.weapon.push(REMINGTON_MSR);
                            if (!counterCond.weapon.includes(M700_XL_ACTION)) counterCond.weapon.push(M700_XL_ACTION);
                            if (!counterCond.weapon.includes(M700_XL_ACTION_NORMA)) counterCond.weapon.push(M700_XL_ACTION_NORMA);
                            if (!counterCond.weapon.includes(AXMC_300_WINMAG)) counterCond.weapon.push(AXMC_300_WINMAG);

                        }
                    }
                }
            }
        }
        // Tarkov Shooter Part 5 (5bc4826c86f774106d22d88b)
        const tsPart5 = quests["5bc4826c86f774106d22d88b"];
        if (tsPart5?.conditions?.AvailableForFinish) {
            for (const condition of tsPart5.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(REMINGTON_MSR)) counterCond.weapon.push(REMINGTON_MSR);
                            if (!counterCond.weapon.includes(M700_XL_ACTION)) counterCond.weapon.push(M700_XL_ACTION);
                            if (!counterCond.weapon.includes(M700_XL_ACTION_NORMA)) counterCond.weapon.push(M700_XL_ACTION_NORMA);
                            if (!counterCond.weapon.includes(AXMC_300_WINMAG)) counterCond.weapon.push(AXMC_300_WINMAG);


                        }
                    }
                }
            }
        }
        // Tarkov Shooter Part 6 (5bc4836986f7740c0152911c)
        const tsPart6 = quests["5bc4836986f7740c0152911c"];
        if (tsPart6?.conditions?.AvailableForFinish) {
            for (const condition of tsPart6.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(REMINGTON_MSR)) counterCond.weapon.push(REMINGTON_MSR);
                            if (!counterCond.weapon.includes(M700_XL_ACTION)) counterCond.weapon.push(M700_XL_ACTION);
                            if (!counterCond.weapon.includes(M700_XL_ACTION_NORMA)) counterCond.weapon.push(M700_XL_ACTION_NORMA);
                            if (!counterCond.weapon.includes(AXMC_300_WINMAG)) counterCond.weapon.push(AXMC_300_WINMAG);


                        }
                    }
                }
            }
        }
        // Tarkov Shooter Part 7 (5bc4856986f77454c317bea7)
        const tsPart7 = quests["5bc4856986f77454c317bea7"];
        if (tsPart7?.conditions?.AvailableForFinish) {
            for (const condition of tsPart7.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(REMINGTON_MSR)) counterCond.weapon.push(REMINGTON_MSR);
                            if (!counterCond.weapon.includes(M700_XL_ACTION)) counterCond.weapon.push(M700_XL_ACTION);
                            if (!counterCond.weapon.includes(M700_XL_ACTION_NORMA)) counterCond.weapon.push(M700_XL_ACTION_NORMA);
                            if (!counterCond.weapon.includes(AXMC_300_WINMAG)) counterCond.weapon.push(AXMC_300_WINMAG);


                        }
                    }
                }
            }
        }

        // Tarkov Shooter Part 8 (5bc4893c86f774626f5ebf3e)
        const tsPart8 = quests["5bc4893c86f774626f5ebf3e"];
        if (tsPart8?.conditions?.AvailableForFinish) {
            for (const condition of tsPart8.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(REMINGTON_MSR)) counterCond.weapon.push(REMINGTON_MSR);
                            if (!counterCond.weapon.includes(M700_XL_ACTION)) counterCond.weapon.push(M700_XL_ACTION);
                            if (!counterCond.weapon.includes(M700_XL_ACTION_NORMA)) counterCond.weapon.push(M700_XL_ACTION_NORMA);
                            if (!counterCond.weapon.includes(AXMC_300_WINMAG)) counterCond.weapon.push(AXMC_300_WINMAG);


                        }
                    }
                }
            }
        }

        // Claustrophobia (669fa3979b0ce3feae01a130)
        const claustrophobia = quests["669fa3979b0ce3feae01a130"];
        if (claustrophobia?.conditions?.AvailableForFinish) {
            for (const condition of claustrophobia.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(BROWNING_AUTO5)) counterCond.weapon.push(BROWNING_AUTO5);
                            if (!counterCond.weapon.includes(BROWNING_AUTO5_BOSS)) counterCond.weapon.push(BROWNING_AUTO5_BOSS);
                            if (!counterCond.weapon.includes(SERBU_SUPER_SHORTY)) counterCond.weapon.push(SERBU_SUPER_SHORTY);
                        }
                    }
                }
            }
        }

        // ====================== MECHANIC QUESTS ======================

        // Psycho Sniper (5c0be13186f7746f016734aa)
        const psycho = quests["5c0be13186f7746f016734aa"];
        if (psycho?.conditions?.AvailableForFinish) {
            for (const condition of psycho.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(REMINGTON_MSR)) counterCond.weapon.push(REMINGTON_MSR);
                            if (!counterCond.weapon.includes(M700_XL_ACTION)) counterCond.weapon.push(M700_XL_ACTION);
                            if (!counterCond.weapon.includes(M700_XL_ACTION_NORMA)) counterCond.weapon.push(M700_XL_ACTION_NORMA);
                            if (!counterCond.weapon.includes(AXMC_300_WINMAG)) counterCond.weapon.push(AXMC_300_WINMAG);


                        }
                    }
                }
            }
        }

        // Shooter Born in Heaven (5c0bde0986f77479cf22c2f8)
        const sbih = quests["5c0bde0986f77479cf22c2f8"];
        if (sbih?.conditions?.AvailableForFinish) {
            for (const condition of sbih.conditions.AvailableForFinish) {
                if (condition.conditionType === "CounterCreator" && condition.counter?.conditions) {
                    for (const counterCond of condition.counter.conditions) {
                        if (counterCond.conditionType === "Kills" && Array.isArray(counterCond.weapon)) {
                            if (!counterCond.weapon.includes(REMINGTON_MSR)) counterCond.weapon.push(REMINGTON_MSR);
                            if (!counterCond.weapon.includes(M700_XL_ACTION)) counterCond.weapon.push(M700_XL_ACTION);
                            if (!counterCond.weapon.includes(M700_XL_ACTION_NORMA)) counterCond.weapon.push(M700_XL_ACTION_NORMA);
                            if (!counterCond.weapon.includes(AXMC_300_WINMAG)) counterCond.weapon.push(AXMC_300_WINMAG);

                        }
                    }
                }
            }
        }

        // Make Amends Equipment (6261482fa4eb80027c4f2e11)
        const makeAmends = quests["6261482fa4eb80027c4f2e11"];
        if (makeAmends?.conditions?.AvailableForFinish) {
            for (const condition of makeAmends.conditions.AvailableForFinish) {
                if (condition.conditionType === "FindItem" || condition.conditionType === "HandoverItem") {
                    if (Array.isArray(condition.target)) {
                        if (!condition.target.includes(SVD_DRAGUNOV)) condition.target.push(SVD_DRAGUNOV);
                        if (!condition.target.includes(SVD_DRAGUNOV_GREEN)) condition.target.push(SVD_DRAGUNOV_GREEN);
                    }
                }
            }
        }
    }
}
