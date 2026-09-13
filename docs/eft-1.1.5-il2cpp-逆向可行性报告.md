# EFT 1.1.5 IL2CPP 原生逆向可行性实验报告

> 日期：2026-09-13 | 目标：验证「能否看到 EFT 1.1.5 的游戏内部逻辑」
> 对象：`E:\Game\EFT_Offline\SPT_5xx`（SPT 5.0.0 / EFT 1.1.5 build 47242，IL2CPP）
> 方法：Il2CppDumper 元数据提取 + Cpp2IL IL 重建，实测而非推测

---

## 0. 一句话结论

**能看到逻辑——但形式是「原生反汇编 + ISIL」，不是干净的 C#。**
元数据（类名/方法名/签名/字段偏移/方法 RVA）**完全开放**；每个方法的 **x64 反汇编与结构化 ISIL 均可读出**（26,440 个 ISIL 文件）。唯一失败的是 Cpp2IL 的**C# IL 发射**步骤（输出全为 `throw null`）。

信心从 6.5/10 更新为 **8/10**（"能看逻辑"已实测成立）。

---

## 1. 实验方法

| 步骤 | 工具 | 版本 | 来源 |
|------|------|------|------|
| 元数据提取 | Il2CppDumper | v6.7.46 | `github.com/Perfare/Il2CppDumper` |
| IL 重建 | Cpp2IL | 2022.1.0-pre-release.21 | `github.com/SamboyCoding/Cpp2IL` |
| 反编译 | ilspycmd | 11.0.0 | 已装（.NET 全局工具） |

> 下载均需代理 `http://127.0.0.1:7890`（直连 GitHub 被重置）。

```powershell
# 1) 元数据提取
Il2CppDumper.exe "<game>\GameAssembly.dll" "<game>\EscapeFromTarkov_Data\il2cpp_data\Metadata\global-metadata.dat" "<out>\dump"
# 2) IL 重建（Cpp2IL）
Cpp2IL.exe --game-path "<game>" --exe-name "EscapeFromTarkov" --output-as dll_il_recovery --output-to "<out>\cpp2il_out"
# 2b) ISIL dump（成功路径）
Cpp2IL.exe --game-path "<game>" --exe-name "EscapeFromTarkov" --output-as isil --output-to "<out>\cpp2il_isil"
```

**前置探测**：`global-metadata.dat` 魔数 = `AF 1B B1 FA`（0xFAB11BAF，IL2CPP 标准魔数），版本 = 31 → **元数据未加密**，这是全部工作的前提。

---

## 2. 结果分级（诚实）

| 层级 | 结果 | 证据 |
|------|------|------|
| **元数据**（类型/方法/字段/偏移/RVA） | ✅ **完全成功** | Metadata v31 / Il2Cpp v31；`CodeRegistration=0x18588EDD0`；映射 **208,898** 个方法指针；**185,344** 个方法 RVA；字段偏移精确（`0x0`/`0x20`/`0x3E0`…） |
| **DummyDll**（真实签名 C# 桩） | ✅ **成功** | `DummyDll\Assembly-CSharp.dll` 21.9 MB，`ilspycmd` 可反编译出真实类名/字段/Token |
| **原生反汇编**（x64） | ✅ **成功** | 26,440 个 ISIL 文件，全部非空 |
| **ISIL**（结构化中间表示，含控制流） | ✅ **成功** | 见下方样例 |
| **C# IL 发射**（Cpp2IL `dll_il_recovery`） | ❌ **失败** | 方法体全为 `throw null;`（IL 提升步骤未产出） |
| Ghidra/IDA 交互分析 | ⚠️ **未做** | 本机未装 Java（Ghidra 前置） |

---

## 3. 关键证据（样例）

**简单方法**——`EFT.BotOwner.get_RaidId()`：

```
Disassembly:  mov eax,[rcx+20h]   /   ret
ISIL:         001 Move rax, [rcx+32]
              002 Return rax
```

**含分支/调用的方法**——`EFT.BotOwner.get_KeepZoneOnSpawn()`：

```
Disassembly:  push rbx / sub rsp,20h / cmp byte ptr [1870DD813h],0 / mov rbx,rcx
              jne short 000000018080E1FAh / lea rcx,[186E1B730h] / call 0000000180525880h
              lock or dword ptr [rsp],0 / mov byte ptr [1870DD813h],1 / ...
ISIL:         001 ShiftStack -8
              002 Move stack:0x0, rbx
              ...
              006 JumpIfNotEqual {11}
              007 LoadAddress rcx, [0x186E1B730]
              008 Call "il2cpp_codegen_initialize_runtime_metadata", rcx, rdx, r8, r9
              ...
              013 JumpIfEqual {21}
              014 Move rdx, typeof(IGetProfileData)
              019 Call 0x1800052D0, rcx, rdx, r8, r9
              020 Return rax
```

→ 变量、常量、分支目标、被调用符号（含 `typeof` 与符号名）**都在 ISIL 里可见**。

**RVA 健全性**：`BotOwner` 某方法 RVA `0x628110` 处字节 = `8B 0D 9A DF 7D 06`（`mov ecx,[rip+…]`）——合法 x64 代码。

---

## 4. 制品与规模

落盘于 `D:\Temp\opencode\il2cpp-re\`（**可随时重跑，非仓库资产**）：

| 目录 | 大小 | 内容 |
|------|------|------|
| `dump/` | 355.6 MB | `dump.cs`(54.8MB，签名+RVA)、`il2cpp.h`(89.8MB)、`script.json`(166MB)、`DummyDll/`(21.9MB) |
| `cpp2il_out/` | 15.7 MB | Cpp2IL 重建 DLL（**方法体为 `throw null`，不可用**） |
| `cpp2il_isil/` | 706.9 MB | **26,440 个 ISIL 文件（可用）** |

---

## 5. 结论与信心更新

| 问题 | 实测答案 |
|------|----------|
| 元数据（名字/签名/偏移）能看到吗？ | **能，完全开放** |
| 方法逻辑能看到吗？ | **能**——以 x64 反汇编 + ISIL 形式 |
| 能拿到干净 C# 吗？ | **本次未成功**（Cpp2IL IL 发射失败）；ISIL 已很接近，复杂方法仍需按汇编阅读 |
| 需要 Ghidra/IDA 吗？ | **建议**——交互式反编译可显著提升复杂方法的可读性（需装 Java 17+） |

**信心：8/10**（"能看逻辑"已证；扣分项是"干净 C#"与"复杂方法可读性"）。

---

## 6. 后续路径（按性价比排序）

1. **立即可用**：`dump.cs`（签名+RVA）+ `DummyDll`（真实类型）+ `ISIL`（逐方法逻辑）三者组合，已足以支撑「精确理解某方法」的需求。
2. **提升可读性**：装 JDK 17+ 与 Ghidra，用 Il2CppDumper 的 `ghidra.py` + `script.json` 载入符号 → 交互式反编译（这是 EFT 社区的主流路线）。
3. **不必要**：为常规 mod 开发（挂钩/加内容/改数值）做原生逆向——签名挂钩即可，无需方法体。
4. **注意**：制品约 1.08 GB，属临时产物，不建议入库；需要时按第 1 节命令重跑（约 5 分钟）。

---

## 7. 与既有资料的关系

- 类名清单与 4.1→1.1.5 改名对照：`docs/eft-1.1.5-类名映射重建报告.md` + `knowledge/spt-kb/archive/eft-1.1.5/`
- SPT 5.0 服务端 mod API：`docs/spt-5.0-mod-api-能力评估报告.md` + `curated/api-notes-5.0/`
- 「改逻辑不需要读逻辑」的说明：见类名映射报告第 7.1 节
