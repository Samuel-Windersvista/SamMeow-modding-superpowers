# 桥接跨语言契约：单一源工件 + 双轴版本语义

状态：已接受（2026-09-17） | 决策者：Overseer | 范围：tarkov-runtime-bridge（C#）/ tarkov-runtime-mcp（TS）/ 便携包打包

## 背景

桥接两端以两种语言各自实现同一套 wire 语义，实测重复面 16 项：协议版本常量、7 端点 payload 字段与顺序、路由表、404/405/500 错误形状、日志归一化 5 规则、聚合语义（500 组上限 / overflowDropped / 组序）、日志级别排名、`detail`/`since`/`limit` 参数语义、时间戳格式。两侧各自持有字面量（`tools/tarkov-runtime-bridge/src/Plugin.cs` 的 `ProtocolVersion = 1` ↔ `tools/tarkov-runtime-mcp/src/bridge/connection.ts` 的 `EXPECTED_BRIDGE_PROTOCOL_VERSION = 1`），仓库内无任何共享文件；语义漂移只能靠人工比对发现。

候选形态：

- **A 文档约定**：把重复面写进文档，靠 review 与测试各自兜底。
- **B 单一源契约工件**：协议版本 + 日志归一化规则表 + 聚合常量收敛到 `shared/bridge-contract/contract.json`，两端消费同一数据；行为由共享 golden 夹具锁定。

判据：漂移可见性、变更成本（一处改两端生效）、跨语言可表达性（JS/.NET 共通 regex 子集）、零 wire 行为变更。

## 决策

采用 **B**。

### 单一源范围

`shared/bridge-contract/contract.json` 为唯一源，范围严格限定为三类数据：

- `protocolVersion`：wire 协议版本（整数）。
- `logNormalization`：有序规则表（`id` / `pattern` / `replacement`）+ `whitespacePattern` / `trim`；规则顺序即应用顺序。
- `logAggregation`：聚合常量（`maxGroups`）。

C# 经 `EmbeddedResource` 编译期嵌入 DLL（`LogicalName=TarkovRuntimeBridge.bridge-contract.json`），由 `src/BridgeContract.cs` 在静态初始化时解析；TS 经 `src/bridge/contract.ts` 运行时读取并校验（仓库布局与便携树布局下同一相对路径均成立）。两端均 fail-loud：解析失败或文件缺失即响亮报错，不静默回退默认值。

范围之外（路由表、payload 字段与顺序、错误形状、参数语义）仍为双实现，由共享夹具的向量 / golden 锁定，不在本轮 codegen 化。正则限定在 JS/.NET 共通子集内：`\b` / `\w` / `\d` 经 `RegexOptions.ECMAScript` 与 JS 对齐；**`\s` 不在共通子集内**（见下），空白收敛改由契约的显式字符类承担。若某条规则无法通约，则该条降级为「双实现 + 向量锁定」并在 spec 记录（当前预期不发生）。

### 双轴版本语义

组件版本与 wire 协议版本是两个独立轴，不得互相推导：

- **组件版本**（`0.2.0`，C5 统一）：发布物标识，随任意改动按 semver bump。
- **wire 协议版本**（`1`）：握手门禁契约，**仅在 wire 不兼容变更时 `+1`**；新增可选字段、内部重构、日志规则增补均不 bump。

两端握手比对协议版本：不匹配 → `BRIDGE_VERSION_MISMATCH`，`details:{expected, actual}`，MCP 侧拒绝消费。契约工件是协议版本的唯一字面量来源；两端源码不得另留协议版本字面量（消费点从契约派生，导出名保持不变）。`ProtocolVersion` 自引入以来保持 1，无 bump 历史。

### 夹具与 golden 机制

`shared/bridge-contract/fixtures/` 三件套：

- `log-normalization.json`：`{ cases: [ { name, input, expected } ] }`，覆盖 GUID / 24hex / hex / 数字 / 空白折叠 / 幂等 / 边界（含 live-raid 录制真实样本）；尾部对抗向量（空白集合 + ECMAScript 词类对齐）源自 `fixtures/adversarial-normalization.json`，由生成器合并。
- `log-aggregation.json`：`{ cases: [ { name, entries, expected } ] }`，覆盖组序 count↓ → lastTs↓ → key↑、500 上限、overflowDropped。
- `payloads/*.json` + `errors.json`：每端点 canonical 样例（断言字段序）+ 404/405/500 错误形状。

两端测试套件消费**同一夹具文件**（TS `npm test`；C# `dotnet test` 经仓库相对路径解析或 CopyToOutputDirectory）。重构前后对固定语料捕获输出到 `.scratch/c10-bridge-contract/goldens/{pre,post}/` 逐字节对照，证明零 wire 行为变更；bootstrap 第 12 项 `tests/bootstrap/verify-bridge-contract.ps1` 做轻检查（工件与夹具可解析、夹具 cases 非空、消费点与便携清单完好）。

### 跨引擎正则对齐与已知残余

`RegexOptions.ECMAScript` 使 .NET 的 `\b` / `\w` / `\d` 与 JS 一致（ASCII 词类 / 数字类），共享夹具以对抗向量锁定该对齐：

- `é5ac66d9b5acfc4001633997a` → `é<id>`：JS 下 `é` 非词字符，24hex 左边界 `(?<!\w)` 成立。
- `٣٤٥ items` → 不变：JS `\d` 仅 `[0-9]`，阿拉伯-印度数字不替换。
- `ß0x1F600` → `ß<hex>`：`ß` 非词字符，`\b0[xX]…\b` 成立。

**`\s` 不共通**：.NET 的 ECMAScript `\s` 仅匹配 ASCII 空白 `[ \t\n\v\f\r]`；JS 的 `\s` 是 25 码点集合（含 U+00A0 / U+1680 / U+2000–U+200A / U+2028 / U+2029 / U+202F / U+205F / U+3000 / U+FEFF，**不含** U+0085）。因此契约不再有 `collapseWhitespace` 布尔，改为 `whitespacePattern`——显式字符类（类内禁 `\s` / `\d` / `\w`），两端直接编译该 pattern；折叠与 `trim` 使用同一空白集合（C# 弃 `char.IsWhiteSpace`、TS 弃 `String.prototype.trim` 默认集合）。

**已声明行为 delta**（相对 pre-C10，向 JS 对齐；TS 侧不变）：

- C#：U+FEFF 边缘由「不折叠 / 不去除」变为「折叠 / 去除」；U+0085 边缘由「折叠 / 去除」变为「不折叠 / 不去除」。

**已知残余**：.NET 的 ECMAScript `\w` 词类仍含 U+0130（İ），JS `\w` 不含——本轮不修 pattern，记录备查（不影响现有规则形状）。

**校验语义对齐**：整数字段按 double 判定整值，JSON `1.0` / `500.0` 形态双端接受；`replacement` 为空串双端拒绝。

**bump 流程**：`protocolVersion` 变更（仅 wire 不兼容时 +1）时，`payloads/bridge-info.json` 等内嵌协议版本的夹具需重新生成，pre/post 捕获同步重跑，两端套件重新验证。

## 非目标

- **不做 payload 全量 codegen**：字段与顺序保持双实现 + 夹具锁定；跨语言 codegen 的构建复杂度与收益不成比例。
- **不做协议 v2**：本决策是零行为变更的工件化，不改 wire 语义、不改 bump 策略。
- **不改传输与端点**：ADR-0007 的 localhost HTTP / MCP 按需拉取形态、路由表、端口策略（默认 49777）均不变。

## 后果

- 协议版本、日志归一化规则、聚合常量从「两处字面量」变为「一处数据 + 两端派生」，漂移面收窄；改动一处即两端生效。
- 契约工件进便携树（`scripts/build-portable-plugin.ps1` 随 `shared/` 树复制 + 必需清单断言），便携部署与仓库同规则。
- 夹具成为跨语言行为的共享事实源：任一端偏离即测试红。
- 代价：新增构建期 / 运行期加载路径（EmbeddedResource / 文件读取），需 fail-loud 覆盖；规则正则受限于共通子集。
- 未来若引入协议 v2，本 ADR 的 bump 策略与握手门禁语义继续适用。
