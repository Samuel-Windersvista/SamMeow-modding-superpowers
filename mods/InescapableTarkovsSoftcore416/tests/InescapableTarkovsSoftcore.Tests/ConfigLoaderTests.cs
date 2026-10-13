using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using InescapableTarkovsSoftcore.Config;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

public class ConfigLoaderTests
{
    [Fact]
    public void DefaultTemplate_AllModulesEnabled_AndRaidMultiplierIsOne()
    {
        var result = ConfigLoader.Parse(ConfigLoader.ReadDefaultConfigJson());

        Assert.Empty(result.Warnings);
        Assert.True(result.Config.General.Enabled);
        Assert.False(result.Config.General.Debug);
        Assert.True(result.Config.SamuelTweaks.Enabled);
        Assert.True(result.Config.TrueItems.Enabled);
        Assert.True(result.Config.NoFirHideout.Enabled);
        Assert.True(result.Config.AntigravArmbands.Enabled);
        Assert.True(result.Config.Backpacks.Enabled);
        Assert.True(result.Config.Softcore.Enabled);
        Assert.True(result.Config.RaidDuration.Enabled);
        Assert.Equal(1.0, result.Config.RaidDuration.Multiplier);
    }

    [Fact]
    public void Parse_ToleratesCommentsAndTrailingCommas()
    {
        const string json = """
        {
          // 注释行
          "general": { "enabled": false, },
          "raidDuration": { "multiplier": 2.0, },
        }
        """;

        var result = ConfigLoader.Parse(json);

        Assert.Empty(result.Warnings);
        Assert.False(result.Config.General.Enabled);
        Assert.Equal(2.0, result.Config.RaidDuration.Multiplier);
    }

    [Fact]
    public void Parse_UnknownKey_ProducesWarning()
    {
        const string json = """{ "general": { "enabled": true, "bogus": 1 }, "nope": {} }""";

        var result = ConfigLoader.Parse(json);

        Assert.Contains(result.Warnings, warning => warning.Contains("bogus", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, warning => warning.Contains("nope", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_MissingKeys_FallBackToDefaults()
    {
        const string json = """{ "general": { "enabled": false } }""";

        var result = ConfigLoader.Parse(json);

        Assert.False(result.Config.General.Enabled);
        Assert.False(result.Config.General.Debug);
        Assert.True(result.Config.SamuelTweaks.Enabled);
        Assert.True(result.Config.Backpacks.Enabled);
        Assert.Equal(1.0, result.Config.RaidDuration.Multiplier);
    }

    [Fact]
    public void Parse_InvalidValue_ProducesWarning_AndUsesDefault()
    {
        const string json = """{ "general": { "enabled": "yes" }, "raidDuration": { "multiplier": "fast" } }""";

        var result = ConfigLoader.Parse(json);

        Assert.Contains(result.Warnings, warning => warning.Contains("general.enabled", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, warning => warning.Contains("raidDuration.multiplier", StringComparison.Ordinal));
        Assert.True(result.Config.General.Enabled);
        Assert.Equal(1.0, result.Config.RaidDuration.Multiplier);
    }

    [Fact]
    public void Parse_NestedUnknownKey_ProducesWarning_WithFullPath()
    {
        const string json = """{ "samuelTweaks": { "magazineResize": { "enabled": true, "bogus": 1 } } }""";

        var result = ConfigLoader.Parse(json);

        Assert.Contains(
            result.Warnings,
            warning => warning.Contains("samuelTweaks.magazineResize.bogus", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_InvalidNestedSectionType_FallsBackOnlyThatSection()
    {
        // magazineResize 以标量替换对象 → 仅该子节回落默认；其余有效键保留（general.enabled=false 不被重置）。
        const string json = """
        {
          "general": { "enabled": false },
          "samuelTweaks": { "armorConflictFix": false, "magazineResize": "oops" }
        }
        """;

        var result = ConfigLoader.Parse(json);

        Assert.Contains(
            result.Warnings,
            warning => warning.Contains("samuelTweaks.magazineResize", StringComparison.Ordinal));
        Assert.False(result.Config.General.Enabled);
        Assert.False(result.Config.SamuelTweaks.ArmorConflictFix);
        Assert.Equal(10, result.Config.SamuelTweaks.MagazineResize.MinCapacity);
        Assert.Equal(50, result.Config.SamuelTweaks.MagazineResize.MaxCapacity);
    }

    [Fact]
    public void Parse_Overrides_InvalidInnerValues_RemovedPerKey_AndValidConfigKept()
    {
        const string json = """
        {
          "general": { "enabled": false },
          "trueItems": {
            "overrides": { "5672cb124bdc2d1a0f8b4568": 5, "badString": "x", "badFloat": 2.5 }
          }
        }
        """;

        var result = ConfigLoader.Parse(json);

        Assert.Contains(
            result.Warnings,
            warning => warning.Contains("trueItems.overrides.badString", StringComparison.Ordinal));
        Assert.Contains(
            result.Warnings,
            warning => warning.Contains("trueItems.overrides.badFloat", StringComparison.Ordinal));
        // 其他有效配置不被重置
        Assert.False(result.Config.General.Enabled);
        // 坏键逐键移除，好键保留
        Assert.Equal(5, result.Config.TrueItems.Overrides["5672cb124bdc2d1a0f8b4568"]);
        Assert.DoesNotContain("badString", result.Config.TrueItems.Overrides.Keys);
        Assert.DoesNotContain("badFloat", result.Config.TrueItems.Overrides.Keys);
    }

    [Fact]
    public void Parse_IntLeaf_RejectsFractional_AndFallsBackToDefault()
    {
        const string json = """{ "samuelTweaks": { "magazineResize": { "minCapacity": 5.5 } } }""";

        var result = ConfigLoader.Parse(json);

        Assert.Contains(
            result.Warnings,
            warning => warning.Contains("samuelTweaks.magazineResize.minCapacity", StringComparison.Ordinal));
        Assert.Equal(10, result.Config.SamuelTweaks.MagazineResize.MinCapacity);
    }

    [Fact]
    public void Parse_Overrides_ValidValues_RoundTripIntoConfig()
    {
        const string json = """
        { "trueItems": { "overrides": { "5672cb124bdc2d1a0f8b4568": 7, "5c164d2286f774194c5e69fa": 3 } } }
        """;

        var result = ConfigLoader.Parse(json);

        Assert.Empty(result.Warnings);
        Assert.Equal(7, result.Config.TrueItems.Overrides["5672cb124bdc2d1a0f8b4568"]);
        Assert.Equal(3, result.Config.TrueItems.Overrides["5c164d2286f774194c5e69fa"]);
    }

    [Fact]
    public void Parse_NullSection_General_FallsBackToDefault_WithWarning()
    {
        var result = ConfigLoader.Parse("""{ "general": null }""");

        Assert.Contains(result.Warnings, warning => warning.Contains("general", StringComparison.Ordinal));
        Assert.NotNull(result.Config.General);
        Assert.True(result.Config.General.Enabled);
    }

    [Fact]
    public void Parse_NullSection_RaidDuration_FallsBackToDefault_WithWarning()
    {
        var result = ConfigLoader.Parse("""{ "raidDuration": null }""");

        Assert.Contains(result.Warnings, warning => warning.Contains("raidDuration", StringComparison.Ordinal));
        Assert.NotNull(result.Config.RaidDuration);
        Assert.Equal(1.0, result.Config.RaidDuration.Multiplier);
    }

    [Fact]
    public void Parse_NullSection_Backpacks_FallsBackToDefault_WithWarning()
    {
        var result = ConfigLoader.Parse("""{ "backpacks": null }""");

        Assert.Contains(result.Warnings, warning => warning.Contains("backpacks", StringComparison.Ordinal));
        Assert.NotNull(result.Config.Backpacks);
        Assert.True(result.Config.Backpacks.Enabled);
    }

    [Fact]
    public void Parse_AllModelDerivedKeys_ProduceNoUnknownKeyWarnings()
    {
        var result = ConfigLoader.Parse(BuildAllModelKeysJson());

        Assert.DoesNotContain(
            result.Warnings,
            warning => warning.Contains("未知配置键", StringComparison.Ordinal));
    }

    [Fact]
    public void DefaultTemplate_DoesNotContainCustomBackgroundKey_AndNoWarning()
    {
        var json = ConfigLoader.ReadDefaultConfigJson();

        // 默认模板不含 customBackground 键（启动器背景为 v0.1.0 移除项）
        var node = JsonNode.Parse(
            json,
            nodeOptions: null,
            documentOptions: new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            })!;
        Assert.DoesNotContain(
            FlattenSectionKeys(node),
            path => path.Contains("customBackground", StringComparison.Ordinal));

        // 模型侧也不应残留该属性
        Assert.Null(typeof(SamuelTweaksConfig).GetProperty("CustomBackground"));

        // 加载默认模板不因该键缺失产生告警
        var result = ConfigLoader.Parse(json);
        Assert.Empty(result.Warnings);
        Assert.DoesNotContain(
            result.Warnings,
            warning => warning.Contains("customBackground", StringComparison.Ordinal));
    }

    [Fact]
    public void DefaultTemplate_KeySetMatchesModel()
    {
        var node = JsonNode.Parse(
            ConfigLoader.ReadDefaultConfigJson(),
            nodeOptions: null,
            documentOptions: new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            })!;

        var actual = FlattenSectionKeys(node).OrderBy(path => path, StringComparer.Ordinal).ToList();
        var expected = ExpectedKeyPaths(typeof(SoftcoreConfig)).OrderBy(path => path, StringComparer.Ordinal).ToList();

        Assert.Equal(expected, actual);
    }

    /// <summary>按模型反射构造一份包含全部键（类型正确、递归展开子节）的 JSON，用于验证键集派生覆盖模型。</summary>
    private static string BuildAllModelKeysJson()
    {
        var root = new JsonObject();
        foreach (var section in typeof(SoftcoreConfig).GetProperties())
        {
            root[JsonName(section)] = BuildObject(section.PropertyType);
        }

        return root.ToJsonString();
    }

    private static JsonObject BuildObject(Type type)
    {
        var obj = new JsonObject();
        foreach (var property in type.GetProperties())
        {
            var name = JsonName(property);
            var leafType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            obj[name] = leafType switch
            {
                var t when t == typeof(bool) => JsonValue.Create(true),
                var t when t == typeof(string) => JsonValue.Create("x"),
                var t when t == typeof(double) || t == typeof(float) || t == typeof(int) || t == typeof(long)
                    => JsonValue.Create(1.0),
                var t when typeof(System.Collections.IDictionary).IsAssignableFrom(t) => new JsonObject(),
                _ => BuildObject(leafType)
            };
        }

        return obj;
    }

    private static IEnumerable<string> FlattenSectionKeys(JsonNode node)
    {
        foreach (var (sectionName, sectionValue) in (JsonObject)node)
        {
            if (sectionValue is JsonObject section)
            {
                foreach (var path in FlattenObjectKeys(section, sectionName))
                {
                    yield return path;
                }
            }
            else
            {
                yield return sectionName;
            }
        }
    }

    private static IEnumerable<string> FlattenObjectKeys(JsonObject obj, string prefix)
    {
        foreach (var (leafName, value) in obj)
        {
            var path = $"{prefix}.{leafName}";
            if (value is JsonObject nested)
            {
                foreach (var child in FlattenObjectKeys(nested, path))
                {
                    yield return child;
                }
            }
            else
            {
                yield return path;
            }
        }
    }

    private static IEnumerable<string> ExpectedKeyPaths(Type rootType)
    {
        foreach (var section in rootType.GetProperties())
        {
            var sectionName = JsonName(section);
            foreach (var path in ExpectedObjectKeyPaths(section.PropertyType, sectionName))
            {
                yield return path;
            }
        }
    }

    private static IEnumerable<string> ExpectedObjectKeyPaths(Type type, string prefix)
    {
        foreach (var property in type.GetProperties())
        {
            var path = $"{prefix}.{JsonName(property)}";
            var leafType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            // 字典型叶键（如 overrides）在模板中以空对象出现，键集展开不计入路径。
            if (typeof(System.Collections.IDictionary).IsAssignableFrom(leafType))
            {
                continue;
            }

            if (leafType == typeof(bool)
                || leafType == typeof(string)
                || leafType == typeof(double) || leafType == typeof(float)
                || leafType == typeof(int) || leafType == typeof(long))
            {
                yield return path;
            }
            else
            {
                foreach (var child in ExpectedObjectKeyPaths(leafType, path))
                {
                    yield return child;
                }
            }
        }
    }

    private static string JsonName(PropertyInfo property) =>
        property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
}
