using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace EftClassMap;

/// <summary>
/// 从两个 .NET 程序集（可跨 Mono / IL2CPP-interop）提取类型及其成员指纹，
/// 对「仅 A 有」「仅 B 有」的类型做成员指纹相似度匹配，用于识别**改名/换命名空间**。
/// </summary>
internal sealed record TypeInfo(string FullName, string Kind, string BaseType, HashSet<string> Members)
{
    public int MemberCount => Members.Count;
}

internal static class Program
{
    private static readonly string[] NoisePrefixes =
        ["<", "__f__AnonymousType", "Il2Cpp", "MethodInfoStoreGeneric_"];

    private static bool IsNoise(string fullName)
    {
        foreach (var p in NoisePrefixes)
        {
            if (fullName.StartsWith(p, StringComparison.Ordinal))
            {
                return true;
            }
        }

        if (fullName.Contains("MethodInfoStoreGeneric_", StringComparison.Ordinal)
            || fullName.Contains("__f__AnonymousType", StringComparison.Ordinal))
        {
            return true;
        }

        // 编译器生成类型的「简单名」判定（跨版本命名方案不同）：
        //   4.1 去混淆方案：CG_ClassNNNN / CG_MethodName / CG_Create`1
        //   1.1.5 interop 方案：__c__DisplayClassN_M / __c
        var simple = fullName;
        var cut = Math.Max(fullName.LastIndexOf('.'), fullName.LastIndexOf('+'));
        if (cut >= 0)
        {
            simple = fullName[(cut + 1)..];
        }

        return simple.StartsWith("CG_", StringComparison.Ordinal)
               || simple.StartsWith("__c", StringComparison.Ordinal)
               || simple.StartsWith("<>", StringComparison.Ordinal);
    }

    // 参与相似度匹配的最小成员数（低于此值区分度不足，会产生虚假匹配）
    private const int MinMatchMembers = 4;

    // Il2CppInterop 在代理程序集里注入的成员，不参与指纹（否则会污染跨引擎比对）
    private static readonly string[] MemberNoisePrefixes =
        ["NativeFieldInfoPtr_", "NativeMethodInfoPtr_", "__il2cppRuntimeField_", "MethodInfoStoreGeneric_"];

    private static bool IsMemberNoise(string memberName)
    {
        foreach (var p in MemberNoisePrefixes)
        {
            if (memberName.StartsWith(p, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    // 归一化：把 Mono 与 Il2CppInterop 的不同成员表示收敛到同一「语义名」。
    // - 自动属性后备字段 `<X>k__BackingField`（Mono）→ `X`
    // - 自动属性后备字段 `_X_k__BackingField`（Il2CppInterop）→ `X`
    // - 访问器方法 `get_X` / `set_X` / `add_X` / `remove_X` → `X`
    private static string NormalizeMember(string n)
    {
        if (n.Length > 16 && n[0] == '<' && n.EndsWith(">k__BackingField", StringComparison.Ordinal))
        {
            return n[1..^16];
        }

        if (n.Length > 16 && n[0] == '_' && n.EndsWith("_k__BackingField", StringComparison.Ordinal))
        {
            return n[1..^16];
        }

        foreach (var p in new[] { "get_", "set_", "add_", "remove_" })
        {
            if (n.StartsWith(p, StringComparison.Ordinal) && n.Length > p.Length)
            {
                return n[p.Length..];
            }
        }

        return n;
    }

    // 去混淆/工具生成的合成成员名（对语义匹配无意义）
    private static readonly System.Text.RegularExpressions.Regex SyntheticName =
        new(@"^(method|field|property|event|type|member)_\d+$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static bool IsSyntheticName(string n) => SyntheticName.IsMatch(n);

    private static string TypeFullName(MetadataReader md, TypeDefinition td)
    {
        var name = md.GetString(td.Name);
        if (td.IsNested)
        {
            var decl = md.GetTypeDefinition(td.GetDeclaringType());
            return TypeFullName(md, decl) + "+" + name;
        }

        var ns = md.GetString(td.Namespace);
        return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
    }

    private static string EntityTypeName(MetadataReader md, EntityHandle h)
    {
        if (h.IsNil)
        {
            return string.Empty;
        }

        switch (h.Kind)
        {
            case HandleKind.TypeReference:
                var tr = md.GetTypeReference((TypeReferenceHandle)h);
                var tn = md.GetString(tr.Name);
                var tns = md.GetString(tr.Namespace);
                return string.IsNullOrEmpty(tns) ? tn : tns + "." + tn;
            case HandleKind.TypeDefinition:
                return TypeFullName(md, md.GetTypeDefinition((TypeDefinitionHandle)h));
            default:
                return "<spec>";
        }
    }

    private static string KindOf(MetadataReader md, TypeDefinition td)
    {
        if ((td.Attributes & TypeAttributes.Interface) != 0)
        {
            return "interface";
        }

        return EntityTypeName(md, td.BaseType) switch
        {
            "System.Enum" => "enum",
            "System.MulticastDelegate" or "System.Delegate" => "delegate",
            "System.ValueType" => "struct",
            _ => "class",
        };
    }

    private static Dictionary<string, TypeInfo> Load(string path)
    {
        using var fs = File.OpenRead(path);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();
        var result = new Dictionary<string, TypeInfo>(StringComparer.Ordinal);

        foreach (var handle in md.TypeDefinitions)
        {
            var td = md.GetTypeDefinition(handle);
            var full = TypeFullName(md, td);
            if (full == "<Module>" || IsNoise(full))
            {
                continue;
            }

            var members = new HashSet<string>(StringComparer.Ordinal);
            void AddMember(string n)
            {
                if (!string.IsNullOrEmpty(n) && !n.StartsWith('.') && !IsMemberNoise(n) && !IsSyntheticName(n))
                {
                    members.Add(NormalizeMember(n));
                }
            }

            foreach (var mh in td.GetMethods())
            {
                AddMember(md.GetString(md.GetMethodDefinition(mh).Name));
            }

            foreach (var fh in td.GetFields())
            {
                AddMember(md.GetString(md.GetFieldDefinition(fh).Name));
            }

            foreach (var ph in td.GetProperties())
            {
                AddMember(md.GetString(md.GetPropertyDefinition(ph).Name));
            }

            foreach (var eh in td.GetEvents())
            {
                AddMember(md.GetString(md.GetEventDefinition(eh).Name));
            }

            result[full] = new TypeInfo(full, KindOf(md, td), EntityTypeName(md, td.BaseType), members);
        }

        return result;
    }

    private static int IntersectCount(HashSet<string> a, HashSet<string> b)
    {
        var (small, large) = a.Count <= b.Count ? (a, b) : (b, a);
        var inter = 0;
        foreach (var x in small)
        {
            if (large.Contains(x))
            {
                inter++;
            }
        }

        return inter;
    }

    // F1 = 2PR/(P+R)。对「改名但成员大体保留」的场景比 Jaccard 更合适：
    // 新版类型常新增成员（如 1.1.5 的 BotOwner 比 4.1 多 134 个），Jaccard 会被稀释。
    private static double F1(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 || b.Count == 0)
        {
            return 0.0;
        }

        var inter = IntersectCount(a, b);
        var precision = (double)inter / b.Count;
        var recall = (double)inter / a.Count;
        var sum = precision + recall;
        return sum == 0 ? 0.0 : 2 * precision * recall / sum;
    }

    private static int Main(string[] args)
    {
        if (args.Length >= 3 && args[0] == "--dump")
        {
            var dumpSet = Load(args[1]);
            var name = args[2];
            if (!dumpSet.TryGetValue(name, out var ti))
            {
                Console.WriteLine($"NOT FOUND: {name}");
                return 2;
            }

            Console.WriteLine($"{ti.FullName} [{ti.Kind}] base={ti.BaseType} members={ti.MemberCount}");
            foreach (var m in ti.Members.OrderBy(x => x, StringComparer.Ordinal))
            {
                Console.WriteLine("  " + m);
            }

            return 0;
        }

        if (args.Length < 3)
        {
            Console.Error.WriteLine("usage: eft-classmap <A.dll> <B.dll> <outDir>");
            Console.Error.WriteLine("       eft-classmap --dump <dll> <fullTypeName>");
            return 1;
        }

        var aPath = args[0];
        var bPath = args[1];
        var outDir = args[2];
        Directory.CreateDirectory(outDir);

        Console.WriteLine($"[load] A = {aPath}");
        var setA = Load(aPath);
        Console.WriteLine($"       {setA.Count} types");
        Console.WriteLine($"[load] B = {bPath}");
        var setB = Load(bPath);
        Console.WriteLine($"       {setB.Count} types");

        var onlyA = setA.Keys.Where(k => !setB.ContainsKey(k)).ToArray();
        var onlyB = setB.Keys.Where(k => !setA.ContainsKey(k)).ToArray();
        Console.WriteLine($"[diff] common={setA.Count - onlyA.Length}  onlyA={onlyA.Length}  onlyB={onlyB.Length}");

        var bByKind = onlyB
            .GroupBy(k => setB[k].Kind)
            .ToDictionary(g => g.Key, g => g.ToArray());

        var results = new List<(string A, string B, string Kind, double Score, int Ma, int Mb)>();
        foreach (var ka in onlyA)
        {
            var ta = setA[ka];

            // 成员太少（1-3 个）的类型的相似度没有区分度，会产生大量虚假匹配
            if (ta.MemberCount < MinMatchMembers)
            {
                continue;
            }

            if (!bByKind.TryGetValue(ta.Kind, out var candidates))
            {
                continue;
            }

            double best = 0;
            var bestB = string.Empty;
            var bestMembers = 0;
            foreach (var kb in candidates)
            {
                var tb = setB[kb];
                // 规模过滤：B 远小于 A（不像改名）或远大于 A（不像同一类型）则跳过
                if (tb.MemberCount * 3 < ta.MemberCount || tb.MemberCount > ta.MemberCount * 10)
                {
                    continue;
                }

                var score = F1(ta.Members, tb.Members);
                if (!string.IsNullOrEmpty(ta.BaseType) && ta.BaseType == tb.BaseType)
                {
                    score += 0.05; // 同基类加权
                }

                if (score > best)
                {
                    best = score;
                    bestB = kb;
                    bestMembers = tb.MemberCount;
                }
            }

            if (best > 0)
            {
                results.Add((ka, bestB, ta.Kind, best, ta.MemberCount, bestMembers));
            }
        }

        results.Sort((x, y) => y.Score.CompareTo(x.Score));

        static string Csv((string A, string B, string Kind, double Score, int Ma, int Mb) r)
            => $"\"{r.A}\",\"{r.B}\",{r.Kind},{r.Score:F3},{r.Ma},{r.Mb}";

        var header = "nameA,nameB,kind,f1,membersA,membersB";
        var exact = results.Where(r => r.Score >= 0.999).ToArray();
        var similar = results.Where(r => r.Score >= 0.70 && r.Score < 0.999).ToArray();
        var possible = results.Where(r => r.Score >= 0.60 && r.Score < 0.70).ToArray();

        File.WriteAllLines(Path.Combine(outDir, "exact-member-match.csv"),
            new[] { header }.Concat(exact.Select(Csv)), Encoding.UTF8);
        File.WriteAllLines(Path.Combine(outDir, "similar-match.csv"),
            new[] { header }.Concat(similar.Select(Csv)), Encoding.UTF8);
        File.WriteAllLines(Path.Combine(outDir, "possible-match.csv"),
            new[] { header }.Concat(possible.Select(Csv)), Encoding.UTF8);

        var resolvedA = exact.Select(r => r.A).Concat(similar.Select(r => r.A)).ToHashSet(StringComparer.Ordinal);
        var resolvedB = exact.Select(r => r.B).Concat(similar.Select(r => r.B)).ToHashSet(StringComparer.Ordinal);
        File.WriteAllLines(Path.Combine(outDir, "unmatched-a.txt"),
            onlyA.Where(k => !resolvedA.Contains(k)).OrderBy(k => k, StringComparer.Ordinal), Encoding.UTF8);
        File.WriteAllLines(Path.Combine(outDir, "unmatched-b.txt"),
            onlyB.Where(k => !resolvedB.Contains(k)).OrderBy(k => k, StringComparer.Ordinal), Encoding.UTF8);

        var summary = new StringBuilder();
        summary.AppendLine($"A = {aPath}");
        summary.AppendLine($"B = {bPath}");
        summary.AppendLine($"typesA = {setA.Count}");
        summary.AppendLine($"typesB = {setB.Count}");
        summary.AppendLine($"common = {setA.Count - onlyA.Length}");
        summary.AppendLine($"onlyA = {onlyA.Length}");
        summary.AppendLine($"onlyB = {onlyB.Length}");
        summary.AppendLine($"exactMemberMatch = {exact.Length}");
        summary.AppendLine($"similar(>=0.70) = {similar.Length}");
        summary.AppendLine($"possible(0.60-0.70) = {possible.Length}");
        summary.AppendLine($"unresolvedA = {onlyA.Length - resolvedA.Count}");
        summary.AppendLine($"unresolvedB = {onlyB.Length - resolvedB.Count}");
        File.WriteAllText(Path.Combine(outDir, "summary.txt"), summary.ToString(), Encoding.UTF8);

        Console.WriteLine($"[match] exact={exact.Length}  similar={similar.Length}  possible={possible.Length}");
        Console.WriteLine($"[match] unresolvedA={onlyA.Length - resolvedA.Count}  unresolvedB={onlyB.Length - resolvedB.Count}");
        Console.WriteLine($"[out]   {outDir}");
        return 0;
    }
}
