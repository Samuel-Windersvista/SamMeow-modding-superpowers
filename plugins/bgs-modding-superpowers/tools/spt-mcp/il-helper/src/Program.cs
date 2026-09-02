using System.Text.Json;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace SptIlReader;

/// <summary>
/// SPT 客户端 DLL IL 读取器：提取 Harmony patch 目标 + 插件元数据。
/// 输出 JSON 到 stdout，供 spt-mcp 子进程调用。
///
/// 检测目标（wayfinder #1 分类学 2.2 节）：
/// - 插件入口：BaseUnityPlugin 子类 + BepInPlugin(GUID, Name, Version) + BepInDependency
/// - Harmony patch：ModulePatch 子类 -> GetTargetMethod() IL 里的 typeof(X)/GetMethod("name")/AccessTools.Method(X, "name")/GetConstructor
/// - patch 类型：PatchPrefix / PatchPostfix / PatchTranspiler / PatchFinalizer 特性
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("usage: spt-il-reader <dllPath> [<dllPath>...]");
            return 1;
        }

        var results = new List<object>();
        foreach (var dll in args)
        {
            results.Add(ReadDll(dll));
        }
        Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = false }));
        return 0;
    }

    private static object ReadDll(string dllPath)
    {
        try
        {
            var resolver = new DefaultAssemblyResolver();
            // 依赖解析路径：DLL 上级目录（BepInEx/core 在游戏根）+ 显式 SPT 安装路径（环境变量）
            var dllDir = Path.GetDirectoryName(dllPath)!;
            resolver.AddSearchDirectory(dllDir);
            var bepCore = FindUpwards(dllDir, "BepInEx/core");
            var managed = FindUpwards(dllDir, "EscapeFromTarkov_Data/Managed");
            if (bepCore != null) resolver.AddSearchDirectory(bepCore);
            if (managed != null) resolver.AddSearchDirectory(managed);
            // 回退：环境变量指定的 SPT 根（含 BepInEx/core + Managed）
            var sptRoot = Environment.GetEnvironmentVariable("SPT_ROOT");
            if (!string.IsNullOrEmpty(sptRoot))
            {
                var sBep = Path.Combine(sptRoot, "BepInEx/core");
                var sMan = Path.Combine(sptRoot, "EscapeFromTarkov_Data/Managed");
                if (Directory.Exists(sBep)) resolver.AddSearchDirectory(sBep);
                if (Directory.Exists(sMan)) resolver.AddSearchDirectory(sMan);
            }

            var asm = AssemblyDefinition.ReadAssembly(dllPath, new ReaderParameters { AssemblyResolver = resolver });
            var module = asm.MainModule;

            // 1. 插件入口（BaseUnityPlugin 子类）
            var plugin = FindPlugin(module);

            // 2. Harmony patch 类（ModulePatch 子类）
            var patches = FindPatches(module);

            // 3. patch 方法类型（PatchPrefix/Postfix/Transpiler）
            foreach (var p in patches)
            {
                if (p.TypeDef is not null)
                {
                    p.PatchTypes = FindPatchTypes(p.TypeDef);
                }
            }

            return new
            {
                path = dllPath,
                ok = true,
                plugin = plugin is null ? null : new
                {
                    type = plugin.FullName,
                    guid = plugin.Guid,
                    name = plugin.Name,
                    version = plugin.Version,
                    dependencies = plugin.Dependencies,
                },
                patchCount = patches.Count,
                patches = patches.Select(p => new
                {
                    patchClass = p.FullName,
                    targetType = p.TargetType,
                    targetMethod = p.TargetMethod,
                    patchTypes = p.PatchTypes,
                    behavior = p.Behavior is null ? null : new
                    {
                        returnsFalse = p.Behavior.ReturnsFalse,
                        callsOriginal = p.Behavior.CallsOriginal,
                        writesField = p.Behavior.WritesField,
                        callsOtherPatch = p.Behavior.CallsOtherPatch,
                        isReadOnly = p.Behavior.IsReadOnly,
                        writesResult = p.Behavior.WritesResult,
                        writesRefParam = p.Behavior.WritesRefParam,
                    },
                }).ToArray(),
            };
        }
        catch (Exception ex)
        {
            return new { path = dllPath, ok = false, error = ex.Message };
        }
    }

    private static string? FindUpwards(string startDir, string targetRel)
    {
        var cur = new DirectoryInfo(startDir);
        for (int i = 0; i < 12 && cur != null; i++, cur = cur.Parent)
        {
            var candidate = Path.Combine(cur.FullName, targetRel);
            if (Directory.Exists(candidate)) return candidate;
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // 插件入口
    // -------------------------------------------------------------------------

    private sealed class PluginInfo
    {
        public string FullName = "";
        public string? Guid;
        public string? Name;
        public string? Version;
        public string[] Dependencies = [];
    }

    private static PluginInfo? FindPlugin(ModuleDefinition module)
    {
        var pluginType = module.Types.FirstOrDefault(t =>
            t.BaseType?.FullName == "BepInEx.BaseUnityPlugin");
        if (pluginType is null) return null;

        var info = new PluginInfo { FullName = pluginType.FullName };

        // BepInPlugin attribute
        var attr = pluginType.CustomAttributes.FirstOrDefault(a =>
            a.AttributeType.FullName == "BepInEx.BepInPlugin" || a.AttributeType.FullName == "BepInEx.BepInPluginAttribute");
        if (attr != null && attr.ConstructorArguments.Count >= 3)
        {
            info.Guid = attr.ConstructorArguments[0].Value?.ToString();
            info.Name = attr.ConstructorArguments[1].Value?.ToString();
            info.Version = attr.ConstructorArguments[2].Value?.ToString();
        }

        // BepInDependency attributes (multiple)
        var deps = pluginType.CustomAttributes
            .Where(a => a.AttributeType.FullName == "BepInEx.BepInDependency" || a.AttributeType.FullName == "BepInEx.BepInDependencyAttribute")
            .Select(a => a.ConstructorArguments.Count > 0 ? a.ConstructorArguments[0].Value?.ToString() : null)
            .Where(s => s is not null)
            .Cast<string>()
            .ToArray();
        info.Dependencies = deps;

        return info;
    }

    // -------------------------------------------------------------------------
    // Harmony patch 提取
    // -------------------------------------------------------------------------

    private sealed class PatchInfo
    {
        public string FullName = "";
        public TypeDefinition? TypeDef;
        public string? TargetType;
        public string? TargetMethod;
        public string[] PatchTypes = [];
        public PatchMethodBehavior? Behavior;
    }

    /// <summary>patch 方法的 IL 行为特征</summary>
    private sealed class PatchMethodBehavior
    {
        /// <summary>是否有 ret false（截断原方法）</summary>
        public bool ReturnsFalse;
        /// <summary>是否调用原方法（base. 或 __original 调用）</summary>
        public bool CallsOriginal;
        /// <summary>是否修改字段（stfld）</summary>
        public bool WritesField;
        /// <summary>是否调用别的 Harmony patch 方法（链式调用）</summary>
        public bool CallsOtherPatch;
        /// <summary>是否有日志/计数行为（只读/只写日志）</summary>
        public bool IsReadOnly;
        /// <summary>是否对 __result 赋值（改返回值）</summary>
        public bool WritesResult;
        /// <summary>是否对引用参数赋值（改参数，可能影响后续）</summary>
        public bool WritesRefParam;
    }

    private static List<PatchInfo> FindPatches(ModuleDefinition module)
    {
        var outList = new List<PatchInfo>();
        foreach (var t in module.Types)
        {
            // ModulePatch 子类（SPT.Reflection.Patching.ModulePatch）
            if (!IsModulePatch(t)) continue;

            var info = new PatchInfo { FullName = t.FullName, TypeDef = t };

            // 提取 GetTargetMethod() IL -> 目标类型 + 目标方法名
            var gtm = t.Methods.FirstOrDefault(m => m.Name == "GetTargetMethod");
            if (gtm?.Body != null)
            {
                (info.TargetType, info.TargetMethod) = ExtractTargetFromIL(gtm.Body.Instructions);
            }

            // 提取 patch 方法的 IL 行为特征
            info.Behavior = AnalyzePatchBehavior(t);

            outList.Add(info);
        }
        return outList;
    }

    private static bool IsModulePatch(TypeDefinition t)
    {
        var cur = t;
        while (cur != null)
        {
            if (cur.BaseType?.FullName == "SPT.Reflection.Patching.ModulePatch") return true;
            cur = cur.BaseType?.Resolve();
        }
        return false;
    }

    /// <summary>
    /// 从 GetTargetMethod() IL 提取目标。
    /// 模式 1: typeof(X) / AccessTools.Method(typeof(X), "name")
    /// 模式 2: typeof(X).GetMethod("name", ...)
    /// 模式 3: typeof(X).GetConstructor(...)
    /// </summary>
    private static (string? type, string? method) ExtractTargetFromIL(Mono.Collections.Generic.Collection<Instruction> instructions)
    {
        string? type = null;
        string? method = null;

        for (int i = 0; i < instructions.Count; i++)
        {
            var instr = instructions[i];

            // callvirt/call to GetMethod / AccessTools.Method / AccessTools.Constructor / AccessTools.PropertyGetter / AccessTools.Field
            if (instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt)
            {
                var mr = instr.Operand as MethodReference;
                if (mr != null)
                {
                    var declType = mr.DeclaringType?.FullName;
                    var name = mr.Name;

                    // AccessTools.Method(Type, string) -> 真实目标方法（取调用前的 ldstr 作为方法名）
                    if (declType == "HarmonyLib.AccessTools" && (name == "Method" || name == "Constructor" || name == "PropertyGetter"))
                    {
                        type = BacktrackTypeArg(instructions, i);
                        if (name == "Constructor") method = ".ctor";
                        else method = BacktrackLdstrArg(instructions, i);
                    }

                    // Type.GetMethod("name", ...) / Type.GetConstructor(...) -> 真实目标方法
                    if (name == "GetMethod" || name == "GetConstructor")
                    {
                        method = BacktrackLdstrArg(instructions, i);
                        if (name == "GetConstructor") method = ".ctor";
                    }
                }
            }

            // ldtoken (typeof pattern) -> typeof(X)
            if (instr.OpCode == OpCodes.Ldtoken)
            {
                var tok = instr.Operand as TypeReference;
                if (tok != null && type is null)
                {
                    type = tok.FullName;
                }
            }
        }

        return (type, method);
    }

    /// <summary>在 AccessTools.Method/GetMethod 调用前回溯找 Type 参数（通常是 ldarg0 或 ldtoken）</summary>
    private static string? BacktrackTypeArg(Mono.Collections.Generic.Collection<Instruction> instructions, int callIndex)
    {
        for (int i = callIndex - 1; i >= Math.Max(0, callIndex - 10); i--)
        {
            var instr = instructions[i];
            if (instr.OpCode == OpCodes.Ldtoken)
            {
                var tok = instr.Operand as TypeReference;
                if (tok != null) return tok.FullName;
            }
        }
        return null;
    }

    /// <summary>在 AccessTools.Method/GetMethod 调用前回溯找方法名字符串参数（ldstr）</summary>
    private static string? BacktrackLdstrArg(Mono.Collections.Generic.Collection<Instruction> instructions, int callIndex)
    {
        for (int i = callIndex - 1; i >= Math.Max(0, callIndex - 10); i--)
        {
            var instr = instructions[i];
            if (instr.OpCode == OpCodes.Ldstr)
            {
                return instr.Operand?.ToString();
            }
        }
        return null;
    }

    /// <summary>
    /// 分析 patch 类中所有 patch 方法（Prefix/Postfix/Transpiler 标记的方法）的 IL 行为特征。
    /// 关键问题：这个 patch 是"只读观察"还是"改行为"？
    /// </summary>
    private static PatchMethodBehavior? AnalyzePatchBehavior(TypeDefinition t)
    {
        var patchMethods = t.Methods.Where(m =>
            m.CustomAttributes.Any(a => a.AttributeType.Name.StartsWith("Patch") && a.AttributeType.Name.EndsWith("Attribute")));

        var behaviors = patchMethods.Select(AnalyzeMethodBehavior).ToList();
        if (behaviors.Count == 0) return null;

        // 合并所有 patch 方法的行为特征
        return new PatchMethodBehavior
        {
            ReturnsFalse = behaviors.Any(b => b.ReturnsFalse),
            CallsOriginal = behaviors.Any(b => b.CallsOriginal),
            WritesField = behaviors.Any(b => b.WritesField),
            CallsOtherPatch = behaviors.Any(b => b.CallsOtherPatch),
            IsReadOnly = behaviors.All(b => b.IsReadOnly),
            WritesResult = behaviors.Any(b => b.WritesResult),
            WritesRefParam = behaviors.Any(b => b.WritesRefParam),
        };
    }

    /// <summary>分析单个方法的 IL 行为特征</summary>
    private static PatchMethodBehavior AnalyzeMethodBehavior(MethodDefinition m)
    {
        var b = new PatchMethodBehavior();
        if (m.Body == null) return b;

        // returnsFalse 只在 Prefix patch 方法上有效（Prefix 返回 false = 截断原方法）；
        // Postfix 方法里的提前 return（void ret）不是截断，不统计。
        var isPrefix = m.CustomAttributes.Any(a =>
            a.AttributeType.Name == "PatchPrefixAttribute" || a.AttributeType.Name == "PatchPrefix");

        var instrs = m.Body.Instructions;
        for (int i = 0; i < instrs.Count; i++)
        {
            var instr = instrs[i];

            // ret false（截断原方法）——仅 Prefix patch 方法统计
            if (isPrefix && instr.OpCode == OpCodes.Ldc_I4_0 && i + 1 < instrs.Count && instrs[i + 1].OpCode == OpCodes.Ret)
            {
                b.ReturnsFalse = true;
            }

            // 调用原方法（__original / base. / original）
            if (instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt)
            {
                var mr = instr.Operand as MethodReference;
                if (mr != null)
                {
                    var name = mr.Name;
                    if (name == "__original" || name.Contains("Original") || name == "Base" || name.StartsWith("get_") && mr.DeclaringType?.FullName == m.DeclaringType?.BaseType?.FullName)
                    {
                        b.CallsOriginal = true;
                    }
                }
            }

            // stfld（修改字段）
            if (instr.OpCode == OpCodes.Stfld)
            {
                b.WritesField = true;
            }

            // 调用别的 Harmony patch（PatchAll/Patch/PatchCategory/UnpatchSelf 等）
            if (instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt)
            {
                var mr = instr.Operand as MethodReference;
                if (mr?.DeclaringType?.FullName == "HarmonyLib.Harmony")
                {
                    b.CallsOtherPatch = true;
                }
            }

            // 对 __result 赋值（改返回值）
            if ((instr.OpCode == OpCodes.Starg || instr.OpCode == OpCodes.Starg_S) && instr.Operand is ParameterDefinition pd && pd.Name == "__result")
            {
                b.WritesResult = true;
            }

            // 对引用参数赋值（改参数）
            if ((instr.OpCode == OpCodes.Starg || instr.OpCode == OpCodes.Starg_S) && instr.Operand is ParameterDefinition pdr && pdr.ParameterType.IsByReference && pdr.Name != "__result")
            {
                b.WritesRefParam = true;
            }

            // 只读/日志行为：只读字段、只调用日志/字符串方法、无 stfld/starg/call 到业务逻辑
            // 简化判断：没有 stfld 且没有 starg 到业务参数
            b.IsReadOnly = !b.WritesField && !b.WritesRefParam && !b.WritesResult;
        }

        return b;
    }

    /// <summary>提取类中 PatchPrefix/Postfix/Transpiler/Finalizer 特性</summary>
    private static string[] FindPatchTypes(TypeDefinition t)
    {
        var types = new List<string>();
        foreach (var m in t.Methods)
        {
            foreach (var attr in m.CustomAttributes)
            {
                var name = attr.AttributeType.Name;
                if (name.StartsWith("Patch") && name.EndsWith("Attribute"))
                {
                    types.Add(name.Replace("Patch", "").Replace("Attribute", ""));
                }
            }
        }
        return types.Distinct().ToArray();
    }
}
