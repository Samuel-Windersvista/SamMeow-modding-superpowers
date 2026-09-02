// MongoIdConverter: SPT 4.0 -> 4.1.2 迁移
// 反编译原样保留，无 API 变更
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace TarkovCraftLoader;

public class MongoIdConverter : JsonConverter<MongoId>
{
	public override MongoId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		string? text = reader.GetString();
		if (text != null)
		{
			return new MongoId(text);
		}
		return default(MongoId);
	}

	public override void Write(Utf8JsonWriter writer, MongoId value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString());
	}
}
