# 09: 臂章表对齐（C 补 15 款）

**What to build:** `armbands.json` 由 22 条扩至 **37 条**：基础库 4 款（`prestige_3/4/5`、`discord`）+ WTT-ContentBackport 11 款（Beta / For Humanity / HEAD EYES / LOOT Prayer / Lighthouse / Prestige 6 / Release / SBEU KOMAR / Survivor / To Fall in the Darkness / Forged In Beta）。按既有减重档位（-6/-8/-10/-15/-20/-25）按稀有度/系列归档分配；测试钉值同步（22→37 + 抽查锚点）。id 清单见 `spec-v0.2.0.md` Further Notes / `align-report-2026-10-16.txt` §C。

**Blocked by:** None（可立即开始）

**Status:** ready-for-agent

- [ ] 追加 15 条（权重档位与既有条目风格一致；prestige 系列档位自洽）
- [ ] `AntigravModuleTests` 钉值与抽查更新；全部测试绿
- [ ] 不做超出风格的档位发明；值分配理由随 Comments 登记

## Comments

实现完成（2026-10-16）。`armbands.json` 22→37 条；`AntigravModuleTests` 钉值同步；全量 228 测试绿。

### 15 条档位分配表（id | 名称 | 档位 | 理由）

| id | 名称 | 档位 | 理由 |
|---|---|---|---|
| 688b2e574172ca83e70cf868 | Discord | -10 | 社区推广款；落在 -8~-15 区间按「推广」取中位 -10（与阵营/推广档同阶） |
| 6841b2506c1fcc41ed0db319 | Prestige 3 | -25 | Prestige 线递进（P1=-20、P2=-25）；P3 起为平台值 -25 |
| 6841b3463fcc417de40a6768 | Prestige 4 | -25 | 同上，Prestige 平台 |
| 6841b3ab322db20d190b4b9b | Prestige 5 | -25 | 同上，Prestige 平台 |
| 68d65766916a108d7a023c98 | Armband (Prestige 6) | -25 | WTT Prestige 6 归入 Prestige 平台，与基础库 P2~P5 一致 |
| 68f25be683ec644ebf046787 | Armband (Beta) | -10 | Beta 活动纪念款；活动推广类取 -10 |
| 68fb9480e6ad35a767066feb | Forged In Beta | -10 | Beta 纪念系列，与 Beta 同档 -10 |
| 68f25b772605ccea240dd684 | Armband (Release) | -10 | 发布纪念款；活动推广类取 -10 |
| 68d41fde2937f3f17c04864d | Armband (For Humanity) | -8 | 阵营/主题款；对齐既有 -8 阵营档（RFARMY/UNTAR/Train Hard） |
| 68f8ccbc4c78615ba6079b20 | Armband (LOOT Prayer) | -8 | 趣味/主题款；低强度主题归 -8 |
| 68d41fb0a1c275dbaa0798bf | Armband (Lighthouse) | -8 | 地图/主题款；低强度主题归 -8 |
| 68f8ccb11cc53ada67036e27 | Armband (HEAD, EYES) | -15 | 防护系列；功能/精英类归 -15 |
| 68f8ccf06a6869fe97026c3b | Armband (SBEU KOMAR) | -15 | 防护系列；功能/精英类归 -15 |
| 68d41e65a706eba9a204ed27 | Armband (Survivor) | -15 | 精英成就款；精英/系列归 -15 |
| 68d41f017cb8c03da8079004 | Armband (To Fall in the Darkness) | -15 | 系列纪念款；精英/系列归 -15 |

### 档位分组计数（新）

`-6=5 / -8=6 / -10=9 / -15=11 / -20=1 / -25=5`，合计 37（原 `5/3/5/7/1/1`）。

### 测试钉值

- `Table_HasTwentyTwoArmbands_WithPinnedWeightTiers` → `Table_HasThirtySevenArmbands_WithPinnedWeightTiers`：总数 37；六档计数重算；新增锚点 `Discord=-10`、`Prestige 5=-25`。
- `Apply_AllTwentyTwoArmbands_...` → `Apply_AllThirtySevenArmbands_...`：changing 计数 22→37。
- `Apply_MissingArmbands_WarnAndSkip`：warning 计数 22→37（report + logger 两处）。
- `AntigravModule` XML 注释「把 22 款臂章…」→「按查找表把各臂章…」（计数无关，避免漂移）。

### 验证

`dotnet test mods\InescapableTarkovsSoftcore416\InescapableTarkovsSoftcore.sln --artifacts-path .artifacts/its-09 -c Debug` → 通过 228 / 失败 0 / 跳过 0（git 未提交）。

### 偏差与边界

- 数据文件第 3 行注释「22 款」→「37 款」（非工单 08 添加的前 2 行；保持计数准确）。
- `FeatureTables.cs` 第 19 行 `<summary>G4：22 款反重力臂章…</summary>` **未改**（按边界留待编排者收口）。
- `data/config/armbands.json` 分发模板由 `build.ps1` 从内嵌资源生成，未手工改动（属构建产物）。
