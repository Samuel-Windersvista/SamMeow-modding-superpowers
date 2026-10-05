# 二、主线剧情（统一叙事）

## 导语

素材来源：笔记叙事草稿（`quests.json` 内嵌 questNoteText，按 `mainQuestNotes.json` 的 10 个 chapterId 归并）、对话摘要（`dialogue.json` 共 195 个元素，work 中分 4 卷摘要）、音频日志（`tapes.json`，18 盘）、结局定义（`endings.json`，4 个）。主线任务 192 条，有叙事文本的并集 138/192；余 54 条未覆盖，绝大多数是索引中标“无名称”的条目（见 `data/main_story_index.md`），素材既无题名也无正文，无法生成叙事，故不逐一征引。个别有名缺口（`68e2d8d5…`“门票”、`68e2ecfe…`“获得逃离塔科夫的门票”）是同一剧情点的条件态条目，内容已由相邻笔记覆盖。

章节排序未沿用草稿 `20-notes-chapters.md` 的数据次序，而采用 KB 07（社区整理）章节顺序（`knowledge/spt-kb/curated/lore/07-story-chapters.md`）；该顺序与本地笔记数据顺序不同：塔科夫之旅 → 陨落星辰 → Batya → 意外证人 → 他们已经来了 → 神秘蓝焰 → 探秘“迷宫” → 无名者 → Boreas → 终局：门票。凡素材未明说而由上下文推得者标“推测/疑为”；取自 KB 者标「KB 补充」并附来源。官方认可 wiki 确认 10 个 Story Chapters 存在，未给出严格线性推进顺序（仅 Falling Skies 前置 Tour、The Ticket 前置 Falling Skies 等前置链明确）。

---

## 一、塔科夫之旅（Tour）

主角突袭 TerraGroup 办公室时，附近高楼顶大爆炸，他与指挥部失联，须先活着离开原爆点 [来源: quests.json#68cbcfb02cdee534ad02d212]。音频日志印证：USEC 小队长报告电子设备尽毁、失去设施控制，命向三号撤离点（港口方向）撤退 [来源: tapes.json#68b6dc376444a29985459601]；BEAR 小队长同样报告通讯中断、行动取消，留下口令“水是湿的” [来源: tapes.json#68b6dc8dffe35767f521778b]。

因通道被切断，主角转向本地商人链。后备撤离点定在海边港口，须 Therapist 路线或经 Ragman 高速公路立交桥 [来源: quests.json#68cbdaac76fe74b1e80bfd98]。Ragman 侦查 Ultra 交换海关线索 [来源: dialogue.json#68c15787024b8fa6a91ea4ef]；Skier 以港口主路情报（须过 Polikhim 16 号化工厂雷区）与 Mechanic 介绍换修仓库 [来源: dialogue.json#68c2b29d53293ff0bb4d91b8]；Mechanic 交穿林小路换取工厂 Scav 情报 [来源: dialogue.json#68c411ffc7e14e77d84e8b7b]。Peacekeeper 索 2 万美元“资助”UNTAR 才开灯塔通道 [来源: dialogue.json#68c7ebcd0437d9925a70209d]；Therapist 收 25 万卢布出护送 [来源: dialogue.json#68c81b3f440c7653f2ccbd58]。Prapor 要 PMC 狗牌换军事基地坐标，那里盘踞前海军步兵 Glukhar [来源: dialogue.json#68c803fe2e9da6811e7f77a3]。主角还发现街区之下的 TerraGroup 秘密实验室 [来源: quests.json#68cc40a2b5a3172cff094670]。

港口撤离被叫停，主角决定经营商人关系、建立藏身处扎根 [来源: quests.json#6903565c39d72aaf3f0c508c]，Prapor 口中的“门票”成为新目标 [来源: quests.json#68cc41151e25f65955034a34]。

---

## 二、陨落星辰（Falling Skies）

主角在自然保护区发现迫降客机残骸，直觉非单纯事故 [来源: quests.json#68cc6b5a7d70b6a2ec06b2ac]。Prapor 让他去黑色大 G 越野车找 U 盘，线人位置经 Therapist（先索 2000 美元，随后失联） [来源: dialogue.json#68b6e6ee79b6d65a0a9e1221]。U 盘证明客机系被击落，机上载 TerraGroup 副总裁与两名安全委员会成员，且无正规救援；下令者不在盘内 [来源: dialogue.json#68b83f6e8d4c1c6e9ce0d95c]。Prapor 派主角回收飞行记录仪交 Elektronik 解密，主角为此收集电子零件 [来源: dialogue.json#680ba3588a6ad2fe936eb77a]。黑匣子解密时烧毁，Elektronik 留有通话记录打印件，其中出现名字 Kerman；Prapor 接到紧急电话后匆匆打发玩家 [来源: dialogue.json#680ba51feaea752ac81f384f]。Kerman 随后亲自来电（用变声器），称机上有加固手提箱、装着离开塔科夫的“门票” [来源: dialogue.json#680bb7377a9aec345b92d08c]。

主角判断 Prapor 比 Kerman 更狠毒，决定自留箱子，并发现 Kerman 的字条，证实其不希望箱子落到 Prapor 手里 [来源: quests.json#68cc6e50be134906dc0f3c42]。此为全局分叉点：交箱给 Prapor，Kerman 会宣布主角丧失离城机会 [来源: dialogue.json#68d9208f8ac91a9b2d205eb5]；自留则获 Kerman 称赞并承诺摆平 Prapor [来源: dialogue.json#68e3b444517473ec08d3a130]。Kerman 后又推断箱子已转到 Lightkeeper 手中 [来源: dialogue.json#68daab9cd1961f02e80dc4dd]；Lightkeeper 以“找回蓝色文件并在立交桥制造袭击”为条件交还箱子，并提醒 Kerman 与 TerraGroup 有关、不可盲信 [来源: dialogue.json#68dabc1697c733d9c34d2d8c] [来源: dialogue.json#68daca29717b887f210d9d71]。开箱需 TerraGroup 特殊干扰器，一步出错箱内物品全毁；设备后经 Mechanic、Elektronik 以比特币换得 [来源: dialogue.json#68dbffa087f8bdb461da4e9e] [来源: dialogue.json#68e6aa8f23bba425729a4641]。

---

## 三、Batya

主角发现 BEAR 派入塔科夫的还有特种单位，找到“Bogatyrs”定制臂章 [来源: quests.json#69125ff1ec30864f580e9efd]。Jaeger 辨认臂章属 Voevoda 亲选的 Bogatyrs，据点“花楸树”“鸟巢”，鸟巢在 Ultra 背面森林营地、曾是监视 USEC 的观察哨 [来源: dialogue.json#68d3fa7c366f703a818cb673]。主角在 Moreman 的墓地与营地收集到狗牌、护身符、妻子寄来的明信片与 Voevoda 的录音机，证明撤离极为仓促 [来源: quests.json#6912600fc03e1e024902a3a3]。拼合数字得 3570 与“27.893.2000”，调频收到循环录音，命其去找 Lightkeeper 交出识别物 [来源: quests.json#69126269d43471933208e619]。无线电里 Voevoda 称 BEAR、USEC 俱亡，只剩“战斗兄弟会”，元凶已转移、下一目标是整个诺文斯克 [来源: dialogue.json#68d9bf7a9817e4f4adf398a6、dialogue.json#68d9bfa5f2a16a75d1a57ad5]。

Moreman 的最后录音揭真相：Yastreb 先朝自己人开枪，小队随即陷入四面埋伏 [来源: tapes.json#68ea4eeff4ab97dd48695f67]；Voevoda 报告战后有人引爆爆炸物、USEC 与平民均有伤亡 [来源: tapes.json#68ea5cf776c0ede0d9c5a817]。经授权追查，Prapor 被证据逼问后承认接到“将军”命令、只负责把纸条留在邪教徒粉笔圈里 [来源: dialogue.json#68dbfd85f995c0ce531f0d8c]；邪教徒便条显示 Lightkeeper 批准了执行将军命令 [来源: quests.json#6912631f788350fa2305e575]。主角假扮 Voevoda 逼问，Lightkeeper 承认只提供通讯渠道，并提出以保障自身安全为条件周旋；Voevoda 要求他交出“无名者”渗透俄军的文件 [来源: dialogue.json#68da68ae8d8c3eb6513216bf]。文件证实“无名者”已在全球政府安插内线、准备夺权 [来源: quests.json#69126351788350fa2305e578]。Bogatyrs 的故事由此连接本地阴谋与全球协议。

---

## 四、意外证人（Accidental Witness）

海关宿舍楼旁轿车上的威胁信牵出被纠缠的居民科兹洛夫，主角察觉讨债反常、疑有阴谋 [来源: quests.json#690806a1c75638a9300f58e8]。科兹洛夫掌握头面人物黑料，曾联系调查记者阿纳斯塔西娅·米哈伊洛娃 [来源: quests.json#690806ed488b6b3f62088dd1]。Skier 以撬开失联哥们公寓门为条件透露其情报 [来源: dialogue.json#68f113f8b94bd10308f3e726]；Ragman 称阿纳斯塔西娅是其前女友、专戳政客痛处，撤离期间失踪，住 Chekannaya 街 13 号 [来源: dialogue.json#68f3b722a671e6185b975c9e]。在 Skier 熟人公寓查到本地最大犯罪头目“切皮加”及 FSB 监视报告 [来源: quests.json#6908071f80aad5e5a603c97d]；Skier 补充其位置已交给疯子 Reshala [来源: dialogue.json#68f3c2e61f17f339b439582f]。

记者邮箱的信指出副部长雅罗斯拉夫·什库连科的丑闻材料 [来源: quests.json#6908076a69e89d21b4052a4e]。线索转向信使帕沙，其自行车被帮派截毁，主角顺藤摸到 Reshala 的赃物窝棚 [来源: quests.json#690807e38c5759308f0d4a70]，Reshala 的笔记暗示其与某商人有往来 [来源: quests.json#69080850d71ec6d7b1047663]。主角在花坛找到科兹洛夫藏的录音带：什库连科早在冲突爆发前就与切皮加勾结，走私武器、控制封锁区药品供应，图谋让匪帮掌控全城 [来源: quests.json#6908087d488b6b3f62088dd9]。原声里什库连科称行动约三个月后开始、以外币支付、警察会打点，切皮加则要先做掉本地几个大人物 [来源: tapes.json#68ea6839c23ff215e422b819]。他们预知冲突将至，令主角判断一切可能是精心策划，这盘录音带亦是终局大证据之一。

---

## 五、他们已经来了（They've Arrived）

城中出现诡异圆圈与符号，主角起初当作涂鸦，后判定是有组织行动，并撞见一个衣着异样、行动无声精准的兜帽怪人 [来源: quests.json#690cc0dd2e5c0e5e0309702c]。调查确认确有一支邪教为“降临”做准备、崇拜“世界之眼” [来源: quests.json#690cc11c534225d94e0b2783]。Mechanic 只有推测，让主角直接去问邪教徒 [来源: dialogue.json#68bec7edbcf41e988381d905]。废弃村庄里，主角发现邪教窝点与行刑室，追查到受害者伊戈尔——NGO Cobalt 员工 [来源: quests.json#690cc1554d2051cf780501a7]。伊戈尔自录带还原遭遇：他收到画满符号的书，读后确信写书人掌握“禁忌的知识”，并发现书中“赐福”影射 TerraGroup 不公开的项目；邪教徒认为他能接触“世界之眼”，称其为“受选者”，他决意逃离 [来源: tapes.json#68ea4b401a05b785a47dcba0]。但他未能逃掉：主角在 Sordi 通讯塔发现符号，并找到已崩溃、投教而死的伊戈尔 [来源: quests.json#690cc6d9239a15bdf70e851b]。

主角查到值班主管 Arshavin 的钥匙卡，对应 14-4 KORD 站点 [来源: quests.json#690cc9ef0ce487a8d10b741e]。Mechanic 指导：修好通讯塔并持卡潜入，重启系统下载终端代码 [来源: dialogue.json#68e3d202756cb6261fc6cbfb]。真相浮现：Cobalt 在发电站附近建有 ARRS 站点，实为接入并劫持全城监控的监听站，“世界之眼”名副其实 [来源: quests.json#690cca86239a15bdf70fa1e3]。机房外有一盘邪教录音：祭司称世界之眼早已注目于他、一切已被预见，他们在等待为降临骑士铺路的“盲目者” [来源: tapes.json#68ea4982dbbc029cd8cba974]。主角回滚站点配置、切断对外联络，防止技术落入邪教之手 [来源: quests.json#690ccb1753ed7944751011a9]。U 盘若完整取出，即终局大证据“ARRS 系统规格”来源 [来源: dialogue.json#68ebb7309f99187159502955]。

---

## 六、神秘蓝焰（Mysterious Blue Flame）

突袭办公室时，有人在摩天楼顶引爆特殊武器，主角昏迷前只见明亮蓝光，随后所有电子设备失效，判断遭遇 EMP [来源: quests.json#69001c0726f726610e0e3f88]。Mechanic 判断大规模断电从市中心辐射而出，是武器在市中心引爆的直接后果；原爆点已被搜刮 [来源: dialogue.json#68e7aee6b7d640f7396629a0]。双方约定：主角收集碎片，Mechanic 研究 [来源: quests.json#69001c147e6dd507c70a57e8]。碎片确认是前所未见、疑为 TerraGroup 的技术，需在实验室服务器机房接入局域网入侵设备提取数据 [来源: dialogue.json#68e7b4191bba71c3e1937bcc]。集团撤离前删光数据，Mechanic 找到残缺的“#1156 项目”规格：俄国本地制造的革命性电磁脉冲放大装置，即断电元凶 [来源: dialogue.json#68e7cfe7418a78f7d60bea0a]。碎片可卖可留 [来源: quests.json#69001c46ac1b5f4cad06e3d5]。

此后 Mechanic 无法再追查 #1156，主角判断项目就在本地设施就地组装 [来源: quests.json#69001c78384a9902240dee5c]，并在实验室找到指示把蓝图以邮件寄出的字条，线索指向市区邮政分局 [来源: quests.json#69001c84e85e475e590c752b]。邮差的电话留言拼出后续：快递员奥金佐夫遇数公里检查站、目睹卡车司机被杀、流弹击中自己的车，遂弃车步行，包裹留在车内、钥匙压在车轮下 [来源: tapes.json#68bfe8a843ed244d2dbfef97]。主角据此前去查货舱 [来源: quests.json#69001ca3384a9902240dee5f]。蓝光的动机或为毁尸灭迹、抹除数据，或干脆测试实验性武器才是冲突真正目的 [来源: quests.json#69001c94ee0991aafd0f3698]。

---

## 七、探秘“迷宫”（The Labyrinth）

主角听说海岸线疗养院地下另有“迷宫” [来源: quests.json#68ffff4e3335c4199504704e]。Jaeger 以查清突入后失踪的 BEAR 小队为条件，提供 Knossos 门禁卡 [来源: quests.json#68ffff7d3caa9d0cab08fc99]；他称“迷宫”是疗养院地下的 TerraGroup 秘密设施，朋友一支 BEAR 战友下入即失联 [来源: dialogue.json#68e3baef4ee98520f72e796a]。设施内，主角还原小队遭遇：指挥官 Leshy 曾令停止找出口、向 #1156 项目车间集合 [来源: quests.json#68ffff9ab90b93f61a0cf6a2]；他在车间 BEAR 尸体中未找到 Leshy，推测其继续深入 [来源: quests.json#68ffffa69f98d8e00f0ce6cf]。TerraGroup 在此进行人体实验，小队突入时工作人员躲入更深处并打开实验对象笼子 [来源: quests.json#68ffffbd2f2048e6e505f0be]。一名高级主管自杀，留下录音带与大量技术笔记 [来源: quests.json#68ffffca5d8e37f04d0ae9cc]。真相完整：工作人员封锁出入口、放出实验对象，即便这些人疯狂疲惫、手无寸铁，仍将 BEAR 队员全部杀害 [来源: quests.json#68ffffef2f2048e6e505f0c2]。

录音来自科学家尼古拉·沃罗年科：他坦言“迷宫”条件最差、已与总部失联；为保卫设施放出第 3、12 组测试对象，事后只闻测试对象之声、再无 BEAR 动静 [来源: tapes.json#68ea3f4de3134607b894de76]。他把所有文件收进一个文件夹冲进下水道，并留一把夺来的手枪作最后手段 [来源: tapes.json#68ea3fee8e6687c4bb654b12]。主角据此推测“迷宫”与疗养院共用排水管线，在下游找到加密文件夹，报告证实集团进行残忍人体实验 [来源: quests.json#690000235b2af7f913036729]。他把录音带交 Jaeger 换得钥匙 [来源: dialogue.json#68e4d95d5253dbe96fcf550a]。这盘“迷宫研发报告”亦是终局大证据之一。

---

## 八、无名者（The Nameless Ones）

Kerman 指认 TerraGroup 背后的操纵者是“无名者” [来源: quests.json#6911dcd8466e028dca0d0d94]。办公纸上“无名者的意志”与“净化燃料”留言指向集团实验室 [来源: quests.json#6911dd02277e44fe9f0507cc]。Polikhim 化工厂与 DP 石油合作开发催化剂，主管为地区分部主管勒热夫斯基 [来源: quests.json#6911dd36466e028dca0d0d97]；手册显示“蓝冰”货物对集团与“无名者”至关重要，他们计划优先转移、贿赂港口人员、准备强行突围 [来源: quests.json#6911de9795febc38b20f4b99]。勒热夫斯基车中硬盘揭示“蓝冰”是双用催化剂，既是燃料也能转武器，测试报告锁在安全屋 [来源: quests.json#6911deafe1112b904206eddd]；报告证实其可与大规模杀伤性武器联用，经销商遍布全球 [来源: quests.json#6911debd466e028dca0d0d9b]。一份烧毁文件提到某项协议及获最高权限的高层级专家 A.P.，主角疑其为“无名者”成员 [来源: quests.json#6911dede461b770ecd071606]。

音频日志记录 A.P. 的撤离讲话：与外界通讯中断、收到纽约指示后疏散，敏感数据正被销毁，重要人员由 USEC 护送至疗养院，抵达后与 USEC 的合作立刻终止 [来源: tapes.json#68c004e2955985870577e03c]；第二场要求 13 小时 47 分钟内清空所有设施，已收到“Braun”“Chernigov”与里斯本的确认 [来源: tapes.json#68ea5b71a0dfaf2f689331f9]。“蔚蓝海岸”疗养院是撤离中转站，A.P. 房间 305，遗留 U 盘与门禁卡数据合并生成独特门禁卡 [来源: quests.json#6911df0aa460b81cc809bbe5]。主角凭卡在市中心 Premier 公寓找到 A.P. 隐藏办公室 [来源: quests.json#6911dfb226fc56a9a3045043]。一份命令末尾“总部并没有进一步指示”证明“无名者”并非 TerraGroup 下属，而是幕后黑手 [来源: quests.json#6911dfee7fed7200770b9036]。主角判定塔科夫只是“无名者”全球协议的第一张骨牌 [来源: quests.json#6911e00195febc38b20f4b9f]。

---

## 九、Boreas

调查从 TerraGroup 并购 Paradigm 航运的海报开始。Mechanic 证实集团收购其控股，船队可把任何货物运往任何地方 [来源: dialogue.json#69a9a20696c6f72568b4cbca]；他截获来自海上的非军方求救信号，需修复自然保护区西北的红白通讯基站 [来源: dialogue.json#69a9a2ff220ad3cba795cf60]。信号恢复后，主角截到船员与 Knight 的通话——后者契约战争期间便脱离指挥单干、灾难后招募前雇佣兵成为本地大祸害 [来源: dialogue.json#69aae362287cd616e57ffde0]。Mechanic 查明目标是从未入港、自持力超乎想象的破冰船 Boreas 号 [来源: dialogue.json#69aaf99d8801db3d0e5310f8]。

交通分两段：登船靠走私者气垫船，只送不接、返程自负，需带修船工具 [来源: dialogue.json#69b2ded90c55602680378950]；返程靠 Prapor 的直升机，主角先替他清理储备站、找到航空润滑油 AMG-10，直升机只用于撤离 [来源: dialogue.json#69b29e9bf914650d77937cc1]。BTR 司机牵线走私者，主角以清理其营地换取协助 [来源: dialogue.json#69b403d4c5050f5e69a86c60]。

登船后游荡者已血洗大部分船员，一名科学家因防御协议封舱幸存 [来源: quests.json#69e725bb8f17e26d49006cd7]。他透露船长在舰桥启动防御协议封死各区域，通往舰桥需海员钥匙卡、进轮机舱需工程师钥匙卡 [来源: dialogue.json#69c6950cf53b8f641cf1abd2]。主角经轮机舱取卡，回城取得聚能炸药破开铁链，打通上层密码锁：上半 312（科学家给），下半 220（离船最近分部的区域代码，Mechanic 远程查得） [来源: quests.json#69e72a56600f351cd1017c11] [来源: dialogue.json#69bbf7f163986b19e0448f4b]。在舰桥发现遭黑衣佣兵突袭的残局，船长已死（死于谁手原文未直述，推断）[来源: quests.json#69e74be2f16c63e3340c4570、dialogue.json#69bc0dc8036ca0848c1bb805]。

三块闪存盘拼出核心阴谋。第一块揭示 USEC 秘密投送真相：芬兰湾三艘 Paradigm 货船“太平洋轴线”“开罗纽带”“东方能源”实为海上预制舰队，油轮疑被故意制造漏油以引开海巡掩护登陆，灯塔区特殊线人协调登陆，Knight 一伙是首批踏足者 [来源: dialogue.json#69bc144761cac6cdf6631151]。第二块显示塔科夫只是更大事变的预演，规划在东京与开罗复刻同类行动（东京为金融供应链枢纽，开罗/苏伊士一旦封锁将致百亿美元损失） [来源: dialogue.json#69c104287afb7c2f938d7ceb]。第三块揭示集团已建成覆盖全球的卫星通讯与干扰网络，可截获 G20 通信、瘫痪任何地方的通讯与电力 [来源: dialogue.json#69c134d21b236e763de53166]。撤离科学家 Grigory Altman 失败：其自录带说明样本容器受损、他按隔离协议封锁舱段并服药观察 [来源: tapes.json#6a0254c11853e610201ab1ef]；接应时他发狂袭击队员、带公文包逃走，只留下钥匙卡 [来源: dialogue.json#69c26a4cf11d46fef41f1940]，主角遂决定独自登船善后 [来源: quests.json#69e75709c61052cfe6060d18]。

---

## 十、终局：门票（The Ticket）

（一）核心设定。主角的加固手提箱即逃出塔科夫的“门票”，由黑匣子里的神秘人 Kerman 引出 [来源: quests.json#68e4e999793f60590607f951]；箱内是 TerraGroup 签发给克鲁格洛夫的钥匙卡及说明书，离港前须重新激活 [来源: quests.json#68e4ec6b2411dc8de20ba1b5]。Kerman 坦白卡是故意失效的、用以考察背景，条件是替他挖出集团黑料 [来源: quests.json#6907b286267c30d8c3020e3c]。主角在 Mechanic、Elektronik 处弄到加密装置、凑齐比特币，再经 Fence 取得哈希代码 U 盘重编卡片，使其在港口生效 [来源: quests.json#6907b453267c30d8c3020e40]。Kerman 摊开计划：把集团罪证发到网上才是真正的门票，交付前须数字化经安全节点发送并保留原件随身带走 [来源: dialogue.json#6914e93111e22e5f1cc2f6a1]。

（二）翻脸与转投。Kerman 挑剔证据、多番训斥，曾给“第二次机会” [来源: dialogue.json#69fbcc42c9c38a74ea5021c8]；主角转求商人乃至 Lightkeeper [来源: quests.json#690fd6ad64bc90f0cc02a102]。后者以收集全城地形情报等委托为条件 [来源: dialogue.json#68ed2d57d49527d4570f610e]，最终交出一张署名被刮掉的钥匙卡，条件是逃出后仍欠他一项任务 [来源: quests.json#690fd7417f0bc488ff0a05bc]。港口通行限夜间 21:00 至凌晨 6:00，罪证原件须放入安全箱随身带走 [来源: quests.json#6907b4a6267c30d8c3020e43]；终点站遇袭，主角自行杀至撤离点逃出，欠下 Lightkeeper 人情 [来源: quests.json#690fd850351b6e11bf035daa]。

（三）四结局与分支。`endings.json` 给出四个结局：`EscapedFromTarkovAndSurvived`（67c08f0268e50a07b10d25a6）、`EscapedFromTarkovForHumanity`（67bdf8c066ca1d79a202463a）、`EscapedFromTarkovToFallInTheDarkness`（67c862bd9f9b7ef9090651d8）、`YouDidntEscapeFromYourself`（67c9877aff0329206209cb67） [来源: endings.json#EscapedFromTarkovAndSurvived 等]。「KB 补充」依次对应**幸存者**（经济逃亡，Skier 线）、**为了全人类**（集齐全部罪证）、**堕入黑暗**（与黑暗势力合作）、**灯塔**（与 Lightkeeper 结盟，对应其“你不可能逃离你自己” [来源: dialogue.json#67ff9d5a472e5a0cd9b10696]）。官方认可 wiki 通称依次为 Savior / Debtor / Survivor / Fallen；触发核心为 Falling Skies 末的 armored case 处置与 The Ticket 中是否接受 Mr. Kerman 帮助。[外部: https://escapefromtarkov.fandom.com/wiki/Endings]、[外部: https://escapefromtarkov.fandom.com/wiki/The_Ticket]（官方认可 wiki）每个结局再分“交箱/不交箱”两支共 8 条线，分支开关为箱子归属与是否完成 Skier 的“识时务者为俊杰”。

（四）9 选 8 罪证。「KB 补充」（来源同上 `07-story-chapters.md`）：须从犯罪证据录音带、守望者文件、间谍网络报告、迷宫研发报告、#1156 项目规格、ARRS 系统规格、燃料催化剂测试报告、A 女士谈话记录、破冰船数据库文件中收集八项。破冰船文件为补救项——若失掉 ARRS 证据可经 Boreas 线补回；另有 36 项小证据须先于大证据交完。素材可直接对应犯罪证据录音带 [来源: tapes.json#68ea6839c23ff215e422b819] 与迷宫研发报告 [来源: quests.json#690000235b2af7f913036729]，“守望者文件”“A 女士谈话记录”未直接出现，属 KB 补充项。

---

## 未解明与分歧

1. 54 条无文本主线未纳入叙事，多为“无名称”条目，使“守望者文件”“A 女士谈话记录”等缺少素材级来源，需回查 `dialogue.json` / `quests.json` 原文。
2. Kerman 真实身份始终未明：他坚持身份不重要、只求扳倒 TerraGroup [来源: dialogue.json#68d91d0a6d5f5227031614b0]；Lightkeeper 却称其与 TerraGroup 有关、真实身份只有他自己知道 [来源: dialogue.json#68daca29717b887f210d9d71]。此为素材明确保留的悬念。
3. “无名者”与“黑色军团”（即 KB 中的 Black Division/黑师；本地文本用词）层级关系有两种表述：前者被指操纵 TerraGroup [来源: quests.json#6911dfee7fed7200770b9036]，后者被 Lightkeeper 称为直接听命于“左右世界局势的大人物”、样本即“无名者的赐福” [来源: dialogue.json#6a60c5d40647b56b26ea26a5]。素材未给最终裁决。
4. “门票”“Boreas”等章含大量互为分支的条目，本篇按主线合并，细节以主线条目为准。
5. 两段对话未并入十章：其一是“信使送卷轴到塔格勒”的互动小说式分支（与塔科夫主线无交集） [来源: dialogue.json#688cdebcf51ac872c8acd64d]；其二是 BTR 司机、Lightkeeper、Ragman 等的大量交易性寒暄与支线委托（裁判脏材料、营地纠纷等），丰富世界观但不构成主线。
