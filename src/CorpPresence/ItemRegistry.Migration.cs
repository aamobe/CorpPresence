using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static partial class ItemRegistry
{
	public static bool Migrate(GameItem item)
	{
		if (item == null)
		{
			return false;
		}
		try
		{
			string text = MigrationTarget(item.identifier, item.spritePath);
			if (text == null)
			{
				return false;
			}
			item.identifier = text;
			return true;
		}
		catch (Exception ex)
		{
			Warn("migrate-item", "迁移旧公司物品失败：" + ex.Message);
			return false;
		}
	}

	private static string MigrationTarget(string id, string sprite)
	{
		string text = ItemIdentity.ResolveId(id, sprite);
		if (text == null || !(text != id) || !IsAvailable(text))
		{
			return null;
		}
		return text;
	}

	public static void BeforeDecode(List<SaveItemNode> __0)
	{
		if (__0 == null)
		{
			return;
		}
		foreach (SaveItemNode item in __0)
		{
			if (item == null)
			{
				continue;
			}
			try
			{
				string text = MigrationTarget(item.identifier, item.spritePath);
				if (text != null)
				{
					item.identifier = text;
				}
			}
			catch (Exception ex)
			{
				Warn("migrate-node", "迁移旧公司存档节点失败：" + ex.Message);
			}
		}
	}

	public static void BeforeEncode(GameItem __0)
	{
		Migrate(__0);
	}

}

