using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;

namespace pitTeam.Server.Services;

[Injectable(InjectionType.Singleton)]
public class FriendlyLanguageService(ISptLogger<FriendlyLanguageService> logger)
{
	private const string ModFolderName = "pitFireTeam-ServerMod";

	private const string LanguageFolderName = "lang";

	private readonly ConcurrentDictionary<string, string> sessionLocales = new ConcurrentDictionary<string, string>();

	private JsonObject? embeddedEnglishFallback;

	private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
	{
		WriteIndented = false
	};

	public string GetLanguageJson(MongoId sessionId, string? requestedLocale, string? embeddedEnglishJson)
	{
		string text = NormalizeLocale(requestedLocale);
		sessionLocales[sessionId.ToString()] = text;
		string languageDirectory = Path.Combine(AppContext.BaseDirectory, "user", "mods", "pitFireTeam-ServerMod", "Resources", "lang");
		JsonObject jsonObject = ParseLanguageJson(embeddedEnglishJson, "embedded English language");
		if (jsonObject != null)
		{
			embeddedEnglishFallback = CloneObject(jsonObject);
		}
		EnsureEnglishLanguageFile(languageDirectory, jsonObject);
		JsonObject jsonObject2 = LoadLanguageFile(languageDirectory, "en") ?? CloneObject(jsonObject) ?? CloneObject(embeddedEnglishFallback) ?? new JsonObject();
		JsonObject jsonObject3 = (string.Equals(text, "en", StringComparison.OrdinalIgnoreCase) ? jsonObject2 : (LoadLanguageFile(languageDirectory, text) ?? new JsonObject()));
		if (jsonObject2 != jsonObject3)
		{
			MergeMissingValues(jsonObject3, jsonObject2);
		}
		return jsonObject3.ToJsonString(SerializerOptions);
	}

	public string[] GetStringArray(MongoId sessionId, string key)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		JsonObject sessionLanguage = GetSessionLanguage(sessionId);
		if (sessionLanguage.TryGetPropertyValue(key, out JsonNode jsonNode) && jsonNode is JsonArray source)
		{
			string[] array = (from value in source
				select value?.GetValue<string>() into value
				where !string.IsNullOrWhiteSpace(value)
				select value).ToArray();
			if (array.Length != 0)
			{
				return array;
			}
		}
		return Array.Empty<string>();
	}

	public Dictionary<string, string> GetStringMap(MongoId sessionId, string key)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		JsonObject sessionLanguage = GetSessionLanguage(sessionId);
		if (!sessionLanguage.TryGetPropertyValue(key, out JsonNode jsonNode) || !(jsonNode is JsonObject jsonObject))
		{
			return new Dictionary<string, string>();
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (KeyValuePair<string, JsonNode> item in jsonObject)
		{
			string value = item.Value?.GetValue<string>();
			if (!string.IsNullOrWhiteSpace(value))
			{
				dictionary[item.Key] = value;
			}
		}
		return dictionary;
	}

	private JsonObject GetSessionLanguage(MongoId sessionId)
	{
		string value;
		string text = (sessionLocales.TryGetValue(sessionId.ToString(), out value) ? value : "en");
		string languageDirectory = Path.Combine(AppContext.BaseDirectory, "user", "mods", "pitFireTeam-ServerMod", "Resources", "lang");
		JsonObject jsonObject = LoadLanguageFile(languageDirectory, "en") ?? CloneObject(embeddedEnglishFallback) ?? new JsonObject();
		JsonObject jsonObject2 = (string.Equals(text, "en", StringComparison.OrdinalIgnoreCase) ? jsonObject : (LoadLanguageFile(languageDirectory, text) ?? new JsonObject()));
		if (jsonObject != jsonObject2)
		{
			MergeMissingValues(jsonObject2, jsonObject);
		}
		return jsonObject2;
	}

	private void EnsureEnglishLanguageFile(string languageDirectory, JsonObject? embeddedEnglish)
	{
		if (embeddedEnglish != null && embeddedEnglish.Count != 0)
		{
			Directory.CreateDirectory(languageDirectory);
			string path = Path.Combine(languageDirectory, "en.json");
			JsonObject jsonObject = LoadLanguageFile(languageDirectory, "en");
			if (jsonObject == null)
			{
				WriteLanguageFile(path, embeddedEnglish);
			}
			else if (MergeMissingValues(jsonObject, embeddedEnglish))
			{
				WriteLanguageFile(path, jsonObject);
			}
		}
	}

	private JsonObject? LoadLanguageFile(string languageDirectory, string locale)
	{
		string text = Path.Combine(languageDirectory, locale + ".json");
		if (!File.Exists(text))
		{
			return null;
		}
		try
		{
			return ParseLanguageJson(File.ReadAllText(text), text);
		}
		catch (Exception ex)
		{
			logger.Warning("Failed to load pitFireTeam language file '" + text + "': " + ex.Message);
			return null;
		}
	}

	private JsonObject? ParseLanguageJson(string? json, string source)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}
		try
		{
			JsonNode jsonNode = JsonNode.Parse(json);
			if (jsonNode is JsonObject result)
			{
				return result;
			}
			logger.Warning("pitFireTeam language source '" + source + "' is not a JSON object.");
			return null;
		}
		catch (Exception ex)
		{
			logger.Warning("Failed to parse pitFireTeam language source '" + source + "': " + ex.Message);
			return null;
		}
	}

	private void WriteLanguageFile(string path, JsonObject language)
	{
		try
		{
			File.WriteAllText(path, language.ToJsonString(SerializerOptions));
		}
		catch (Exception ex)
		{
			logger.Warning("Failed to write pitFireTeam language file '" + path + "': " + ex.Message);
		}
	}

	private static string NormalizeLocale(string? locale)
	{
		if (string.IsNullOrWhiteSpace(locale))
		{
			return "en";
		}
		string text = locale.Trim().ToLowerInvariant();
		int num = text.IndexOfAny(new char[2] { '-', '_' });
		if (num > 0)
		{
			text = text.Substring(0, num);
		}
		if (text.StartsWith("zh") || text.StartsWith("ch"))
		{
			return "chs";
		}
		if (text == "de")
		{
			return "ge";
		}
		return text;
	}

	private static bool MergeMissingValues(JsonObject target, JsonObject fallback)
	{
		bool flag = false;
		foreach (KeyValuePair<string, JsonNode> item in fallback)
		{
			if (!target.TryGetPropertyValue(item.Key, out JsonNode jsonNode) || jsonNode == null)
			{
				target[item.Key] = item.Value?.DeepClone();
				flag = true;
			}
			else if (!IsCompatibleLanguageNode(jsonNode, item.Value))
			{
				target[item.Key] = item.Value?.DeepClone();
				flag = true;
			}
			else if (jsonNode is JsonObject target2 && item.Value is JsonObject fallback2)
			{
				flag |= MergeMissingValues(target2, fallback2);
			}
		}
		return flag;
	}

	private static bool IsCompatibleLanguageNode(JsonNode target, JsonNode? fallback)
	{
		if (fallback == null)
		{
			return true;
		}
		if (!(fallback is JsonObject))
		{
			if (!(fallback is JsonArray))
			{
				if (fallback is JsonValue)
				{
					return target is JsonValue;
				}
				return true;
			}
			return target is JsonArray;
		}
		return target is JsonObject;
	}

	private static JsonObject? CloneObject(JsonObject? value)
	{
		return value?.DeepClone() as JsonObject;
	}
}

