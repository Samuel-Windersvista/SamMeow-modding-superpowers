# EFT 1.1.5 Ghidra 反编译报告（原生逆向 · 第三套工具链）

> 日期：2026-09-13 | 目标：验证 Ghidra 能否把 EFT 1.1.5 的 IL2CPP 原生代码反编译成可读逻辑
> 对象：`E:\Game\EFT_Offline\SPT_5xx\GameAssembly.dll`（118 MB，Unity 2022.3.43f2 / IL2CPP metadata 31.1）
> 前置：`docs/eft-1.1.5-il2cpp-逆向可行性报告.md`（Il2CppDumper + Cpp2IL 已验证）

---

## 0. 一句话结论

**Ghidra 反编译成功——产出带真实函数名的 C 风格伪代码。** 代价是**时间**（分析 118 MB 二进制约 4 小时 20 分，反编译目标函数约 33 分）与**类型信息缺失**（参数是 `longlong`/`undefined8`，字段访问是裸偏移 `param_1 + 0x358`）。至此三套工具链全部验证通过，能力互补。

---

## 1. 环境搭建

| 组件 | 版本 | 位置 |
|------|------|------|
| JDK | Temurin 21.0.12.1 | `D:\Temp\opencode\ghidra-re\jdk\jdk-21.0.12.1+1` |
| Ghidra | 12.1.3 PUBLIC | `D:\Temp\opencode\ghidra-re\ghidra_12.1.3_PUBLIC` |
| Jython 扩展 | 12.1.3 自带 zip | `Ghidra\Extensions\Jython`（需手动移动到位，见坑 1） |

```powershell
$env:JAVA_HOME = "D:\Temp\opencode\ghidra-re\jdk\jdk-21.0.12.1+1"
$env:GHIDRA_HEADLESS_MAXMEM = "8G"      # 默认仅 2G，118MB 二进制不够
```

## 2. 分析过程

### Step A：导入 + 自动分析 + 应用符号
```powershell
& "$gh\support\analyzeHeadless.bat" "<proj>" SPT5 `
  -import "E:\Game\EFT_Offline\SPT_5xx\GameAssembly.dll" `
  -scriptPath "D:\Temp\opencode\ghidra-re" `
  -postScript ghidra_headless.py "D:\Temp\opencode\il2cpp-re\dump\script.json"
```
- **耗时约 4 小时 20 分**（远超前次预估的 30–120 分钟）；首次 2 小时超时被截断，改后台分离进程后完成。
- 结果：`Import succeeded`，程序库 `SPT5.rep` 落盘 **2.0 GB**。

### Step B：反编译目标函数
```powershell
& "$gh\support\analyzeHeadless.bat" "<proj>" SPT5 -process "GameAssembly.dll" `
  -scriptPath "D:\Temp\opencode\ghidra-re" `
  -postScript decompile_targets.py "D:\Temp\opencode\ghidra-re\decompiled.txt" "BotOwner" "KeepZoneOnSpawn" "RaidId"
```
- 耗时 **33 分 16 秒**；共 **224,462 个函数**。
- 命中：`BotOwner` 102 / `KeepZoneOnSpawn` 3 / `RaidId` 5。

## 3. 反编译结果（样例）

```c
// EFT.BotOwner$$get_KeepZoneOnSpawn @ 18080e1d0
void EFT_BotOwner__get_KeepZoneOnSpawn(longlong param_1)
{
  if (DAT_1870dd813 == '\0') {
    FUN_180525880(&DAT_186e1b730);
    LOCK(); UNLOCK();
    DAT_1870dd813 = '\x01';
  }
  if (*(longlong *)(param_1 + 0x3e0) != 0) {
    FUN_1800052d0(0xf, DAT_186e1b730);
    return;
  }
                    /* WARNING: Subroutine does not return */
  FUN_1805d2bf0();
}
```

```c
// EFT.BotOwner$$set_SearchData @ 1806f8660（IL2CPP 引用写屏障）
void EFT_BotOwner__set_SearchData(longlong param_1, undefined8 param_2)
{
  bVar5 = DAT_1870db640 != 0;
  *(undefined8 *)(param_1 + 0x358) = param_2;
  if (bVar5) {
    uVar4 = (uint)(param_1 + 0x358U >> 0xc);
    puVar1 = (ulonglong *)(&DAT_187144e40 + (ulonglong)((uVar4 & 0x1fffff) >> 6) * 8);
    do { uVar3 = *puVar1; LOCK(); uVar2 = *puVar1;
         if (uVar3 == uVar2) { *puVar1 = uVar3 | 1L << (uVar4 & 0x3f); }
         UNLOCK(); } while (uVar3 != uVar2);
  }
}
```

**可读性评价**：
- ✅ **真实函数名已应用**（`EFT.BotOwner$$get_KeepZoneOnSpawn`）——符号脚本生效
- ✅ 控制流、局部变量、常量、调用目标可读
- ✅ 能识别 Unity/IL2CPP 惯用法（如引用赋值写屏障 `DAT_1870db640` 检查 + 位图 `LOCK` 循环）
- ❌ 参数/返回类型未恢复（`longlong`/`undefined8`）
- ❌ 字段访问是裸偏移（`param_1 + 0x358`），需配合 `dump.cs` 的字段偏移表人工对应
- ❌ 跨函数符号未命名（`FUN_180525880`），因符号脚本中途异常（见坑 3）

## 4. 三个坑与修复

| # | 坑 | 现象 | 修复 |
|---|----|------|------|
| 1 | **Jython 扩展路径** | 解压到 `Extensions\Ghidra\`，Ghidra 不加载 | 移到 `Ghidra\Extensions\Jython` |
| 2 | **PyGhidra 抢占 `.py`** | `ERROR: Ghidra was not started with PyGhidra. Python is not available` | 禁用 `Ghidra\Features\PyGhidra`（改名 `.disabled`），`.py` 回落到 Jython |
| 3 | **符号脚本泛型名非法** | `InvalidInputException: Symbol name contains invalid characters: System.Array.InternalEnumerator<...>$$.ctor` | 脚本只做了空格→连字符替换；需对 `<`/`>` 等非法字符做清洗。**因中止点在 ScriptMethod 循环中途，目标函数符号已写入，反编译仍成功** |

另：Il2CppDumper 原版 `ghidra.py` 用交互式 `askFile()`，headless 下必失败 → 已改为读 `getScriptArgs()`（`ghidra_headless.py`）。

## 5. 三套工具链能力矩阵（全部实测）

| 工具 | 产出 | 可读性 | 耗时（118MB） | 适用 |
|------|------|--------|----------------|------|
| **Il2CppDumper** | 类型/方法/字段/偏移/**RVA**、DummyDll | 签名级（无逻辑） | **分钟级** | 定位、取地址、拿真实签名 |
| **Cpp2IL `isil`** | 原生反汇编 + **ISIL**（结构化） | 中（指令级） | **分钟级** | 快速通读单方法逻辑 |
| **Ghidra** | **C 风格伪代码** + 真实符号名 | 高（缺类型） | **小时级** | 深度理解复杂算法 |

**推荐工作流**：Il2CppDumper 定位 → Cpp2IL ISIL 快速通读 → Ghidra 精读难点。

## 6. 结论

| 问题 | 答案 |
|------|------|
| 能看到游戏逻辑吗？ | **能**，且有三条路径 |
| 能拿到干净 C# 吗？ | 不能直接；Ghidra 给 C 伪代码（缺类型），Cpp2IL 给 ISIL |
| 能改逻辑吗？ | **能**——Harmony 按签名挂钩，多数场景无需方法体 |
| 成本？ | Ghidra 全量分析约 4 小时（一次性，可复用项目库）；单次反编译约 33 分 |

**信心：8.5/10**（较前次 8/10 提升：Ghidra 路径已实证打通；扣分项为类型信息缺失与分析耗时）。

## 7. 制品与复现

| 制品 | 位置 | 说明 |
|------|------|------|
| Ghidra 程序库 | `D:\Temp\opencode\ghidra-re\proj\SPT5.rep` | 2.0 GB，**可复用**（后续反编译无需重分析） |
| 反编译输出 | `D:\Temp\opencode\ghidra-re\decompiled.txt` | 584 行，3 个目标 |
| 日志 | `D:\Temp\opencode\ghidra-re\stepA2.*.log`、`stepB.log` | — |
| 工具 | `D:\Temp\opencode\ghidra-re\`（JDK/Ghidra/脚本） | 约 1.5 GB，临时产物 |

> 后续若需完整符号（字符串/元数据/未定义函数），须先修 `ghidra_headless.py` 的泛型名清洗，再对**已保存的项目库**重跑符号脚本（无需重新分析）。

## 8. 与既有资料的关系

- 元数据/ISIL 实验：`docs/eft-1.1.5-il2cpp-逆向可行性报告.md`
- 类名清单与 4.1→1.1.5 改名对照：`docs/eft-1.1.5-类名映射重建报告.md`
- 「改逻辑不需要读逻辑」：见类名映射报告第 7.1 节

---

## 9. 闭环验证 mod（供实测）

**目标**：证明「反编译读懂逻辑 → 写补丁 → 游戏内可观察」闭环成立。

**位置**：`mods/SPT5-NoStaminaDrain/`（BepInEx 6 IL2CPP / net6.0，**已构建通过** 0 错误 0 警告，产物 6 KB）

**选它的理由**：ISIL 反汇编显示 `Stamina.Consume(Physical.Consumption, bool)` 是体力扣减唯一入口
（`amount = consumption × ConsumptionMultiplier(0x3C) × Multiplier(0x40)`，`Current(0x10) -= amount`）。
→ 让 `Consume` 返回 0 并跳过原方法 = 体力无限。**补丁逻辑直接来自反编译结论。**

**安装**（本项目硬规则不允许直接写入游戏安装目录，需手动执行一次）：
```powershell
Copy-Item "E:\云文件\GitHub\SamMeow-modding-superpowers\mods\SPT5-NoStaminaDrain\bin\Release\SPT5NoStaminaDrain.dll" "E:\Game\EFT_Offline\SPT_5xx\BepInEx\plugins\" -Force
```

**验证**：进战局 → 全力冲刺 10 秒 → 体力条**不下降**；`BepInEx\LogOutput.log` 出现
`[SPT5-NoStaminaDrain] Stamina.Consume() 已拦截`。
若体力照常下降，说明方法被 IL2CPP 内联，需换目标（如 `Stamina.Process`）。

详见 `mods/SPT5-NoStaminaDrain/README.md`。
