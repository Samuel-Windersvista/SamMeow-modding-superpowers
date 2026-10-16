namespace SamMeow.LLMTraderChat.Tests;

/// <summary>测试共用的仓库路径定位辅助。</summary>
internal static class TestRepoPaths
{
    /// <summary>从测试输出目录向上定位包含解决方案文件的仓库根（用于读取随包样例文件）。</summary>
    public static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (directory.GetFiles("SamMeow.LLMTraderChat.sln").Length > 0)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("未找到解决方案根目录（SamMeow.LLMTraderChat.sln）。");
    }
}
