# Slice 5 素材 — 05-jaeger-therapist-fence
> 生成: tools/dump_slices.py | 数据: data/quests_joined.json（源自 SPT 5.x quests.json localization + locales global ch/en）
> 任务 141（支线 130 / 主线 11）| ch 字符 47844
> 主线任务（isStoryQuest=True）在本素材中只有目标/条件文本；完整叙事见 work/main-story-text.md（第二阶段产出）。
> 排序: 商人 > 支线优先 > 前置数 > id；无文本任务为紧凑 stub（[主线|无文本]/[支线|无文本]）。
> 本素材不含 extras（主线叙事日志孤儿键）；如需核对见 data/quests_joined.json。

## 商人: Jaeger (5c0647fdd443bc2504c2d371) — 任务 65

### [支线] 塔科夫神射手 - 1 | 5bc4776586f774512d07cf05
- id=5bc4776586f774512d07cf05 | 类型=Elimination | 地点=any | 前置=无 | 后继=5bc479e586f7747f376c7da3 | notDisplayed=False
- description: 你终于来了。看来只有你和神枪手先生才能在战场上生存这么久，一次又一次从战局中全身而退。他最近在寻找一个可靠的人，一个可以被委托重任、绝不会让人失望的伙计。他说测试一个人实战能力的最好方法是用栓动式步枪来一场实战演练。如果这个测试通过了，那继续谈下去才有意义。他是个严肃认真的人，不会无理取闹，所以看起来他的确是需要一个人来托付什么重要的任务。你觉得怎样，我能推荐你去吗？就当作自己是“塔科夫神射手”一样试试看吧，如果你能通过这次考验，我会安排你和神枪手先生联系的。
  所以这是他准备的第一项任务：测试你在中距离使用机械瞄具开火的准度，就先从 40 米开始吧。
- successMessageText: 所以，你觉得是不是已经有扎伊采夫内味了？神枪手先生对首次测验的结果很满意，他已经准备好了下一个任务。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 使用栓动式步枪在 40 米开外通过机械瞄具击杀 Scav

### [支线] 熟人 | 5d24b81486f77439c92d6ba8
- id=5d24b81486f77439c92d6ba8 | 类型=PickUp | 地点=any | 前置=无 | 后继=5d25e48d86f77408251c4bfb | notDisplayed=False
- description: 哦，你好啊！我想请你帮我个忙，不过鉴于我们彼此之间还不太熟，有些要紧的事情我暂时还不会拜托你。
  你可能已经注意到了，我孤身一人住在林子里。在这个艰难的时刻，一个人想要保持自我是很难的，而我仍然试图依照自己设下的原则生活，把那些病毒传染源、那些把我们拉进深渊的祸害从塔科夫的土地上清理出去。打击犯罪、恢复正义与秩序花费了我不少时间和经历，有时候我的补给也会告罄，通常大多是在我没预料到的时候就消耗干净了……
  你能帮我找些吃的来吗？不用什么特别的东西，能吃就行，比如你们士兵吃的口粮就很不错。
- successMessageText: 谢谢你，孩子。时局艰难，现在食物也相当宝贵了。填饱肚子之后我就能专注真正重要的事情，不用担心补给短缺了。
- 条件:
  - 条件[AvailableForFinish/HandoverItem]: 上交任意饮食

### [支线] 生存者之路 - 省吃俭用 | 5d25b6be86f77444001e1b89
- id=5d25b6be86f77444001e1b89 | 类型=Completion | 地点=5704e3c2d2720bac5b8b4567 | 前置=无 | 后继=5d25bfd086f77442734d3007 | notDisplayed=False
- description: 你好啊！你来的正是时候，我正收拾着准备去打猎呢。帮我个忙吧——不不，这不算是要求你为我服务，就当作是朋友之间互相搭把手。路途很长，又充满了危险，我总得找几个地方歇歇脚；所以我需要你帮忙在返程途中帮我安排几个休息点。尽量找个安全的地方，每个地方放一份俄式单兵口粮和一瓶水就行，那我们说好咯？
  在你跑这一趟的时候，别忘了温习一下之前我教给你的生存技巧，要随时做好在突发情况下求生的准备。比如我就在塔科夫四处埋藏了物资储备，既有防潮的塑料物资桶，也有用植被伪装的活板门，但详细情况没必要和你透露太多。
- successMessageText: 事情办成了？谢谢你，我刚准备好出发，你可真是帮了大忙了。那就几天后再见了，朋友。
- 条件:
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在森林的ZB-016地堡里藏匿Iskra个人配给口粮
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在森林的ZB-016地堡里藏匿瓶装水
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在森林的ZB-014地堡里藏匿Iskra个人配给口粮
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在森林的ZB-014地堡里藏匿瓶装水

### [支线] 猎人之路 - 周边安全 | 5d25e2b486f77409de05bba0
- id=5d25e2b486f77409de05bba0 | 类型=Completion | 地点=55f2d3fd4bdc2d5f408b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 我听说最近化工厂那儿出了不小的乱子，到处都是目无法纪的强盗，就连你的佣兵同事们也加入了抢劫的行列，把工厂大卸八块，看到东西就拿走，也不管还有没有价值。看到老工厂沦落成今天的样子实在是令人伤感，我依旧记得工厂曾经的样子，它的每一个角落和缝隙……在 TerraGroup 接管整个厂子之前，我还在工厂里当了半年的安保队长。
  无论如何，抢劫和破坏行为都是不可容忍的。所以我们得去教训一下这帮强盗。让土匪和你的那些同事们不要再打工厂的主意，至少暂时不要。
- successMessageText: 干得好！当然了，整个塔科夫的人渣不止这么点，在你走之后，肯定还会有其他的人来这里劫掠，但是他们下次再来的时候，绝对不会再像之前一样随心所欲了。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在工厂消灭 PMC 行动人员

### [支线] 猎人之路 - 战利品 | 5d25e2c386f77443e7549029
- id=5d25e2c386f77443e7549029 | 类型=Elimination | 地点=56f40101d2720b2a4d8b45d6 | 前置=无 | 后继=无 | notDisplayed=False
- description: 今天我有份特别的活儿给你，孩子，一个新的猎物……当地人都叫他 Reshala，你八成也听说过他，就是那个挎着黄金手枪的土老帽。这个白痴出门的时候总带着他的那帮子人，撮合生意，给其他混混撑腰。许多好人的死都和他脱不了干系。至于 Reshala 本人没有什么好多介绍的，就是个缩头乌龟，全靠自己的小弟撑场子——天知道这种货色是怎么当上地头蛇的。
  总之，就当是帮帮塔科夫的人们，替这座城市除掉这个祸害。顺便把他的枪带来，这件战利品作为 Mechanic 的藏品肯定很合适。猎杀这些鼠辈的时候千万要小心，不要反过来被他们打个措手不及。再会，猎人，祝你狩猎愉快。
- successMessageText: 你做了件好事。这片土地上终于又少了一个混蛋。拿走这个，这是我和 Mechanic 给你准备的礼物。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 找到并消灭 Reshala
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Reshala 的黄金 TT-33 手枪

### [支线] 猎人之路 - 杀戮森林 | 5d25e2cc86f77443e47ae019
- id=5d25e2cc86f77443e47ae019 | 类型=Completion | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 日安，孩子。这么多年来，我一直在这些森林里狩猎，除此之外，我还了解到动物比人更有人情味。当冲突开始后，人性就几乎绝迹了。我简直难以形容外边横行着的大批野兽。这些东西已经不再是人类了，但就连动物也算不上，它们是更糟的东西……是人心底最肮脏最邪恶成分的沉淀。我们可不能让这些怪物毁掉我们的城市，必须赶紧解决掉它们。行动起来，猎人。
- successMessageText: 当他们踏上这条路时，他们就做出了自己的选择。你做了一件正确的事儿，谢谢你，猎人。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在塔科夫街区、储备站、灯塔或海岸线消灭 Scav

### [支线] 猎人之路 - 脱销 | 5d25e2e286f77444001e2e48
- id=5d25e2e286f77444001e2e48 | 类型=Elimination | 地点=5714dbc024597771384a510d | 前置=无 | 后继=无 | notDisplayed=False
- description: 保重身体，士兵。你喜欢购物吗？我以前非常喜欢，我们这儿有个购物中心。那里非常适合消磨时光，但是当局势突变、一切都急转直下时，就没有那么多闲工夫娱乐了，那些商店很快被洗劫一空，而那些没被抢的商店都被人渣们当成了自己的窝点。我听说商场里有个特殊的恶霸——他是一个前运动员，早在冲突爆发前就以敲诈勒索为生，如今这个混蛋摸到了枪，品尝到了比以往任何时候都无可比拟的权力，于是就肆意杀人，从来不需要理由。人们叫他 Killa，他有个很特别的头盔……你看到了就知道了。干掉他，人们一定会感激你的。完事之后记得把他的头盔当作证据带回来，以防万一。
- successMessageText: 终于，他得到了自己的报应。我相信总有一天这座城市不再会有这样的人渣出现。谢谢你，孩子。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 找到并消灭Killa
  - 条件[AvailableForFinish/FindItem]: 在战局中找到Killa的头盔
  - 条件[AvailableForFinish/HandoverItem]: 上交Killa的头盔

### [支线] 猎人之路 - 森林管理员 | 5d25e2ee86f77443e35162ea
- id=5d25e2ee86f77443e35162ea | 类型=Completion | 地点=5704e3c2d2720bac5b8b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 作为一个有多年经验的猎场看守人，我很清楚如果森林里发生了疫病，那就必须立刻把感染源从林子里移除，有些时候甚至不惜烧掉林子患了病的部分，从而防止整片森林都被感染。现在就有一个感染源在森林里扎了根，人们叫他 Shturman，他和他的帮派就驻扎在锯木厂里——你能想象吗？现在我在自己的保护区里都得提心吊胆了。解决这个麻烦，把这个家伙除掉对森林和人们都好。但千万小心，孩子，这个 Shturman 有好几个同伴，他们也都是神枪手。
- successMessageText: 也就是说，你终于把 Shturman 赶出了自己的老巢？干得不错，很多人都能松一口气了。也许我该搬到更靠近市区的地方了，距离我的家也更近些。这是给你的奖励，都是在锯木厂一个奇怪的箱子里找到的东西，不知道是谁留下的存货。我用不着它们，但是估计你用得上。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 找到并消灭Shturman
  - 条件[AvailableForFinish/FindItem]: 搜索 Shturman 的尸体，找到他的储物箱钥匙
  - 条件[AvailableForFinish/HandoverItem]: 上交Shturman的宝箱钥匙

### [支线] 猎人之路 - 正义 | 5d25e43786f7740a212217fa
- id=5d25e43786f7740a212217fa | 类型=Completion | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 见到你可真好，孩子。你打过猎吗？跟着受伤的猎物留下的踪迹，为它们送上最后的仁慈…… 当塔科夫的骚乱爆发后，许多事情就再也不是以前的样子了，不仅兄弟阋墙司空见惯，就连当局里那些曾经的执法者都变成了些人面兽心的怪物。虽说当地警察在战前就臭名远扬，但有些家伙甚至连制服都没换就投奔了帮派分子，你能想象这种事情吗？
  我要他们消失自然有我的理由。清除掉 Reshala 的残部，就是那些穿着公路警察制服的保镖，你肯定一眼就能认出他们来。带一件他们的防弹衣回来当作战利品吧。
- successMessageText: 你做了正确的事。捍卫制服的荣誉？真正在打击罪恶的人可没有穿着警察制服。给，我和 Mechanic 共同的一点谢礼。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 消灭 Reshala 的保镖
  - 条件[AvailableForFinish/HandoverItem]: 上交 Reshala 保镖的防弹衣

### [支线] 猎人之路 - 愤怒守望者 | 5d25e44386f77409453bce7b
- id=5d25e44386f77409453bce7b | 类型=Completion | 地点=56f40101d2720b2a4d8b45d6 | 前置=无 | 后继=无 | notDisplayed=False
- description: 下午好啊。我这儿有个任务要给你。在这一切发生之前，海关和化工厂的工作人员都住在海关的宿舍里，但现在那只有抢劫犯了。好吧，傻瓜 Scav 们尚且可以理解，这些家伙的脑子里只剩下空气，整天想着偷窃和抢劫。但你的那些雇佣兵同事们也经常去那里，这可一点也不好，他们见门就踹，看到什么东西都往包里塞，我恨透了这些该死的抢劫犯！无论什么时候，你都必须把保持人性放在第一位，否则又和那些街头混混们有什么区别？扫荡宿舍楼区域，让整个地方安静一会吧。
- successMessageText: 我已经听说了你做的事。看起来现在外面平静多了，抢劫犯少了很多。请收下我的谢意，孩子。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在海关宿舍楼区域击杀PMC

### [支线] 猎人之路 - 蒸发密令 | 5d25e44f86f77443e625e385
- id=5d25e44f86f77443e625e385 | 类型=Completion | 地点=5704e5fad2720bc05b8b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 请进，坐吧。有个特别的任务要给你。
  你知道海关北边的军事基地吗？有个狠角色、前海军步兵，把那个地方变成了自己的犯罪老窝。早在塔科夫全面爆发武装冲突之前，他就已经从祖国母亲的保卫者堕落成了一个彻头彻尾的罪犯——他们用暴力和金钱开道，把一切都拢到自己的手里：安保、海关通关服务，任何你能叫得出的名目他们都有染指。契约战争打响后，帮派分子们四散奔逃，但这个人没有......相反，他召集了一批最忠诚的打手——都是些退伍兵——在储备基地扎根下来。现在他公然在黑市上叫卖各种军事物资，这家伙有自己的交易渠道，不欢迎任何自己找上门的主顾。最糟的是，我听说他还在越过封锁线走私货物。我们必须立刻制止他的这些勾当。
  替塔科夫除掉这家伙吧，还有他的那帮保镖。别忘了把战利品带回来：盛大的狩猎活动需要与之相称的纪念。
- successMessageText: 欢迎回来，我的好猎手。也就是说，你把那些强盗都搞定了？简直难以想象你经历了什么样的恶战……无论如何，干得漂亮。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 找到并消灭Glukhar
  - 条件[AvailableForFinish/HandoverItem]: 上交在战局中找到的 Glukhar 保镖的头盔
  - 条件[AvailableForFinish/FindItem]: 搜索 Glukhar 保镖的尸体，搜集保镖佩戴的头盔（防护等级四级以上）

### [支线] 猎人之路 - 解放 | 5d25e45e86f77408251c4bfa
- id=5d25e45e86f77408251c4bfa | 类型=Completion | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 极端环境往往会以最快速度让一个人暴露出自己的真实本性。一部分人还能坚守人性，可有些人则不然。
  最近我听到了些传言，是关于当地涌现出的一股新势力的。这些家伙仿佛是一夜之间冒出来的，此前根本没有听说过他们的存在，所到之处大肆抢劫杀戮，拿走一切有价值的东西之后又消失无踪，仿佛从来没有出现过一样。这些家伙自称 Raider，也就是所谓的掠夺者。这些恶棍主要由服过役的混混和离队的雇佣兵们组成，在丢掉了身份和荣誉感之后决定联手组成新的势力，他们强行进驻各种设施，自称是在“保护”这些地方。我还听说他们背后有一个有权势的人，从国外操纵他们的一举一动。
  找到这群匪徒，用实际行动让他们明白，张牙舞爪的鼠辈在食物链中到底处于什么位置。
- successMessageText: 所以说，他们一直在实验室和军事设施附近活跃……啊，我希望他们不会挑起新的战争，光是眼下正在打的这一场就够我们受的了。好了，孩子，至少我们暂时不用再担心这个问题了。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 击杀掠夺者

### [支线] 急救措施 | 5d25e46e86f77409453bce7c
- id=5d25e46e86f77409453bce7c | 类型=Completion | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 你好。你也看到了，我孤身一人住在旷野之中。即使是在这片树林里，也总有各种不幸在上演。我偶尔会发现有无辜的好人正在挣扎求生，而我却无能为力。有一次，我亲眼看着一个人在我怀里停止了呼吸……你能明白那种感觉吗？
  我想准备一个小急救包，里面装些必备的医疗用品，这样我就能把它带在身边了。你能帮我弄来吗？
- successMessageText: 也许这样就能多挽回几条生命，不用眼睁睁看着人们死去了……谢谢你，孩子。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在战局中找到任意医疗包
  - 条件[AvailableForFinish/HandoverItem]: 上交物品
  - 条件[AvailableForFinish/FindItem]: 在战局中找到任意创伤处理类医疗物品
  - 条件[AvailableForFinish/HandoverItem]: 上交物品

### [支线] 礼节性拜访 | 5d25e48186f77443e625e386
- id=5d25e48186f77443e625e386 | 类型=Completion | 地点=5704e554d2720bac5b8b456e | 前置=无 | 后继=无 | notDisplayed=False
- description: 进来吧，我有些事情要问你。你知道海岸线的那座疗养院吧？最初那里是工厂的工人们度假疗养的地方，后来 TerraGroup 来了，把整个地方拿来安置他们自己的员工。我上一回去疗养院已经是很久之前的事情了，我有几个同学也曾住在周围，所以我想请你帮我打探一下他们的近况。你要去的是一个有着教堂的老旧村子。教堂有个牧师，他叫 Peter。还有 Misha，是个渔夫。还有村主席 Stepan。去他们的房子逛一圈，看看他们还在不在那儿。
- successMessageText: 你是说只有村主席的房子还完好无损？但你没有看到他本人？真的没有吗？天啊，Stepan……
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在海岸线的废弃村庄里找到村庄主席的房子
  - 条件[AvailableForFinish/CounterCreator]: 在海岸线的废弃村庄里找到渔夫的房子
  - 条件[AvailableForFinish/CounterCreator]: 在海岸线的废弃村庄里找到牧师的房子
  - 条件[AvailableForFinish/CounterCreator]: 以“幸存”状态撤离海岸线

### [支线] 怀旧之情 | 5d25e4ad86f77443e625e387
- id=5d25e4ad86f77443e625e387 | 类型=Completion | 地点=5704e554d2720bac5b8b456e | 前置=无 | 后继=无 | notDisplayed=False
- description: 来坐坐吧。你明白那种感受吗？有时候我坐在篝火旁，不禁回忆起这一切是怎么开始的。当初我得到了一张去疗养院的度假票。记得当时是晚上，大家都刚吃完晚饭各自回到房间，我坐在窗前凝视着海湾，景色看起来美极了——回忆起这副景象简直就像是在昨天一样——然后我就听到了皮卡的轰鸣声，不是一辆，而是一整个车队。那是你的雇佣兵同事、USEC 们。他们把所有人都赶出了疗养院，甚至不让人拿上自己的东西。
  我不太在乎衣服什么的，只是一直挂念着留在房间里的那本相册。去帮我把它找回来吧——比起委托，我更希望你能出于朋友间的情谊帮我这个忙。对我来说，这是对那些平静日子的痛苦回忆。房间在疗养院西楼顶层，靠近中间的部分；我会尽我所能报答你的。
- successMessageText: 天哪！你居然真的找到了！真的太感谢你了，孩子。给，拿着这个。几天前我在森林里碰到了一个重伤的 USEC。我尝试替他包扎，但那个可怜虫没能挺过来。我在他的包里找到了这把钥匙。看起来这是他们一伙人藏在疗养院区某个地方的仓库。我不太需要这个，但我想你可能用得上。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在疗养院找到Jaeger的海景房间
  - 条件[AvailableForFinish/FindItem]: 找到并获取 Jaeger 的相册
  - 条件[AvailableForFinish/HandoverItem]: 上交相册

### [支线] 鱼塘 | 5d25e4b786f77408251c4bfc
- id=5d25e4b786f77408251c4bfc | 类型=Completion | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 这不是我们的战士吗！你好吗？有没有在执行心中的正义？听着，我有个不寻常的任务要交给你，Mechanic 让我给他家准备一个特别的东西，这个“东西”实在是太精巧了，需要先进的电子设备才行。城市里根本没有哪个地方能弄到这种元件，该死的。但是有传闻说，TerraGroup 的秘密实验室里可是有很多这样的设备，里面甚至可能还包含了我们所需要的其他技术细节。我不想动动嘴皮子就把你送去实验室里冒险，但至少你可以帮我拿到进入实验室的通行证——我听说是某种钥匙卡。你能帮我这个忙吗？没错，卡片应该是全新的，别从其他人那买，谁知道那些阴险的商人为了赚钱能干出什么事来……
- successMessageText: 你拿到钥匙卡了？太棒了，孩子。现在我们只用想办法找到实验室就行了。我会自己处理好这事儿的，不用操心。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：TerraGroup 实验室访问钥匙卡
  - 条件[AvailableForFinish/HandoverItem]: 上交物品

### [支线] 狩猎之旅 | 5d25e4ca86f77409dd5cdf2c
- id=5d25e4ca86f77409dd5cdf2c | 类型=Completion | 地点=5704e3c2d2720bac5b8b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 进来吧，要来点茶吗？嗯，随便了。
  听我说，事情是这样的。不久之后我要招待几个朋友，我想和他们一起去打猎，但那几支破猎枪可上不了台面，是吧？还好我手头还有一些体面的栓动式步枪，都是西方的好货，但首先我得想办法校准这些枪，确保归零准确才行。紧接着我突然想到……为什么不来个一石二鸟呢？不仅校准了步枪，还能让塔科夫的土地上又少一个祸害。别担心，孩子，我开出的报酬是不会让你失望的。
- successMessageText: 你改的那支步枪真他娘的正啊！你知道吗？连我都有点想试试枪匠的活了，看着的确妙趣横生。这是你的奖励，我答应过你的。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 使用栓动式步枪消灭任意 Boss

### [支线] 储备 | 5d25e4d586f77443e625e388
- id=5d25e4d586f77443e625e388 | 类型=Completion | 地点=5704e5fad2720bc05b8b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 看看是谁来了，你好啊！听着，你知道储备站吗？就是靠近海岸线的那座旧军事基地。你大概已经去过那里了。所以情况是这样：我听到一些传言，他们说旧的军事基地仓库里还存放着很多食物。在军队离开后，强盗们开始在那里游荡——不只是一般的 Scav 而已，而是一些装备精良的前雇佣兵们。我想请你帮个忙，去看看地下仓库里还有没有什么物资剩下。千万注意安全，那里可是有许多混蛋在四处游荡。
- successMessageText: 东西都被他们一扫而空了？明白了，我们得尽快行动了。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在储备站找到地下食品仓库
  - 条件[AvailableForFinish/CounterCreator]: 以幸存状态撤离该区域

### [支线] 猎人之路 - 虐待狂 | 5edab4b1218d181e29451435
- id=5edab4b1218d181e29451435 | 类型=Elimination | 地点=5704e554d2720bac5b8b456e | 前置=无 | 后继=5edac34d0bb72a50635c2bfa | notDisplayed=False
- description: 你好啊，孩子。我突然有种怀旧的感觉。我记得沿着海岸散步的感觉，那里很美，海浪滚滚而来，阳光、海滩和日落都很壮丽，现在呢？到处都是腐烂和荒凉的气息，连城市里都无法呼吸……
  这还不算完，又有一个新的白痴出现了。外边的人似乎叫他 Sanitar。这个人渣在人身上做活体实验，也许还做了更可怕的事情。你不能就这样撒手不管，这样的人……不，这个恶心的怪胎根本不配继续活下去，他的存在就是对这片土地的亵渎。没了他世界只会变得更干净。管一管吧。
- successMessageText: 你做了正确的事，虽然别人说 Sanitar 做了一些好事，但他带来了更大的邪恶。不要听信别人的谎言。我以前见过他这样的人，一旦他们疯狂起来，就没有回头路了。你只能像射杀疯狗一样射杀他们。
- failMessageText: 所以你听信了那个女人的鬼话？你真的没发现她全程只是在利用你吗？就因为 Sanitar 给一些人看过病，他就冰清玉洁了，就不会去残害更多的人了？你打仗已经把脑子都给打坏了，连是非黑白都分不清。别出现在我面前了，我想一个人好好待着。
- whileAvailableMessageText: 所以你听信了那个女人的鬼话？你真的没发现她全程只是在利用你吗？就因为 Sanitar 给一些人看过病，他就冰清玉洁了，就不会去残害更多的人了？你打仗已经把脑子都给打坏了，连是非黑白都分不清。别出现在我面前了，我想一个人好好待着。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 找到并消灭Sanitar
  - 条件[AvailableForFinish/HandoverItem]: 上交在战局中找到的 Sanitar 背包

### [支线] 污秽遍地…… | 600302d73b897b11364cd161
- id=600302d73b897b11364cd161 | 类型=Elimination | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 你好，孩子。突然之间，城市又一次陷入了混乱，而据我所知，冲突活动主要集中在四个地方……必须有人坚决制止这一切骚动，如果你不害怕和犯罪作斗争的话，就来帮我一把手。将这些正在渗入城市骨髓的污秽势力彻底清除掉。
- successMessageText: 看来你并没有退缩……很好，这让我很欣慰。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在立交桥、中心区、森林或海关消灭 Scav

### [支线] 害虫防治 | 608a768d82e40b3c727fd17d
- id=608a768d82e40b3c727fd17d | 类型=Elimination | 地点=5704e5fad2720bc05b8b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 请进，找个地方坐吧。你知道本地的这些人渣把我搞得多生气吗？尤其是那些该死的拾荒者，行径简直和抢劫犯没有两样，我忍不了他们了。有些人从别人那偷东西是为了谋生，但是有些人却以此牟利——在塔科夫，后者可是数不胜数。就拿军事基地打比方吧，在冲突爆发前我去过那儿，当时的样子可棒了：到处都干干净净井然有序，就连操场也被打扫得铮亮，仿佛在阳光下闪闪发光。可是现在整个军事基地挤满了那些 Scav 人渣，还有掠夺者。帮我办件事儿：到那里去给混蛋们上一课，教训他们不管在什么时候都不能丢掉人性，否则和动物还有什么区别？
- successMessageText: 事情办成了？真是神清气爽，感觉连空气都变得清新了些。真可惜，孩子，除了用这种手段给他们点教训之外别无他法。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 消灭储备站兵营大楼周围的 Scav

### [支线] 猎人之路 - 工厂头目 | 60c0c018f7afb4354815096a
- id=60c0c018f7afb4354815096a | 类型=Elimination | 地点=55f2d3fd4bdc2d5f408b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 别来无恙啊，猎人。你一定知道老化工厂，TerraGroup 为了掩盖自己见不得人的生意一度占据了那里，而现在又有一个新的疯子出现在那里，当地人管他叫 Tagilla。这家伙要么是磕了集团留下的怪药，要么是整天在厂区里横冲直撞把脑子给撞坏了——总之工厂已经乱了套，因为这个疯子可以说是见人就杀，不分青红皂白地送所有人上西天。这个 Tagilla 用的是一把大锤，即使是训练有素的战士见了他挥舞凶器的样子也两腿发软。工厂的走廊上四处印着这个疯子留下的暗红“纪念”，那些无法无天的 Scav 们居然也学会了什么叫尊重，看到他就退避三舍。我们这座城市可容不下他这样的人……你应该明白你的职责所在，猎手。作为这场危险狩猎的证明，我要见到他的棒球帽。
- successMessageText: 真是个好消息。世界又清净了一点。你没被那个混蛋的大锤伤到吧？
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 找到并消灭Tagilla
  - 条件[AvailableForFinish/FindItem]: 在战局中找到BOSS鸭舌帽
  - 条件[AvailableForFinish/HandoverItem]: 上交BOSS鸭舌帽

### [支线] 猎人之路 - 无情杀手 | 60e71e8ed54b755a3b53eb67
- id=60e71e8ed54b755a3b53eb67 | 类型=Elimination | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 孩子，请进。有啥趣事想分享吗？呃，别说了，我不想听这些战争故事。我已经受够了。我已经没什么可以教你的了，你已经学会了自力更生，也懂得了正义。你也见过了本地的混混头子们。但是你有试过一次性把他们都拿下吗？这项任务并不容易，但我们值得为清除犯罪的目标付出努力。最重要的是，我们的选择都将留下怎样的印记。
- successMessageText: 我都听说了，都听说了……好啊——干的好啊，孩子，你做了正确的事情。我打心底里感谢你。
- failMessageText: 让我猜猜，你失败了？别气馁，我不怪你，孩子，这本来就不是简单的事情。加油，恢复好之后再回来。
- whileAvailableMessageText: 让我猜猜，你失败了？别气馁，我不怪你，孩子，这本来就不是简单的事情。加油，恢复好之后再回来。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 找到并消灭Tagilla
  - 条件[AvailableForFinish/CounterCreator]: 找到并消灭Killa
  - 条件[AvailableForFinish/CounterCreator]: 找到并消灭Reshala
  - 条件[AvailableForFinish/CounterCreator]: 找到并消灭Shturman
  - 条件[AvailableForFinish/CounterCreator]: 找到并消灭Glukhar
  - 条件[AvailableForFinish/CounterCreator]: 找到并消灭Sanitar
  - 条件[Fail/CounterCreator]: 任务进行过程中不得死亡，或以其它状态离开战局（阵亡、擅离、失踪、匆匆逃离均会导致失败）

### [支线] 快枪手 | 60e729cf5698ee7b05057439
- id=60e729cf5698ee7b05057439 | 类型=Elimination | 地点=5704e3c2d2720bac5b8b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 我们一起经历的也不算少了，孩子，你肯定也学到了不少东西。现在轮到你向初来乍到的新手展示技巧了。必须让他们明白，护甲是最重要的生存工具，在做好防护准备之前，他们最好别踏出藏身处半步。
- successMessageText: 你的表现让我这个老人大开眼界，但作为老战士，你就是自己的武器与坚城，身上披挂的钢铁护甲只是无关紧要的东西。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在森林不穿戴任何防弹护甲与头盔击杀任意目标

### [支线] 猎人之路 - 流浪汉 | 6179ad0a6e9dd54ac275e3f2
- id=6179ad0a6e9dd54ac275e3f2 | 类型=Elimination | 地点=5704e4dad2720bb55b8b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 嘿，孩子，我有个坏消息要告诉你。消息说有一群人渣在海岸边倒塌的隧道那里驻扎起来了。他们肆意妄为，入侵整个区域——就像一群野狼一样。他们还不是什么当地的混混，而是你的老同事们。据我所了解，冲突开始时，他们的基地就在湾区的某处。和你不一样，这些大兵并不想回家，而是占领了我们的地盘干坏事儿，真是些狗娘养的！干掉他们，但也要保持警惕，猎人。
- successMessageText: 你终于回来了！我差点都开始担心你了。过来吧，起码我还能给你拿点吃的。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 消灭游荡者

### [支线] 隐士 | 61904daa7d0d857927447b9c
- id=61904daa7d0d857927447b9c | 类型=Exploration | 地点=5704e4dad2720bb55b8b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 你好，雇佣兵。你知道吗？因为这场浩劫，我几乎失去了身边所有亲近的人。甚至吃晚饭的时候想找人聊天都找不到了。你倒是隔三差五会过来待上一两个小时，但是你还是得回去打你那已经没有意义的仗……呃，不是说要给你施压，只是想拜托你帮我办件事。我曾经有一个好朋友，我们一直形影不离：不仅性格相投，彼此也相当默契。他就住在 Dalniy 海角的那座旧村子里，但是远离人烟——他自己在郊外造了个独木舟。我不知道村子里还有没有人住，但我真的希望你能帮我找到我的朋友。我在这等你的消息，孩子。
- successMessageText: 哦，也就是说你找到了一份留言？等等，现在别打开它——我之后会自己看的。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在灯塔找到Jaeger朋友的藏身处
  - 条件[AvailableForFinish/FindItem]: 找到写给Jaeger的留言
  - 条件[AvailableForFinish/HandoverItem]: 上交留言

### [支线] 流浪狗 | 626bdcc3a371ee3a7a3514c5
- id=626bdcc3a371ee3a7a3514c5 | 类型=Elimination | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 你好啊，大兵。不过，这日子又有什么好的呢？一位智者曾经说过，人类生来就是为了互相折磨。起初读到这句话时，我其实并不相信会有这样的事情。但如今，每当想起这句话，我的心都会痛得滴血。
  城市新出现了一伙强盗，他们四处流窜，到处制造恐怖。我派人去与他们谈判，甚至想引导他们走上正道，但那些野蛮人却把信使的头砍了下来，把我写给他们的信塞在尸首嘴里，一起送了回来……
  这些家伙不是本地人，都是那些曾经的 USEC，甚至和自己的雇佣兵同事都不再往来了。这些人必须被消灭掉。这座城市是我们的家园，不是他们的猎场，没有谈和的必要了。
- successMessageText: 大兵，不要觉得你的手又沾满了鲜血。你想，你把邪恶从这片土地驱赶了出去。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在同场战局中找到并消灭 Knight
  - 条件[AvailableForFinish/CounterCreator]: 在同场战局中找到并消灭 Big Pipe
  - 条件[AvailableForFinish/CounterCreator]: 在同场战局中找到并消灭 Birdeye

### [支线] 猎人之路 - 管理者 | 639136df4b15ca31f76bc31f
- id=639136df4b15ca31f76bc31f | 类型=Completion | 地点=5704e5fad2720bc05b8b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 干我们这一行可没有停下来休息的资格。邪恶并不会因为我们有所怜悯而消失。所以，这是我交给你的下一个任务。
  有消息说，储备基地的火车站仓库附近出现了一些相当可疑的动静。无论是什么邪恶势力在那里聚集，我们都有必要查出真相，把他们绳之以法。别忘了按照约定先发射信号弹，这样我就能知道你开始干活了。如果那些强盗打算逃跑，我会负责解决掉他们。
- successMessageText: 看来我们配合得天衣无缝。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在储备站火车站台上发射一枚黄色信号弹（在同场战局中完成）
  - 条件[AvailableForFinish/CounterCreator]: 在储备站火车站处消灭任意敌对目标（在同场战局中完成）

### [支线] 别开枪！ | 639136e84ed9512be67647db
- id=639136e84ed9512be67647db | 类型=Completion | 地点=5714dc692459777137212e12 | 前置=无 | 后继=无 | notDisplayed=False
- description: 可愁死我了，勇士。有一些好人想要离开这座城市。最近的路线是走那条死亡大道，但是那里有狙击手。有个家伙告诉我说，如果你在那里发射一个信号弹，狙击手就会让你通行了，仅限于那一块区域。谁知道他是不是在骗人呢？有可能他是想让我们去送死。我们要怎么知道，狙击手看到信号会不会停火呢...我这有一些信号弹，要不你去试试？
- successMessageText: 看来他说的是真话，真的可以停火，我记下来了。你真的是豁出老命了，不是吗？我很欣赏你。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在塔科夫街区幸存，并通过 Klimov 大街信号弹撤离点撤离

### [支线] 美味香肠 | 63a88045abf76d719f42d715
- id=63a88045abf76d719f42d715 | 类型=Discover | 地点=5714dc692459777137212e12 | 前置=无 | 后继=无 | notDisplayed=False
- description: 请进，我想托你办一件事儿！听好了，你来到城里多久了？嗯，我有事儿需要你帮忙。过来点，尊重老年人嘛。城市里肯定还有一些咸狗香肠之类的东西。Mechanic告诉我有这玩意儿，我现在很想吃到它。我想疯了！
- successMessageText: 找到了？！真是奇迹啊！你有检查保质期吗？
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 搜索Nikitskaya大街的Shestyorochka（Шестёрочка）商店
  - 条件[AvailableForFinish/CounterCreator]: 搜索Primorsky大道的Sparja（Спаржа）商店
  - 条件[AvailableForFinish/CounterCreator]: 搜索Pinewood酒店的Sparja（Спаржа）商店
  - 条件[AvailableForFinish/CounterCreator]: 搜索Concordia区的Goshan商店
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：咸狗牛肉肠

### [支线] 屠宰场 | 63a9b36cc31b00242d28a99f
- id=63a9b36cc31b00242d28a99f | 类型=Elimination | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 你好啊，雇佣兵。又来看望老年人了？坐下吧，想喝点茶吗？不要？随你吧。听好了，附近又出现了一伙新的强盗。我们需要给这些人一点颜色看看，让那些闻风而来想要分一杯羹的小混混们也胆战心惊。所以我在想，你为什么不去拿着斧头大砍一通呢？这种事情你应该能办到吧？我就不信在你把整座城市变成屠宰场之后，还有哪个混蛋还敢再去淌这一条血路。
- successMessageText: 你过来之前为什么不先把自己收拾干净？拿着，带上你的报酬走人。我的老天爷，现在的孩子都怎么回事。记得洗澡！
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在日间工厂使用近战武器消灭 Scav
  - 条件[AvailableForFinish/CounterCreator]: 在塔科夫街区使用近战武器消灭 Scav
  - 条件[AvailableForFinish/CounterCreator]: 在灯塔使用近战武器消灭 Scav
  - 条件[AvailableForFinish/CounterCreator]: 在海岸线使用近战武器消灭 Scav
  - 条件[AvailableForFinish/CounterCreator]: 在储备站使用近战武器消灭 Scav
  - 条件[AvailableForFinish/CounterCreator]: 在中心区使用近战武器消灭 Scav

### [支线] 猎人之路 - 大动作 | 64e7b971f9d6fa49d6769b44
- id=64e7b971f9d6fa49d6769b44 | 类型=Elimination | 地点=5714dc692459777137212e12 | 前置=无 | 后继=无 | notDisplayed=False
- description: 我最近在树林里遇到了一对父子。两个人都已经精疲力尽，那个孩子还中了一枪。我把他们带回了住处，替那孩子处理了伤口，又给他们父子俩弄了点吃的。
  那位父亲告诉我，他们在撤离的时候没能逃出去。后来，一伙匪徒像圈养牲口一样把他们抓了起来，逼着他们在一家汽车修理厂干活，不干的话，等着他们的就是一颗子弹。那伙人的头目叫 Kaban。据说，在他们那群人里，他很有威望。父子俩趁着夜里逃了出来，冒着枪林弹雨一路逃进树林，我就是在那里发现他们的。可那个孩子伤得太重了，终究还是没能撑过那一夜。
  好了，回归正题。又有一伙新的恶棍出现在了城里，而能够出手惩治他们的只有你和我。帮这位父亲复仇，不要再让这样的惨剧降临在别人身上了。
- successMessageText: 这个世界又少了一个人渣。这是为了正义，孩子。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 找到并消灭Kaban

### [支线] 猎人之路 - 坏警察 | 6578eb36e5020875d64645cd
- id=6578eb36e5020875d64645cd | 类型=Elimination | 地点=5714dc692459777137212e12 | 前置=无 | 后继=无 | notDisplayed=False
- description: 过来吧，在篝火旁边找个位置坐坐。我有事跟你谈。我们净化塔科夫的努力绝不能半途而废。
  下一个目标外号叫 Kollontay，之前干过警察。这个家伙在和平时期就习惯了四处勒索，现在就更是变本加厉了。找到这个警察里的败类，把他干掉。据说他一直躲在市区的内务部学院里，附近的商场也有人见到过他的踪迹。
- successMessageText: 完事儿了？真是好消息。城里又干净了一点。这是给你的报酬。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 找到并消灭Kollontay
  - 条件[AvailableForFinish/HandoverItem]: 上交在战局中找到的Kollontay的警棍

### [支线] 口干舌燥 - 猎犬行动 | 665eeacf5d86b6c8aa03c79b
- id=665eeacf5d86b6c8aa03c79b | 类型=Elimination | 地点=5704e554d2720bac5b8b456e | 前置=无 | 后继=665eec1f5e47a79f8605565a | notDisplayed=False
- description: 我们得聊聊了：Sanitar 手下的混混一直趁着夜色出动，在海岸线大肆劫掠。他们显然是在搜寻什么东西。这种程度的骚动当然值得好好调查一番。我会去试着查查看他们到底在搞什么名堂，在此之前，帮我吓吓他们。
- successMessageText: 你把他们都赶跑了？太棒了！我得到的消息是，那帮混混在找渴死鬼藏身的地方——这人出门的时候总是会多带一瓶水，莫名其妙的。自从他和 Skier 混在一起之后我就没见过他了，我猜这人并没有从 Skier 那落到什么好下场。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 22:00-07:00 期间在海岸线消灭 Scav

### [支线] 幽闭恐惧症 | 669fa3979b0ce3feae01a130
- id=669fa3979b0ce3feae01a130 | 类型=Elimination | 地点=55f2d3fd4bdc2d5f408b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 好久不见，昨天我和朋友聊天的时候还提到了你呢，聪明能干的小伙子……接着他跟我讲了一个故事，曾经有一群菜鸟士兵去了那条破旧的地下通道。其中一个家伙慌了神，开始四处乱开枪，然后另外一个人丢了一枚手雷，把所有人都炸死了。我的朋友最后连绊线都没用到。他还挺失望的。
  如果你不想落个同样的下场，最好是带上趁手的武器，去化工厂的地下通道练练胆子。如果你能够在那样的条件下依旧沉稳杀敌，那么这片幽闭的空间将会成为你最大的安全感来源。
- successMessageText: 让我看看，这不活得好好的吗？现在你就是地下城的超级捍卫者了。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在工厂的老旧地下通道内消灭任意目标

### [支线] 妥善保管 | 669fa3a08b4a64b332041ff7
- id=669fa3a08b4a64b332041ff7 | 类型=Discover | 地点=55f2d3fd4bdc2d5f408b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 你好，大兵。看起来所有人都对化工厂很感兴趣，想方设法寻找它背后的秘密……当然，我有些发现：看样子那些 TerraGroup 的混蛋们在化工厂的地下层开辟了一座秘密仓库，专门用来储藏一些高度机密的货物，据说里面连安瓿瓶装的都是些极度少见的试剂。
  你应该明白，倘若这座仓库的秘密被其他人发现，那工厂将永无宁日了：一些人会试着强行闯入带走东西，而剩下的人自然会忙着在周围设伏拦截。想象一下，如果这些东西落入了不法分子手中，情况会有多糟糕。最好的办法就是，我们抢先一步拿到里面的东西，免得走漏消息闹出大事。这个活非常重要，你愿意帮忙吗？
- successMessageText: 真是个好孩子。我的同志会把它藏在安全的地方，永远都不会有人发现的。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在工厂的TerraGroup仓库找到化学品容器
  - 条件[AvailableForFinish/HandoverItem]: 上交化学品容器

### [支线] 卑鄙的外乡人 | 66ab9da7eb102b9bcd08591c
- id=66ab9da7eb102b9bcd08591c | 类型=Elimination | 地点=marathon | 前置=无 | 后继=无 | notDisplayed=False
- description: 你好啊。我曾经走过不少自己的路，但是时间已经将我打败了。那些捡破烂的人已经把所有热门区域搜刮了个遍，现在他们准备冲进森林了。
  我有天看到了一些Scav。他们试图把一些东西拖过海岸旁边的沼泽区域。即使是在战争之前，人们也说那里是大自然的死亡陷阱。只有我那位采蘑菇的朋友才知道如何活着通过那里。
  你愿意帮我吓走那些混蛋吗？让他们知道，森林不欢迎他们。记住，你也要穿过那边区域。
- successMessageText: 森林喜欢安静，我们可不能让这些人破坏掉这份静谧。
  我不知道他们从哪得知的这条路线... 我的朋友从来不会告诉别人。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在灯塔消灭 Scav（单场战局内完成）
  - 条件[AvailableForFinish/CounterCreator]: 使用转移功能从灯塔转移到海岸线（在单次战局中）
  - 条件[AvailableForFinish/CounterCreator]: 在海岸线消灭 Scav（单场战局内完成）

### [支线] 危机四伏塔科夫 | 66b38c7bf85b8bf7250f9cb6
- id=66b38c7bf85b8bf7250f9cb6 | 类型=Exploration | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 我感觉你对自己的生存技巧很有自信。但是，过于自信的人反而容易丢掉小命，因为你的感知被弱化了。塔科夫不会给这样的人提供容错机会。
  我之前在和我的一位同志讨论。这样说吧，绊线就是对你警惕性的终极考验。Partisan已经成功让一大群混蛋人间蒸发了，而且大部分情况下，那些人毫无察觉。没错，他的手段十分极端，他从阿富汗回来就一直是这样。
  尽管你对我的朋友而言，算是“善茬”，但即使没有他的参与，塔科夫还是四处散落者不少“礼物”。当然不止在森林里。有些时候，甚至一件不起眼的房间也会充满“炸裂的意外”。所以我需要你克制住自己此前的爱好，而是多去看看这样的地方。
  知己知彼，百战不殆。
- successMessageText: 瞧瞧，你活着回来了！你在外头奔波的时候，我整了这么个小玩意儿，你可以自己尝试摸索诡雷的奥秘了。就把它当做是课后作业吧，只是可别把自己炸开上天了。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在森林 USEC 营地周围找到高密度雷区
  - 条件[AvailableForFinish/CounterCreator]: 在中心区找到阔剑地雷

### [支线] 猎人必修课 | 66b38e144f2ab7cc530c3fe7
- id=66b38e144f2ab7cc530c3fe7 | 类型=Exploration | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 请进。你的绊线搞得怎么样了？不太行？这和朝别人瞎突突区别大了。你需要拥有正确的直觉，知道他们会往哪走。如果你是一名合格的猎人，你现在已经明白我的意思了。
  试着观察观察，那些混蛋经常在哪里出没。然后动动脑思考，把绊线放在哪里可以完美干掉他们。心急吃不了热豆腐，用你最好的判断力来完成这件事儿吧。
  只有这样，你的狩猎才能有所收获。等你找到一些好的点位了，再来找我。我不会很快布下绊线，否则你的队友们一不小心就要死翘翘了。
- successMessageText: 干得好，你找的这些位置都很不错。绊线是门手艺活，现在你只算个新手。如果你还需要更多的绊线，随时来找我。
  记住，时刻保持小心谨慎！不是只有你才会布设这些绊线，这意味着，每一片灌木丛、每一间屋子甚至每一块石头都可能让你随时毙命。一定要铭记在心。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在工厂需要破门的位置找到合适的绊线布设点位
  - 条件[AvailableForFinish/CounterCreator]: 在海关三层宿舍楼楼梯间处找到合适的绊线布设点位

### [支线] 事倍功半 | 675c1cf4a757ddd00404f0a3
- id=675c1cf4a757ddd00404f0a3 | 类型=Completion | 地点=56f40101d2720b2a4d8b45d6 | 前置=无 | 后继=675c1ec7a46173572a0bf20a | notDisplayed=False
- description: 你也明白，塔科夫周边算得上安全的逃生路线都挤满了人。无功而返死在了回程路上的好人要远多于丧命在路途中的。
  这就是为什么对于一个真正的猎人来说，必须时刻做好准备，个人逃生计划就很重要。现在时局艰难，所以我不建议冒不必要的风险。
  以海关为例。我听说有些撤离点已经被人控制了，他们只允许自己人进入。也就是说，外人没法随心所欲进入这些地方了。你最好能想办法打通这些撤离路径。
  这可能需要一些技巧，但对于真正的勇士来说，这点麻烦应该不成问题。
- successMessageText: 你已经得到了教训，可别轻易忘记了。
  你的回程路径随时都有可能被切断，要对这点有心理准备。无论你身处塔科夫的什么地方，只要能对地形、通路和安全逃生路线了然于胸，就能化为你的战术优势。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 获得指定道具并从秘密撤离点撤离
  - 条件[AvailableForFinish/CounterCreator]: 在海关找到秘密撤离点

### [支线] 猎人之路 - 管理者 | 6a45208043b8d7604d00b8d5
- id=6a45208043b8d7604d00b8d5 | 类型=Completion | 地点=5704e4dad2720bb55b8b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 当然，你不属于这里，因此没法真正融入我们，但我能感觉到你身上有内在的特质……某种心意相通、能够理解我们对这片土地的感情的特质。在我们这一行，绝不能停下脚步。稍有松懈，邪恶就会卷土重来。所以，这是我交给你的下一个任务。
  有消息说，污水处理厂里最近一直有可疑的人在活动，主要集中在厂房后面的铁路装卸区。无论是什么邪恶势力在那里聚集，都有必要打探真相，把他们绳之以法。别忘了按照约定先发射信号弹，这样我就能知道你开始干活了。如果那些强盗打算逃跑，我会负责解决掉他们。
- successMessageText: 我们俩配合得还不错，这倒是意外惊喜。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在灯塔火车站发射一枚黄色信号弹
  - 条件[AvailableForFinish/CounterCreator]: 在灯塔火车站范围内消灭任意目标

### [支线] 猎人之路 - 控制 | 6a4532e48e82d8ffea0c3eae
- id=6a4532e48e82d8ffea0c3eae | 类型=Completion | 地点=5b0fc42d86f7744a585f9105 | 前置=无 | 后继=无 | notDisplayed=False
- description: 最近还好吗，孩子。我又有新任务要交给你了。
  最近大家都在谈论那个所谓的实验室，说里面有成堆的值钱设备，只管去拿。这一切骚动真是令人作呕，尤其是想到这些强盗肯定会把整个地方洗劫一空，能搬动的一切都往背包里塞，在走不动路前绝不善罢甘休。我想拜托你下到那个设施里，狠狠地给他们点教训。最好当着他们的面杀一儆百，这样之后就没人敢继续效仿这群贪婪鬼四处抢劫。行动必须要快，否则这些老鼠是不会醒悟过来的。
- successMessageText: 你在实验室里清理罪犯的消息我都听说了，继续努力。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 同场战局中在实验室内消灭任意目标

### [支线] 塔科夫神射手 - 2 | 5bc479e586f7747f376c7da3
- id=5bc479e586f7747f376c7da3 | 类型=Elimination | 地点=any | 前置=5bc4776586f774512d07cf05 | 后继=5bc47dbf86f7741ee74e93b9 | notDisplayed=False
- description: 我的一个朋友——他叫沙贝尔斯基，他过去常常写关于战争的故事，甚至我都时常读他的书。最近他把自己书的版权全卖了，他们拿着版权在西方拍了一部电影。但是现在他想从电影公司得到更多的钱，显然，他现在有些进退维谷。他甚至给我发消息要我帮他从塔科夫里找一些像样的战士。看起来全世界都遗忘了我们的城市……只有当他们需要雇佣枪手时才想起我们，然后当一切都变好的时候他们又会忽视我们的处境。
  算了，我跑题了。你准备好接受神枪手先生的第二个考验了吗？你得从同一个距离命中目标的头部和腿部，还是 40 米开外。
- successMessageText: 不错，真不错。就连我都没法射得这么准了，我的年纪已经太大了。在你忙着精进枪法的时候，神枪手先生已经为你准备了新的试炼。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 使用栓动式步枪在 40 米开外命中任意目标的腿部
  - 条件[AvailableForFinish/CounterCreator]: 使用栓动式步枪在 40 米开外命中任意目标的头部

### [支线] 塔科夫神射手 - 3 | 5bc47dbf86f7741ee74e93b9
- id=5bc47dbf86f7741ee74e93b9 | 类型=Elimination | 地点=any | 前置=5bc479e586f7747f376c7da3 | 后继=5bc480a686f7741af0342e29 | notDisplayed=False
- description: 从以往的经验来看，你是个有能耐的枪手。神枪手先生知道了你的事迹，现在他想考验考验你的反应时间。天下武功，唯快不破，就像那部电影里那样，“黄金三镖客”，记得吗？不过这一回不是左轮决斗，而是只用栓动步枪的狙击对决，哈哈！
  塔科夫的生活自然不像电影里演的那么戏剧化，但是反应速度以及选择交战位置仍然至关重要，在城市环境下更是如此。如果你能挺下来，给自己整一顶牛仔帽吧，我这儿就有一顶。
- successMessageText: 既然你还完好无损地站在我面前，你的反应速度绝对比蛇还快！就像我之前承诺的那样，帽子是你的了。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 使用栓动式步枪在 25 米以内消灭 PMC 行动人员

### [支线] 塔科夫神射手 - 4 | 5bc480a686f7741af0342e29
- id=5bc480a686f7741af0342e29 | 类型=Elimination | 地点=any | 前置=5bc47dbf86f7741ee74e93b9 | 后继=5bc4826c86f774106d22d88b,5bc4836986f7740c0152911c,5c51aac186f77432ea65c552 | notDisplayed=False
- description: 你能撑下来实在是太好了，神枪手先生的任务可并不简单。在进行下一步试炼前，让我们先等上一会，毕竟从现在开始，他派给你的任务只会越来越复杂。我的建议是你必须增进自己的技术，做到和手中的栓动式步枪人枪合一才行。准备好了就回来找我吧。
- successMessageText: 准备好了吗？那就继续吧，神枪手先生对你的潜力可是很感兴趣。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 使用栓动式步枪消灭任意敌对目标

### [支线] 塔科夫神射手 - 5 | 5bc4826c86f774106d22d88b
- id=5bc4826c86f774106d22d88b | 类型=Elimination | 地点=56f40101d2720b2a4d8b45d6 | 前置=5bc480a686f7741af0342e29 | 后继=无 | notDisplayed=False
- description: 我已经在这里准备好了所有可能用得上的东西，不仅是为了这场冲突，也是为了将来其他什么糟糕的状况。我注意到很多幸存者都有所准备，但是我认为，这场危机只会不断恶化。尤其是食物，水，还有燃料的问题。每个人都想要温暖且干燥的庇护所。我想你是个幸运儿，能被神枪手先生注意到，他说我们得试试你在夜间行动的表现如何，就选在海关好了。
- successMessageText: 干得漂亮，简直是猫头鹰一样。你的身手让那些夜间猎手都相形见绌。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 21:00-05:00 期间在海关使用栓动式步枪消灭 Scav

### [支线] 塔科夫神射手 - 5 | 5bc4836986f7740c0152911c
- id=5bc4836986f7740c0152911c | 类型=Elimination | 地点=any | 前置=5bc480a686f7741af0342e29 | 后继=5bc4856986f77454c317bea7 | notDisplayed=False
- description: 每一个狙击手都会这么说：“对付狙击手的最好的工具就是另一个狙击手”。这在战场上不算什么稀罕事儿，但是你得记住，在真正战斗的中，大多数敌人并不是拿上枪没两天的 Scav 混混。他们往往训练有素、准备充分，说不定是和你一样的狙击手。这样的敌人很聪明、很难缠，会占据复杂地形，把你耍的团团转。就算他不是一个职业军人，而是前猎场看守人什么的也是一样。
  你得把敌人的狙击手死死压制住，让他在你的掌控之中——然后就任你宰割了，这就是神枪手先生要我转达给你的建议。有些胆大的 Scav 在摸到带瞄准镜的步枪后开始变得无法无天，爬到厂房屋顶上，甚至是山顶，觉得自己是真正的狙击大师了。你的任务是把这些不自量力的家伙捅下来，告诉他们站得越高跌得越狠的道理。
- successMessageText: 在狙击手的对决中没有什么鬼把戏这一说。干得好，孩子。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 使用栓动式步枪消灭狙击 Scav

### [支线] 塔科夫神射手 - 6 | 5bc4856986f77454c317bea7
- id=5bc4856986f77454c317bea7 | 类型=Elimination | 地点=any | 前置=5bc4836986f7740c0152911c | 后继=5bc4893c86f774626f5ebf3e | notDisplayed=False
- description: 无声击杀永远是最有效的手段，如果能在安全距离上办到这一点就更好了。神枪手安排的下一个任务可是实打实玩真的了。你需要装上消音器，然后在中距离狙杀一些老练的敌人。
- successMessageText: 敌人们肯定倍感无助，毕竟他们都不知道打他们的人在哪里。你做得很好，神枪手。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 使用消音栓动式步枪消灭 PMC 行动人员

### [支线] 塔科夫神射手 - 7 | 5bc4893c86f774626f5ebf3e
- id=5bc4893c86f774626f5ebf3e | 类型=Elimination | 地点=any | 前置=5bc4856986f77454c317bea7 | 后继=无 | notDisplayed=False
- description: 你喜欢古典文学吗？你认为陀思妥耶夫斯基为什么会受到那些从未去过俄罗斯的人欢迎？这个问题困扰了我很久，直到TerraGroup的丑闻被曝光。我想，它的高管们对经典著作及其对我们的影响捻熟于心。只有在这样一个社会里，人们才能把谎言、背叛和非理性的、无法解释的自我牺牲结合起来，而这样的自我牺牲在塔科夫里要远比其他地方多。陀思妥耶夫斯基曾经写过这个，这些人可以对他人做出最牢靠的承诺，同时也能成为最擅长杀戮的人。有时候我们都忘了这点。神射手给你的下一个任务是对你的个人的人格考量，不止是钱的事，这显而易见。
- successMessageText: 看来有时光是有点小聪明还不够，真正的聪明人总是会随机应变的。不过一把可靠的栓动式步枪总是没错的，狙击手现在还没有回话，所以得等上一会才会有下一次任务。如果后续还有新的考验，我会通知你的。
- failMessageText: 这可不是什么容易事儿。再来一次吧。
- whileAvailableMessageText: 这可不是什么容易事儿。再来一次吧。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 使用栓动式步枪一命消灭 PMC 行动人员
  - 条件[Fail/CounterCreator]: 在完成任务之前，你不能阵亡或是离开战局（状态：阵亡、匆匆逃离、失踪）

### [支线] 生存者之路 - 危险零距离 | 5d25aed386f77442734d25d2
- id=5d25aed386f77442734d25d2 | 类型=Completion | 地点=any | 前置=5d25bfd086f77442734d3007 | 后继=5d25c81b86f77443e625dd71 | notDisplayed=False
- description: 你好啊，孩子，快进来坐坐吧。我们选择的这条道路可不简单，任何时候都充满了挑战。你得有些真本事，才能和本地的人渣还有你们 PMC 自己里的那些渣滓对抗。比如，行动得更快、更隐蔽，在战斗中更灵敏、更致命。绕到敌人侧面，在他自己的地盘上击败他。光靠把自己绑在防弹衣里可没法精通这些技能。因此，在出发去干些真正的大事之前，你得先掌握这些技能才行。
- successMessageText: 就像某个名人所说的，“轻盈如蝶舞，锋利如蛰刺”。对于我们来说算是至理名言，你最好能在行动中牢牢记住这句话。感觉到自己脱下护甲的时候有多轻盈了吗？只要能掌握足够的身法技巧，或者在远距离一招毙敌，那么护甲对于你就完全是身外之物。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 不穿戴任何防弹衣或插板胸挂消灭 PMC 行动人员

### [支线] 生存者之路 - Zhivchik | 5d25bfd086f77442734d3007
- id=5d25bfd086f77442734d3007 | 类型=Experience | 地点=any | 前置=5d25b6be86f77444001e1b89 | 后继=5d25aed386f77442734d25d2 | notDisplayed=False
- description: 你好勇士！狩猎已按计划进行。你知道吗，你给我准备的水真的派上用场了。顺便问一下，你在处理脱水这方面的能力有多好？让我看看一个缺水的PMC到底能撑多久。
- successMessageText: 不可能！这么久？真是令我印象深刻。拿着这个走吧，喝完之后你会神清气爽。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 获得“脱水”状态，并维持状态生效 5 分钟时间（工厂除外）
  - 条件[AvailableForFinish/CounterCreator]: 以幸存状态撤离该区域

### [支线] 生存者之路 - 受伤的野兽 | 5d25c81b86f77443e625dd71
- id=5d25c81b86f77443e625dd71 | 类型=Completion | 地点=any | 前置=5d25aed386f77442734d25d2 | 后继=5d25cf2686f77443e75488d4 | notDisplayed=False
- description: 你好。你有没有遇到过正身处困境，却因为要拼搏求生，而来不及处理伤口的情况？在绝境中，抗压能力是能否生存下去的关键。想在塔科夫艰难度日，这项技能非常重要。给我展示展示你忍受痛苦的能力。
- successMessageText: 办妥了？干得漂亮。如果我们继续保持这种节奏，我想我们就能比计划中更早的恢复这座城市的秩序。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在疼痛状态下消灭 Scav

### [支线] 生存者之路 - 硬汉 | 5d25cf2686f77443e75488d4
- id=5d25cf2686f77443e75488d4 | 类型=Completion | 地点=5704e3c2d2720bac5b8b4567 | 前置=5d25c81b86f77443e625dd71 | 后继=5d25e2d886f77442734d335e | notDisplayed=False
- description: 你知道的，战斗中和狩猎中一样，只有两种行为。你要么是猎人，要么是猎物。既然你想要当个真正的猎人，那你必须要学会在不被察觉的情况下追踪你的猎物。
- successMessageText: 解决了？太棒了！多流汗少流血。别放松，你要干的事儿还多着呢。
- failMessageText: 所以你搞砸了是吧？没事，就当是别人给了你一点教训。现在重新来过，但是教训要记好了。
- whileAvailableMessageText: 所以你搞砸了是吧？没事，就当是别人给了你一点教训。现在重新来过，但是教训要记好了。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在森林单局内不使用医疗物品的情况下击杀Scav
  - 条件[Fail/CounterCreator]: 任务进行期间不得使用任何医疗用品

### [支线] 生存者之路 - 冷血 | 5d25d2c186f77443e35162e5
- id=5d25d2c186f77443e35162e5 | 类型=Completion | 地点=any | 前置=5d25e2d886f77442734d335e | 后继=5d25e29d86f7740a22516326 | notDisplayed=False
- description: 在任何情况下。如果你失去控制或惊慌失措了，哪你就死定了。不管发生了什么，不管你是被子弹击中还是喝醉了。你都必须始终保持自制。跟我证明你能做到它。
- successMessageText: 你能控制住？好啊！说老实话，我当时没想过你能成功，毕竟这次任务不简单。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在“颤栗”效果下爆头消灭 PMC 行动人员

### [支线] 生存者之路 -  雕鸮 | 5d25e29d86f7740a22516326
- id=5d25e29d86f7740a22516326 | 类型=Completion | 地点=any | 前置=5d25d2c186f77443e35162e5 | 后继=5d25e2a986f77409dd5cdf2a,5eaaaa7c93afa0558f3b5a1c | notDisplayed=False
- description: 根据我读过的一篇文章所说，人类的眼睛是能够适应黑暗的。你知道的，我去试过，结果我差点在一片防风林里把自己的腿摔成两截。显然，我已经过了这种蜕变的年纪了。但我想，这应该是你的黄金时期。经过良好训练的夜间视觉是可以在那些艰难的任务中极大地帮助我们的。认真对待吧。可不要作弊啊！不能用夜视仪或热成像！
- successMessageText: 你说他们有多少人来着？6 个？你没用夜视仪就把他们给揍趴下了？你做到了，孩子，我们的暗夜猎手已经横空出世了。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 不使用任何夜视仪或热成像瞄具，在 21:00-04:00 期间消灭 Scav （工厂除外）

### [支线] 生存者之路 - 战地军医 | 5d25e2a986f77409dd5cdf2a
- id=5d25e2a986f77409dd5cdf2a | 类型=Completion | 地点=any | 前置=5d25e29d86f7740a22516326 | 后继=无 | notDisplayed=False
- description: 我已经数不清在当猎场看守的时候，见过多少狗娘养的。有茫茫多的人在森林中迷过路，这其中有些人能脱离困境，活的好好的，而有些人就惨了。要想活着走出森林，你他妈得足够混蛋才行。你得自己会成为一个真正的生存者。准备好了再来找我。
- successMessageText: 苏沃洛夫元帅怎么说的来着？平时多流汗、战时少流血！这项技能会很有用的，相信我吧。对你的训练就快收尾了。看起来你已经准备好成为真正的猎手了。
- 条件:
  - 条件[AvailableForFinish/Skill]: 达到指定的活力技能等级

### [支线] 猎人之路 - 支配者 | 5d25e2d886f77442734d335e
- id=5d25e2d886f77442734d335e | 类型=Completion | 地点=any | 前置=5d25cf2686f77443e75488d4 | 后继=5d25d2c186f77443e35162e5 | notDisplayed=False
- description: 请进。作为一名猎人，我可以很确信地告诉你保持对目标的控制是非常重要的。很多时候一旦你设法致盲了猎物，那么即使是最危险的猎物，也会变得不那么危险。这对人来说也一样。如果一个人像鼹鼠一样瞎，那谁还会在乎他使的什么枪？那就向我证明，你有办法弄瞎你的敌人。
- successMessageText: 学到了？很好。你看，你已经有一百种方法置敌人于死地了。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 击杀被闪光弹致盲的PMC

### [支线] 生存者之路 - 瘾君子 | 5eaaaa7c93afa0558f3b5a1c
- id=5eaaaa7c93afa0558f3b5a1c | 类型=Elimination | 地点=5704e3c2d2720bac5b8b4567 | 前置=5d25e29d86f7740a22516326 | 后继=无 | notDisplayed=False
- description: 现在的日子很艰难，你肯定有着亲身体会。为了能活下去，人们往往需要穷尽一切手段，哪怕是不利于健康的东西也只能咬牙接受。那些战斗兴奋剂就是最好的例子——究其本质而言，它们和毒品无异，但这些东西在当地出现的频率越来越高，就连本地人也偶尔会带上那么一两根。我觉得 TerraGroup 肯定和这种东西的泛滥脱不了干系。但不管你是否心存抗拒，你都必须学会战斗中使用它们，这些药物不光是能致人死地，如果运用得当，说不定能帮你挺过战斗，让你多一分逃离的希望。
- successMessageText: 所以，看起来你成功了。头感觉怎么样？头晕？感觉要倒下去了？这些是使用药物造成的副作用，是你为了活下去所必须付出的代价，现在你得用余生的健康去补偿你那一时的潜力释放了。但不管怎样，听我说，我有一些能帮你减轻这些副作用的建议，所以下次你会感觉好受些。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在任意兴奋剂效果影响下，在森林消灭 Scav

### [支线] 直播 - 4 | 6391372c8ba6894d155e77d7
- id=6391372c8ba6894d155e77d7 | 类型=Completion | 地点=5714dc692459777137212e12 | 前置=63a511ea30d85e10e375b045 | 后继=64ee99639878a0569d6ec8c9 | notDisplayed=False
- description: 同样的情节又一次发生了，一段新的录像，还带着一个奇怪的符号。到底发生了什么？他们难道在搞活人献祭？但到底又是献祭给谁、出于什么目的这么做的？问题一个接一个冒出来，但我们根本没法得到答案。每次看到那些血腥场面的时候，我都以为不可能会有更糟糕的事情发生了，但是到下一次，他们总能证明我低估了这些人的底线。
  我们曾经和图财害命的强盗交手，就连那些以杀人为乐的变态都不是我们的对手——这些家伙虽然低劣，但至少我们能够理解他们行为的动机……他们内心的兽性击溃了人性，所以才堕落成那个样子。但是人们是出于什么目的才会……呃，我完全没法搞懂。所以，大兵，我会在森林里寻找他们的踪迹，你去街区做同样的事情。他们肯定在什么地方有个藏身处。找到这个符号，我相信我们会有发现的。
- successMessageText: 你找到了？在哪？那里有人吗？那个符号是什么意思？疑问不仅没有得到解答，反而冒出了更多的问题……
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在塔科夫街区找到邪教徒的聚集处
  - 条件[AvailableForFinish/CounterCreator]: 以幸存状态撤离该区域

### [支线] 直播 - 3 | 63a511ea30d85e10e375b045
- id=63a511ea30d85e10e375b045 | 类型=Completion | 地点=5714dc692459777137212e12 | 前置=63913715f8e5dd32bf4e3aaa | 后继=6391372c8ba6894d155e77d7 | notDisplayed=False
- description: 你好，Mechanic 把情况告诉我了。这真的太可怕了，把我都给吓坏了。但是我们还是要查明真相。你知道更可怕的事情是什么吗？Mechanic 那的摄像机里不仅有我们已经看过的片段，还有更多在其他地方拍摄的视频。就好像背后的人在和我们玩猫鼠游戏、故意让我们看到那些录像似的。画面里有一间老房子，桌子上堆着一些尸首，摆得就像是圣经里的场面一样。去找找具体位置，感觉整件事情都不太对劲。
- successMessageText: 你找到了？你说这就是那个地方？那里又有一个摄像机？不要碰它，我会让 Mechanic 手下的专业人士去处理的。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在塔科夫街区的老房子里找到血腥直播的拍摄地点
  - 条件[AvailableForFinish/CounterCreator]: 以幸存状态撤离该区域

### [支线] 直播 - 5 | 64ee99639878a0569d6ec8c9
- id=64ee99639878a0569d6ec8c9 | 类型=Exploration | 地点=any | 前置=6391372c8ba6894d155e77d7 | 后继=无 | notDisplayed=False
- description: 好了，是时候开展下一步行动了。现在我们已经知道了，那些戴兜帽的邪教徒和这件事有关系。让他们招供！逼他们现出原形！用实际行动证明我们不是好惹的。想办法逮住这帮怪胎，干掉一两个，最好是头目什么的。不过就在市区里应该是找不到他们的，能藏身的地方太多了。但我们要把战利品留在他们的据点里，让这些邪教疯子明白我们的意思。我听说在废弃工厂之后，这些人又有了新的仪式地点。去那座旧公寓楼看看吧，孩子。
- successMessageText: 好！你觉得他们明白我们的意思了？我也觉得！谢谢你。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 找到并消灭邪教徒祭司
  - 条件[AvailableForFinish/CounterCreator]: 在塔科夫街区的Chekannaya大街找到仪式地点
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在仪式地点藏匿一把邪教徒的刀

### [支线] 试炼之路 | 675c1ec7a46173572a0bf20a
- id=675c1ec7a46173572a0bf20a | 类型=Elimination | 地点=56f40101d2720b2a4d8b45d6 | 前置=675c1cf4a757ddd00404f0a3 | 后继=无 | notDisplayed=False
- description: 你好，雇佣兵。我有一份新的任务要交给你，不过我还不确定这到底能不能算一项差事。
  不过既然你总是乐意帮忙，那我就给你个大显身手的机会好了。
  那些为城市的利益而战的资深战士们需要补充他们的燃油储备。
  所以这回你可以帮帮他们，顺道向我展示一下你能轻松解决外边游荡的那些醉鬼。
  给我带两桶燃料来，但不要逃避战斗。要想彻底清除这座城市的污点，你们必得时刻准备好和敌人硬碰硬才行。
- successMessageText: 瞧瞧是谁回来了。你遇到那帮土匪了吗？好嘛，你还真适合干这一行啊。
  现在快去休息会吧，不过记得保持联系。还有不少正经的差事等着你呢。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在海关的老加油站击杀 Scav
  - 条件[AvailableForFinish/CounterCreator]: 在海关的新加油站击杀 Scav
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：金属油桶

### [支线] 如楔在侧 | 69ce1cfb298a6529b30d712b
- id=69ce1cfb298a6529b30d712b | 类型=Elimination | 地点=69af492a4819ea4ba10a69c5 | 前置=69bbfae89ce356593c0e2f35 | 后继=无 | notDisplayed=False
- description: 坐吧，孩子，我有些事情要找你聊聊。Partisan 在海边看到了几架直升机，机身上没有任何识别标记，直奔外海某艘船的方向去了，可以肯定的是，它们绝对不是朝着城市来的。那些直升机都是军用的，我们能确定 UNTAR 维和部队没有这种东西；至于城里的 PMC 势力，也没见过哪一方能拥有这样的装备。Partisan 说他过去见识过这样的场面，但不是在塔科夫，而是在阿富汗的群山之间……
  我只知道，如果那些直升机是什么特种部队派来的，那就准没好事。他们八成是在从其他精英单位抽调老兵来，也许是 SAS，搞不好是摩萨德，或者鬼知道其他什么部队。
  我问了 Mechanic，想知道他那里掌握的情况有多少。碰巧他一直在监视那些人的通信频段，还截获了指挥官的呼号……Wedge，虽然听着很模糊，但应该就是这个。听着，孩子，我们不能放任这个”楔子“在塔科夫安营扎寨，不只是船上，更不能放任他们把手伸到城市里来。我可全都指望你了。最好是以牙还牙、以血洗血——就像他们喜欢说的那样。
- successMessageText: 还好我相信自己的直觉，在第一时间让你出手了。我们必须时刻警惕，保护塔科夫免受这种杀人狂的侵扰。要是让这种人占据了立足之地，那么整个城市马上就会沦入他们的掌控之中，到时候就再也没有人能阻止他们为所欲为了。
  下次再看见那些全身黑衣服的佣兵，千万不要轻易把他们放跑了，说不定关系到整个城市的存亡。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 找到并消灭 Wedge 小队
  - 条件[AvailableForFinish/FindItem]: 从 Wedge 身上取得战利品
  - 条件[AvailableForFinish/HandoverItem]: 上交找到的物品

### [支线] 黑幕交易 | 5d25e48d86f77408251c4bfb
- id=5d25e48d86f77408251c4bfb | 类型=PickUp | 地点=any | 前置=5979ed3886f77431307dc512,5d24b81486f77439c92d6ba8 | 后继=无 | notDisplayed=False
- description: 嘿，借你一分钟。塔科夫这有个阴险的家伙。伙计们都叫他 Skier，这个人真的很阴险，我相信以他的道德标准，就算是公然犯罪也毫不羞耻，我一定要把这个家伙给看住了。有人说他正在寻找某些 U 盘，虽然我不知道里面都有些什么。但以我对他这类人的了解，我敢打赌里面一定有什么淫荡的东西，我挺想亲自确认一下。
  我不知道他在找什么样的U盘，但只要给我拿几块，我们随后就会知道了——这也是为什么你必须自己去找这些 U 盘，可不能让商人们拿点废货就把我们给糊弄过去了。Mechanic 将帮我们查清楚里头到底都是些什么东西。
- successMessageText: 搞到了？让我们看看他在干嘛。我和Mechanic会检查这个U盘的，等会儿再来一趟，我们会告诉你其中的内容。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在战局中找到加密U盘
  - 条件[AvailableForFinish/HandoverItem]: 上交加密U盘

### [主线] (无名称) | 68e3a3d2ff05916c250a898b
- id=68e3a3d2ff05916c250a898b | 类型=Completion | 地点=any | 前置=68e3a3bc02661eb2d30ce389 | 后继=68ffe7f5955bb2fc200c6f5f,68ffe7ff6120149f4f0f2d61,68ffe808f4ec6f4d8d078232,68ffe8149f98d8e00f0ce6a8,68ffe821841a7a1cac00dd05 | notDisplayed=False
- 条件:
  - 条件[AvailableForFinish/CompletableItem]: 调查BEAR小队的遭遇
  - 条件[AvailableForFinish/CounterCreator]: 找到疗养院地下设施的入口通道
  - 条件[AvailableForFinish/CounterCreator]: 进入设施
  - 条件[AvailableForFinish/CompletableItem]: 在“迷宫”中找到 BEAR 小队的踪迹
  - 条件[AvailableForFinish/CounterCreator]: 调查 1156 项目周围的 BEAR 小队集结点
  - 条件[AvailableForFinish/CounterCreator]: 找到小队长
  - 条件[AvailableForFinish/CompletableItem]: 收集小队的更多消息 (提示: 看样子这个呼号 Leshy 的人留下了记录——我得去检查一下)
  - 条件[AvailableForFinish/CounterCreator]: 进入上锁的办公室 (提示: 无论是谁把测试对象从笼子里放出来的，之后肯定必须躲藏起来)
  - 条件[AvailableForFinish/CompletableItem]: 听办公室里的录音带 (提示: 科学家在自杀之前留下了一段录音——我应该听听看)
  - 条件[AvailableForFinish/GlobalVariableValue]: 调查科学家的尸体 (提示: 也许我能在他们身上找到上锁办公室的钥匙)
  - 条件[AvailableForFinish/GlobalVariableValue]: 将录音带交给 Jaeger (提示: Jaeger 肯定不会相信我所说的，所以还是直接把录音带给他吧)

## 商人: Therapist (54cb57776803fa99248b456e) — 任务 57

### [支线] 短缺 | 5967733e86f774602332fc84
- id=5967733e86f774602332fc84 | 类型=PickUp | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 你好，雇佣兵。我有一份小小的兼职工作，你感兴趣吗？不是什么难题，但是做了有好处。别忘了，我这里能管你吃喝，还能提供专业的医疗服务。但是在这之前，先和你聊聊这份工作。
  我需要三个急救包。它们叫做“Salewa” —— 是西方那边的红色急救包。我需要你自己找到它们，而不是随便找个地方买来。毕竟，我们对待这件事情很严肃的。我们已经互相认识了，但我还没法那么信任你。你愿意接下这个活吗？
- successMessageText: 谢谢你，小伙子。这些急救包会派上用场的。不过，别的就不会告诉你了。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：Salewa 急救包
  - 条件[AvailableForFinish/HandoverItem]: 上交Salewa急救包

### [支线] 卫生标准 | 59689ee586f7740d1570bbd5
- id=59689ee586f7740d1570bbd5 | 类型=PickUp | 地点=55f2d3fd4bdc2d5f408b4567 | 前置=无 | 后继=596a204686f774576d4c95de | notDisplayed=False
- description: 下午好，小伙子。很高兴来的人正好是你，我有一件急事儿要交给你。这关系到所有幸存者，或者说，那些不幸的人能否继续在这片战区中生存下去的关键问题。
  你知道化工厂在哪，对吧？像你一样的雇佣兵曾经为那座工厂里产出的东西大打出手。不过事到如今这一切已经不重要了——我高度怀疑某种毒素正在从厂区渗入地下水，由于来自外界的饮用水补给经常受到干扰，如今地下水已经是我们唯一的饮水来源。你明白现在的情况有多严重吗？就算是滤水器对这种污染也完全派不上用场。如果没法尽快解决水污染的事情，我们迟早都会被毒死。
  为了弄清楚污染的源头，我需要设备对水源的放射性与蒸汽成分进行分析——工厂车间里应该有不少气体分析仪之类的地下。找到了之后，就把分析仪留在工厂的实验小楼里，我的人之后会去取的。
- successMessageText: 谢谢你，你今天所做的对于塔科夫所有幸存下来的不幸之人都意义重大。
- 条件:
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：气体分析仪
  - 条件[AvailableForFinish/CounterCreator]: 在工厂找到有气体分析仪的房间
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在日间工厂的医学实验室内藏匿气体分析仪

### [支线] 水瓶座行动 | 59689fbd86f7740d137ebfc4
- id=59689fbd86f7740d137ebfc4 | 类型=PickUp | 地点=56f40101d2720b2a4d8b45d6 | 前置=无 | 后继=无 | notDisplayed=False
- description: 很高兴见到你，年轻人，你来得正巧。我有一些事情要委托你去办，报酬自然是不会亏欠你的。感兴趣吗？
  你应该已经知道，当前这里面临着饮水污染问题，我们已经发现了地下含水层受到了污染，不过别担心，我和我手下的工程师团队已经开始想办法解决污染问题了。我找你来是为了其他事情。
  就在这个时候，有些人却决定利用大家的不幸来牟利。我知道的就有盘踞在宿舍楼的那个帮派，他们搜罗了大量纯净水囤积在自己的窝点里。我并不否认每个人都有生存下去的权利，但当有人比他们更需要净水来维持生存时，囤积居奇就是一种罪恶。当所有人都在忍受缺水的痛苦的时候，他们却在背后高价出售纯净水牟利！你应该知道他们的窝点在哪，收缴他们的水源，带到我这里来。
- successMessageText: 真是太棒了！你说，要我分一些利润给你……？你难道以为我会像那些混混一样用水去牟利吗？这纯粹是一项人道主义行动！
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在海关找到被藏在宿舍楼里的水
  - 条件[AvailableForFinish/HandoverItem]: 上交任意在战局中找到的饮用水

### [支线] 带血的水 | 5968eb3186f7741dde183a4d
- id=5968eb3186f7741dde183a4d | 类型=Elimination | 地点=56f40101d2720b2a4d8b45d6 | 前置=无 | 后继=无 | notDisplayed=False
- description: 我的人已经搞清楚究竟是哪些人参与了这些囤积饮用水的不法行动。如果换做是其他时候，我们肯定会通过法律途径打击这种投机行为。但正如你所知道的那样，秩序和法律在塔科夫已经不存在了。
  长话短说，我们确定了是海关的那伙 Scav 搞的鬼，他们的势力范围远超宿舍楼区域。我提这种要求时真的很不舒服，但只有做出武力回应，他们才会知道自己惹错了人。去给他们上一课，让他们这辈子都忘不了这个教训，这样其他投机分子也会瑟瑟发抖，亲身体会到自己的勾当都给其他人带来了什么灾难。你能办好这件事吗？
- successMessageText: 都处理好了吗？虽然你今天夺走了几条生命，但多亏了这一必要之恶，你让许多平民又一次有了继续生存下去的希望。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在海关击杀Scav

### [支线] 善良之针 | 5969f90786f77420d2328015
- id=5969f90786f77420d2328015 | 类型=PickUp | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 小伙子，我这还有一项任务等着你去办。你应该知道，我一直在帮忙照顾那些没法越过封锁线的平民，他们没能及时疏散，只能在庇护所里找到容身之地。
  至少，你比你的那些雇佣兵同伴们更像是一个正常人，而不是像大多数来到我这儿的年轻人一样，完全丧失人性，堕落为了怪物。 因此，请再帮我个忙吧。我需要肾上腺素注射器，这种药物在抢救的时候能派上很大用场。我是绝对不会出手卖给其他人的，小伙子。毕竟我是个医生，正经宣过誓的那种，只要我还活着，就会继续救助平民，这其中有妇女、老人和孩子。这些人不是战场硬汉，没法光靠咬着步枪背带硬挺过去；他们不能、也不应该忍受这样的痛苦。
  据我所知，双方 PMC 执行任务的时候都会在车里备上不少医疗补给，其中就有这种注射器，但我的人没法冒着危险去取。请帮我找几根肾上腺素来，帮我抚平战争带给平民的创伤。
- successMessageText: 我打心底里地感谢你，对你的感激之情简直无以言表。在相同的处境下，大多数人只会想自己藏着，但是你却无私地分享给有需要的人。再次谢谢你！
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：肾上腺素注射器
  - 条件[AvailableForFinish/HandoverItem]: 上交物品

### [支线] 药剂师 | 5969f9e986f7741dde183a50
- id=5969f9e986f7741dde183a50 | 类型=PickUp | 地点=56f40101d2720b2a4d8b45d6 | 前置=无 | 后继=无 | notDisplayed=False
- description: 我想请你帮个忙，别摆出那副样子，当然不是义务劳动！在海关宿舍区那座较矮的宿舍楼里，没错，就是那个被强盗霸占的地方，一楼的某个地方住过一个来自工厂的年轻急救员。不幸的是，我不记得房间是多少号了。在医务工作者日那天，天色已晚，聚会正酣，你懂的。作为一名年轻的专业人员，他虽然还是个实习生，但是非常有上进心，一心准备投考圣彼得堡的军医学院。在他房间的保险箱里，有一个很特别的新型医疗装置，那是他的雇主新发给他的。
  在冲突刚爆发那会，那个急救员曾试着开上他的圣彼得堡牌照蓝色伏尔加 2109，赶在城市被彻底封锁前出城。我不知道他成没成功，但就我听说的情况来看，他要么被加油站附近的路障给拦住了，要么就是被军方抓走了。不管怎样，我觉得我能把那个工具用在正当的地方。倘若情况继续恶化下去，你也出了什么意外的时候，说不定能救你一条命——当然，我不希望这样的事发生，别把我想那么坏。不过……我觉得你比我更看的开，所以帮我这个忙吧。啊对了，我差点忘了，Arshavir，就是经手服装生意的那位，我们在这件事上的利益是相通的。他有个好东西给你，当然你得先把差事干完。
- successMessageText: 合作愉快，小伙子。我会跟Arshavir提到你的。这是你的报酬。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在海关找到装有设备的箱子
  - 条件[AvailableForFinish/HandoverItem]: 上交手提箱
  - 条件[AvailableForFinish/CounterCreator]: 在海关找到急救员的车
  - 条件[AvailableForFinish/CounterCreator]: 进入海关双层宿舍楼的114房间

### [支线] 供给计划 | 596a0e1686f7741ddf17dbee
- id=596a0e1686f7741ddf17dbee | 类型=PickUp | 地点=5704e3c2d2720bac5b8b4567 | 前置=无 | 后继=596a101f86f7741ddb481582,63ab180c87413d64ae0ac20a | notDisplayed=False
- description: 你好啊，小伙子，真的很高兴见到你。我有一个非常重要的任务，一般人可办不好，你要接下吗？那就听好了：你知道森林的锯木厂吧？在“契约战争”时期，那里曾经被 TerraGroup 后座临时后勤补给基地。我对 TerraGroup 在当地的供给计划相当感兴趣，相关的文件应该还在留在锯木厂里的什么地方。试着搜搜锯木厂角落里工人们的临时宿舍，你要找的应该是一个带有特殊标记的保险文件盒。
- successMessageText: 真是一如既往的可靠，小伙子，拿走你的这份报酬吧。
- failMessageText: 实话说，我可没想到你会这样做！有了那份计划，我们本可以找到并卖给……咳，向平民发放食物，但现在这些好东西都跑到 Skier 那去了，他只会把这些东西的价格翻上十倍！你怎能和这个无赖合作？
- whileAvailableMessageText: 实话说，我可没想到你会这样做！有了那份计划，我们本可以找到并卖给……咳，向平民发放食物，但现在这些好东西都跑到 Skier 那去了，他只会把这些东西的价格翻上十倍！你怎能和这个无赖合作？
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在森林锯木厂小屋里取得机密文件夹0052
  - 条件[AvailableForFinish/CounterCreator]: 以幸存状态撤离森林
  - 条件[AvailableForFinish/HandoverItem]: 上交机密文件夹
  - 条件[Fail/Quest]: 暗中破坏 - 成功

### [支线] 一般储备 | 596a1e6c86f7741ddc2d3206
- id=596a1e6c86f7741ddc2d3206 | 类型=Completion | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 又见面了，小伙子。我这儿的食品储备暂时出现了短缺，像肉罐头这样容易长期保存的食物更是急需。你能帮我解决这个问题吗？我需要储备几听牛肉罐头。我知道有几个地方可以找到宝贵的库存——当然是从我们的 Scav 朋友们那里大方拿过来就是了。海关的混混们曾经在厂区附近的加油站里囤积了一些食品——说不定在那儿还能找到些库存。他们在宿舍楼里还有另一处仓库，那里的好东西可不止罐装食品。但现在我只对罐头感兴趣，所以除此以外的东西就任你支配了。成交吗？我知道这两伙混混是狼狈为奸的，但为了防止他们自己人盗窃仓库，他们的领导人交换了对方仓库的钥匙，因此对你来说应该不成问题。
- successMessageText: 好吧，现在我们总算能在“多种多样”的“精致食谱”里添加点肉类食物了。小伙子，多谢了。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：牛肉罐头（小包装）
  - 条件[AvailableForFinish/HandoverItem]: 上交物品

### [支线] 汽车修理 | 596a218586f77420d232807c
- id=596a218586f77420d232807c | 类型=PickUp | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 下午好，小伙子。我们的公益事业需要帮助，我想你早就猜到了，我一直在收集食品，药品和其它必需品来帮助人们撤离。我不骗你，这是真的，现在他们有足够的贮备出发了。最重要的是，我已经和外界的人协商成功，他们允许我们通过。你能想象到吗？但这次只允许平民撤离，不准60岁以下的男性通过。因此，第一批主要是带着孩子的妈妈和几位老人。现在几乎一切都准备就绪了，只有一个问题，我们需要运输工具。我们有几辆车，刚刚好够第一批撤离的人用，但它们都没法发动了。这些车已经闲置了太久，但据我所知，只需要更换电池和火花塞，连汽油不用加。4个电池和8个火花塞，你能搞定吗？
- successMessageText: 你绝对无法想象你做的这些事对于这些人来说意味着什么。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：汽车蓄电池
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：火花塞
  - 条件[AvailableForFinish/HandoverItem]: 上交物品
  - 条件[AvailableForFinish/HandoverItem]: 上交物品

### [支线] 救助站点 | 59c9392986f7742f6923add2
- id=59c9392986f7742f6923add2 | 类型=PickUp | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 又见面了，小伙子。人道主义危机在塔科夫已经持续了数年时间。经过了这么长时间的战乱，我相信你能理解平民需要在 Scav 和雇佣兵之间的夹缝中求生存，哪怕是只有几处能够容身的庇护所也是好的。虽然我已经尽我所能帮助平民安顿下来，但在最近，就连这样的临时措施也变得越来越困难了。
  我又找到了几处合适的地方，你能帮我把那儿的钥匙给弄来吗？我的人会在这些地方建立安全屋，收容那些颠沛流离的老百姓。
- successMessageText: 漂亮！很高兴能和你再次合作，你已经重新赢得了我的信任。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 获得物品：宿舍 303 房钥匙
  - 条件[AvailableForFinish/FindItem]: 获得物品：ZB-014 钥匙
  - 条件[AvailableForFinish/FindItem]: 获得物品：军事检查站钥匙
  - 条件[AvailableForFinish/FindItem]: 获得物品：加油站储藏室钥匙
  - 条件[AvailableForFinish/HandoverItem]: 上交物品
  - 条件[AvailableForFinish/HandoverItem]: 上交物品
  - 条件[AvailableForFinish/HandoverItem]: 上交物品
  - 条件[AvailableForFinish/HandoverItem]: 上交物品

### [支线] 医疗隐私 - 1 | 5a68661a86f774500f48afb0
- id=5a68661a86f774500f48afb0 | 类型=Discover | 地点=5704e554d2720bac5b8b456e | 前置=无 | 后继=5a68663e86f774501078f78a | notDisplayed=False
- description: 你好，小伙子。许多人为了摆脱目前的困境已经不顾手段，绝望之下，一些人的行径已经称不上是人类了。但对于我们医务人员来说，我们的职责是不会有任何变化的：不管情况什么样，拯救生命仍然是我们的首要目标。当海岸线隧道坍塌时，一开始还有不少幸存者，但都受了伤。我们的救护车立即出动响应呼救，至于之后发生的事情我们都知道了……长话短说，那些救护车都留在那了，我知道整个海岸线大概有三到四辆救护车散落在各地，车上的大部分设备和药品很可能还在里面。如果你碰巧遇到了其中一辆，请帮忙用指示器标记出来，剩下来的事情我的人会负责接手。
- successMessageText: 所以，你的意思是有一辆救护车停在了疗养院旁边？嗯……事情有些不太对劲。总之，我会派出小队，我的同事们会负责回收那些车上的物资。
- 条件:
  - 条件[AvailableForFinish/PlaceBeacon]: 在海岸线找到第一辆救护车，并使用MS2000指示器标记
  - 条件[AvailableForFinish/PlaceBeacon]: 在海岸线找到第二辆救护车，并使用MS2000指示器标记
  - 条件[AvailableForFinish/PlaceBeacon]: 在海岸线找到第三辆救护车，并使用MS2000指示器标记
  - 条件[AvailableForFinish/PlaceBeacon]: 在海岸线找到第四辆救护车，并使用MS2000指示器标记

### [支线] 灭虫服务 | 5c0d1c4cd0928202a02a6f5c
- id=5c0d1c4cd0928202a02a6f5c | 类型=Elimination | 地点=5b0fc42d86f7744a585f9105 | 前置=无 | 后继=无 | notDisplayed=False
- description: 我有件棘手的活儿要交给你。最近实验室里可以说是人头攒动，无论是走投无路的掠夺者还是想发笔横财的 PMC 蜂拥而至，想在整个设施被彻底搬空前分一杯羹。我的同事们一直在调查那个地下设施，并且有条不紊地回收里面的医疗设备——当然，这是为了民众的福祉，以及其他需要这些材料的人。但不断有人渣对我派出去的小队下手，就像是有意盯住我的人一样。
  我希望你去给这伙人一个教训，也许这些废物还能走上正轨。但是这活儿有一个条件：你在清理这些人渣的时候必须戴着呼吸面罩或者防毒面具——那些渣滓必须清楚地知道到底是什么人在制裁他们。所以你必须在近距离下手，让同伴死亡的场景成为陪伴终身他们的痛苦回忆。
- successMessageText: 感谢你的付出，别把这件事放在心上。有些人只有自己处在弱势地位的时候才具备同理心，所以让他们跌落也是理所应当的。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在实验室穿戴防毒面具或呼吸面罩，在 60 米内消灭任意敌对目标

### [支线] 同事 | 5edab736cc183c769d778bc2
- id=5edab736cc183c769d778bc2 | 类型=Discover | 地点=5704e554d2720bac5b8b456e | 前置=无 | 后继=无 | notDisplayed=False
- description: 下午好。在当前的困难形势下，每一个有医学知识的人都举足轻重，执业医生就更不在话下了，相信你也同意这一点。而现在，似乎终于有一位这样的医生现身了：有人告诉我，海岸线有人给当地人治病，甚至还有多余的药品能卖给他们。
  我雇了几个小组出发去海岸线，我给他们的任务是找到这位医生，和他谈判并试探出双方都满意的最佳条件。但今天，所有的小组都联系不上了。我们需要这些药品，我可是有客户……咳嗯，我是说，有我的人民需要照看。找到我的人，看看究竟发生了什么。
- successMessageText: 三组人都被杀了？我不明白到底为什么会这样。他们不应该引起注意或卷入战斗。他们是谈判者，还带去了丰厚的交易条件。显然，谈判中出现了可怕的错误。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 找到被派往海岸线疗养院的小组
  - 条件[AvailableForFinish/CounterCreator]: 找到被派往海岸线码头的小组
  - 条件[AvailableForFinish/CounterCreator]: 找到被派往海岸线别墅的小组
  - 条件[AvailableForFinish/CounterCreator]: 以幸存状态撤离该区域

### [支线] 塔科夫式手腕 | 5edaba7c0c502106f869bc02
- id=5edaba7c0c502106f869bc02 | 类型=PickUp | 地点=5704e554d2720bac5b8b456e | 前置=无 | 后继=无 | notDisplayed=False
- description: 下午好，小伙子。让我们直奔正题吧。
  最近我在海岸线损失了不少人手，虽然的确很不幸，但为民众找到新的医疗物资显然重要得多。我们必须换种办法来解决这个问题了。绝对不能冒险使用物理。
  我的助手设法收买了海岸线的一个当地人，从他口中问出了 Sanitar 的一些消息——没错，我们要找的那名医生就叫这个。 他雇佣了一小支保镖队伍随时护卫左右，经常在海岸线的几处地点游荡，和当地人做生意。但这个人的习惯很古怪，直接在大街上给人做手术，还经常把医疗器械就近藏在房子里，一点也不遵循无菌规范。我想请你把这些工具带来，如果 Sanitar 意识到我们可以随时接管他的领地，显然会更加合作。
- successMessageText: 谢谢你，之后就由我来接手吧。我会把这些工具还给 Sanitar 的，再让我们的线人带上消息留言一并送过去。这样一来，他就会明白我们想和他做什么生意。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在海岸线找到并获取 Sanitar 的手术包
  - 条件[AvailableForFinish/HandoverItem]: 上交物品
  - 条件[AvailableForFinish/FindItem]: 在海岸线别墅区找到并获取 Sanitar 的检眼镜
  - 条件[AvailableForFinish/HandoverItem]: 上交物品

### [支线] 病历 | 60896e28e4a85c72ef3fa301
- id=60896e28e4a85c72ef3fa301 | 类型=Completion | 地点=5704e5fad2720bc05b8b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 下午好，年轻人。刚才我还在寻找像你一样的小伙子呢。我得到消息说，那座储备基地先前在测试一种针对罕见流感病毒的疫苗，当然，这是官方层面的说法……至于真正测试的东西我没法告诉你，但 TerraGroup 不知为何也牵扯其中。这些测试结果对全人类的医疗进步会很有价值……对我的西方客户来说也很重要。我需要找到这些实验数据。你得去一趟储备站，然后找到受试者的医学观察报告。这份文件应该就在军医院，在二楼某个上锁的医疗室里。你要接这个活儿吗？当然，你会拿到应得的奖励。
- successMessageText: 你成功找到它们了？十分感激。当然，请相信我，这些数据只会用来做对人类有益的事情。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在储备站取得第一份医学报告
  - 条件[AvailableForFinish/FindItem]: 在储备站取得第二份医学报告
  - 条件[AvailableForFinish/HandoverItem]: 上交第一份报告
  - 条件[AvailableForFinish/HandoverItem]: 上交第二份报告

### [支线] 急单 | 60e71c48c1bfa3050473b8e5
- id=60e71c48c1bfa3050473b8e5 | 类型=PickUp | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 小伙子，你现在有空吗？有个国外的大客户从封锁线另一头传消息过来，他们对本地的一些高价值医疗设备相当感兴趣。在我看来，与其把这些宝贵的医疗资源留在这里任混蛋们蹂躏，不如为它们另寻出路，你明白我的意思吗？要是你能找到客户需要的医疗器械就帮上大忙了。拿着，这是所需设备的清单。
- successMessageText: 很好，真的太棒了。你为所有需要帮助的人做了一件好事。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：便携式除颤器
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：检眼镜
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：LEDX 皮肤透照仪
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：一堆药
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：OLOLO 瓶装复合维生素
  - 条件[AvailableForFinish/HandoverItem]: 上交物品
  - 条件[AvailableForFinish/HandoverItem]: 上交物品
  - 条件[AvailableForFinish/HandoverItem]: 上交物品
  - 条件[AvailableForFinish/HandoverItem]: 上交物品
  - 条件[AvailableForFinish/HandoverItem]: 上交物品

### [支线] 海边假期 | 6179ad56c760af5ad2053587
- id=6179ad56c760af5ad2053587 | 类型=PickUp | 地点=5704e4dad2720bb55b8b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 下午好，小伙子，我有一个特殊委托要交给你。很不幸，我没法将这项任务交给我自己的人完成。你之前帮我搜寻了很多数据，所以你以后就当我的私家侦探吧！之前我有一个关系很近的工作伙伴，他住在 Dalniy 海角区域的某个地方。他总是随身带着一个公文包，里面装着他所有的医学研究和调查数据。问题是，我的同事在过去两周时间里音信全无——我现在有点担心了。如果他的研究落到坏人的手里，我就得花上大价钱封锁消息了。
  帮帮我吧，雇佣兵，请你去找到他的公文包。他一般开一辆红色的 Merin 双门跑车，还经常告诉我自己喜欢在远离城市喧嚣的海边休息。你准备好开始这项任务了吗，小伙子？
- successMessageText: 干得真不错啊，小伙子！你肯定想象不到这台笔记本里的数据有多重要、在接下来的商业谈判里能占到怎样的地位！呃……抱歉，一时兴奋就说了多余的话。你现在可以走了，雇佣兵。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在灯塔找到并获取线人的公文包
  - 条件[AvailableForFinish/HandoverItem]: 上交找到的包裹

### [支线] 消失的线人 | 6179afd0bca27a099552e040
- id=6179afd0bca27a099552e040 | 类型=Exploration | 地点=5704e4dad2720bb55b8b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 小伙子，我需要你的帮助。前几天我派了一队人前往 Dalniy 海角，去度假村区域搜寻补给品。那里曾经是 USEC 在灯塔的行动基地，鉴于你的工作性质，我相信你对那里的了解不是一点半点。从这些地方撤出补给物资的任务至关重要，关乎人民的利益。但非常不幸，小队从昨天起就音信全无。我们怀疑有人发现了我们的行动。雇佣兵，求求你，找到我的人，更重要的是，找到他们拼上生命也要带出来的货物。
- successMessageText: 也就是说，所有人都被处决了……货物也消失不见了，雇佣兵？这真是太糟糕了。好吧，虽然是坏消息，你算是完成了我交给你的任务，报酬还是要给的。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在灯塔度假村区域找到失踪的小队
  - 条件[AvailableForFinish/CounterCreator]: 以幸存状态撤离该区域

### [支线] 缉毒行动 | 626bd75b05f287031503c7f6
- id=626bd75b05f287031503c7f6 | 类型=PickUp | 地点=5704e4dad2720bb55b8b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 请进，雇佣兵。我高度怀疑有人在塔科夫建了新的毒窝，甚至还可能是完整的毒品实验室。我是怎么知道的？首先，我最近治疗了许多症状相似的病人：瞳孔放大、颤栗、意识模糊等等。其次，我的人之前一直在护送难民撤离，他们注意到了灯塔地界周边那些可疑的集装箱。我的一位手下打算调查，却险些把命丢掉。绝对有人在守卫那个地方。你能找到那个集装箱，在里面安装一个摄像头监视整个地方吗？我还没打算让你把整个地方夷为平地，或者把那些自诩“大厨”的疯子全部干掉。你明白我的意思吗？千万不要作无谓的牺牲。现在，我需要的只是录像。
- successMessageText: 你弄好了？谢谢你。不，雇佣兵，我可不会告诉你这些录像的用处。对了，上传按钮在哪里，你说呀？...
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在灯塔找到隐蔽的毒品实验室
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在毒品实验室中藏匿WI-FI摄像头

### [支线] 人口普查 | 639135d89444fb141f4e6eea
- id=639135d89444fb141f4e6eea | 类型=Completion | 地点=5714dc692459777137212e12 | 前置=无 | 后继=无 | notDisplayed=False
- description: 下午好，雇佣兵。你来的正是时候，我有事情要托你来做。我需要知道城市里有多少人处于失踪状态。也就是说，我需要拿到塔科夫市所有居民的完整名单，再去掉那些已经撤离的、确认死亡的和已经在医院得到庇护的人。你的任务就是帮我找到那份名单。去 Primorsky（Приморский）区的房屋管理处把名单带给我。
- successMessageText: 雇佣兵，你也许亲眼目睹过战争夺走了多少生命，但是根本不知道有多少人没有死于刀剑枪炮，而是直接被城市吞噬，从这个世界上彻底消失了——有些人没能逃过轰炸，埋葬在自家房子的瓦砾下；多少人本来能有一线生机，却因为找不到食物在地下室活生生饿死；而更多的人则被感染所吞噬，因为他们没有必需的药物，也因为炮火连天而无法去到医院寻求帮助……记录里至少百分之十五的人就这么没了，仿佛从来没有存在过。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在塔科夫街区取得记载有人口数据的记录
  - 条件[AvailableForFinish/HandoverItem]: 上交日志

### [支线] 城市的解药 | 639135e0fa894f0a866afde6
- id=639135e0fa894f0a866afde6 | 类型=Completion | 地点=5714dc692459777137212e12 | 前置=无 | 后继=无 | notDisplayed=False
- description: 你好，雇佣兵。医院接收的瘾君子数量越来越多，每天都有新的一批人等待脱瘾治疗。这一现象实在是令人担心：就算你能当场捣毁一个毒窝，马上又会有更多的毒品窝点开张。
  也许，是时候换一种疗法了：我们必须尝试救治那些已经染上毒瘾的人。为了找到对症的药物，我必须弄到样本才能进行分析。你能帮我带一份回来吗？另外我还有一个请求：必须坚决惩治那些散布毒品的无良贩子，不要手下留情。我的情报来源表明，背后参与制毒和贩卖的正是那些 Scav。
- successMessageText: 谢谢你的帮助。这是给你的报酬。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在塔科夫街区找到化学品实验室
  - 条件[AvailableForFinish/FindItem]: 取得装有毒品样本的容器
  - 条件[AvailableForFinish/HandoverItem]: 上交容器
  - 条件[AvailableForFinish/CounterCreator]: 在塔科夫街区消灭 Scav

### [支线] 急诊室的故事 | 64f3176921045e77405d63b5
- id=64f3176921045e77405d63b5 | 类型=Completion | 地点=5714dc692459777137212e12 | 前置=无 | 后继=无 | notDisplayed=False
- description: 小伙子，又有工作机会来了，很适合你。
  我认识的一位年轻急救员来了一趟，他透露了一段相当恐怖的故事。灾难爆发前夕，他不断接到有着奇怪症状的患者，就像是某种新疾病正在爆发一样。很快一群陌生人强行从医院里带走了这些患者，说是送进了私人病房。那孩子在那帮人带走另一位新收的患者时拍下了他们的观察日志。
  但是，在一片混乱中，他把手机给弄丢了，所有的信息都在里面。他说当时他们正开着救护车去 Concordia 公寓附近接一个新的患者，手机很可能还留在救护车附近。
  千万要找到那个手机，雇佣兵！这是为了我们所有人！我的人正护送那个急救员在 Primorsky 大道尽头的 SUV 里等着，他会确认手机是不是他的。绝对不能出差错，我对你抱以厚望，小伙子。
- successMessageText: 是的，我收到消息了，这就是我们要找的东西。我的人马上就会开始提取信息。小伙子，你现在可以走了。谢谢你的帮助。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在塔科夫街区找到并获取救护车急救员的手机
  - 条件[AvailableForFinish/CounterCreator]: 通过Primorsky大道出租车载具撤离点撤离
  - 条件[AvailableForFinish/HandoverItem]: 上交手机

### [支线] 兽医也是医 | 64f731ab83cfca080a361e42
- id=64f731ab83cfca080a361e42 | 类型=Exploration | 地点=5714dc692459777137212e12 | 前置=无 | 后继=6573387d0b26ed4fde798de3 | notDisplayed=False
- description: 在这种困难时期，找到合适的药物十分困难。在相当长的一段时间里，诊所一直是有什么用什么，所幸我们这里聚集了不少专业人士，所以即使是看上去不那么合适的药物也能派上用场。但即便如此，合用的医疗物资还是越来越少。
  前不久我派人进城，想看看最后几处会有剩余医疗物资的地方，但我的人没能成功找到地方。但是如果是你的话……我相信你有能力完成这项任务。
  去市区的药房和的兽医诊所看看，说不定那里还剩了一些药品，我可不觉得除了我们之外还有人能让这些东西派上用场，记得顺路检查一下本地 X 光技师的诊所。如果找到了医疗物资，请务必带回来给我，我愿意开高价收购。还有请记住，你所做的一切都在帮助那些饱受折磨的平民脱离苦难。
- successMessageText: 谢谢你的帮助，年轻人。这些物资应该足够我们维持一段时间了。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在塔科夫街区找到并检查兽医诊所
  - 条件[AvailableForFinish/CounterCreator]: 在塔科夫街区找到并检查 X 光技师的工作间
  - 条件[AvailableForFinish/CounterCreator]: 在塔科夫街区 Primorsky 大街找到第一家药店
  - 条件[AvailableForFinish/CounterCreator]: 在塔科夫街区 Primorsky 大街找到第二家药店
  - 条件[AvailableForFinish/CounterCreator]: 在塔科夫街区 Cardinal 公寓区附近找到第三家药店
  - 条件[AvailableForFinish/HandoverItem]: 上交任意在战局中找到的医疗物品

### [支线] 新手上路 | 657315ddab5a49b71f098853
- id=657315ddab5a49b71f098853 | 类型=PickUp | 地点=653e6760052c01c1c805532f | 前置=无 | 后继=无 | notDisplayed=False
- description: 你好，小伙子。我想请你帮我一个小忙，暂时没什么特别重大的事情，毕竟我们才刚开始合作。
  城市里还有一些平民努力想要离开这片战区。Emercom，也就是紧急状态部的检查站是组织大家疏散最合适的集结点：那里应该还有交通工具，和各种必要的设备——至少我希望如此。但我对那部分的城区不太熟，所以我不太确定检查站到底在什么地方。帮我找到那个检查站，Emercom 的站点都有橙蓝相间的条带，应该不难辨认。找到地方之后，我会派我的人前去检查那个区域。
  还有一件事。我需要一些医疗物资来帮助受伤的平民。眼下的情况让我没办法从外面进货，所以只能在塔科夫本地找找了。无论你找到什么医疗物品，我统统都要：医疗包、绷带、止痛药、各种针剂和手术包之类的。任何东西都能派上用场，我会在这等着你的。
- successMessageText: 任务完成？真是好消息，谢谢你。我希望我们未来的合作能互相有利。
- failMessageText: 看起来你已经经历过很多更复杂的情况了。还是让新手来做这种事情吧。
- whileAvailableMessageText: 看起来你已经身经百战了，那今后踩点的活我还是交给新手去做吧。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在中心区找到 Emercom 站点
  - 条件[AvailableForFinish/HandoverItem]: 上交任意在战局中找到的医疗物品

### [支线] 刨根问底 | 669fa39ee749756c920d02c8
- id=669fa39ee749756c920d02c8 | 类型=Discover | 地点=55f2d3fd4bdc2d5f408b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 你好啊，小伙子。我得到消息，化工厂的一个化学品容器发生了罐体破损。里面的液体看起来不像是任何记录在册的物质。
  我觉得那些一定是有害物质，所以我需要你来帮我尽快提取到样本。只有这样，我们才可以评估塔科夫的生化安全性。
- successMessageText: 太好了！我已经找来了能够研究这种物质的专家。你对城市安全的保障功不可没！
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在工厂提取破损罐体中的化学品样本
  - 条件[AvailableForFinish/HandoverItem]: 上交采集到的样本

### [支线] 街区之下 | 66aba85403e0ee3101042877
- id=66aba85403e0ee3101042877 | 类型=Discover | 地点=marathon | 前置=无 | 后继=无 | notDisplayed=False
- description: 下午好。我接到情报，街区的地下有一处可以前往实验室的废弃秘密通道。
  很显然，TerraGroup将一处旧的防空洞改成了实验室的应急出口。这让我们有了以最小风险取得那些宝贵物资的机遇！
  然而，我没法在尚未探明的情况下，派手下们去那里，毕竟实验室还是太过危险。最好有一位经验丰富的专业人士，去检查一下那条通道是否畅通，通往的区域是否安全。你会帮我们的吧？
- successMessageText: 看起来你找到那条通道了。干得漂亮！情况如何，是安全的吧？你是说，它处于年久失修的状态？这样的话，看来这个机遇不会一直存在了。我会尽快派手下出发。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在同场战局中找到塔科夫街区通往实验室的道路
  - 条件[AvailableForFinish/CounterCreator]: 使用转移功能从塔科夫街区转移到实验室（在单次战局中）
  - 条件[AvailableForFinish/CounterCreator]: 在同场战局中侦查实验室的服务器机房
  - 条件[AvailableForFinish/CounterCreator]: 在同场战局中侦查实验室存放危险化合物的圆顶帐篷
  - 条件[AvailableForFinish/CounterCreator]: 在同场战局中侦查实验室库房大门的控制室
  - 条件[AvailableForFinish/CounterCreator]: 在同场战局中找到实验室通往塔科夫街区的道路

### [支线] 无主货物 | 675c03d1f7da9792a405549a
- id=675c03d1f7da9792a405549a | 类型=Exploration | 地点=56f40101d2720b2a4d8b45d6 | 前置=无 | 后继=无 | notDisplayed=False
- description: 下午好，小伙子。我接到消息说，最近又有一批神秘的 TerraGroup 货物被挖了出来。情报显示里面应该装有不少珍贵的数据记录，甚至生物样本也有可能。
  理论上来说，这些货物的优先级相当高，本该在他们撤离时一起带走的，但不知道为什么这批货物却被丢在海关办公区无人过问。我需要知道这批货物都有什么，但是在此之前，我需要知道它们都在什么地方。
  你能帮我找到这些货物吗？货箱包装上都有 TerraGroup 的标志，很容易就能辨别出来。
- successMessageText: 太棒了！你没有打开那些箱子，对吗？谨慎的选择，雇佣兵。这种危险的事情最好还是交给专业人士干比较好。
- 条件:
  - 条件[AvailableForFinish/PlaceBeacon]: 在海关使用 MS2000 指示器标记任意一个 TerraGroup 特殊货箱
  - 条件[AvailableForFinish/CounterCreator]: 在海关找到第一批 TerraGroup 特殊货物
  - 条件[AvailableForFinish/CounterCreator]: 在海关找到第二批 TerraGroup 特殊货物
  - 条件[AvailableForFinish/CounterCreator]: 在海关找到第三批 TerraGroup 特殊货物
  - 条件[AvailableForFinish/CounterCreator]: 在海关找到第四批 TerraGroup 特殊货物
  - 条件[AvailableForFinish/CounterCreator]: 在海关找到第五批 TerraGroup 特殊货物
  - 条件[AvailableForFinish/CounterCreator]: 在海关找到第六批 TerraGroup 特殊货物
  - 条件[AvailableForFinish/CounterCreator]: 在海关找到第七批 TerraGroup 特殊货物

### [支线] 货运追踪 | 675c047fa46173572a0bd878
- id=675c047fa46173572a0bd878 | 类型=PickUp | 地点=56f40101d2720b2a4d8b45d6 | 前置=无 | 后继=675c04f4db8807b75d0f38e8 | notDisplayed=False
- description: 我的人还没能把那些货物弄回来。不过如果真的有任何高价……我是说，高风险内容的话，那么显然一秒都不能浪费。
  这些货物肯定都留有某种形式的记录，如果能找到行程单或者货运清单的话，那么事情的进展就会容易得多了。你要找的就是那种信息。
  打探一下海关那些头头脑脑：主管，督察之类的人，在他们的办公室里找找。如果真的有货运记录留存，出现在那种地方的概率会很高。
- successMessageText: 彻底研究这些文件会花上一点时间。无论如何，我们很快就能查明这批货到底出了什么毛病，以及里面都装着些什么东西。
  等到我的专家总结出报告之后，我们就能对事情的全貌有所了解了。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在海关找到并获取 TerraGroup 货运清单
  - 条件[AvailableForFinish/HandoverItem]: 上交找到的信息

### [支线] 塔科夫屠夫 | 67a09673972c11a3f507731d
- id=67a09673972c11a3f507731d | 类型=PickUp | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 你听说过哪个曾在城里作案的连环杀手的故事吗？关于所谓的”塔科夫屠夫“的消息最早在契约战争的时候就四处流传。但在冲突爆发后，调查就无限期陷入了停滞……
  最近我的一位熟人对这个故事又产生了兴趣，他同时也在追查这位“屠夫”的线索。但是可以想见，这样一位危险人物不会轻易走漏自己的踪迹。无论这个变态狂的心理有多扭曲，我们只有试着去理解他的动机和目标，才有希望找到更多线索。
  我们设法从本地的警察部门搞来了一本犯罪记录。根据档案记载，这个“屠夫”在市区的一家肉制品包装厂担任司机。而警方在他用来运输冻肉的的冷藏货车里发现了被害人的尸体……但随着冲突升级，官方层面的调查就止步于此了。 现在你已经知道了这个案子的所有细节，如果你能找到更多有用的线索，我将不胜感激。
  还有一件事，我那位熟人需要一些特定的药物。请帮我把这个化学品容器转交给他，里面装着他需要的所有东西。
- successMessageText: 你的发现将会帮助我们追查那名杀手。谢谢你。这是你的报酬。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在中心区找到并获取化学品容器
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在塔科夫街区将化学容器放置藏匿在警察局

### [支线] 秋季综合征 | 6a5424ae135497b9df0c68be
- id=6a5424ae135497b9df0c68be | 类型=PickUp | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 下午好，小伙子。去年秋天，塔科夫经历了一场严重的流感爆发，就连我自己也不幸中招了两次。不过今年不一样了，这一次我们可早有准备，我想办法通过 UN 维和部队的后勤通道弄到了一批流感疫苗来……但这不是我今天找你来唯一的原因。
  我打算在秋天组织一场疫苗预防行动，免得更多人陷入病痛的苦楚，而在此之前，我得先给自己以及诊所里的其他人先打上疫苗才行。非常不巧，无菌注射器现在相当紧缺。你能帮我找 5 支注射器来吗？我希望你亲自去找，只要包装完好的那种，不要去市场上买，天知道 Scav 的脏手拿那些针头做过什么……
- successMessageText: 都找到了吗？太棒了，我明天就开始给大家打疫苗。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：一次性注射器
  - 条件[AvailableForFinish/HandoverItem]: 上交物品

### [支线] 侵入性疗法 | 6a7ae35b2dbf91eb050af95a
- id=6a7ae35b2dbf91eb050af95a | 类型=Completion | 地点=5704e4dad2720bb55b8b4567 | 前置=无 | 后继=无 | notDisplayed=False
- description: 你好，小伙子。请不要向任何其他人提起这项任务，至于你心里有什么疑问，也最好不要说出来。
  最近，游荡者截走了我的一批针剂。看样子在经历了被逐出污水处理厂的战斗之后，你的那些前同事们为了挽回自己的损失，不惜以更加大胆激进的方式获取装备和医疗物资。
  如果是为了救治伤员，我当然乐意以公平的方式出让一些物资，但这些人竟然背着我直接上手抢劫！不好意思……总之，我绝不能容忍任何人以这样的方式打破规则，同时我们也别无选择，只能以暴制暴。必须给他们留下深刻教训，让他们明白，没有我点头，他们别想心安理得地享受自己不告而取的赃物！
  他们把从我这里抢走的物资都存放在度假村的别墅区了。找到那些药，然后就把其中的一部分换成 Sanitar 制造的实验针剂吧。
- successMessageText: 希望你没有把我们的交易透露给其他人。我想你应该明白，在如今的塔科夫，表现出任何软弱都相当危险。但同时我们又不能冒险失去自己的中立地位。
  毕竟，我付钱给你，看中的就是你办事时的专业素养和不偏不倚的立场。希望我们在今后的合作中能继续恪守这些标准。
- 条件:
  - 条件[AvailableForFinish/HandoverItem]: 上交在战局中找到的 P22 (22 号化合物) 兴奋剂注射器
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：P22 (22 号化合物) 兴奋剂注射器
  - 条件[AvailableForFinish/HandoverItem]: 上交在战局中找到的 xTG-12 解毒剂注射器
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：xTG-12解毒剂注射器
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在灯塔上层度假村内的游荡者医疗区处藏匿任意 Obdolbos 鸡尾酒注射器
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在灯塔上层度假村内的游荡者客厅物资区处藏匿任意 Obdolbos 鸡尾酒注射器
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在灯塔下层度假村内的游荡者弹药堆放处藏匿任意 Obdolbos 鸡尾酒注射器

### [支线] 邮递员派特 - 2 | 596760e186f7741e11214d58
- id=596760e186f7741e11214d58 | 类型=PickUp | 地点=any | 前置=59675ea386f77414b32bded2 | 后继=无 | notDisplayed=False
- description: 下午好。Prapor告诉我，你就是那个愿意帮我们重新建立联系的小伙子，能帮我们找回丢失的信真是太棒了！我会静待佳音。
- successMessageText: 哎呦，谢谢你！难怪Prapor说你是一个可靠又负责任的人。
- 条件:
  - 条件[AvailableForFinish/HandoverItem]: 上交Prapor信使身上的信件

### [支线] 卫生标准 - 2 | 596a204686f774576d4c95de
- id=596a204686f774576d4c95de | 类型=Completion | 地点=any | 前置=59689ee586f7740d1570bbd5 | 后继=无 | notDisplayed=False
- description: 你好啊。来了正好，既然你已经轻车熟路了，我就特地把这活留给你了。我们的工程师已经成功解决了工厂化学品泄露的问题，但是水的问题还是没有完全处理好。现在污染特征仅限于石油制品，因此我们推测这和工厂旁边的 TPP 储油罐有关。你肯定已经见过那些设施了，那么一大堆你，肯定不会错过的。理论上储油罐和熔炉设备的维护室必须配有工厂泵站同款的气体分析仪——当然，前提是 Scav 还没有把它们洗劫一空。找到那些设备并把它们带来，我们也许就可以解决整个地区的供水问题，至少也能撑上一段时间。
- successMessageText: 真高兴能找到个既可靠又有正义感的勇者，你的确冒了不少风险，但这是为了大众的福祉。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在战局中找到气体分析仪
  - 条件[AvailableForFinish/HandoverItem]: 上交气体分析仪

### [支线] 出于好奇 | 597a160786f77477531d39d2
- id=597a160786f77477531d39d2 | 类型=PickUp | 地点=56f40101d2720b2a4d8b45d6 | 前置=597a0f5686f774273b74f676 | 后继=无 | notDisplayed=False
- description: 小伙子，这一次我不能袖手旁观了。我知道你现在正在和那个叫 Skier 的家伙合作。这个人早就声名狼藉，他的眼睛里只有利益，只要有钱赚，他完全可以出卖一切，这让我非常担心。尤其是你提到的秘密实验，还有那种所谓“黄色化学品”的事情。这种物质极度危险，不能落到错误的人手中。因此，我恳求你，能把化学药品的坐标给我吗？没错，我知道你和 Skier 那笔交易的所有细节，为了弥补你的损失，我会一样向你支付你一笔可观的报酬。再强调一遍，这一切都是为了我们的民众。
- successMessageText: 漂亮！我都能看到Skier的脸黑成什么样了。他肯定会生你的气，但是你实打实为平民们干了件好事，所以没必要沮丧。说真的，这坨东西最好放在有能力控制它的人手中，我会负责转运它的。拿走你的这一份，我们说好的。你这次做了正确的选择。
- failMessageText: 我不知道该说什么了。光是想想是谁、又为什么需要这些化学品就让我头皮发麻。
- whileAvailableMessageText: 我不知道该说什么了。光是想想是谁、又为什么需要这些化学品就让我头皮发麻。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在海关找到装有化学品的载具
  - 条件[AvailableForFinish/PlaceBeacon]: 使用MS2000指示器标记载具

### [支线] 医疗隐私 - 2 | 5a68663e86f774501078f78a
- id=5a68663e86f774501078f78a | 类型=Discover | 地点=5704e554d2720bac5b8b456e | 前置=5a68661a86f774500f48afb0 | 后继=5a68665c86f774255929b4c7 | notDisplayed=False
- description: 救护车的事情多亏了有你帮忙。我们的人刚好赶在 Scav 们之前到达了现场，至少他们还没有把所有东西都洗劫一空。在调查隧道倒塌后果的同时，我们找到了一辆 G 型越野车，以前 TerraGroup 的高管们经常坐着这些大 G 招摇过市。看样子除了平民以外，有几位在集团里身居高位的大人物也在撤离途中丧生了。无论他们打造的外部形象如何，医学研究一直是整个集团最重要的核心业务之一，而且我几乎可以肯定，集团根本来不及撤离所有数据和器材。
  无论 TerraGroup 在当地进行的研究内容到底是什么，对于我们和整个医学界无疑都非常有价值。它能为我们赢得逃离塔科夫的门票，甚至不止一张。此前我一直定期回访几位 Terragroup 员工，和他们讨论健康问题，因此我对他们的一些工作非常熟悉。我的一位熟人就曾经住在疗养院里，顺带一提，他直接向集团高管们汇报。让我看看......哦，这里，306 房。检查房间里留下的东西，如果有任何进展，务必告诉我一声。
- successMessageText: 你拿到那些文件了？让我好好看看……没错，这确实是医生特有的笔迹。谢谢你，小伙子。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在海岸线疗养院西楼的房间内找到任何关于 TerraGroup 研究的文件
  - 条件[AvailableForFinish/HandoverItem]: 上交找回的信息
  - 条件[AvailableForFinish/CounterCreator]: 以幸存状态撤离该区域

### [支线] 医疗隐私 - 3 | 5a68665c86f774255929b4c7
- id=5a68665c86f774255929b4c7 | 类型=Discover | 地点=5704e3c2d2720bac5b8b4567 | 前置=5a68663e86f774501078f78a | 后继=5a68667486f7742607157d28 | notDisplayed=False
- description: 在你搜查房间的时候，有没有注意到任何异样？什么？两具尸体，太残忍了……不可能是巧合，怪不得会这样。你找到的文件指向了免疫学领域的一些重要研究，据我所了解到的部分，他们在这一方面取得了重大进展。从文件上的签名判断，这项研究受疗养院的医务主管直接监督。他年事已高，我在医务工作者日的聚会上见过他几次。作为一位可敬的学者，比起在喧嚣的实验室，他更喜欢宁静的乡村，经常开着一辆白色的小货车在森林里兜风。
  随着研究获得进展，我相信他将受到鼓舞展开人体试验——根据医学传统，第一个试验对象可能就是他自己，科学家总是这样的。找到他的车，如果你能碰巧找到他的血样，那就太好了。
- successMessageText: 一路追踪血迹到了这个地方，我真的十分心碎。但是无论发生了什么，我也得把血样送去分析。感谢你的努力。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在森林找到那辆曾属于疗养院医疗服务负责人的车
  - 条件[AvailableForFinish/FindItem]: 提取血液样本
  - 条件[AvailableForFinish/HandoverItem]: 上交血液样本

### [支线] 医疗隐私 - 4 | 5a68667486f7742607157d28
- id=5a68667486f7742607157d28 | 类型=Skill | 地点=any | 前置=5a68665c86f774255929b4c7 | 后继=5a68669a86f774255929b4d4,5c0be5fc86f774467a116593,5c0d0d5086f774363760aef2,5d6fb2c086f77449da599c24 | notDisplayed=False
- description: 你好，小伙子。我真佩服你的机敏和过硬的身体素质，但我觉得，你可不能就此满足。你应该知道，训练需要持之以恒才会有效果，对吧？在塔科夫求生存更是离不开强健的体魄，坚持适当的体育锻炼对你没害处。
  试着挑战自己的极限吧。你可以试着把自己的训练成绩和挑战结果记录下来，我会帮你看看的，至于要不要这么做还是看你自己的意思。
- successMessageText: 你有没有发现，随着你一次次将身体逼近极限，身体机能保持在巅峰状态的时间可以持续更久了？真棒，现在去好好休息吧。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 耗尽腿部耐力获得“疲劳”效果，并持续至少 8 分钟

### [支线] 医疗隐私 - 5 | 5a68669a86f774255929b4d4
- id=5a68669a86f774255929b4d4 | 类型=Multi | 地点=59fc81d786f774390775787e | 前置=5a68667486f7742607157d28 | 后继=669fa3a3ad7f1eac2607ed48 | notDisplayed=False
- description: 血液是一个很好的信息来源。虽然比不上文献研究，但已经很不错了。距离我最后一次正经做医学方面的研究已经过了有一会了，我还记得我当时在协助一名常驻病毒学家完成他的论文，真是恍如隔世啊。互联网的普及使我们与其他国家同行的信息交流更加轻松了，现在我开始怀念当初能联网检索 Scopus 数据库的时候了，我相当确定数据库里包含的内容能够解决那份血液样本给我们带来的疑问。你可能会问，以现在的情况到底要怎么才能接上互联网呢？有人建议我联系一名叫做Mechanic的人。事实证明，他不但能访问互联网，而且网速相对来说还挺快。我已经让他帮忙寻找一些必要的文献了，而那他自然要求一笔小小的劳务费——不知怎的，他需要的是一些发射药。看来他一直在做军火供应或者类似的生意，我对这些不怎么感兴趣，唯一重要的是，他有我们想要的信息，这就够了。
  帮我找来他要的东西，把那些火药留在工厂的指定交货点，就在办公区三楼某个房间里。他说那扇门看着锁上了，实际一踹就开。整个地方白天 Scav 人头攒动，所以还是尽量在夜间行动吧。
- successMessageText: 干得好，Mechanic已经收到了发射药。谢谢你。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在夜间工厂找到交货点
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 将任意类型的火药埋藏在指定地点

### [支线] 私人诊所 | 5c0be5fc86f774467a116593
- id=5c0be5fc86f774467a116593 | 类型=Completion | 地点=any | 前置=5a68667486f7742607157d28 | 后继=无 | notDisplayed=False
- description: 日安，小伙子。作为一名医生，我想即使是在这样一个艰难时刻，我也应该遵守希波克拉底的誓言。所以，我想要开设一家诊所。身体健康的照顾一直都很重要，在这种情况下更是如此。但是我得去哪里弄到那些必要的设备呢？这样吧，我会给你一个我需要的物品的清单。我不需要竞争者，所以你得保证这活没有第三方参与。当然了，我会想方设法来给你弄到报酬的。
- successMessageText: 你把它们带回来了？很好，放在那边的角落里吧。小心点！这些设备非常的脆弱易损。即使整个诊所计划失败了，我也知道某些人也会对这些设备感兴趣的。当然了这只是你我之间的秘密，我们何必浪费这么昂贵的硬件设备呢？
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在战局中找到检眼镜
  - 条件[AvailableForFinish/FindItem]: 在战局中找到LEDX皮肤透照仪
  - 条件[AvailableForFinish/HandoverItem]: 上交检眼镜
  - 条件[AvailableForFinish/HandoverItem]: 上交LEDX皮肤透照仪

### [支线] 运动员 | 5c0d0d5086f774363760aef2
- id=5c0d0d5086f774363760aef2 | 类型=Skill | 地点=any | 前置=5a68667486f7742607157d28 | 后继=无 | notDisplayed=False
- description: 你知道，作为一个医生，我的天职就是治病救人，即使是在这样一个艰难时刻。不过因为你常常帮我跑腿做事，我必须再三确认你的健康状况良好。别多虑，只是为了感到更安心，这样我才能托付给你更多的差事。
- successMessageText: 实际上，我从未怀疑过你是一个值得信任的人，不仅仅是在工作中。
- 条件:
  - 条件[AvailableForFinish/Skill]: 达到指定的健康技能等级

### [支线] 一天一个苹果 - 医生远离我 | 5d6fb2c086f77449da599c24
- id=5d6fb2c086f77449da599c24 | 类型=Completion | 地点=any | 前置=5a68667486f7742607157d28 | 后继=无 | notDisplayed=False
- description: 啊，是你呀？关心身体健康？这再好不过了。我这儿有好几种非常有效的进口疫苗，不过你懂我们现在的情况，买疫苗可真不容易，得花上很多钱。当然这取决于你自己，钱和命，哪个对你更重要……
- successMessageText: 嗯，你决定了？不过打几针而已。这儿，还有这儿。保持冷静，至少要几天时间，这段日子多休息，多补充维生素。
- 条件:
  - 条件[AvailableForFinish/HandoverItem]: 上交卢布

### [支线] 艰难抉择 | 5edac34d0bb72a50635c2bfa
- id=5edac34d0bb72a50635c2bfa | 类型=PickUp | 地点=any | 前置=5edab4b1218d181e29451435 | 后继=无 | notDisplayed=False
- description: 年轻人，等等。我知道 Jaeger 会让你杀了那个 Sanitar，但你不一定非得这么做。我明白在 Jaeger 那个家伙看来，这位医生不过是一个扭曲的怪胎，但他真的不是那样的人。我很确定 Sanitar 只是想帮助当地人而已。
  考虑到目前的情况，他被迫在最恶劣的条件下以复杂手段为当地人治疗，结果往往很糟糕也就不足为奇了。他是一位拥有最宝贵知识的医生，你应该明白这对于时下的情况有多宝贵。更不用提他还拥有大量的医疗物资储备。我们已经和他进行了谈判，并将很快开始合作。
  但为了这个，我需要一些他感兴趣的东西——他应该会对实验室里的东西感兴趣，比如访问钥匙卡和各种战斗兴奋剂的样本。他应该能用得到这些东西继续进行实验。你能帮我找到它们吗？钥匙卡你可以通过各种方式搞到，但针剂必须是全新封装的，因此你得自己去找。
- successMessageText: 谢谢。我就知道你会明白现在 Sanitar 和他的……呃，资源对于我们有多重要。他已经对钥匙卡和兴奋剂表现出极大的兴趣，并向我们发来了第一批物资。给，这是你的那一份。
- failMessageText: 很遗憾你听信了那个 Jaeger 的片面之词，而不是从理性的角度好好分析利弊。以 Sanitar 的医学知识和药品库存，他本来能在许多方面帮上大忙。没错，他不是圣人，但每个人都应该有机会赎罪，不是吗？你就这样武断地剥夺了他的那次机会，我对你非常失望。
- whileAvailableMessageText: 很遗憾你听信了那个 Jaeger 的片面之词，而不是从理性的角度好好分析利弊。以 Sanitar 的医学知识和药品库存，他本来能在许多方面帮上大忙。没错，他不是圣人，但每个人都应该有机会赎罪，不是吗？你就这样武断地剥夺了他的那次机会，我对你非常失望。
- 条件:
  - 条件[AvailableForFinish/Quest]: 不要杀死Sanitar
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：TerraGroup 实验室访问钥匙卡
  - 条件[AvailableForFinish/HandoverItem]: 上交钥匙卡
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：AHF1-M 兴奋剂注射器
  - 条件[AvailableForFinish/HandoverItem]: 上交注射器
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：3-(b-TG) 兴奋剂注射器
  - 条件[AvailableForFinish/HandoverItem]: 上交注射器

### [支线] 危险之路 | 63ab180c87413d64ae0ac20a
- id=63ab180c87413d64ae0ac20a | 类型=Exploration | 地点=5714dc692459777137212e12 | 前置=596a0e1686f7741ddf17dbee | 后继=无 | notDisplayed=False
- description: 你好啊，小伙子！近来身体如何？有什么烦心事儿吗？我召集了许多人转运困在城里的那些民众，现在的问题是，有一位信使的下一趟行程没有人护送，老实说还挺让人担心的。你能和他碰头然后陪他走一趟吗？我保证你不会被亏待的！他会在 Primorsky 大道尽头的一辆黑色 SUV 里等你。
- successMessageText: 怎么样，小菜一碟吧？从现在开始，经常使用那位司机的服务吧。他是个可靠的伙计。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在塔科夫街区幸存，并通过Primorsky（Приморский）大道出租车载具撤离点撤离

### [支线] 兽医也是医 - 2 | 6573387d0b26ed4fde798de3
- id=6573387d0b26ed4fde798de3 | 类型=Discover | 地点=5714dc692459777137212e12 | 前置=64f731ab83cfca080a361e42 | 后继=无 | notDisplayed=False
- description: 小伙子。我需要你的帮助。
  从兽医诊所和 X 光办公室找到的那点药物根本不够，我们的下一个目标是药店。市区里有好几家药店，所以我觉得那些盗贼不会一次性偷走所有东西。帮我去那些药店踩点，然后我会派出人手把东西搬回来。据我所知，市区里至少有 3 家药房，去看看那里现在是什么情况。
- successMessageText: 大部分药店也被抢了？我并不意外，但总比什么都没有要好。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在塔科夫街区的Primorsky大街找到第一家药店
  - 条件[AvailableForFinish/CounterCreator]: 在塔科夫街区的Primorsky大街找到第二家药店
  - 条件[AvailableForFinish/CounterCreator]: 在塔科夫街区的Cardinal公寓区找到第三家药店
  - 条件[AvailableForFinish/CounterCreator]: 以幸存状态撤离该区域

### [支线] 口干舌燥 - 往日回响 | 665eeca45d86b6c8aa03c79d
- id=665eeca45d86b6c8aa03c79d | 类型=PickUp | 地点=5704e554d2720bac5b8b456e | 前置=665eec4a4dfc83b0ed0a9dca | 后继=665eeca92f7aedcc900b0437 | notDisplayed=False
- description: 很高兴你能联系我，小伙子。我知道你在调查那个叫“渴死鬼”的人。出于某种原因，Sanitar 也在展开行动。他一直在搜集 TerraGroup 在塔科夫非法活动的情报，这一切都能对上了。
  如果你能提前截下那份数据的话，请把它交给我，我的报酬不会让你失望的。此外，至少这些数据不会引发另一场灾难了。
- successMessageText: 只有一本日志，没别的了吗？嗯，可能还是会有点用的……这笔迹潦草得连我都看不下去了。解读这份材料得花上好一会，这是你的奖励。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在海岸线找到与“渴死鬼”有关的任何信息
  - 条件[AvailableForFinish/FindItem]: 在“渴死鬼”的藏身处找到信息
  - 条件[AvailableForFinish/HandoverItem]: 上交找到的信息

### [支线] 口干舌燥 - 秘密配方 | 665eeca92f7aedcc900b0437
- id=665eeca92f7aedcc900b0437 | 类型=PickUp | 地点=any | 前置=665eeca45d86b6c8aa03c79d | 后继=无 | notDisplayed=False
- description: 你好啊。我设法从你之前带来的日志中提炼出了一些只言片语，是关于某种药物的配方。笔记的前主人经常使用它，应该是某种战斗用兴奋剂，你可能会感兴趣的。
  如果你能找来制造原料、帮我测试一下它的效用的话，我可以提供几份样品。这里是所需材料的清单。
- successMessageText: 我相信你会对它的功效感兴趣的！这是测试结果。
- 条件:
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：肾上腺素注射器
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：OLOLO 瓶装复合维生素
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：瓶装生理盐水

### [支线] 质量标准 | 666314b696a9349baa021bac
- id=666314b696a9349baa021bac | 类型=PickUp | 地点=5b0fc42d86f7744a585f9105 | 前置=666314b4d7f171c4c20226c3 | 后继=666314b8312343839d032d24 | notDisplayed=False
- description: 下午好。我听说 Prapor 同志最近心生不少怀旧之情。尽管我每天忙得没时间想这种事情，你和 Prapor 之间的交易倒是给了我一个好主意。
  当地人意外发现 TerraGroup 的地下实验室那会，可是搅得满城风雨！我的人从实验室里搜刮来的各种特殊设备让生意一下子突飞猛进。
  实验室里可能还剩下几部美国原产的 LEDX 皮肤透照仪，你能为我弄一个来吗？这对我们的事业会很有帮助。
- successMessageText: 向你致谢，小伙子。如果连你这样的战士都进不去实验室，那我也不知道该怎么办了。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在实验室找到并获取特殊版本的 LEDX 皮肤透照仪
  - 条件[AvailableForFinish/HandoverItem]: 上交找到的物品

### [支线] 健全替代 | 669fa3910c828825de06d69f
- id=669fa3910c828825de06d69f | 类型=Discover | 地点=any | 前置=669fa38fad7f1eac2607ed46 | 后继=无 | notDisplayed=False
- description: 你好，我知道你刚刚被指派去化工厂寻找一份实验室日志。很显然，TerraGroup 为了掩盖自己的秘密，出手可是相当阔绰。
  我建议你重新考虑这单差事。如果集团对这份东西那么上心，那里面所包含的信息一定价值连城！当然，是文件对于这座城市和市民来说价值不菲。这么重要的东西就让我来帮他们妥善保管吧。
- successMessageText: 你做了正确的选择！这本日志对我一位老朋友的实验意义重大... 我是说，研究，科学研究！这样我们就能更好地拯救那些被困在塔科夫的平民了。
- failMessageText: 你真的下定决心把日志交回给集团了？你这个人难道就一点都不在乎市民吗？我对你的选择感到无比失望。
- whileAvailableMessageText: 你真的下定决心把日志交回给集团了？你这个人难道就一点都不在乎市民吗？我对你的选择感到无比失望。
- 条件:
  - 条件[AvailableForFinish/HandoverItem]: 上交实验室日志

### [支线] 医疗隐私 - 6 | 669fa3a3ad7f1eac2607ed48
- id=669fa3a3ad7f1eac2607ed48 | 类型=Discover | 地点=55f2d3fd4bdc2d5f408b4567 | 前置=5a68669a86f774255929b4d4 | 后继=无 | notDisplayed=False
- description: 你好，年轻人。我们一路追查塔科夫居民离奇死亡的谜团已经取得了很多进展。而现在我又得知了新的情报。
  有人最近在化工厂里找到了一些工人的尸体，奇怪的是，死者身上没有任何打斗痕迹。他们很可能正是我们所追查的神秘病症的受害者……我们不能错过这次机会，必须收集血样回来进行分析。你已经是个血样采集熟手了，这件事就交给你吧。还有，请帮我找一个 LEDX 设备来。
- successMessageText: 你把样本采集回来了吗？很好，把它们交给我。嗯……这些血液的性状和我预想的不一样。看来，这一切恐怕都和 TerraGroup 脱不了关系。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在工厂找到死亡的工人，并获取他们的血液样本
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：LEDX 皮肤透照仪
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在实验室将血液样本藏匿在 Sanitar 的办公室里

### [支线] 平易近人 | 675c04f4db8807b75d0f38e8
- id=675c04f4db8807b75d0f38e8 | 类型=PickUp | 地点=56f40101d2720b2a4d8b45d6 | 前置=675c047fa46173572a0bd878 | 后继=无 | notDisplayed=False
- description: 坏消息，小伙子。我和派出去回收货物的小组失去了联络，而之前你给我找来的文件又偏偏是一份抹去了所有敏感信息的脱敏文件，线索一下子都断了。
  我们既不知道这次任务的代号，也没掌握这批货物的具体行程。甚至连内容清单都没能搞到！我甚至没法让你自己去打开那些货箱——万一就是里面的东西害了他们呢？
  不过，我想到了另一个办法。合同文件的免责条款写着，运输途中遇到不可控风险时，货运班组有权偏离既定路线完成运输。
  假如他们真的是在转移货物途中遭到袭击才偏离了路线，那这些情况一定会被记录下来。检察那些货运员可能停留的地方，看看能不能找到相关消息。
- successMessageText: 怎么可能呢……记录表明，根本没有人袭击货运班组，货运通道是完全畅通的！
  然而，在箱子附近呆了几个小时的工人们却报告说身体发痒、发烧，宁可放下工作也要回家。
  工头的记录甚至显示有两个送货人员在运输过程中直接丢弃了箱子，并断然拒绝继续工作！
  运输这些箱子应该需要特殊的防护设备才能保障安全，但是当时没有人考虑为普通工人提供这些设备。目前还不清楚这些箱子有多危险，但我不建议在没有穿戴适当防护装备的情况下贸然接近它们。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在海关找到并获取货运班组的日志
  - 条件[AvailableForFinish/HandoverItem]: 上交找到的信息

### [支线] 这带子糟透了 | 67a0972e77dd677f600804bd
- id=67a0972e77dd677f600804bd | 类型=Exploration | 地点=6733700029c367a3d40b02af | 前置=67a0970744893b9f3f0d9b68 | 后继=无 | notDisplayed=False
- description: 很高兴见到你。我最近收治了一位从疗养院地下迷宫中逃出来的伤员，他差一点就被那个戴着牛头面具的怪物给抓住了。显然，地牢里的那个怪物有着严重的精神创伤，心理变态程度比那个杀人狂屠夫还重。但除去关于米诺陶的故事，伤员还提到了地下有不少的牢房，甚至还有行刑室。
  你肯定明白，在人道层面上是绝不允许这样的监禁行为的，就算是战争状态下也不例外，所以我需要你的帮助。我们必须查清楚这些牢房的用途，或者至少里面发生过什么。
  请逐一搜寻那些牢房，尽力找到任何可能有帮助的信息，以便我们查明这一切背后的真凶。更重要的是，他们到底是出于什么目的安排了这些“实验”。
- successMessageText: 一份录像带……你还没看过它，对吧？现在我知道这里发生什么了，我先前的猜想并不准确。
  在冲突爆发的时候，疗养院是 TerraGroup 的主要疏散集结点之一，而下面的地牢则被用作，呃，不同的用途……当时的情况还不像现在那么严峻，那群人至少试过将停尸房和人群密集处隔开。
  这份录像带本身没有太大风险……不过换成是我的话，我是不会再靠近那些牢房的。这种防空洞的通风质量往往都相当糟糕，而潮湿更是会助长中毒的危险……
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在迷宫中找到行刑室
  - 条件[AvailableForFinish/FindItem]: 找到并获取任何酷刑折磨的证据
  - 条件[AvailableForFinish/HandoverItem]: 上交找到的信息

### [支线] 战争从未改变 | 69ce204c8702b378f9091e4b
- id=69ce204c8702b378f9091e4b | 类型=PickUp | 地点=69af492a4819ea4ba10a69c5 | 前置=69bbfae89ce356593c0e2f35 | 后继=69ce213a298a6529b30d7134 | notDisplayed=False
- description: 你好，雇佣兵。在野战环境下，对于任何潜在风险的预防工作都至关重要。等到病人真的出现了症状，甚至是上了手术台后，我们能做的就只有利用手头并不完备的条件进行救治了。可以想见，预后并不理想……
  正因如此，我希望你能帮我找一些高剂量的碘化钾药片来。是的，碘化钾的确是严重辐射暴露环境下常用的预防药物。我知道你要问什么，但是不，塔科夫近期并没有检测到任何辐射泄漏事故。尽管如此，我还是想请你不要向外界透露这笔订单的消息。你可能意识不到，一旦民众陷入不必要的恐慌，他们会给医疗部门带来多大的压力。
  我只是为了未来可能发生的事故未雨绸缪，仅此而已。不过问题在于，我们找遍了整个塔科夫也没能发现类似的药品。核工业部门或者与核反应堆相关的地方可能会有存货，毕竟这是这种药品最主要的用途。也许你能找到相关的地方，希望你的搜寻工作比我的同事们更有成效。
- successMessageText: 你是从哪找来这么多药片的？我原本以为最多也就 5 瓶……你说核动力破冰船？真是意外惊喜！船上的物资和设备对于我们的工作可是意义重大。
  我必须得回医院里确认一件事，之后我会给你安排一项新任务。这项机会实在是太宝贵了，我们绝对不能错过。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：Nooby Shield 碘化钾片
  - 条件[AvailableForFinish/HandoverItem]: 上交物品

### [支线] 生化分析 | 69ce213a298a6529b30d7134
- id=69ce213a298a6529b30d7134 | 类型=PickUp | 地点=69af492a4819ea4ba10a69c5 | 前置=69ce204c8702b378f9091e4b | 后继=无 | notDisplayed=False
- description: 你找到的那艘破冰船可真是个大发现！如果它真的是在离岸边那么远的地方下锚的话，或许船上还有些医疗设备没被市中心的爆炸波及，也许还是完好无损的……像这样的天降大礼不会有第二次，所以我们必须抓紧机会。
  在封锁条件下，由于不具备完善的医疗设备和检测条件，我们通常依靠专用的生化分析仪来测定样本的理化性质。这套工具让我们能够对血样和其他样品进行一系列快速检测，精度也算说得过去。但是塔科夫大部分仪器都在疏散的时候被急救部门带走了。
  但那艘破冰船上也许还有能用的分析仪。如果那艘船与 TerraGroup 有关系的话，那船上器械齐备的可能性就更高了。请再帮我找找，越多越好。像这样的大型舰船通常都会设有单独的医疗舱室，里面可能还有一些存货。
- successMessageText: 你找到 Aceso 分析仪了？这也许是今天我听到的最好的消息了。鉴于塔科夫当前的物资短缺情况，可以说这些设备价值连城。
  当然，只有在万不得已的情况下，我才会考虑将它们出手，这一点我可以向你保证。至于现在，这批设备将直接用于治疗用途，我的同事们终于能从繁重的工作中解脱出来了，多少也能减轻病人承受的痛苦。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：Aceso Xpress 半自动生化分析仪
  - 条件[AvailableForFinish/HandoverItem]: 上交物品

### [主线] (无名称) | 678fa1463977eb69290a3a06
- id=678fa1463977eb69290a3a06 | 类型=Merchant | 地点=any | 前置=678f6bd1e8d46e40ff021605 | 后继=68b58c2745550ec0f408ee56,68c02056b4000f84a4026e06 | notDisplayed=False
- 条件:
  - 条件[AvailableForFinish/FindItem]: 搜集足够数量的美元
  - 条件[AvailableForFinish/HandoverItem]: 把现金交给 Therapist

### [主线] (无名称) | 6895bed8e7dac53c7c08797d
- id=6895bed8e7dac53c7c08797d | 类型=Merchant | 地点=any | 前置=6895bbb0e7dac53c7c08797b | 后继=无 | notDisplayed=False
- 条件:
  - 条件[AvailableForFinish/HandoverItem]: 把现金交给 Therapist
  - 条件[AvailableForFinish/GlobalVariableValue]: 和 Therapist 交谈
  - 条件[AvailableForFinish/FindItem]: 搜集足够数量的卢布

### [主线] (无名称) | 68b58c2745550ec0f408ee56
- id=68b58c2745550ec0f408ee56 | 类型=Merchant | 地点=any | 前置=678fa1463977eb69290a3a06 | 后继=无 | notDisplayed=False
- whileAvailableMessageText: Young man, please visit me when you can. I have news for you.
- 条件:
  - 条件[AvailableForFinish/GlobalVariableValue]: 从 Therapist 处获得信息

### [主线] (无名称) | 68c02056b4000f84a4026e06
- id=68c02056b4000f84a4026e06 | 类型=Merchant | 地点=any | 前置=678fa1463977eb69290a3a06 | 后继=无 | notDisplayed=False
- 条件:
  - 条件[AvailableForFinish/Quest]: 等待 Therapist 的消息 (提示: 应该花不了太长时间)

## 商人: Fence (579dc571d53a0658a154fbec) — 任务 19

### [支线] 建立联系 | 6672d9def1c88688a707d042
- id=6672d9def1c88688a707d042 | 类型=Loyalty | 地点=any | 前置=无 | 后继=66631489acf8442f8b05319f | notDisplayed=False
- description: 你好，雇佣兵。我听说你想要增进关系，让我们的合作更深一层？你应该知道责任越大，信任越重的道理吧？那么，向我证明你的可靠程度。
- successMessageText: 现在我们可以开始商量你的差事了。
- 条件:
  - 条件[AvailableForFinish/TraderStanding]: Fence 信任度达到 5.0

### [支线] 这是什么梗？ | 66d9cbb67b491f9d5304f6e6
- id=66d9cbb67b491f9d5304f6e6 | 类型=Multi | 地点=any | 前置=无 | 后继=无 | notDisplayed=False
- description: 你好，雇佣兵。你有没有发现自己在塔科夫路过的某些地方似曾相识？就好像之前在别的什么地方看到过一样？
  我在外面的一个联络人就是这种巧合的狂热崇拜者。现在我需要有人去帮我监视这些地方。
  还有一件事，客户让我告诉接手的人要 “尽最大努力 ”完成任务，还提到了什么 “人体里的 207 块骨头”，不管是什么意思。看样子那个小丑已经无聊疯了，连话都不会好好说了。
- successMessageText: 好吧，来瞧瞧这个。客户看到你传回来的画面喜不自胜，甚至说自己拿着刀和手枪也能完美融入我们这的环境。
  我们这的疯子已经够多了，再来个太刀爱好者只会更麻烦，更别提他还穿着那身搞笑的紧身皮衣……希望他对塔科夫只是一时的兴趣，来的快去的也快。
- 条件:
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在塔科夫街区的水陆两栖披萨爱好者藏身处安装 WI-FI 摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在塔科夫街区的烧伤女孩病房处安装 WI-FI 摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在塔科夫街区被铁丝网缠绕的尸体处安装 WI-FI 摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在塔科夫街区特别吓人的大洞处安装 WI-FI 摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在中心区虎胆龙威一展身手处安装 WI-FI 摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在中心区只有程序员才懂的笑话处安装 WI-FI 摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在中心区五黑一白椅子处安装 WI-FI 摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在灯塔你的朋友 Wilson 处安装 WI-FI 摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在灯塔点燃的篝火处安装 WI-FI 摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在灯塔的诡异欢迎招牌处安装 WI-FI 摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在灯塔的超天才科学家座椅处安装 WI-FI 摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在储备站义务兵进行最重要的任务处安装 WI-FI 摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在储备站的两把椅子谜团处安装 WI-FI 摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在储备站所有坦克手的第一款游戏处安装 WI-FI 摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在海关的水桶头人偶处安装 WI-FI 摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在海关 Anvil 3-4 乘员被处决处安装 WI-FI 摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在海关所有独立开发者的梦魇处安装 WI-FI 摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在工厂消防员烧书取乐处安装WI-FI摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在工厂阀门技术人员不会数三安装WI-FI摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在工厂的终结者爱莲巧娃娃处安装WI-FI摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在工厂的“阴谋论学家”处安装WI-FI摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在工厂的“魔法学校传送门”处安装WI-FI摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在工厂的“策略游戏之母”处安装WI-FI摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在工厂的登天楼梯处安装WI-FI摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在森林过火车辆残骸里的小熊身边安装WI-FI摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在海岸线的反重力椅子处安装WI-FI摄像头
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在海岸线的面面相觑卫生间里安装WI-FI摄像头

### [支线] (无名称) | 6902443b0d98240ccf07e8b4
- id=6902443b0d98240ccf07e8b4 | 类型=Completion | 地点=any | 前置=无 | 后继=无 | notDisplayed=True
- whileAvailableMessageText: Come. We gotta talk.

### [支线] 抉择 | 60effd818b669d08a35bfad5
- id=60effd818b669d08a35bfad5 | 类型=Loyalty | 地点=any | 前置=59ca2eb686f77445a80ed049 | 后继=无 | notDisplayed=False
- description: 别来无恙呀，雇佣兵。看起来，你并没有荒废人生，甚至还在自己的道路上走出了些事业。人在一生中，有些时候需要选择牺牲一些东西，来换取更大的利益，你明白吗？不是所有人都下得了手。很多人一辈子都不像冒冒险，他们胆小如鼠，一点都不自信。那么你呢？你准备好以小博大了吗？
- successMessageText: 勇敢的选择，雇佣兵。
- 条件:
  - 条件[AvailableForFinish/HandoverItem]: 上交Epsilon安全箱

### [支线] 陌路相交 | 66631489acf8442f8b05319f
- id=66631489acf8442f8b05319f | 类型=Elimination | 地点=any | 前置=6672d9def1c88688a707d042 | 后继=6663148ca9290f9e0806cca1 | notDisplayed=False
- description: 看起来，你在这里闯荡出了不小的名堂。我有份差事可以考虑给你这样的狠角色做。但在这之前，我需要考察你明辨是非的能力——哪些人是可以利用的资产，而哪些人只能构成威胁。很简单，只许对 PMC 开枪，不许碰我的人，懂了吗？
- successMessageText: 这是你的报酬。
- failMessageText: 看样子你并不是很在乎我手下的性命安全。事实证明，招募你没有任何好处，只会给我惹上麻烦。如果你真的打算替我干活，你就得证明自己的价值。
- whileAvailableMessageText: 看样子你并不是很在乎我手下的性命安全。事实证明，招募你没有任何好处，只会给我惹上麻烦。如果你真的打算替我干活，你就得证明自己的价值。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在不消灭 Scav 的情况下消灭 PMC

### [支线] 免疫力 | 6663148ca9290f9e0806cca1
- id=6663148ca9290f9e0806cca1 | 类型=Completion | 地点=any | 前置=66631489acf8442f8b05319f | 后继=6663148ed7f171c4c20226c1 | notDisplayed=False
- description: 既然你已经证明了自己不会无缘无故地开枪伤人，现在该直入正题了。最近我有个手下在行动的时候中了毒，没来得及撑到撤离就毒发身亡了。因此我们没法搞明白到底是什么毒素把他害死了。出事之前，我的人在那些白色的粉笔圈周围打探动静。为了搞清楚到底出了什么事，我需要一份更新鲜的活体样本——简单来说，这人的身体素质得足够过硬，在中毒之后能活着回来才行。
- successMessageText: 你带着毒素撑回来了？我会尝试从你的血样里提取任何有用的信息，再找找专家搞明白这是什么。无论最终结果如何，这是你的报酬。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在“未知毒素”效果影响下撤离区域

### [支线] 小本生意 - 1 | 6663148ed7f171c4c20226c1
- id=6663148ed7f171c4c20226c1 | 类型=PickUp | 地点=any | 前置=6663148ca9290f9e0806cca1 | 后继=6663149196a9349baa021baa | notDisplayed=False
- description: 嗯，我有一份差事要交给你。有一位手下似乎认为我的分销管路可以为他所用，而这位“创业者”最近正在向邪教徒推销他的纪念玩偶。给我找一些这种玩偶回来，我要看看里面到底有什么名堂。
- successMessageText: 谢谢，你带回来的东西证实了我的猜测。我想你应该明白保密的重要性。
- 条件:
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：圣诞老人玩偶
  - 条件[AvailableForFinish/HandoverItem]: 上交物品
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：Mutkevich 政客玩偶
  - 条件[AvailableForFinish/HandoverItem]: 上交物品
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：Killa 玩偶
  - 条件[AvailableForFinish/HandoverItem]: 上交物品
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：Reshala 玩偶
  - 条件[AvailableForFinish/HandoverItem]: 上交物品
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：Ryzhy 玩偶
  - 条件[AvailableForFinish/HandoverItem]: 上交物品
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：Scav 玩偶
  - 条件[AvailableForFinish/HandoverItem]: 上交物品
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：Tagilla 玩偶
  - 条件[AvailableForFinish/HandoverItem]: 上交物品
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：邪教徒玩偶
  - 条件[AvailableForFinish/HandoverItem]: 上交物品
  - 条件[AvailableForFinish/FindItem]: 在战局中找到物品：Den 玩偶
  - 条件[AvailableForFinish/HandoverItem]: 上交物品

### [支线] 小本生意 - 2 | 6663149196a9349baa021baa
- id=6663149196a9349baa021baa | 类型=Elimination | 地点=any | 前置=6663148ed7f171c4c20226c1 | 后继=66631493312343839d032d22 | notDisplayed=False
- description: 还记得我的那位“创业者”吗？我追查到他了，事实证明他是本地帮派的老大，具体是谁就不需要你来过问了。你的任务是上门拜访每一位匪帮头目，警告他们我的交易路线不容插手。不过不需要你出手消灭那些大人物，他们的命留着更有用。但警卫就不一样了，你需要下点狠手段，展现武力警告他们。
- successMessageText: 办成了？很好，现在没人敢对我的生意有非分之想了。
- failMessageText: 我说过不要碰 Boss 本人！我要的是恐吓他们，不是不分青红皂白杀光。
- whileAvailableMessageText: 我说过不要碰 Boss 本人！我要的是恐吓他们，不是不分青红皂白杀光。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 消灭任意 Scav 阵营 Boss 的保镖，但不击杀 Boss 本人

### [支线] 小本生意 - 3 | 66631493312343839d032d22
- id=66631493312343839d032d22 | 类型=Completion | 地点=any | 前置=6663149196a9349baa021baa | 后继=无 | notDisplayed=False
- description: 那位“创业人士”已经被处理掉了，但 城市里还是有人绕开我私下串货。记得那些喜欢用毒刀捅人玩的邪教徒吗？就是他们。给这些狂热分子带个信，让他们知道整座城市只有 Fence 能说了算。替我办成这件事，我会给你准备特别的奖赏。
- successMessageText: 你找到所有祭祀圈了吗？能干成这件事的确不简单，没有辜负我的信赖。这是你的报偿。
- 条件:
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在海岸线沉陷村庄内的祭祀圈埋藏一把邪教徒的刀
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在海岸线疗养院内的祭祀圈埋藏一把邪教徒的刀
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在森林河边村庄内的祭祀圈埋藏一把邪教徒的刀
  - 条件[AvailableForFinish/LeaveItemAtLocation]: 在森林锯木厂旁的祭祀圈埋藏一把邪教徒的刀

### [支线] 两害相权（PVE） | 683425dd8f5b18d29a05d9d1
- id=683425dd8f5b18d29a05d9d1 | 类型=Completion | 地点=56f40101d2720b2a4d8b45d6 | 前置=68342446a8d674b5740b31fc | 后继=6834254f2f0e2a7eb90b62ef,683427418f5b18d29a05d9e3 | notDisplayed=False
- description: 所以，你看见了一具尸体。那你好好搜过尸体的身了吗？有没有检查过周围的东西？这与贪婪无关，我只是指出了你的盲目而已。据我所知，那个冠军总是随身携带一个笔记本。没错，和小孩子似的，但是有时候真的能派上用场。
  那你为什么不再回去仔细看看呢？笔记本里总归会有些关于裁判的消息吧，某种黑料也不是没有可能。如果你不想继续在竞技场被当成炮灰，里面的东西都会有用的。
  最后别忘了：只要你带回来任何有价值的消息，我都会给你丰厚的报酬。
- successMessageText: 非常好。很高兴你能掌握自己命运的主动权。
  记住在那个裁判面前别提一个字。
- failMessageText: 所以你决定继续给那个裁判当狗？随便你。
- whileAvailableMessageText: 所以你决定继续给那个裁判当狗？随便你。
- 条件:
  - 条件[AvailableForStart/Quest]: 找到海关的前冠军藏身处
  - 条件[AvailableForFinish/CounterCreator]: 返回海关的前冠军藏身处
  - 条件[AvailableForFinish/FindItem]: 找到并获取竞技场裁判的黑料
  - 条件[AvailableForFinish/HandoverItem]: 上交找到的信息
  - 条件[Fail/Quest]: 该任务线与“两难抉择”冲突
  - 条件[Fail/Quest]: 该任务线与“天降大礼”冲突

### [支线] 外部利益 | 686530ba9ed06113720e2c37
- id=686530ba9ed06113720e2c37 | 类型=PickUp | 地点=any | 前置=686524fe9809a149400dd301 | 后继=无 | notDisplayed=False
- description: 你好。我最近听到了些有趣的传闻……没错，都是关于你和储备基地某位程序员之间的故事。你看，塔科夫就这么大，真是无巧不成书：看样子我们都对这位“朋友”深感兴趣。当然，具体说来，是对这位黑客达人在塔科夫地界留下的东西感兴趣。无论你都找到了什么，我们全部都要，你没听错，所有的。那么，你会当个好心人，乖乖地把我们需要的东西交出来的，对吧？
- successMessageText: 好，很好。不要向任何人提起我们之间的交易。你应该知道，我喜欢快刀斩乱麻。而那些喋喋不休纠缠不清的人最终只会成为麻烦。
- 条件:
  - 条件[AvailableForFinish/HandoverItem]: 上交程序员日志的副本

### [支线] 良心作祟 - 1（PVE） | 6834233fecd5cf3a440d855b
- id=6834233fecd5cf3a440d855b | 类型=Discover | 地点=56f40101d2720b2a4d8b45d6 | 前置=68341c4babec72d95d0c1260,697878057aa1273126030fb0 | 后继=68342446a8d674b5740b31fc | notDisplayed=False
- description: 你好，我听说你最近在为那个裁判效劳。不需要知道具体细节，我关注你已经有一会了。
  我之前认识过另一个人，和你一样，他也和那个裁判纠缠不清。这人甚至一度成为了竞技场的冠军——至于你为什么没有听说过他，自然是因为幸运女神没有垂青他太久，前任冠军先生还没来得及享受名声带来的一切，就人间蒸发、消失得无影无踪了。我这边可以肯定的是，Kaban 和 Kollontay 兄弟俩和这件事没关系，剩下的部分你就自己发挥想象吧。
  如果你不想落得和那个家伙一样的下场，就去帮我找到前任冠军的公寓。我敢打赌，那里肯定还有些有价值的东西值得打探。那个冠军曾经和海关宿舍楼的走私者们混得很熟。也许你可以从那里入手，找到进入公寓的办法。
- successMessageText: 天真到打算亲自去找裁判问个明白？好吧，很高兴认识你，小朋友。
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 在海关找到前冠军的房间
  - 条件[AvailableForFinish/CounterCreator]: 调查前任冠军的遭遇
  - 条件[AvailableForFinish/CounterCreator]: 以幸存状态撤离该区域
  - 条件[AvailableForFinish/FindItem]: 在海岸线的走私者营地找到并获取钥匙

### [支线] 收藏家 | 5c51aac186f77432ea65c552
- id=5c51aac186f77432ea65c552 | 类型=PickUp | 地点=any | 前置=597a0e5786f77426d66c0636,5ae4497b86f7744cf402ed00,5bc480a686f7741af0342e29,5c0bde0986f77479cf22c2f8 | 后继=无 | notDisplayed=False
- description: 是你啊，雇佣兵，本地的麻烦克星。我观察你已经有一段时间了，眼看着你解决其他商人指派给你的各种任务。但是你最好明白一件事，你可不是什么独一无二的存在，比你更专业的家伙大有人在。我也有一份委托要交给你，准确地来说，是一个机会：这份事业的宏大程度远超你的想象，而你将有机会加入其中。如果你准备好了，那就仔细听好：
  我的人足迹遍布整个塔科夫，效命于我的同时也在身后留下了印记，某种纪念品，可以这么说。而那些东西正是我需要的，其他的就不必多问了。这些东西相当少见，但你得靠自己找到它们——这点应该不用我多提醒吧？相信我，我分辨谎言的能力远超其他人。当一切准备就绪之后，会有人把交货点的消息带给你的。
  还有一件事，我的这些伙伴早在很久之前就已经通过了我的试炼，证明了自己除了战斗技巧高超之外还懂得忠诚的价值。如果你也想成为其中的一员，那就证明自己不比他们差。不过就别白费心思试图用花言巧语蒙蔽我了，等到时机到来时，答案自会揭晓。
- successMessageText: 你都搞到我要的东西了？很好。雇佣兵，你用行动证明了自己的价值。你的奖励已经在交货点等着你了。祝贺你，欢迎成为我们的一员。
- 条件:
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：古董斧头
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：破旧的古董书
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：#FireKlean牌枪润滑油
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：银徽章
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Deadlyslob的胡须油
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：金色1GPhone
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：假胡子
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：乌鸦雕像
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Pestily瘟疫面具
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Shroud半面巾
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Dr. Lupo 的咖啡豆
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：42 Signature Blend英式茶
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Veritas吉他拨片
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：罐装六可乐
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Loot Lord毛绒玩具
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Smoke巴拉克拉瓦头套
  - 条件[AvailableForFinish/HandoverItem]: 上交WZ钱包
  - 条件[AvailableForFinish/HandoverItem]: 上交LVNDMARK的老鼠药
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Missam叉车钥匙
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：《天雷地火》录像带
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：BakeEzy烹饪书
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：JohnB Liquid DNB墨镜
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Baddie的红胡子
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：DRD防弹衣
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Gingy钥匙串
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：金蛋
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：记者证（签发给NoiceGuy）
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Axel鹦鹉雕像
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：BEAR Buddy毛绒玩具
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Glorious E轻型防弹面具
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Inseq 燃气扳手
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Viibiin 跑鞋
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Tamatthi 苦无（仿制品）
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Nut Sack 巴拉克拉瓦头套
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Mazoni 金色哑铃
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Tigzresq 夹板
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Domontovich 毛帽
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：Dunduk 软盘
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：DesmondPilak CD 专辑
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：SheefGG 存钱罐
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：GigaBeef 咸牛肉罐头
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：LM KC-130 飞机模型
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：法国面包房特产法棍
  - 条件[AvailableForFinish/HandoverItem]: 上交战局中找到的物品：油墨香车瓶装水

### [主线] (无名称) | 67bdf2cecf0cc29475092199
- id=67bdf2cecf0cc29475092199 | 类型=Multi | 地点=5704e554d2720bac5b8b456e | 前置=无 | 后继=69247ccc803723e83c0439fd,6924820dbf8cf5498b05dac0 | notDisplayed=False
- 条件:
  - 条件[AvailableForFinish/Quest]: 完成 Fence 指派的任务
  - 条件[AvailableForFinish/TraderStanding]: 将 Fence 的信任度保持在 4.0 以上 (提示: 要是我让 Fence 失望，或者惹恼了他，他很可能就再也不会回话了)
  - 条件[AvailableForFinish/GlobalVariableValue]: 告诉 Fence 任务已经圆满完成

### [主线] (无名称) | 68e90a73a3d110355b03e3a2
- id=68e90a73a3d110355b03e3a2 | 类型=Skill | 地点=any | 前置=67bdeeee884b40ea9d08b5c4 | 后继=无 | notDisplayed=False
- 条件:
  - 条件[AvailableForFinish/Quest]: 等待 Kerman 先生信任的可靠联络人发来联系 (提示: Kerman 保证我可以从他在塔科夫信得过的联络人那里收到对应的哈希代码)

### [主线] (无名称) | 68ea9b0b8bfb4ae0aa04ddf9
- id=68ea9b0b8bfb4ae0aa04ddf9 | 类型=Skill | 地点=any | 前置=67bdeeee884b40ea9d08b5c4 | 后继=无 | notDisplayed=False
- whileAvailableMessageText: I have news for you. I'll be waiting.
- 条件:
  - 条件[AvailableForFinish/GlobalVariableValue]: 和 Fence 交谈

### [主线] (无名称) | 68f03d79d0745b2b100be32c
- id=68f03d79d0745b2b100be32c | 类型=Multi | 地点=5704e554d2720bac5b8b456e | 前置=67bdeeee884b40ea9d08b5c4 | 后继=无 | notDisplayed=False
- 条件:
  - 条件[AvailableForFinish/TraderStanding]: Fence 信任度达到 4.0 (提示: 我得完成 Fence 的差事，帮助 Scav，购买那些付费撤离服务)
  - 条件[AvailableForFinish/GlobalVariableValue]: 和 Fence 交谈

### [主线] (无名称) | 69247ccc803723e83c0439fd
- id=69247ccc803723e83c0439fd | 类型=Skill | 地点=any | 前置=67bdf2cecf0cc29475092199 | 后继=无 | notDisplayed=False
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 单场战局中在立交桥消灭 PMC 行动人员 (提示: Fence 只为我设定了一个目标：在完成任务的时候不能杀 Scav，一个也不行)

### [主线] (无名称) | 6924820dbf8cf5498b05dac0
- id=6924820dbf8cf5498b05dac0 | 类型=Skill | 地点=any | 前置=67bdf2cecf0cc29475092199 | 后继=无 | notDisplayed=False
- 条件:
  - 条件[AvailableForFinish/CounterCreator]: 单场战局中在海岸线消灭 PMC 行动人员 (提示: Fence 只为我设定了一个目标：在完成任务的时候不能杀 Scav，一个也不行)
