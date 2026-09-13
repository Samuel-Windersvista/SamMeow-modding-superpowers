# SPT5-NoStaminaDrain —— 反编译验证 mod

> 目的：验证「**反编译读懂游戏逻辑 → 写 Harmony 补丁 → 游戏内可观察效果**」闭环成立
> 目标：SPT 5.0 / EFT 1.1.5 build 47242（IL2CPP + BepInEx 6）
> 状态：**已构建通过**（0 错误 0 警告）；**未安装**（见下方安装命令）

---

## 1. 反编译依据（为什么是这个方法）

对 `GameAssembly.dll` 的 ISIL 反汇编显示，`Stamina.Consume(Physical.Consumption, bool)` 是体力扣减的唯一入口：

```asm
comiss xmm8,dword ptr [rcx+40h]      ; 比较 0 与 this.Multiplier(0x40)
movss  xmm6,dword ptr [rcx+3Ch]      ; xmm6 = this.ConsumptionMultiplier(0x3C)
call   rax                           ; 取消耗值 consumption
mulss  xmm2,xmm6                     ; amount *= ConsumptionMultiplier
mulss  xmm2,dword ptr [rbx+40h]      ; amount *= Multiplier
subss  xmm1,xmm2                     ; Current(0x10) -= amount
movss  dword ptr [rbx+10h],xmm1      ; 写回 this.Current
```

字段偏移与 `dump.cs` 一致（`Current`=0x10、`ConsumptionMultiplier`=0x3C、`Multiplier`=0x40）。
→ **推论**：让 `Consume` 直接返回 0 并跳过原方法，`Current` 永不减少 = 体力无限。

## 2. 补丁（两个，缺一不可）

**① `src/Patches/StaminaConsumePatch.cs`** —— 拦截「一次性」扣减（添加消耗项时）：

```csharp
[HarmonyPatch(typeof(Stamina), "Consume", new Type[] { typeof(Physical.Consumption), typeof(bool) })]
internal static class StaminaConsumePatch
{
    [HarmonyPrefix]
    private static bool Prefix(ref float __result) { __result = 0f; return false; }
}
```

**② `src/Patches/StaminaProcessPatch.cs`** —— 拦截**逐帧**扣减（真正的持续消耗）：

```csharp
[HarmonyPatch(typeof(Stamina), "Process", new Type[] { typeof(float) })]
internal static class StaminaProcessPatch
{
    [HarmonyPrefix]  private static void Prefix(Stamina __i, out float __state) => __state = __i.Current;
    [HarmonyPostfix] private static void Postfix(Stamina __i, float __state)
    {
        if (__i.Current < __state) __i.Current = __state;   // 还原本帧扣减，保留回复
    }
}
```

### v1 失败教训（重要）

v1 只打了 `Consume`，**日志显示补丁已触发但体力照降**。定位过程：
1. 在 ISIL 反汇编里搜索所有写 `Current`（偏移 `0x10`）的位置 → 发现 6 处
2. 归属：`.ctor` / **`Consume`(×2)** / `AddConsumption` / **`Process`(×5)** / `UpdateStamina`
3. 读 `Process` 的 ISIL 行 148-161，确认 `Current -= consumption*dt` 在此逐帧执行
4. → **`Consume` 只是入口之一，持续扣减在 `Process`**

这正是「反编译读懂逻辑」的价值：**光看方法名会选错目标，读汇编才能定位真凶。**

## 3. 与 4.1 的关键差异（为什么不能套用旧模板）

| 项 | SPT 4.1 | **SPT 5.0 / EFT 1.1.5** |
|----|---------|--------------------------|
| 运行时 | Mono | **IL2CPP** |
| BepInEx | 5.x | **6.0.0-be** |
| 插件基类 | `BaseUnityPlugin`（`Awake()`） | **`BepInEx.Unity.IL2CPP.BasePlugin`（`Load()`）** |
| 目标框架 | netstandard2.1 | **net6.0** |
| 游戏程序集引用 | `EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll` | **`BepInEx\interop\Assembly-CSharp.dll`**（Il2CppInterop 代理） |

## 4. 构建

```powershell
dotnet build "E:\云文件\GitHub\SamMeow-modding-superpowers\mods\SPT5-NoStaminaDrain\SPT5NoStaminaDrain.csproj" -c Release
```
产物：`mods\SPT5-NoStaminaDrain\bin\Release\SPT5NoStaminaDrain.dll`（约 6 KB）

## 5. 安装（**需你手动执行**，本项目硬规则不允许直接写入游戏安装目录）

```powershell
Copy-Item "E:\云文件\GitHub\SamMeow-modding-superpowers\mods\SPT5-NoStaminaDrain\bin\Release\SPT5NoStaminaDrain.dll" "E:\Game\EFT_Offline\SPT_5xx\BepInEx\plugins\" -Force
```

## 6. 验证步骤

1. 启动游戏（`sptvfsbridge.bat` 或正常启动器流程）
2. 进入任意战局
3. **全力冲刺**（Shift + W）持续 10 秒以上
4. **预期**：体力条**不下降**（正常情况会迅速耗尽并进入喘息）
5. 检查日志 `E:\Game\EFT_Offline\SPT_5xx\BepInEx\LogOutput.log`，应出现：
   ```
   [SPT5-NoStaminaDrain] Stamina.Consume() 已拦截（首次触发，体力扣减被置 0）
   ```

**判定**：
- 体力不降 + 日志出现 → **反编译理解正确，补丁生效** ✅
- 体力照常下降 → 补丁未命中（可能方法被内联），需改用其他目标方法（如 `Stamina.Process`）

## 7. 卸载

```powershell
Remove-Item "E:\Game\EFT_Offline\SPT_5xx\BepInEx\plugins\SPT5NoStaminaDrain.dll" -Force
```

## 8. 参考

- 反编译可行性：`docs/eft-1.1.5-il2cpp-逆向可行性报告.md`
- Ghidra 反编译：`docs/eft-1.1.5-ghidra-反编译报告.md`
- 类名映射：`docs/eft-1.1.5-类名映射重建报告.md`
