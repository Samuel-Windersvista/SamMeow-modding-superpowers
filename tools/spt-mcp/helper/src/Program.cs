using System.Text.Json;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;

namespace SptMetadataReader;

/// <summary>
/// SPT 4.1 server mod 元数据读取器。
/// 读 user/mods/&lt;mod&gt;/ 下的顶层 DLL，用 AsmResolver 找实现 IModMetadata 的类型，
/// 从其无参构造器的 IL（ldstr 常量）提取 ModGuid / Name / Author / Version / SptVersion 等。
/// 输出 JSON 到 stdout，供 spt-mcp 子进程调用。
/// </summary>
public static class Program
{
    private static bool IsOp(AsmResolver.PE.DotNet.Cil.CilOpCode op, string name) =>
        op.ToString() == name;

    public static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("usage: spt-metadata-reader <dllPath> [<dllPath>...]");
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
            var module = ModuleDefinition.FromFile(dllPath);
            var metaType = module.GetAllTypes().FirstOrDefault(t =>
                t.Interfaces.Any(i => i.Interface?.Name?.ToString() == "IModMetadata"));
            if (metaType is null)
            {
                return new { path = dllPath, ok = false, error = "no IModMetadata implementation found" };
            }

            var ctor = metaType.Methods.FirstOrDefault(m =>
                m.Name == ".ctor" && m.Signature is MethodSignature sig && sig.ParameterTypes.Count == 0);
            if (ctor?.CilMethodBody is null)
            {
                return new { path = dllPath, ok = false, error = "no parameterless ctor found" };
            }

            // 属性名 -> 构造器 IL 里的 ldstr 常量
            var values = new Dictionary<string, string?>();
            var instructions = ctor.CilMethodBody.Instructions;
            for (int i = 0; i < instructions.Count; i++)
            {
                var instr = instructions[i];
                // 模式: ldarg.0 ; ldstr <value> ; stfld <BackingField>
                if (IsOp(instr.OpCode, "ldstr") &&
                    i + 1 < instructions.Count &&
                    IsOp(instructions[i + 1].OpCode, "stfld") &&
                    instructions[i + 1].Operand is IFieldDescriptor field)
                {
                    var fieldName = field.Name?.ToString() ?? "";
                    // <ModGuid>k__BackingField -> ModGuid
                    var prop = fieldName.Replace("<", "").Replace(">k__BackingField", "");
                    if (fieldName.Contains("k__BackingField"))
                    {
                        values[prop] = instr.Operand?.ToString();
                    }
                }
            }

            // Version / SptVersion 是 new SemanticVersioning.Version(ldstr, bool) — 前一条 ldstr 是原始字符串
            var versionMap = new Dictionary<string, string?>();
            for (int i = 0; i < instructions.Count - 2; i++)
            {
                var a = instructions[i];
                var b = instructions[i + 1];
                var c = instructions[i + 2];
                // ldstr "x"; ldc.i4.*; newobj SemanticVersioning.X::.ctor
                if (IsOp(a.OpCode, "ldstr") &&
                    (IsOp(b.OpCode, "ldc.i4.0") || IsOp(b.OpCode, "ldc.i4")) &&
                    IsOp(c.OpCode, "newobj") &&
                    c.Operand is IMethodDescriptor newobj &&
                    (newobj.DeclaringType?.Name?.ToString() is "Version" or "Range"))
                {
                    var fieldAfter = i + 3 < instructions.Count ? instructions[i + 3] : null;
                    if (fieldAfter?.OpCode is not null && IsOp(fieldAfter.OpCode, "stfld") && fieldAfter.Operand is IFieldDescriptor f2)
                    {
                        var prop = f2.Name?.ToString()?.Replace("<", "").Replace(">k__BackingField", "");
                        if (prop is not null) versionMap[prop] = a.Operand?.ToString();
                    }
                }
            }

            var guid = values.GetValueOrDefault("ModGuid");
            var name = values.GetValueOrDefault("Name") ?? Path.GetFileNameWithoutExtension(dllPath);
            var sptVersion = versionMap.GetValueOrDefault("SptVersion");

            return new
            {
                path = dllPath,
                ok = true,
                name,
                guid,
                author = values.GetValueOrDefault("Author"),
                version = versionMap.GetValueOrDefault("Version") ?? values.GetValueOrDefault("Version"),
                sptVersion,
                license = values.GetValueOrDefault("License"),
                modDependencies = ParseDependencies(),
            };
        }
        catch (Exception ex)
        {
            return new { path = dllPath, ok = false, error = ex.Message };
        }
    }

    /// <summary>ModDependencies 深解析留待后续；当前返回空数组（依赖冲突走文件级 + BepInEx 层面）</summary>
    private static string[] ParseDependencies()
    {
        return [];
    }
}
