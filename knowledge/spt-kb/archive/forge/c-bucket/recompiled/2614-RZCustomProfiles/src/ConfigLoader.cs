using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;

namespace RZCustomProfiles;

[Injectable(InjectionType.Singleton)]
public class ConfigLoader(ILogger<ConfigLoader> logger, ModHelper modHelper)
{
	private readonly Dictionary<(Type, string), object> _cachedConfigs = new Dictionary<(Type, string), object>();

	private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
	};

	public T Load<T>(string filename, Assembly callerAssembly) where T : new()
	{
		string absolutePathToModFolder = modHelper.GetAbsolutePathToModFolder(callerAssembly);
		string text = callerAssembly.GetName().Name ?? "RZ";
		(Type, string) key = (typeof(T), absolutePathToModFolder);
		if (_cachedConfigs.TryGetValue(key, out object value))
		{
			return (T)value;
		}
		string path = Path.Combine(absolutePathToModFolder, "config", filename);
		if (!File.Exists(path))
		{
			logger.LogError("[{Tag}] {File} not found in '{Dir}' : using defaults.", text, filename, absolutePathToModFolder);
			T val = new T();
			_cachedConfigs[key] = val;
			return val;
		}
		T val2 = JsonSerializer.Deserialize<T>(File.ReadAllText(path), _serializerOptions);
		T val3 = val2 ?? new T();
		_cachedConfigs[key] = val3;
		return val3;
	}

	public IEnumerable<T> LoadAll<T>(string subfolder, Assembly callerAssembly, IEnumerable<string>? exclude = null) where T : new()
	{
		string text = Path.Combine(modHelper.GetAbsolutePathToModFolder(callerAssembly), subfolder);
		string tag = callerAssembly.GetName().Name ?? "RZ";
		if (!Directory.Exists(text))
		{
			logger.LogWarning("[{Tag}] Folder '{Dir}' not found.", tag, text);
			return Array.Empty<T>();
		}
		HashSet<string> excludeSet = new HashSet<string>(exclude ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
		return Directory.GetFiles(text, "*.json")
			.Where(path => !excludeSet.Contains(Path.GetFileName(path)))
			.OrderBy(path => path)
			.Select(path =>
			{
				try
				{
					T val = JsonSerializer.Deserialize<T>(File.ReadAllText(path), _serializerOptions);
					if (val == null)
					{
						logger.LogWarning("[{Tag}] Could not deserialize '{File}' : skipping.", tag, path);
					}
					return val;
				}
				catch (Exception ex)
				{
					logger.LogWarning("[{Tag}] Failed to load '{File}' : {Msg} : skipping.", tag, path, ex.Message);
					return default(T);
				}
			})
			.Where(r => r != null)
			.Cast<T>();
	}
}
