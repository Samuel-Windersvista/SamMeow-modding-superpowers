using System.Text;
using InescapableTarkovsSoftcore.Features;
using InescapableTarkovsSoftcore.Features.TrueItems;
using InescapableTarkovsSoftcore.Features.Softcore;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

/// <summary>
/// 外置文件加载与分发（工单 08）：外部优先 / 缺失重建 / 损坏回落 / JSONC / 条目级容错。
/// 全部用例经唯一共享缝 <see cref="ExternalJsonFile"/> 与各加载器的 internal 显式目录重载，
/// 使用 %TEMP% 下唯一子目录夹具并清理，避免静态缓存串扰。
/// </summary>
[Collection("ExternalJsonFile")]
public class ExternalJsonFileTests
{
    [Fact]
    public void ReadTableText_MissingFile_RebuildsFromEmbeddedTemplate_AndWarns()
    {
        using var workspace = new TempWorkspace();
        var warnings = new List<string>();

        var text = ExternalJsonFile.ReadTableText(
            workspace.Root,
            FeatureTables.ArmbandsRelativePath,
            FeatureTables.ArmbandsResourceName,
            warnings);

        var expectedPath = Path.Combine(workspace.Root, "config", "armbands.json");
        Assert.True(File.Exists(expectedPath), "缺失文件应从内嵌模板重建到磁盘");
        Assert.Equal(ExternalJsonFile.ReadEmbeddedText(FeatureTables.ArmbandsResourceName), text);
        Assert.Equal(text, File.ReadAllText(expectedPath));
        var warning = Assert.Single(warnings);
        Assert.Contains("重建", warning, StringComparison.Ordinal);
        Assert.Contains(FeatureTables.ArmbandsRelativePath, warning, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadTableText_ExistingExternalFile_WinsOverEmbedded()
    {
        using var workspace = new TempWorkspace();
        const string external = """
        // JSONC：注释与尾随逗号
        [
          { "id": "external-id", "name": "External", "weight": -42, },
        ]
        """;
        workspace.Write(FeatureTables.ArmbandsRelativePath, external);
        var warnings = new List<string>();

        var text = ExternalJsonFile.ReadTableText(
            workspace.Root,
            FeatureTables.ArmbandsRelativePath,
            FeatureTables.ArmbandsResourceName,
            warnings);

        Assert.Equal(external, text);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ReadArmbands_ExternalOverride_TakesEffect_AndSupportsJsonc()
    {
        using var workspace = new TempWorkspace();
        workspace.Write(FeatureTables.ArmbandsRelativePath, """
        [
          // 覆盖：外部文件优先
          { "id": "test-armband", "name": "Test", "weight": -42 },
        ]
        """);
        var warnings = new List<string>();

        var entries = FeatureTables.ReadArmbands(workspace.Root, warnings);

        var entry = Assert.Single(entries);
        Assert.Equal("test-armband", entry.Id);
        Assert.Equal(-42.0, entry.Weight);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ReadBackpacks_ExternalOverride_TakesEffect()
    {
        using var workspace = new TempWorkspace();
        workspace.Write(FeatureTables.BackpacksRelativePath, """
        [
          { "id": "test-bag", "name": "Test Bag", "cellsH": 20, "cellsV": 30 }
        ]
        """);
        var warnings = new List<string>();

        var entries = FeatureTables.ReadBackpacks(workspace.Root, warnings);

        var entry = Assert.Single(entries);
        Assert.Equal(20, entry.CellsH);
        Assert.Equal(30, entry.CellsV);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ReadTable_CorruptExternal_FallsBackToEmbedded_AndDoesNotOverwrite()
    {
        using var workspace = new TempWorkspace();
        const string broken = "{ \"broken\": ";
        var path = workspace.Write(FeatureTables.ArmbandsRelativePath, broken);
        var warnings = new List<string>();

        var entries = FeatureTables.ReadArmbands(workspace.Root, warnings);

        // 回落内嵌默认：条目数等于内嵌臂章表
        var embedded = ExternalJsonFile.ReadEmbeddedText(FeatureTables.ArmbandsResourceName);
        var expected = ExternalJsonFile.ParseArray<ArmbandEntry>(embedded, "expected", []);
        Assert.Equal(expected.Count, entries.Count);
        Assert.Contains(warnings, warning => warning.Contains("解析失败", StringComparison.Ordinal));
        // 不覆写用户文件
        Assert.Equal(broken, File.ReadAllText(path));
    }

    [Fact]
    public void ReadTable_SyntacticallyValidButWrongShape_FallsBackToEmbedded()
    {
        using var workspace = new TempWorkspace();
        // 语法合法（数组）但对象表模型非法 → 结构回落
        var path = workspace.Write(ScavCaseResourceLoader.RelativePath, "[1, 2, 3]");
        var warnings = new List<string>();

        var data = ScavCaseResourceLoader.LoadFrom(workspace.Root, warnings);

        Assert.Equal(5, data.Recipes.Count); // 内嵌默认的配方数
        Assert.Contains(warnings, warning => warning.Contains("结构非法", StringComparison.Ordinal));
        Assert.Equal("[1, 2, 3]", File.ReadAllText(path));
    }

    [Fact]
    public void ParseArray_SkipsBadEntries_AndKeepsRest()
    {
        var warnings = new List<string>();
        const string json = """
        [
          { "id": "a", "name": "A", "weight": -6 },
          "not-an-object",
          { "id": "c", "name": "C", "weight": -8, },
        ]
        """;

        var entries = ExternalJsonFile.ParseArray<ArmbandEntry>(json, "臂章表 config/armbands.json", warnings);

        Assert.Equal(2, entries.Count);
        Assert.Equal(new[] { "a", "c" }, entries.Select(entry => entry.Id));
        var warning = Assert.Single(warnings);
        Assert.Contains("非法条目", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void TrueItemsLoader_ExternalOverride_TakesEffect()
    {
        using var workspace = new TempWorkspace();
        workspace.Write("config/trueitems/barter.json", """
        {
          // 外部覆盖 Active / StackMult
          "Active": false,
          "StackMult": 7,
          "List": [],
          "ParentList": [],
        }
        """);
        var warnings = new List<string>();

        var tables = TrueItemsResourceLoader.LoadTables(workspace.Root, warnings);

        Assert.False(tables.Barter.Active);
        Assert.Equal(7, tables.Barter.StackMult);
        // 其余五表缺失 → 重建内嵌默认，产生告警
        Assert.True(tables.Clothing.Active);
        Assert.Contains(warnings, warning => warning.Contains("config/trueitems/clothing.json", StringComparison.Ordinal));
    }

    [Fact]
    public void FleaMarketLoader_ExternalOverride_TakesEffect()
    {
        using var workspace = new TempWorkspace();
        workspace.Write(FleaMarketResourceLoader.RelativePath, """
        {
          "whitelist": [ "override" ],
          "bsgBlacklist": [ "x", "y" ],
        }
        """);
        var warnings = new List<string>();

        var data = FleaMarketResourceLoader.LoadFrom(workspace.Root, warnings);

        Assert.Equal(new[] { "override" }, data.Whitelist);
        Assert.Equal(2, data.BsgBlacklist.Count);
        Assert.Empty(warnings);
    }

    [Fact]
    public void CraftingLoader_ExternalOverride_TakesEffect()
    {
        using var workspace = new TempWorkspace();
        workspace.Write(CraftingResourceLoader.RebalanceRelativePath, """{ "recipeAdjustments": [] }""");
        var warnings = new List<string>();

        var rebalance = CraftingResourceLoader.LoadRebalanceFrom(workspace.Root, warnings);

        Assert.Empty(rebalance.RecipeAdjustments);
        Assert.Empty(warnings);
    }

    [Fact]
    public void PendingWarnings_AddAndDrain_AreQueueSemantics()
    {
        ExternalJsonFile.DrainPendingWarnings(); // 排空其它测试/静态初始化遗留

        ExternalJsonFile.AddPendingWarnings(["[ITS] a", "[ITS] b"]);
        ExternalJsonFile.AddPendingWarnings(["[ITS] c"]);

        var drained = ExternalJsonFile.DrainPendingWarnings();

        Assert.Equal(new[] { "[ITS] a", "[ITS] b", "[ITS] c" }, drained);
        Assert.Empty(ExternalJsonFile.DrainPendingWarnings());
    }

    [Fact]
    public void TrueItemsLoader_EntryLevelTolerance_SkipsBadListAndParentListEntries()
    {
        using var workspace = new TempWorkspace();
        workspace.Write("config/trueitems/barter.json", """
        {
          // Active/StackMult 正常取；List / ParentList 逐元素容错
          "Active": true,
          "StackMult": 5,
          "List": [
            { "name": "Good", "_id": "good-1", "_props": { "StackMaxSize": 10 } },
            "not-an-object",
            { "name": "Good2", "_id": "good-2", "_props": { "StackMaxSize": 20 }, }
          ],
          "ParentList": [
            "bad-parent",
            { "name": "PGood", "_id": "parent-1", "StackMaxSize": 3 }
          ]
        }
        """);
        var warnings = new List<string>();

        var tables = TrueItemsResourceLoader.LoadTables(workspace.Root, warnings);

        Assert.Equal(5, tables.Barter.StackMult);
        Assert.Equal(new[] { "good-1", "good-2" }, tables.Barter.List.Select(entry => entry.Id));
        var parent = Assert.Single(tables.Barter.ParentList);
        Assert.Equal("parent-1", parent.Id);
        Assert.Contains(warnings, warning => warning.Contains("List", StringComparison.Ordinal)
                                             && warning.Contains("非法条目", StringComparison.Ordinal));
        Assert.Contains(warnings, warning => warning.Contains("ParentList", StringComparison.Ordinal)
                                             && warning.Contains("非法条目", StringComparison.Ordinal));
    }

    [Fact]
    public void FleaMarketLoader_DictionaryTolerance_SkipsBadKey()
    {
        using var workspace = new TempWorkspace();
        workspace.Write(FleaMarketResourceLoader.RelativePath, """
        {
          "requestWhitelist": {
            "goodKey": 1480000,
            "badKey": "not-a-number"
          }
        }
        """);
        var warnings = new List<string>();

        var data = FleaMarketResourceLoader.LoadFrom(workspace.Root, warnings);

        Assert.Equal(1480000d, data.RequestWhitelist["goodKey"]);
        Assert.DoesNotContain("badKey", data.RequestWhitelist.Keys);
        Assert.Contains(warnings, warning => warning.Contains("requestWhitelist", StringComparison.Ordinal)
                                             && warning.Contains("badKey", StringComparison.Ordinal));
    }

    [Fact]
    public void ScavCaseLoader_EntryLevelTolerance_SkipsBadRecipe()
    {
        using var workspace = new TempWorkspace();
        workspace.Write(ScavCaseResourceLoader.RelativePath, """
        {
          "recipes": [
            {
              "id": "recipe-1",
              "requiredItem": "item-x",
              "productionTime": 100,
              "common": { "min": 1, "max": 2 },
              "rare": { "min": 1, "max": 2 },
              "superrare": { "min": 1, "max": 2 }
            },
            "garbage-recipe",
            {
              "id": "recipe-2",
              "requiredItem": "item-y",
              "productionTime": 200,
              "common": { "min": 3, "max": 4 },
              "rare": { "min": 3, "max": 4 },
              "superrare": { "min": 3, "max": 4 }
            }
          ]
        }
        """);
        var warnings = new List<string>();

        var data = ScavCaseResourceLoader.LoadFrom(workspace.Root, warnings);

        Assert.Equal(new[] { "recipe-1", "recipe-2" }, data.Recipes.Select(recipe => recipe.Id));
        Assert.Contains(warnings, warning => warning.Contains("recipes", StringComparison.Ordinal)
                                             && warning.Contains("非法条目", StringComparison.Ordinal));
    }

    [Fact]
    public void TrueItemsLoader_NullListElement_IsSkipped_WithWarning()
    {
        using var workspace = new TempWorkspace();
        workspace.Write("config/trueitems/barter.json", """
        {
          "Active": true,
          "StackMult": 1,
          "List": [
            null,
            { "name": "Good", "_id": "good-1", "_props": { "StackMaxSize": 10 } },
            null
          ]
        }
        """);
        var warnings = new List<string>();

        var tables = TrueItemsResourceLoader.LoadTables(workspace.Root, warnings);

        var entry = Assert.Single(tables.Barter.List);
        Assert.Equal("good-1", entry.Id);
        Assert.Contains(warnings, warning => warning.Contains("字段 List", StringComparison.Ordinal)
                                             && warning.Contains("非法条目", StringComparison.Ordinal));
    }

    [Fact]
    public void TrueItemsLoader_CaseVariantFieldNames_AreMatched_AndBadEntriesStillPruned()
    {
        using var workspace = new TempWorkspace();
        // 字段名大小写变体（list/stackmult）应与 SerializerOptions 一致被定位：
        // 坏条目仍走条目级剪枝（而非整表"结构非法"回落内嵌）。
        workspace.Write("config/trueitems/barter.json", """
        {
          "active": true,
          "stackmult": 7,
          "list": [
            "bad-entry",
            { "name": "Good", "_id": "good-1", "_props": { "StackMaxSize": 10 } }
          ]
        }
        """);
        var warnings = new List<string>();

        var tables = TrueItemsResourceLoader.LoadTables(workspace.Root, warnings);

        Assert.Equal(7, tables.Barter.StackMult);
        var entry = Assert.Single(tables.Barter.List);
        Assert.Equal("good-1", entry.Id);
        Assert.Contains(warnings, warning => warning.Contains("非法条目", StringComparison.Ordinal));
        Assert.DoesNotContain(warnings, warning => warning.Contains("结构非法", StringComparison.Ordinal));
    }

    /// <summary>%TEMP% 下唯一子目录夹具，Dispose 时递归清理。</summary>
    private sealed class TempWorkspace : IDisposable
    {
        public TempWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), "its-extjson-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public string Write(string relativePath, string content)
        {
            var path = Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content, new UTF8Encoding(false));
            return path;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch
            {
                // 清理失败不影响断言
            }
        }
    }
}
