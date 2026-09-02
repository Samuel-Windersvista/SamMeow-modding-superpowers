using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace SamSWAT.HeliCrash.ArysReloaded.Utils;

internal static class FileUtil
{
    public static string Directory { get; } =
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

    public static T LoadJson<T>(string path)
    {
        string json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<T>(json);
    }
}
