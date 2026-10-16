# 12: trueItems 同类新增（按整合包内容补档）

**What to build:** 按纳入口径（食品饮料→provisions、医疗→medicals、贵重杂物→barter、衣物→clothing；`partsnmods` 维持 `Active=false` 现状）枚举整合包（基础库 + 内容 mod）中的新增同类物品，按既有风格补条目。枚举基于 `align-report-2026-10-16.txt` 的合并物品清单（BASE + MOD）与复跑脚本；条目与建议值先成列、可核对，再入表。

**Blocked by:** 11（同族文件先完成清理，避免同文件并发）

**Status:** ready-for-agent

- [x] 候选枚举成列（id / 名称 / 来源 / 建议值），随 Comments 登记
- [x] 按风格补条目（与既有同族条目值域一致；不扩大纳入范围到武器配件等既有口径之外）——provisions +4 已补；barter/clothing 触发范围护栏，登记待裁，未硬塞
- [x] 测试钉值更新；全部测试绿（229/229）

## Comments

### 实现登记（2026-10-16）

**口径（可复跑）**
- 脚本：`D:\Temp\opencode\enum_ta2.py`（复用 `align_enum.py` 的 BASE/MOD 加载与 `_id` 提取逻辑）。
- LINE/UNIVERSE：
  - `BASE_new` = `E:\Game\EFT_Offline\SPT_41x\SPT_Runtime\SPT_Data\database\templates\items.json` → 4673 项（含 `_parent`）。
  - `BASE_old` = `E:\Game\EFT_Offline\SPT_40x\SPT\SPT_Data\database\templates\items.json` → 4582 项（旧包 4.0.x 线基础库，旧包 MO2 根无自带 SPT 安装，故以该 4.0.x 基础库近似旧包基建）。
  - `MOD_new` = `Norvinsk_Forever\mods` 下路径含 `CustomItems` 的 JSON 顶层键（去重 3318，387 文件）。
  - 旧包注入项：`Life_in_Norvinsk_v0.3.2\mods` 无 `CustomItems/` 约定（4.0.x 用 `db/`），故对旧包 mods 做通用扫描（`db` 路径下 JSON 顶层 24-hex 键且值为 dict），得 5537 项，合并 `BASE_old` 得旧包物品全集 6884。
- **「新增」定义** = `(BASE_new ∪ MOD_new) \ (BASE_old ∪ 旧包通用扫描)` = 1902 项，再 ∩ 四表族父类。
- **族父类**：从四表已覆盖 `_id` 反推实际使用的 `_parent` 集合（含向上追一层）：
  - barter(11)：电子器件/其他/珠宝/工具/日用/建材/情报/润滑剂/医疗耗材/电池/货币
  - clothing(1)：头部装备（`5a341c4086f77401f2541505`）
  - medicals(1)：兴奋剂（`5448f3a64bdc2d60728b456a`）
  - provisions(2)：食品 / 饮料
- **同族先例核验依据**：旧包 True Items 源配置 `Life_in_Norvinsk_v0.3.2\mods\[10]物品真实堆叠-True Items Redux\...\config\*.json`（即本表数据源）——clothing 表 22 条全为软质帽（棒球帽/便帽/贝雷帽），**无任何弹道头盔/面罩**；provisions 源 13 条（本表 19 = 旧包 overlay 曾扩展）。

**护栏触发情况**
- barter：候选 **85 > 60** → **触发数量护栏**，停止插入，登记待裁。
- clothing：候选 **56 ≤ 60**，但候选集含**无同族先例的子类**（弹道头盔/面罩 43 条，另含 base 未本地化模板 18 条）→ **触发无先例护栏**，停止插入，登记待裁。
- medicals：候选 0。
- provisions：候选 4 ≤ 60 且食品/饮料族有先例 → **纳入**。
- `partsnmods` 维持 `Active=false`，未触碰；未扩大到武器配件等。

**最终纳入（provisions +4，19 → 23）**

| id | 名称 | 来源 | 值 | 同族依据 |
|---|---|---|---|---|
| `69774bb0a247161ff1068335` | Can of duck pate | mod | 2 | 罐头子类：表内 6 条罐头全为 2；克隆源=Can of beef stew (Small)(表内2) |
| `6a35322b81d315afe1018ef3` | French bakery baguette | mod | 3 | 面包/干粮子类先例 2–4，取食品族中位 3 |
| `6a3532423ec9d7082a05d430` | Can of GigaBeef meat | mod | 2 | 罐头子类=2 |
| `6a3557f841667bc4bb00fea4` | Bottle of YXMC water | mod | 4 | 瓶装饮水：Emergency Water Ration=4；饮料族众数 4 |

**barter 候选（85，待裁，未插入）**

| id | 名称 | 来源 | 建议值 | 同族依据 |
|---|---|---|---|---|
| `67c0345b354fca26a0008036` | Flash drive with Mr. Kerman's hash codes | mod | 5 | 克隆源=quest_sas(表内5) |
| `67c04a9bd98287be7b0923d8` | Flash drive with Prapor's hash codes | mod | 5 | 克隆源=quest_sas(表内5) |
| `68c944200bf756126e00d801` | item_container_arena_battlepass_s1_01 | base | 3 | 族=其他中位 |
| `68c954d8ecbf3abc9907ee9b` | item_container_arena_battlepass_s1_02 | base | 3 | 族=其他中位 |
| `68da4fa81ddecb1cc0077aee` | Moreman's dogtag | mod | 4 | 族=珠宝中位 |
| `68da4fe6f0e86677fe06bfe4` | Strelets' amulet | mod | 4 | 族=珠宝中位 |
| `68df9908972cf1e1ec07256a` | Dogtag BEAR | mod | 3 | 族=其他中位 |
| `68df991142e96f583d042b8a` | Dogtag BEAR | mod | 3 | 族=其他中位 |
| `68df991dbeba86572e097ceb` | Dogtag BEAR | mod | 3 | 族=其他中位 |
| `68df9927a38a5e37d80df6c9` | Dogtag BEAR | mod | 3 | 族=其他中位 |
| `68df99975d4e135b130392ca` | Dogtag USEC | mod | 3 | 族=其他中位 |
| `68df99a614ca2428b2017cd8` | Dogtag USEC | mod | 3 | 族=其他中位 |
| `68df99b2f716906506098308` | Dogtag USEC | mod | 3 | 族=其他中位 |
| `68df99c05d4e135b130392cc` | Dogtag USEC | mod | 3 | 族=其他中位 |
| `68e6394e658d876c930977b1` | Unknown device fragment | mod | 3 | 族=电子器件中位 |
| `68f0f60a121d878a2303eedb` | Dogtag BEAR | mod | 3 | 族=其他中位 |
| `68f0f63c645c14a02104142a` | Dogtag BEAR | mod | 3 | 族=其他中位 |
| `68f0f64f183146ea530330aa` | Dogtag USEC | mod | 3 | 族=其他中位 |
| `68f0f662859ebec8d501b76a` | Dogtag USEC | mod | 3 | 族=其他中位 |
| `68f11224197b61ffe703b16f` | Hardened PMC figurine | mod | 4 | 族=珠宝中位 |
| `68f119c6121d878a2303eee3` | Right half of a Physical Bitcoin | mod | 15 | 克隆源=item_barter_valuable_gp(表内15) |
| `68f11adfcd0babab2c0fb003` | Left half of a Physical Bitcoin | mod | 15 | 克隆源=item_barter_valuable_gp(表内15) |
| `68f153aa7da590b6df0515da` | Dogtag BEAR | mod | 3 | 族=其他中位 |
| `68f15cf222c8979ee308f495` | Dogtag BEAR | mod | 3 | 族=其他中位 |
| `68f15dbab2b53abd200b9378` | Dogtag USEC | mod | 3 | 族=其他中位 |
| `68f15e26f1aa7e100a0ca208` | Dogtag BEAR | mod | 3 | 族=其他中位 |
| `68f15e53103c5d9d4f022c78` | Dogtag USEC | mod | 3 | 族=其他中位 |
| `68f213f8ea61d1803707cf7a` | Prapor's letter for the port checkpoint | mod | 5 | 克隆源=quest_sas(表内5) |
| `68f25c64b2b53abd200b954f` | Prapor figurine | mod | 4 | 族=珠宝中位 |
| `68f25ce9b2b53abd200b9551` | Therapist figurine | mod | 4 | 族=珠宝中位 |
| `68f25da3b84c6e1f8a09cffb` | Fence figurine | mod | 4 | 族=珠宝中位 |
| `68f25eba19a2b503d70dc28f` | Skier figurine | mod | 4 | 族=珠宝中位 |
| `68f25eef19a2b503d70dc291` | Peacekeeper figurine | mod | 4 | 族=珠宝中位 |
| `68f25f1201fb9de974058be4` | Mechanic figurine | mod | 4 | 族=珠宝中位 |
| `68f25f3a5d977110860c3350` | Ragman figurine | mod | 4 | 族=珠宝中位 |
| `68f25faa22c8979ee308f4da` | Jaeger figurine | mod | 4 | 族=珠宝中位 |
| `68f25fdf928cd23ddf0471fb` | Lightkeeper figurine | mod | 4 | 族=珠宝中位 |
| `68f26063b2b53abd200b9553` | Ref figurine | mod | 4 | 族=珠宝中位 |
| `68f2608301fb9de974058be6` | BTR figurine | mod | 4 | 族=珠宝中位 |
| `68f260d7b84c6e1f8a09cffd` | Hideout Cat figurine | mod | 4 | 族=珠宝中位 |
| `68f26124b84c6e1f8a09cfff` | Walking Tank figurine | mod | 4 | 族=珠宝中位 |
| `68f26161b84c6e1f8a09d001` | Escape figurine | mod | 4 | 族=珠宝中位 |
| `68f261f6928cd23ddf0471fd` | Mastichin figurine | mod | 4 | 族=珠宝中位 |
| `68fa8e253666e2fd5b00a626` | Armored case | mod | 5 | 克隆源=quest_sas(表内5) |
| `68fb41120760c7891606613c` | Dogtag BEAR | mod | 3 | 族=其他中位 |
| `68fb412b0760c7891606613e` | Dogtag BEAR | mod | 3 | 族=其他中位 |
| `68fb4143a854bc7ae80fad3e` | Dogtag USEC | mod | 3 | 族=其他中位 |
| `68fb4157b280c103230e3b3c` | Dogtag USEC | mod | 3 | 族=其他中位 |
| `6900a54ae85e475e590c96be` | Interchange underground utility plan | mod | 5 | 克隆源=quest_sas(表内5) |
| `690c70a2a1461a01d605a1bd` | Elvisvista figurine | mod | 4 | 族=珠宝中位 |
| `6937ed118715e9fd1b0f286d` | Tangerine | mod | 3 | 克隆源=item_barter_valuable_rolex(表内3) |
| `6937edb912d456a817083e82` | Mazoni golden dumbbell | mod | 3 | 克隆源=item_barter_valuable_rolex(表内3) |
| `694da5f84825e8c609000001` | Crate of OFZ 30x165mm shells | mod | 3 | 克隆源=item_barter_other_30mmround(表内3) |
| `697cddb414e932098005748d` | Duck figurine | mod | 4 | 族=珠宝中位 |
| `699f09de81a6c812900a77b7` | AMG-10 hydraulic fluid | mod | 3 | 克隆源=item_quest_fuelconditioner(表内3) |
| `69bb41c03b5fb75517065960` | Ultralink satellite communication module | mod | 3 | 族=电子器件中位 |
| `69bb4203f94327bc0f0230cd` | IBX Gigachad cryptographic processor | mod | 3 | 族=电子器件中位 |
| `69bb424e99f3fda8f107247d` | Memento Server RAM Module | mod | 3 | 族=电子器件中位 |
| `69bb435ff609db77390b0e25` | Aceso Xpress semi-automatic biochemical analyzer | mod | 5 | 克隆源=item_barter_medical_pile(表内5) |
| `69bb43df99f3fda8f1072483` | Nooby Shield potassium iodide tablets | mod | 5 | 克隆源=item_barter_medical_pile(表内5) |
| `69d39aacf1954acdb3083285` | Echo Belli crate | mod | 4 | 克隆源=quest_reagent(表内4) |
| `69dd0de23dfe95d9e70b5ebb` | TarCoin | mod | 15 | 族=货币中位 |
| `69f9d547b98cc4120608692a` | DesmondPilak CD | mod | 2 | 克隆源=item_barter_other_tea(表内2) |
| `69f9d60b5de6674f08060f2a` | Dunduk floppy disk | mod | 5 | 族=情报中位 |
| `6a04fab776cb0efb8f05969f` | Rogue intel maps | mod | 5 | 族=情报中位 |
| `6a3548e72a963bcdd8098649` | Double dogtag USEC | mod | 3 | 族=其他中位 |
| `6a3549435288a3fe6e085224` | Double dogtag BEAR | mod | 3 | 族=其他中位 |
| `6a354a077e4ccdbe95005266` | Marked dogtag USEC | mod | 3 | 族=其他中位 |
| `6a354a30652075cf460944c6` | Marked dogtag BEAR | mod | 3 | 族=其他中位 |
| `6a354a6240e6797a930312e5` | Marked dogtag USEC | mod | 3 | 族=其他中位 |
| `6a354a6f8d465a55c70065c8` | Marked dogtag USEC | mod | 3 | 族=其他中位 |
| `6a354a7e6632508bd10476d8` | Marked dogtag USEC | mod | 3 | 族=其他中位 |
| `6a354a9a9cd7a91b45027a89` | Marked dogtag BEAR | mod | 3 | 族=其他中位 |
| `6a354aa56632508bd10476dc` | Marked dogtag BEAR | mod | 3 | 族=其他中位 |
| `6a354abe73339990030ca469` | Marked dogtag BEAR | mod | 3 | 族=其他中位 |
| `6a3d38063357ef2b260d1891` | Icebreaker model kit | mod | 4 | 族=珠宝中位 |
| `6a461aed7391ab085a093760` | BD dogtag Ferrum | mod | 3 | 族=其他中位 |
| `6a461bf82b2264dbe10d0ee6` | BD dogtag Green | mod | 3 | 族=其他中位 |
| `6a461c41ec88c6b9a509fb17` | BD dogtag Red | mod | 3 | 族=其他中位 |
| `6a4ce086b5644e9f0a08d08a` | LM KC-130 model aircraft | mod | 4 | 族=珠宝中位 |
| `6a4ff1e23e95f68a650fcb13` | Icebreaker model | mod | 4 | 族=珠宝中位 |
| `6a5f9cfcc409750b6e065592` | Black Division encryption keys | mod | 5 | 族=情报中位 |
| `6a843f6224d911fccb0d45ca` | Panacea crate | mod | 3 | 族=其他中位 |
| `6a843f89e0ad22393c004188` | Morbus crate | mod | 3 | 族=其他中位 |
| `6a9980872a87c16fef04789f` | Everclear two-stage water filter | mod | 3 | 族=其他中位 |

> barter 说明：85 条候选大量为「新内容 mod（WTT-ContentBackport / Black Division 等）注入的狗牌/雕像/箱体」子类，既有表内该类先例稀少；建议在裁决时按子类分批评审，而非整表套用。

**clothing 候选（56，待裁，未插入）**

| id | 名称 | 来源 | 建议值 | 同族依据 |
|---|---|---|---|---|
| `68a6d95addf0111c2f04c9c3` | SHIELD Absolute Cinema hard hat (Orange) | mod | 2 | 克隆源=Baseball cap(表内2) |
| `68a6d96fddf0111c2f04c9c9` | SHIELD Absolute Cinema hard hat (White) | mod | 2 | 克隆源=Baseball cap(表内2) |
| `68a986ca5c0073fa2d0d8cb8` | item_equipment_helmet_vulkan5_eightball_bp2 | base | 2 | 族=头部装备中位（base 未本地化模板） |
| `68a98762609a5cb2120ebd26` | item_equipment_helmet_vulkan5_flame_bp2 | base | 2 | 族=头部装备中位（base 未本地化模板） |
| `68a987a1609a5cb2120ebd2f` | item_equipment_helmet_vulkan5_camo_bp2 | base | 2 | 族=头部装备中位（base 未本地化模板） |
| `68a9a85c3e1ee5a70504c12e` | item_equipment_facecover_devtac_gen2_venom_bp2 | base | 2 | 族=头部装备中位（base 未本地化模板） |
| `68a9a92b838d65bcb3050176` | item_equipment_facecover_devtac_gen2_green_bp2 | base | 2 | 族=头部装备中位（base 未本地化模板） |
| `68a9b3ca0a9c4f9398032c46` | item_equipment_helmet_galvion_caiman_multicam_bp2 | base | 2 | 族=头部装备中位（base 未本地化模板） |
| `68a9b5a5863d2a71fa0494a6` | item_equipment_helmet_galvion_caiman_venom_bp2 | base | 2 | 族=头部装备中位（base 未本地化模板） |
| `68a9be93f260f4e1c2038686` | item_equipment_helmet_neosteel_orange_stains_bp2 | base | 2 | 族=头部装备中位（base 未本地化模板） |
| `68a9beecbba00932ed0bc256` | item_equipment_helmet_neosteel_aces_bp2 | base | 2 | 族=头部装备中位（base 未本地化模板） |
| `68bee28f79c8186398098e6f` | item_equipment_helmet_champion_bp2 | base | 2 | 族=头部装备中位（base 未本地化模板） |
| `68bee2ccd6da72c13f03db95` | item_equipment_helmet_ulach_bp2_sand | base | 2 | 族=头部装备中位（base 未本地化模板） |
| `68bee2d9af253218c00ebbb4` | item_equipment_helmet_ulach_bp2_green_stripes | base | 2 | 族=头部装备中位（base 未本地化模板） |
| `68bee2e0ede5c8489f08e1b5` | item_equipment_helmet_ulach_bp2_green_dot | base | 2 | 族=头部装备中位（base 未本地化模板） |
| `68bee2e876e02b9e340ef113` | item_equipment_helmet_ulach_bp2_coyote_stripes | base | 2 | 族=头部装备中位（base 未本地化模板） |
| `68daab6d1e0a6a3f2f0db279` | item_equipment_head_arena_coyote | base | 2 | 族=头部装备中位（base 未本地化模板） |
| `68deaddfd149b3493e049c13` | item_equipment_head_eft_arena_coyote | base | 2 | 族=头部装备中位（base 未本地化模板） |
| `68f263f099d2172e150d7ff8` | Baseball cap (EFT Patch) | mod | 2 | 克隆源=Baseball cap(表内2) |
| `6937efd50543dcc2560a2c81` | New Year ushanka hat | mod | 2 | 族=头部装备中位 |
| `6937f02dfd6488bb27024839` | Domontovich ushanka hat | mod | 2 | 族=头部装备中位 |
| `693be003582cc8870b090b41` | Galvion Caiman Hybrid helmet (MultiCam Alpine) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `693be8f650fafa102607aed4` | HighCom Striker ULACH IIIA helmet (Winter Mesh Spray) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `697c79b1daf4828686033fcb` | Maska-1SCh bulletproof helmet (Vida Edition) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `69c26722bf4ff19f50057643` | Team Wendy EXFIL Ballistic Helmet (MultiCam) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `69c26fa8add25b3623091e89` | HighCom Striker ACHHC IIIA helmet (Coyote Brown) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `69cbfe1f6dbaa9badb0c6b13` | Crye Precision AirFrame helmet (Mesh Green) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `69cbfe34c293038df7002963` | Crye Precision AirFrame helmet (OD Green) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `69cbfe3a2f657c59da06e262` | Crye Precision AirFrame helmet (Shark) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `69cbfe44897389c1870b2337` | Crye Precision AirFrame helmet (Tan) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `69cd423343c6b278a2076cca` | Crye Precision AirFrame M-LOK helmet (Black) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `69ce47ca49b447e32200cbe9` | Crye Precision AirFrame M-LOK helmet (Bull) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `69ce47f01bb66daf5b0d62fd` | Crye Precision AirFrame helmet (Old School) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `69ce70a321c1fc42cd01affa` | Atlant Armour Titan Aramid helmet (OD Green) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `69ce70df606696c00301a760` | Atlant Armour Titan Aramid helmet (MultiCam) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `69ce70ff49b447e32200cc1c` | Atlant Armour Titan Aramid helmet (Rudiarius) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `69ce8ea6c5fbc777150faf69` | Baseball cap (Zaebtsa Berezka) | mod | 2 | 族=头部装备中位 |
| `69ce8fa8579d5d18300a1fd3` | Baseball cap (Zaebtsa Flecktarn) | mod | 2 | 族=头部装备中位 |
| `69ceab1a8562e71bbe063511` | Baseball cap (Boston) | mod | 2 | 族=头部装备中位 |
| `69ceab3a8562e71bbe063514` | Baseball cap (MultiCam Black) | mod | 2 | 族=头部装备中位 |
| `6a567ae55cbe811cbe0136a8` | Crye Precision AirFrame helmet (Botched) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `6a568340263768e2ae0c9696` | Atlant Armour Titan Aramid helmet (EMR) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `6a568dda1d89fcfb35010389` | MTEK STRIKE Ballistic helmet (Carbon Fiber) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `6a622ebeace4474db90fc85c` | Baseball cap (Zaebtsa Woodland) | mod | 2 | 克隆源=Baseball cap(表内2) |
| `6a622f797a1ca14e57077819` | Baseball cap (Zaebtsa Red Tiger Stripe) | mod | 2 | 克隆源=Baseball cap(表内2) |
| `6a670ae11ab6ac490a0b9a99` | DevTac Ronin Respirator (LABS Security) | mod | 2 | 族=头部装备中位（面罩，无先例） |
| `6a82d9800e3db08b8b0af0eb` | Altyn assault helmet (Impact Tested) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `6a8316cb0ccd0eb28d02f264` | Baseball cap (MultiCam) | mod | 2 | 克隆源=Baseball cap(表内2) |
| `6a8316e7608278aaac018b5b` | Baseball cap (Trauma) | mod | 2 | 克隆源=Baseball cap(表内2) |
| `6a83392e992762b7bf08ffb8` | Diamond Age NovaSteel helmet (Olive Drab) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `6a8339b9512ae66ef40af89b` | Diamond Age NovaSteel helmet (Custos Mortis) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `6a8339d551cfbe231b091a24` | Diamond Age NovaSteel helmet (Toxin) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `6a84620eb9a884488e00094c` | DevTac Ronin ballistic helmet (Raptor) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `6a846223460e01329005b720` | DevTac Ronin ballistic helmet (Prototype) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `6a8462325494d3719f000824` | DevTac Ronin ballistic helmet (Olive) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |
| `6a84624507f6c3032e00c65d` | DevTac Ronin ballistic helmet (Olive Drab) | mod | 2 | 族=头部装备中位（弹道头盔，无先例） |

> clothing 说明：56 条中约 13 条为与既有同类的软质帽（棒球帽/护耳帽，可安全补入），其余 43 条为弹道头盔/面罩（源配置无先例），另 18 条为 base 未本地化模板（`_name` = 内部 id）。建议裁决：帽类可补，头盔/面罩与未本地化模板暂缓。

**medicals 候选**：0 条（新内容中无兴奋剂族新物品），未改动。

**测试数字**
- `TrueItemsResourceTests.Load_ReturnsSixTables_WithExpectedEntryCountsAndFlags`：barter 175（不变）/ clothing 22（不变）/ medicals 21（不变）/ provisions 23（19→23）。
- 新增 `TrueItemsResourceTests.Load_ProvisionsNewAdditions_HaveSameClassValues`：4 条新人抽查锚点（值 2/3/2/4）。
- `dotnet test ... -c Debug`：**229 通过 / 0 失败**（基线 228 + 本 lane 新增 1）。
- `TrueItemsModuleTests` 无按资源计数的断言，未受影响。
- 文件变更：`data/trueitems/provisions.json`（+4 条）、`tests/.../TrueItemsResourceTests.cs`（计数钉值 + 新增锚点 Fact）；barter/clothing/medicals/keycards/partsnmods 及 src 未改动。

---

### 编排者裁定与最终纳入（2026-10-16）

**裁定原则**
- 纳入 = 有直接克隆源证据（最强风格谱系）/ 族内安全子类（雕像模型、情报文件、货币、电子小件、珠宝）；值 = 克隆源值或族中位。
- 暂缓 = 狗牌类（唯一性击杀数据，原表零先例）、容器/箱体类（类型排除，克隆证据亦不覆盖）、弹道头盔/面罩/未本地化模板/硬壳帽（clothing 源表仅软质帽）。

**最终纳入（55 条 = barter 44 + clothing 11）**

barter.json（175 → 219）：

| 组 | id | 名称 | 值 | 依据 |
|---|---|---|---|---|
| 克隆源证据 | `67c0345b354fca26a0008036` | Flash drive with Mr. Kerman's hash codes | 5 | 克隆源 quest_sas（表内5） |
| 克隆源证据 | `67c04a9bd98287be7b0923d8` | Flash drive with Prapor's hash codes | 5 | 克隆源 quest_sas（表内5） |
| 克隆源证据 | `68f213f8ea61d1803707cf7a` | Prapor's letter for the port checkpoint | 5 | 克隆源 quest_sas（表内5） |
| 克隆源证据 | `68fa8e253666e2fd5b00a626` | Armored case | 5 | 克隆源 quest_sas（表内5） |
| 克隆源证据 | `6900a54ae85e475e590c96be` | Interchange underground utility plan | 5 | 克隆源 quest_sas（表内5） |
| 克隆源证据 | `68f119c6121d878a2303eee3` | Right half of a Physical Bitcoin | 15 | 克隆源 GP coin（表内15） |
| 克隆源证据 | `68f11adfcd0babab2c0fb003` | Left half of a Physical Bitcoin | 15 | 克隆源 GP coin（表内15） |
| 克隆源证据 | `6937ed118715e9fd1b0f286d` | Tangerine | 3 | 克隆源 Roler Submariner（表内3） |
| 克隆源证据 | `6937edb912d456a817083e82` | Mazoni golden dumbbell | 3 | 克隆源 Roler Submariner（表内3） |
| 克隆源证据 | `699f09de81a6c812900a77b7` | AMG-10 hydraulic fluid | 3 | 克隆源 Fuel conditioner（表内3） |
| 克隆源证据 | `69bb435ff609db77390b0e25` | Aceso Xpress semi-automatic biochemical analyzer | 5 | 克隆源 Pile of meds（表内5） |
| 克隆源证据 | `69bb43df99f3fda8f1072483` | Nooby Shield potassium iodide tablets | 5 | 克隆源 Pile of meds（表内5） |
| 克隆源证据 | `69f9d547b98cc4120608692a` | DesmondPilak CD | 2 | 克隆源 42 Signature Blend English Tea（表内2） |
| 雕像/模型 | `68f11224197b61ffe703b16f` | Hardened PMC figurine | 4 | 族=珠宝中位 |
| 雕像/模型 | `68f25c64b2b53abd200b954f` | Prapor figurine | 4 | 族=珠宝中位 |
| 雕像/模型 | `68f25ce9b2b53abd200b9551` | Therapist figurine | 4 | 族=珠宝中位 |
| 雕像/模型 | `68f25da3b84c6e1f8a09cffb` | Fence figurine | 4 | 族=珠宝中位 |
| 雕像/模型 | `68f25eba19a2b503d70dc28f` | Skier figurine | 4 | 族=珠宝中位 |
| 雕像/模型 | `68f25eef19a2b503d70dc291` | Peacekeeper figurine | 4 | 族=珠宝中位 |
| 雕像/模型 | `68f25f1201fb9de974058be4` | Mechanic figurine | 4 | 族=珠宝中位 |
| 雕像/模型 | `68f25f3a5d977110860c3350` | Ragman figurine | 4 | 族=珠宝中位 |
| 雕像/模型 | `68f25faa22c8979ee308f4da` | Jaeger figurine | 4 | 族=珠宝中位 |
| 雕像/模型 | `68f25fdf928cd23ddf0471fb` | Lightkeeper figurine | 4 | 族=珠宝中位 |
| 雕像/模型 | `68f26063b2b53abd200b9553` | Ref figurine | 4 | 族=珠宝中位 |
| 雕像/模型 | `68f2608301fb9de974058be6` | BTR figurine | 4 | 族=珠宝中位 |
| 雕像/模型 | `68f260d7b84c6e1f8a09cffd` | Hideout Cat figurine | 4 | 族=珠宝中位 |
| 雕像/模型 | `68f26124b84c6e1f8a09cfff` | Walking Tank figurine | 4 | 族=珠宝中位 |
| 雕像/模型 | `68f26161b84c6e1f8a09d001` | Escape figurine | 4 | 族=珠宝中位 |
| 雕像/模型 | `68f261f6928cd23ddf0471fd` | Mastichin figurine | 4 | 族=珠宝中位 |
| 雕像/模型 | `690c70a2a1461a01d605a1bd` | Elvisvista figurine | 4 | 族=珠宝中位 |
| 雕像/模型 | `697cddb414e932098005748d` | Duck figurine | 4 | 族=珠宝中位 |
| 雕像/模型 | `6a3d38063357ef2b260d1891` | Icebreaker model kit | 4 | 族=珠宝中位 |
| 雕像/模型 | `6a4ce086b5644e9f0a08d08a` | LM KC-130 model aircraft | 4 | 族=珠宝中位 |
| 雕像/模型 | `6a4ff1e23e95f68a650fcb13` | Icebreaker model | 4 | 族=珠宝中位 |
| 情报文件 | `69f9d60b5de6674f08060f2a` | Dunduk floppy disk | 5 | 族=情报中位 |
| 情报文件 | `6a04fab776cb0efb8f05969f` | Rogue intel maps | 5 | 族=情报中位 |
| 情报文件 | `6a5f9cfcc409750b6e065592` | Black Division encryption keys | 5 | 族=情报中位 |
| 货币 | `69dd0de23dfe95d9e70b5ebb` | TarCoin | 15 | 族=货币中位 |
| 电子器件/小件 | `68e6394e658d876c930977b1` | Unknown device fragment | 3 | 族=电子器件中位 |
| 电子器件/小件 | `69bb41c03b5fb75517065960` | Ultralink satellite communication module | 3 | 族=电子器件中位 |
| 电子器件/小件 | `69bb4203f94327bc0f0230cd` | IBX Gigachad cryptographic processor | 3 | 族=电子器件中位 |
| 电子器件/小件 | `69bb424e99f3fda8f107247d` | Memento Server RAM Module | 3 | 族=电子器件中位 |
| 电子器件/小件 | `6a9980872a87c16fef04789f` | Everclear two-stage water filter | 3 | 族=电子器件中位 |
| 珠宝 | `68da4fe6f0e86677fe06bfe4` | Strelets' amulet | 4 | 族=珠宝中位 |

clothing.json（22 → 33）：

| id | 名称 | 值 | 依据 |
|---|---|---|---|
| `68f263f099d2172e150d7ff8` | Baseball cap (EFT Patch) | 2 | 族=头部装备（软帽）族值 2；克隆源 Baseball cap |
| `6937efd50543dcc2560a2c81` | New Year ushanka hat | 2 | 族=头部装备族值 2 |
| `6937f02dfd6488bb27024839` | Domontovich ushanka hat | 2 | 族=头部装备族值 2 |
| `69ce8ea6c5fbc777150faf69` | Baseball cap (Zaebtsa Berezka) | 2 | 族=头部装备族值 2 |
| `69ce8fa8579d5d18300a1fd3` | Baseball cap (Zaebtsa Flecktarn) | 2 | 族=头部装备族值 2 |
| `69ceab1a8562e71bbe063511` | Baseball cap (Boston) | 2 | 族=头部装备族值 2 |
| `69ceab3a8562e71bbe063514` | Baseball cap (MultiCam Black) | 2 | 族=头部装备族值 2 |
| `6a622ebeace4474db90fc85c` | Baseball cap (Zaebtsa Woodland) | 2 | 族=头部装备族值 2 |
| `6a622f797a1ca14e57077819` | Baseball cap (Zaebtsa Red Tiger Stripe) | 2 | 族=头部装备族值 2 |
| `6a8316cb0ccd0eb28d02f264` | Baseball cap (MultiCam) | 2 | 族=头部装备族值 2 |
| `6a8316e7608278aaac018b5b` | Baseball cap (Trauma) | 2 | 族=头部装备族值 2 |

**暂缓清单（86 = barter 41 + clothing 45）**

barter 41（详见上文「barter 候选（85）」表，此处为未纳入项）：
- 狗牌类 35（唯一性击杀数据，原表零先例）：`68da4fa81ddecb1cc0077aee` `68df9908972cf1e1ec07256a` `68df991142e96f583d042b8a` `68df991dbeba86572e097ceb` `68df9927a38a5e37d80df6c9` `68df99975d4e135b130392ca` `68df99a614ca2428b2017cd8` `68df99b2f716906506098308` `68df99c05d4e135b130392cc` `68f0f60a121d878a2303eedb` `68f0f63c645c14a02104142a` `68f0f64f183146ea530330aa` `68f0f662859ebec8d501b76a` `68f153aa7da590b6df0515da` `68f15cf222c8979ee308f495` `68f15dbab2b53abd200b9378` `68f15e26f1aa7e100a0ca208` `68f15e53103c5d9d4f022c78` `68fb41120760c7891606613c` `68fb412b0760c7891606613e` `68fb4143a854bc7ae80fad3e` `68fb4157b280c103230e3b3c` `6a3548e72a963bcdd8098649` `6a3549435288a3fe6e085224` `6a354a077e4ccdbe95005266` `6a354a30652075cf460944c6` `6a354a6240e6797a930312e5` `6a354a6f8d465a55c70065c8` `6a354a7e6632508bd10476d8` `6a354a9a9cd7a91b45027a89` `6a354aa56632508bd10476dc` `6a354abe73339990030ca469` `6a461aed7391ab085a093760` `6a461bf82b2264dbe10d0ee6` `6a461c41ec88c6b9a509fb17`
- 容器/箱体类 6（类型排除，克隆证据不覆盖）：`68c944200bf756126e00d801` `68c954d8ecbf3abc9907ee9b` `69d39aacf1954acdb3083285` `694da5f84825e8c609000001` `6a843f6224d911fccb0d45ca` `6a843f89e0ad22393c004188`

clothing 45（详见上文「clothing 候选（56）」表，此处为未纳入项）：
- 弹道头盔/面罩 43（clothing 源表仅软质帽，无先例；其中含 base 未本地化 helmet/facecover 模板 16）：`68a986ca5c0073fa2d0d8cb8` `68a98762609a5cb2120ebd26` `68a987a1609a5cb2120ebd2f` `68a9a85c3e1ee5a70504c12e` `68a9a92b838d65bcb3050176` `68a9b3ca0a9c4f9398032c46` `68a9b5a5863d2a71fa0494a6` `68a9be93f260f4e1c2038686` `68a9beecbba00932ed0bc256` `68bee28f79c8186398098e6f` `68bee2ccd6da72c13f03db95` `68bee2d9af253218c00ebbb4` `68bee2e0ede5c8489f08e1b5` `68bee2e876e02b9e340ef113` `68daab6d1e0a6a3f2f0db279` `68deaddfd149b3493e049c13` `693be003582cc8870b090b41` `693be8f650fafa102607aed4` `697c79b1daf4828686033fcb` `69c26722bf4ff19f50057643` `69c26fa8add25b3623091e89` `69cbfe1f6dbaa9badb0c6b13` `69cbfe34c293038df7002963` `69cbfe3a2f657c59da06e262` `69cbfe44897389c1870b2337` `69cd423343c6b278a2076cca` `69ce47ca49b447e32200cbe9` `69ce47f01bb66daf5b0d62fd` `69ce70a321c1fc42cd01affa` `69ce70df606696c00301a760` `69ce70ff49b447e32200cc1c` `6a567ae55cbe811cbe0136a8` `6a568340263768e2ae0c9696` `6a568dda1d89fcfb35010389` `6a670ae11ab6ac490a0b9a99` `6a82d9800e3db08b8b0af0eb` `6a83392e992762b7bf08ffb8` `6a8339b9512ae66ef40af89b` `6a8339d551cfbe231b091a24` `6a84620eb9a884488e00094c` `6a846223460e01329005b720` `6a8462325494d3719f000824` `6a84624507f6c3032e00c65d`
- SHIELD Absolute Cinema hard hat 2（硬壳帽，源表仅软质帽）：`68a6d95addf0111c2f04c9c3` `68a6d96fddf0111c2f04c9c9`

**暂缓复访条件**：未来 SPT/整合包版本更新引入对应物品、或用户明确指定需纳入时，按同族风格重新评审（狗牌类需先确立族内值先例；容器/箱体类需确认类型归属；头盔/面罩类需确认是否将 clothing 表扩展出软帽范畴）。

**测试数字（本轮）**
- `TrueItemsResourceTests.Load_ReturnsSixTables_WithExpectedEntryCountsAndFlags`：barter **175 → 219**、clothing **22 → 33**、medicals 21（不变）、provisions 23（不变）、partsnmods 104/10（不变，Active=false）。
- 锚点新增：`Load_BarterSamples_MatchDeltaTable` 增 TarCoin=15 / Prapor figurine=4；新增 `Load_ClothingNewAdditions_HaveSameClassValues`（Baseball cap (Trauma)=2 / New Year ushanka hat=2）。
- `dotnet test ... --artifacts-path .artifacts/its-12b -c Debug`：**230 通过 / 0 失败**（基线 229 + 本 lane 新增 1）。
- 文件变更（本轮）：`data/trueitems/barter.json`（+44）、`data/trueitems/clothing.json`（+11）、`tests/.../TrueItemsResourceTests.cs`（钉值 + 锚点）。未 commit。

