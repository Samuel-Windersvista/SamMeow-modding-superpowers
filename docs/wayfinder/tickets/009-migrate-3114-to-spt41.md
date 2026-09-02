# Ticket: Migrate 3114 modpack to SPT 4.1 (Inescapable Tarkov)

> Label: `wayfinder:epic`
> Status: **in-progress** (created 2026-08-07)
> Blocks: none (consumes #4 pipeline stages [2]/[2b]/[3]/[5]/[6])
> Blocked by: nothing — all prerequisites closed (#8 usvfs, spt-mcp, IL pipeline)

## MO2 overlay 状态跟踪（权威清单，每次会话必读）

> **整合包的 mod 只放 MO2 overlay：`E:\Game\EFT_Offline\Inescapable Tarkov\mods\<overlayName>\`**
> 游戏本体 `E:\Game\EFT_Offline\SPT_410\` 只保留 SPT 官方模块（`BepInEx\plugins\spt\`），**绝不直接写入 mod**。
> MO2 通过 customExecutables 里的 `sptvfsbridge.bat` 以 VFS 方式启动（usvfs 注入 3 进程链，wayfinder #8 已验证）。
> 部署流程：mod → overlay（`BepInEx\plugins\` 或 `SPT_Runtime\` 子目录）→ 加入 `profiles\Default\modlist.txt`（`+` 前缀启用）。
> 教训（2026-08-07）：曾直接写入 SPT_410 游戏本体导致 Stock Game 污染，已全部回迁 overlay。检查用 `mods\` 目录 + modlist.txt，不是游戏目录。

### 当前 overlay 清单（2026-08-08，87 个已启用）

| overlay | forgeId | 来源 | 内容 |
|---------|---------|------|------|
| 《生活在诺文斯克》腰包小容器-WTT-PackNStrap沉浸感增强设置 | 1278 | Wave4c-2 移植 | BepInEx/plugins/WTT-PackNStrap.dll + SPT_Runtime/user/mods/WTT-PackNStrap/ |
| [1]前置-《无法逃离塔科夫》好奇猫自用游戏设置及按键 | — | 3114 原包 | SPT_Runtime 配置 |
| [1]前置-《无法逃离塔科夫》修改本体及MOD排序文件和整合包说明文档 | — | 3114 原包 | 文档 |
| [1]前置框架-WTT公共库-WTT-ClientCommonLib | 2310 | A 桶 3.0.3 | BepInEx+SPT_Runtime |
| [1]前置框架-KmyTarkovApi | 898 | B 桶移植 | BepInEx/plugins/KmyTarkovApi*.dll ×4 |
| [1]前置框架-自定义任务加载器-Virtual's Custom Quest Loader | 649 | B 桶移植 | BepInEx/plugins/VCQLQuestZones.dll |
| [2]新物品-镀金钥匙容器-GildedKeyStorage客户端文件 | 865 | Wave3 移植 | BepInEx/plugins/DrakiaXYZ-GildedKeyStorage.dll（服务端 TS 未移植） |
| [2]新物品-藏身处猫咪-hideoutcat | 2038 | Wave4b 移植 | BepInEx/plugins/hideoutcat.bepinex.dll（bundle 资源缺口） |
| [2]新酒水-HoodsEnergyDrinks | 1688 | C桶第一优先 | SPT_Runtime/user/mods/HoodsEnergyDrinks/（87 bundles） |
| [2]高清材质-TarkovHDRework | 1896 | C桶第一优先 | SPT_Runtime/user/mods/TarkovHDRework/（47 bundles） |
| [2]新装备、衣服-TacticalGearComponent | 1125 | C桶第二优先 | SPT_Runtime/user/mods/TGC-NG/（4.1.2 重编译 ✓） |
| [4]新商人-荷官-Croupier (random loadouts + flea quicksell) | 1971 | C桶第一优先 | SPT_Runtime/user/mods/Croupier/（6 bundles+数据） |
| [4]新商人-竞技场商人-ref-sptfriendly-quests | 1538 | C桶第一优先 | SPT_Runtime/user/mods/acidphantasm-reffriendlyquests/ |
| [4]新商人-Artem | 1023 | C桶第二优先 | SPT_Runtime/user/mods/WTT-Artem/（4.1.2 重编译 ✓，304 files） |
| [4]新商人-油漆匠-Painter | 1025 | C桶第二优先 | SPT_Runtime/user/mods/Painter-4.0/（4.1.2 重编译 ✓） |
| [5]全地图间移动套装-IEAPI+PTT - AI优化版 | 1676 | Wave4b 移植 | BepInEx/plugins/InteractableExfilsAPI.dll |
| [6]AI拟人MOD的前置框架-BigBrain - 已AI优化 | 902 | A 桶 1.5.0 + 本体回迁 | BepInEx/plugins/DrakiaXYZ-BigBrain.dll |
| [6]AI增强-SAIN-Moew-v4.4.0增强版 | 791 | 本体回迁（4.4.3 移植） | BepInEx/plugins/SAIN + SPT_Runtime/user/mods/Solarint-SAIN-ServerMod |
| [6]AI路径扩展-Waypoints - 已AI优化 | 827 | 本体回迁（1.9.0 移植） | BepInEx/plugins/DrakiaXYZ-Waypoints |
| [6]AI自动搜索物品-LootingBots - 已AI优化 | 812 | 本体回迁（7/7 patches） | BepInEx/plugins/skwizzy.LootingBots.dll |
| [6]AI锁头重定向-Headshot Damage Redirection | 2115 | Wave2 移植 | BepInEx/plugins/perfk.HeadShotRedirect.dll |
| [6]分离敌意-SeparateHostility | 2248 | Wave4a 移植 | BepInEx/plugins/dk.SeparateHostility.dll |
| [7]更好的枪匠界面-Tradermodding客户端文件 | 1038 | Wave3 移植 | BepInEx/plugins/ChooChoo-TraderModding.dll |
| [7]更好的枪匠界面-Tradermodding服务器文件 | 1038 | Wave3 移植 | SPT_Runtime/user/mods/ChooChoo-TraderModding_Server/ |
| [7]平衡的夜视装备视效-BRNVG客户端文件 | 954 | Wave3 移植 | BepInEx/plugins/BorkelRNVG.dll |
| [7]平衡的夜视装备视效-BRNVG服务器文件 | 954 | Wave3 移植 | SPT_Runtime/user/mods/BorkelRNVGServer/ |
| [7]瞄准视野大修-FOV-Fix | 701 | Wave4a 移植 | BepInEx/plugins/FOVFix.dll |
| [7]武器动画更新器-AnimatorUpdater | 2006 | Wave4c 移植 | BepInEx/plugins/UpdateHierarchy.dll |
| [7]可以合并消耗品-MergeConsumables | 1657 | Wave4c-2 移植 | BepInEx/plugins/MergeConsumables.dll + SPT_Runtime/user/mods/MergeConsumablesServer/ |
| [7]武器配件位置自定义-WeaponCustomizer | 1950 | Wave4c-2 移植 | BepInEx/plugins/Tyfon.WeaponCustomizer.dll + SPT_Runtime/user/mods/Tyfon.WeaponCustomizer.Server/ |
| [7]更亮的镭射线-brightlasers | 1358 | C桶第一优先 | SPT_Runtime/user/mods/acidphantasm-brightlasers/（laser.bundle） |
| [8]boss刷新通知-BossNotifier | 1247 | Wave1 移植 | BepInEx/plugins/BossNotifier.dll |
| [8]可拾取穿墙物品-ReachExtender | 1260 | Wave1 移植 | BepInEx/plugins/ReachExtender.dll |
| [8]直接使用未捡物品-UseLooseLoot | 933 | Wave1 移植 | BepInEx/plugins/Gaylatea-UseLooseLoot.dll |
| [8]直接搜索已打开容器-SearchOpenContainers | 934 | Wave1 移植 | BepInEx/plugins/DrakiaXYZ-SearchOpenContainers.dll |
| [8]禁用Scav模式-DisableScavMode | 977 | Wave1 移植 | BepInEx/plugins/DisableScavMode-egboggied.dll |
| [8]门的状态随机化-DoorRandomizer | 820 | A 桶 1.8.0 | BepInEx |
| [8]门可以锁-LockableDoors | 1923 | Wave3 移植 | BepInEx/plugins/LockableDoors.dll（控制台命令剥离；Server 4.0.5 待定） |
| [8]任务跟踪显示-QuestTracker | 1140 | C桶第一优先 | BepInEx/Plugins/DrakiaXYZ-QuestTracker/（DLL+bundle） |
| [8]可以攀爬更高的地方-Increase Climb Height | 1575 | C桶第一优先 | SPT_Runtime/user/mods/Increase Climb Height/ |
| [8]Boss身上有lega徽章-bosseshavelegamedals | 1539 | C桶第一优先 | SPT_Runtime/user/mods/acidphantasm-bosseshavelegamedals/ |
| [8]好莱坞级视觉系统合集-HollywoodFX+HollywoodGraphics-已AI优化 | 2003 | C桶第一优先 | BepInEx/plugins/HollywoodFX/ + HollywoodGraphics/ |
| [12]尸体可删除-SPTCorpseCleaner | 2058 | Wave2 移植 | BepInEx/plugins/SPTCorpseCleaner.dll |
| [8]身上任何物品均可绑定快捷键-UseItemsFromAnywhere | 2386 | Wave3 移植 | BepInEx/plugins/UseItemsFromAnywhere.dll |
| [8]技能速度调节器-SkillMultiplier | 2162 | Wave3 移植 | BepInEx/plugins/dazzuh.skillmultiplier.dll（服务端 TS 未移植） |
| [8]战场环境临场感增强-SPTBattleAmbience | 2264 | Wave3 移植 | BepInEx/plugins/SPTBattleAmbience.dll + assets\ |
| [8]坠落的补给直升机-HeliCrash.ArysReloaded | 1292 | Wave4c 移植 | BepInEx/plugins/SamSWAT.HeliCrash.ArysReloaded\（4.1 DLL + bundle 28MB + json） |
| [8]超重也能练耐力-Always-Level-Endurance | 697 | Wave4a 移植 | BepInEx/plugins/Endurance.dll |
| [8]武器破门-BackdoorBandit客户端文件 | 858 | Wave4a 移植 | BepInEx/plugins/dvize.BackdoorBandit.dll |
| [8]增加拾取距离-LootRadius | 1349 | Wave4a 移植 | BepInEx/plugins/DrakiaXYZ-LootRadius.dll |
| [8]更有意义的武器精通-MeaningfulWeaponMasteries | 1989 | Wave4a 移植 | BepInEx/plugins/MeaningfulWeaponMasteries.dll |
| [9]预览窗口尺寸调整-previewsizer | 2339 | A 桶 1.1.0 | BepInEx |
| [9]简单的锻炼QTE-simpleworkoutqte | 1437 | A 桶 2.2.0 | BepInEx+SPT_Runtime |
| [9]动态物品重量-DynamicItemWeights | 2344 | Wave2 移植 | BepInEx/plugins/Tosox.DynamicItemWeights.dll |
| [9]子弹专家-MunitionsExpert | 972 | Wave4b 移植 | BepInEx/plugins/MunitionsExpert.dll（BepInEx 壳） |
| [9]腰带槽-BeltSlot | 2181 | Wave4b 移植 | BepInEx/plugins/BeltSlot.dll |
| [9]任务交接自动化-TaskAutomation | 2238 | Wave4c 移植 | BepInEx/plugins/TaskAutomation.dll |
| [9]物品打勾标记提供更多信息-MoreCheckmarks | 861 | Wave4c-2 移植 | BepInEx/plugins/MoreCheckmarks/（DLL+Assets 无扩展名 bundle）+ SPT_Runtime/user/mods/MoreCheckmarksBackend/ |
| [10]从武器架装备武器-EquipFromWeaponRack | 1136 | A 桶 1.6.0 | BepInEx |
| [10]物品容器间快速移动-QuickMoveToContainer | 1341 | Wave2 移植 | BepInEx/plugins/DrakiaXYZ-QuickMoveToContainer.dll |
| [10]仓库搜索框-StashSearch | 2148 | Wave4b 移植 | BepInEx/plugins/StashSearch.dll + stashsearch.bundle（A 层已补） |
| [10]物品自动分类功能-INVENTORY ORGANIZING FEATURES | 856 | Wave4c 移植 | BepInEx/plugins/Seion.Iof.dll |
| [10]UI大修-UIFixes | 1342 | Wave4c-2 移植 | BepInEx/plugins/Tyfon.UIFixes.dll + SPT_Runtime/user/mods/Tyfon.UIFixes.Server/ |
| [11]扫敌雷达-RadarStandalone | 1100 | A 桶 1.3.0 | BepInEx |
| [11]内置动态地图-DynamicMaps | 1431 | A 桶 1.2.0 | BepInEx+SPT_Runtime |
| [11]任务跳过功能-Skipper | 1343 | Wave1 移植 | BepInEx/plugins/Terkoiz.Skipper.dll |
| [11]更多界面信息-GamePanelHUD | 456 | B 桶移植 | BepInEx/plugins/GamePanelHUD*.dll ×8 |
| [11]物品感应-AmandsSense | 2521 | Wave3 移植 | BepInEx/plugins/AmandsSense.dll |
| [11]声音可视化-accessibilityindicators | 1760 | Wave3 移植 | BepInEx/plugins/acidphantasm-accessibilityindicators.dll |
| [11]我就是爹作弊工具-DadGamerMode | 686 | Wave4a 移植 | BepInEx/plugins/dvize.DadGamerMode.dll |
| [11]好莱坞投掷武器视觉辅助-VisualAssist | 2213 | Wave4c 移植 | BepInEx/plugins/VisualAssist.dll |
| [12]任务列表修复-TaskListFixes | 824 | Wave2 移植 | BepInEx/plugins/DrakiaXYZ-TaskListFixes.dll |
| [12]防卡手-HandsAreNotBusy | 1298 | Wave4a 移植 | BepInEx/plugins/HandsAreNotBusy.dll |
| [12]商人列表滚动-TraderScrolling | 1089 | Wave2 移植 | BepInEx/plugins/Kaeno-TraderScrolling.dll |
| 《生活在诺文斯克》战局大修-RaidOverhaul沉浸感增强设置 | 1192 | C桶第二优先 | SPT_Runtime/user/mods/RaidOverhaul/ + BepInEx（SptVersion 补丁） |
| 《生活在诺文斯克》AI生成-botplacementsystem沉浸感增强设置 | 2097 | C桶第二优先 | SPT_Runtime/user/mods/acidphantasm-botplacementsystem/ + BepInEx/plugins（SptVersion 补丁） |
| [2]自定义配方-tarkovcraft | 2628 | C桶第二优先 | SPT_Runtime/user/mods/TarkovCraft-Loader/（SptVersion 补丁） |
| [2]新物品-地图容器-SecureMapbookMod | 2060 | C桶第二优先 | SPT_Runtime/user/mods/MrVibesRSA-SecureMapbook/（SptVersion 补丁） |
| [3]新配件-ConsortiumOfThings | 2195 | C桶第二优先 | SPT_Runtime/user/mods/EukyreECOT/（263 files，SptVersion 补丁） |
| [4]新商人-藏身处物资商人-acidphantasm-harryhideout | 1303 | C桶第二优先 | SPT_Runtime/user/mods/acidphantasm-harryhideout/（SptVersion 补丁） |
| [4]新商人-毒蝎-acidphantasm-scorpion | 1348 | C桶第二优先 | SPT_Runtime/user/mods/acidphantasm-scorpion/（SptVersion 补丁） |
| [5]新增5个开局角色-CustomProfiles | 2614 | C桶第二优先 | SPT_Runtime/user/mods/RZCustomProfiles/（SptVersion 补丁） |
| [6]友方AI系统-friendlypmc与PITFireTeam合并版本 | 2676 | C桶第二优先 | SPT_Runtime/user/mods/pitFireTeam-ServerMod/ + BepInEx/plugins（SptVersion 补丁） |
| [6]Acid的AI装备管理系统-progressivebotsystem - 已AI优化 | 1594 | C桶第二优先 | SPT_Runtime/user/mods/acidphantasm-progressivebotsystem/（SptVersion 补丁） |
| [7]更好的机瞄-BetterRearSights | 1591 | C桶第二优先 | SPT_Runtime/user/mods/SPTBetterRearSights/（SptVersion 补丁） |
| [8]边走边装子弹-ContinuousLoadAmmo - 已AI优化 | 2112 | C桶第二优先 | BepInEx/plugins/ContinuousLoadAmmo.dll（客户端） |
| [11]新商人-可购买自己创建的预设枪械-Hephaestus | 874 | C桶第二优先 | SPT_Runtime/user/mods/AES/（94 files，SptVersion 补丁） |

### MO2 操作注意事项（2026-08-07 教训）

1. **改 modlist.txt 必须用 Python `write_text(encoding='utf-8')`（无 BOM）**，PowerShell 5.1 `Set-Content -Encoding UTF8` 追加会破坏中
2. **验证一致性**：`mods\` 目录数 == modlist `+` 数，无乱码、无空 overlay。用 Python 脚本检查（PowerShell 处理中文路径会失败）。
3. 游戏本体 `SPT_410\BepInEx\plugins\` 只留 `spt\` 官方模块，其余全是 MO2 overlay 管理。
4. **分类排序规则（2026-08-08 固化）**：overlay 目录名必须带 `[数字]` 前缀（继承 3114 原包分类语义：1=前置 2=材质/物品 3=配件 4=商人 5=全地图 6=AI 7=武器/枪匠 8=环境 9=平衡/技能 10=物品栏/显示 11=界面/任务 12=任务/商人修复 13=工具 14=汉化）。**modlist.txt 顺序 = 无前缀(0) → [14] → [13] → ... → [2] → [1] 数字降序**（与 3114 原包 modlist 完全一致；[1] 前置在最底部=最高 priority）。新增 overlay 用 `D:\Temp\opencode\deploy-mo2.py --add "<overlay>" --plugin <dll>` 自动插入分类位置，禁止手写追加。验证用 `sort-modlist.py` 或 `deploy-mo2.py --sort-only`。
5. **Bundle 适配检查（2026-08-08 固化，每次部署带 bundle 的 mod 必查）**：
   - **环境 Unity 版本**：SPT_410 = Unity 2022.3.43f1（UnityPlayer.dll 实测）。3114 原包也是 2022.3.43f1 → 从原包同步的 bundle 天然适配。
   - **bundle 内脚本引用检查**（UnityPy 深读）：bundle 内 MonoBehaviour 的 m_Script 若引用外部 DLL（如 GamePanelHUDHealth.dll），必须核对 4.1 重编译 DLL 的**程序集名 + 命名空间 + 类名**一致；若只引用 UnityEngine.UI/TMP 或游戏内置 Assembly-CSharp（HeliCrash 的 BallisticCollider/Door/LootableContainer）则安全。
   - 检查脚本：`D:\Temp\opencode\inspect-bundle-scripts.py`（列出 bundle 全部 MonoScript 引用）、`check-bundle-version.py`（Unity 版本头）、`check-bundle-gaps.py`（原包 vs overlay 遗漏对照）。
   - **2026-08-08 已检查 26 个 bundle 全部安全**：StashSearch/accessibility 纯系统 UI；GamePanelHUD ×6 引用的 GamePanelHUDHealth.dll 3 类（HealthHUDController/HealthHUDView/HealthUIView）与 4.1 DLL 完全匹配；KmyTarkovApi config bundle 引用的 KmyTarkovConfiguration.dll 9 类匹配；Waypoints navmesh 纯数据；HeliCrash/BRNVG 游戏内置组件。
   - **遗漏 2 处已补（check-bundle-gaps.py 发现）**：KmyTarkovApi `kmytarkovconfiguration.bundle`（87KB）+ BRNVG 服务器文件 6 个夜视 bundle（33MB）——已从原包同步。

## Goal

Port the Life_in_Norvinsk_v0.3.2 modpack (SPT 3.11, 165 enabled mods) to
SPT 4.1.1 at MO2 instance `E:\Game\EFT_Offline\Inescapable Tarkov`
(game install `E:\Game\EFT_Offline\SPT_410`), preserving the 3114 pack's
style/direction. This is the first full-chain exercise of the whole
toolchain: mod matching, mod porting, mod writing, ordering, compatibility,
build, verify.

## Per-mod triage rule (Overseer directive 2026-08-07)

For each enabled mod:
1. **Check Forge for updates** — if a 4.1-compatible version exists, use it (bucket A)
2. **No 4.1 version but 3114 source available** (`archive/forge/mods/<id>_source/`) — port it (bucket B)
3. **Neither** — record and escalate to Overseer, who decides: rewrite from
   scratch / drop / substitute with another mod (bucket C)

## Environment facts (verified 2026-08-07)

- SPT **4.1.2**-40743 installed at `E:\Game\EFT_Offline\SPT_410` (server verified 4.1.2-RELEASE+cf04a11; client binary identical to 4.1.1 -- compatibility rules unchanged)
- MO2 instance `E:\Game\EFT_Offline\Inescapable Tarkov` exists: 2 Chinese
  prerequisite mods already in `mods/`, Default profile, downloads empty
- Local Forge archive: 95 hot-mods version files with `spt_version_constraint`
  per version -- primary classification source
- Live Forge still reachable (mod pages show latest version + SPT compat);
  use for mods missing from hot-mods or stale entries
- Data point: ALP latest (5.5.0+beta, 2026-05) still targets SPT 3.11.4 --
  expect a large bucket B/C
- **Source backup expanded (2026-08-07)**: ALL 4.x-compatible hot mods now have
  source in `archive/forge/mods/<id>_source/` (84/84, 124 dirs). B bucket mods
  already ported this session: LootingBots 812 (verified 7/7 patches).

## 4.1-compatibility rule (Overseer directive 2026-08-07)

Target install is SPT **4.1.x** (4.1.2)-40743. A version counts as 4.1-compatible iff
its `spt_version_constraint` mentions 4.1.x (any x, including 4.1.1) AND is
not an upper-bound exclusion (`<4.1.y` with y<=1 excludes our 4.1.2 install).
`4.0`/`~4.0` does NOT count. Ranges that include 4.1 without naming it
(e.g. `>4.0 <4.2.0`) are a known unhandled edge -- flag for manual review.
Classifier: `D:/Temp/opencode/m0-classify.js` (rule verified conformant).

**Compatibility ruling (Overseer directive 2026-08-07)**: `~4.0`-constrained
mods are DEFINITIVELY incompatible with 4.1.x (client binary bump breaks client
mods; server mods cannot be trusted either). NO Batch 0 empirical pre-test --
B bucket = full recompile-fix for every mod, no verify-as-is short-circuit.

## Stage plan

```
[M0] Inventory/classify  165 mods -> migration-manifest.json   [DONE 2026-08-07]
     (mod dir name -> forge id -> latest version -> spt_version_constraint -> bucket A/B/C)
[M1] Bucket A assemble   download 4.1 versions -> MO2 overlays   [DONE 2026-08-07]
     8/8 downloaded (forge direct link, curl + browser UA; 4 files are .7z not .zip --
     extracted via py7zr) and filled into Inescapable Tarkov\mods\ overlays:
     1100 RadarStandalone 1.3.0 / 1431 DynamicMaps 1.2.0 / 1136 EquipFromWeaponRack 1.6.0 /
     2339 previewsizer 1.1.0 / 1437 simpleworkoutqte 2.2.0 / 820 DoorRandomizer 1.8.0 /
     902 BigBrain 1.5.0 / 2310 WTT-CommonLib 3.0.3. Note: DynamicMaps/simpleworkoutqte/
     WTT-CommonLib include server-mod parts (SPT_Runtime\user\mods) -- VFS handles both.
[M2] Bucket B port       server: recompile vs SPT_410 refs + API-break fixes
                         client: recompile vs new assemblies + Harmony target fixes
                         (FULL recompile-fix for every B mod -- no verify-as-is,
                          per Overseer directive 2026-08-07)
                         [Batch 1 foundation, 2026-08-07] KmyTarkovApi 898 DONE
                         (15 type mappings, 6-project solution compiles 0 errors,
                          knowledge: pilot-experience-kmytarkovapi.md; 1 deferred:
                          ResourceKeyManagerAbstractClass deleted in 4.1 -> null)
                         GamePanelHUD 456 DONE (10/10 projects 0 errors; verified
                          KmyTarkovApi dependency chain; Map semi-stub + Build tool
                          downgraded per PORT-NOTE)
                         Virtual's Custom Quest Loader 649 DONE (0 errors; ConsoleScreen
                          moved bsg.console.core->EFT.UI; global Utils conflict; 4.1
                          TriggerWithId.Awake now virtual)
                         [Batch 1 remaining] SAIN 791 / Waypoints 827 / BigBrain-dep
                         mods / UIFixes 1342 etc.
[M3] Bucket C decisions  escalate list -> Overseer per-mod verdicts
[M4] Conflict analysis   spt-mcp analyze-conflicts (B/O/I tiers) on assembled set
[M5] Build               MO2 profile in Inescapable Tarkov instance
[M6] Verify              Level B: launch chain + server log + BepInEx log per-mod confirmation
```

## M0 results (2026-08-07, final manifest: D:/Temp/opencode/3114-to-41-migration-final.json)
## REVISED 2026-08-07: B bucket reconciled against deployed plugins -- 6 mods were
## already ported in earlier sessions (SAIN/Waypoints/LootingBots/KmyTarkovApi/
## GamePanelHUD/VCQL) and moved to DONE. Remaining B = 49 unique mods.

| Bucket | overlays | unique mods | meaning |
|--------|----------|-------------|---------|
| A | 8 | 8 | has official 4.1 version (RadarStandalone, DynamicMaps, EquipFromWeaponRack, previewsizer, simpleworkoutqte, DoorRandomizer, BigBrain, WTT-CommonLib) |
| B | 61 | 49 | no 4.1, local source exists -> port queue (revised; 6 previously-ported moved to DONE) |
| C | 69 | 62 | no 4.1, no local source -> Overseer decision (mostly Chinese AI-modified versions of Forge originals) |
| U | 12 | -- | unresolved even via catalog (SkillsExtended x2, Samuel's Tweaks, Volcano-subtitle, QBZ191, AK50...) |
| COMM | 8 | -- | Chinese community configs/translations (F12 manager, font replace, AI preset, keybinds...) |
| DONE | 7 | 6 | already ported & deployed: SAIN 791 (4.4.3, 106 patches), Waypoints 827 (1.9.0), LootingBots 812 (7/7), KmyTarkovApi 898, GamePanelHUD 456, VCQL 649 |

Live-check infrastructure: `D:/Temp/opencode/live-check.ps1` (idempotent, api/v0 +
custom UA + 3s throttle); archive hot-mods version files 95 -> 190.
Pipeline scripts: m0-classify.js, u-match.js, final-merge.js (all D:/Temp/opencode).

Key open strategic question surfaced by M0: do `~4.0.0`-constrained mods
actually run on SPT 4.1.x? **RESOLVED 2026-08-07 (Overseer directive): NO.**
`~4.0` mods are definitively incompatible with 4.1.x (client binary bump breaks
client mods; server mods untrustworthy). B bucket = full recompile-fix; the
proposed Batch 0 empirical test is cancelled.

## Known gaps (2026-08-08)

- **Localization (mod 中文汉化)**: 原包 mod 是中文二改版，但 A/B 桶用的是 Forge 英文原版
  （移植后文本会变英文）。本次 M0-M6 不处理；**另立任务**排期汉化。
  记录点：M2 移植时若发现 mod 自带语言文件（json/UI text），可顺带迁入 overlay，
  但不阻塞移植。
- **2299 ShowMeTheMoney 源码错误（2026-08-08）**: Forge 存档
  `Show-Me-The-Money_2299_source` 是**同名不同项目**——普渡大学黑客松的股票分析应用
  （bloomberg.js / Sentimental / "Stock data visualization"，依赖 Bloomberg API 订阅），
  与 SPT 显示价格 mod 完全无关。需要从别处找正确源码（Nexus/Forge 重新下载或作者 GitHub），
  否则该 mod 标记 source-wrong 待补。
- **服务端 TS mod → 4.1 需重写 C#（2026-08-08）**: SPT 4.x 服务端改为 C#（SPTarkov），
  **3.x 的 TS/JS 服务端 mod 不再兼容**，需重写为 C#（`[Injectable]` + `IOnLoad` + 
  PackageReference SPTarkov.Server.Core）或找作者 4.x 版本。受影响：865 GildedKeyStorage 
  （TS 服务端）、2162 SkillMultiplier（TS 服务端）、2246 WTT-Armory（VCQL quest 包+JS）。
  与此相反，**954 BorkelRNVG / 1038 TraderModding 的 Forge 源码已是作者适配的 4.x 版本**
  （服务端 C# SPTarkov.Server.Core 4.1.0，net10.0），可直接编译。
- **客户端引用 spt-* 全套模块（2026-08-08）**: 4.x 客户端 mod 引用
  spt-common/spt-core/spt-custom/spt-debugging/spt-reflection/spt-singleplayer——
  已从 `SPT_410\BepInEx\plugins\spt\` 复制全部 6 个进 Refs-410（现 47 个 dll）。
- **SPT 4.1.2 = EFT 0.16.9.40743（2026-08-08 确认）**: `EscapeFromTarkov.exe` 
  FileVersion 0.16.9.40743 / FilePrivatePart 40743。**与 3.11 时代 DrakiaXYZ 等作者 
  版本门禁常量完全匹配**（如 2386 UseItemsAnywhere 的 TarkovVersion=40743）——带版本
  门禁的 mod 无需改门禁。这也解释了 Wave 2 的 DrakiaXYZ 作品零源码改动即可编译。
- **运行时资源缺口（2026-08-08）**: 以下 mod 源码移植编译成功，但运行时资源（AssetBundle）
  不在源码仓库。**A 层已执行**：从 3114 原包 `E:\Game\EFT_Offline\Life_in_Norvinsk_v0.3.2\mods\`
  同步到 overlay（脚本 `D:\Temp\opencode\sync-bundles.py`）：
  - 2148 StashSearch：`stashsearch.bundle` ✓ 已同步
  - 1760 accessibilityindicators：`accessibilityindicators.bundle` ✓
  - 456 GamePanelHUD：`gamepanel*.bundle` ×6 ✓
  - 2038 hideoutcat：`tarkin\`（AssetBundleLoader.dll + hideoutcat/hideoutcat_audio/hideoutcat_props bundles + CatNodeGraph.json）✓
  （hideoutcat 的 3.11 版 hideoutcat.dll 不复制，保留 4.1 重编译版）
  - 后续带 bundle 的 mod 部署后：用 `sync-bundles.py` 模式从原包补资源
- **3.11 AKI 入口不兼容（2026-08-08）**: 3.11 mod 用 `private static Main()`（AKI 自动调用），
  4.1 BepInEx 不调用——需包 `[BepInPlugin]` 壳（实测 972 MunitionsExpert，已补壳）。
- **HeliCrash 1292 运行时依赖 UnityToolkit 2.0.1（2026-08-08）**: Forge 源码是作者更新的 4.x 版，
  依赖 UnityToolkit 插件（VContainer/UniTask/ZLinq 运行时提供）。3114 原包 HeliCrash 是旧版无此依赖，
  全包无 UnityToolkit dll——**运行时需另找 UnityToolkit 2.0.1**（Nexus/Forge），否则 HeliCrash 启动报
  依赖错误。编译已用 stub 通过，运行时待补。（另：B 桶 49 中 1292 的"坠落的补给直升机"overlay 已部署
  4.1 DLL + 28MB bundle，资源已从原包同步）
- **106 ProfileEditor = 独立 WPF 工具（2026-08-08 侦察）**: net10.0-windows + MahApps.Metro
  桌面应用（编辑存档），**不是游戏内 mod**，不走 M2 移植/overlay 部署。且为 SPT 3.x 存档
  编写，4.1 存档结构（trader/quest 数据）可能不兼容——标记"独立工具，M2 之外单独评估"。

## Batch strategy

Not all 165 at once. Layered batches (per curating-spt-modpack):
Batch 1 = foundation/libs -> Batch 2 = overhauls -> Batch 3 = content ->
Batch 4 = fine-tuning. Each batch runs the M1-M6 mini-loop; rollback point
between batches.

## Open questions

- ~~Forge live API shape for version listing~~ -- **RESOLVED**: page fetch works;
  archive `api/` layout mirrors live endpoints (`api/v0/mod/<id>/versions`)
- ~~Download mechanics for bucket A~~ -- **RESOLVED 2026-08-07**: direct link
  `forge.sp-tarkov.com/mod/download/<id>/<slug>/<version>` works via curl with
  browser UA, no auth (verified during 4.1.2 KB refresh; 95 zips ~398MB already
  archived at `archive/forge/` fetch-releases.ps1)
- ~~Batch 0 compatibility pre-test~~ -- **CANCELLED 2026-08-07**: 4.0 mods
  definitively incompatible with 4.1.x, B bucket = full recompile-fix
- Porting baseline: compile refs from installed SPT_410 (decided in #5),
  API-break reference = SPT 4.1 server source fork + wiki migration docs
