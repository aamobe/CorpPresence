using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CorpPresence;

internal static class Json
{
	private sealed class PropertiesOnly : DefaultContractResolver
	{
		protected override List<MemberInfo> GetSerializableMembers(Type type)
		{
			return (from m in base.GetSerializableMembers(type)
				where m is PropertyInfo
				select m).ToList();
		}
	}

	private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
	{
		ContractResolver = new PropertiesOnly()
	};

	public static string Write<T>(T value)
	{
		return JsonConvert.SerializeObject(value, Settings);
	}

	public static T Read<T>(string json)
	{
		return JsonConvert.DeserializeObject<T>(json, Settings);
	}
}
