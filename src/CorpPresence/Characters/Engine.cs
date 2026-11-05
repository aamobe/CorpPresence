using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CorpPresence;

internal static partial class Engine
{
	public static readonly Character[] All = new Character[6]
	{
		new Isa(),
		new Milo(),
		new Seline(),
		new Rosen(),
		new Doreen(),
		new Mentor()
	};

	public static volatile bool BuyActive;

	private const int MinStock = 3;

	private const int MaxStock = 10;

	[ThreadStatic] private static bool _scheduling;



	public static Character Find(string id)
	{
		foreach (Character character in All) if (character.Id == id) return character;
		return null;
	}

	private static CharacterState StateOf(Character c)
	{
		return CorpState.Get()?.Of(c.Id);
	}
	public static string DebugQueue(string id, string kind)
	{
		if (!PlayerStore.IsInstanceExist())
		{
			return null;
		}
		Character c = Find(id);
		CharacterState s = StateOf(c);
		if (kind == null)
		{
			int num = Enumerable.Range(1, c.Chapters).FirstOrDefault((int ch) => c.IsSide(ch) && !s.SideDone.Contains(ch) && s.Chapter >= c.SideAfter(ch));
			int num2 = c.NextMain(s);
			kind = ((num2 > 0 && (num == 0 || num2 < num)) ? ("ch" + num2) : ((num > 0) ? ("ch" + num) : "daily"));
		}
		PlayerStore.Instance.storeClientManager.AddNextClient(Create(c, kind));
		LogVisit("调试：" + c.Name + "（" + kind + "）排为下一位");
		return kind;
	}

	public static int Reputation(string faction)
	{
		try
		{
			return StoreReputation.GetStoreReputation(faction).GetReputation();
		}
		catch
		{
			return 0;
		}
	}

	public static string[] Pick(string[] pool, int min, int max)
	{
		System.Random rng = new System.Random();
		return (from _ in Enumerable.Range(0, rng.Next(min, max + 1))
			select pool[rng.Next(pool.Length)]).ToArray();
	}

	public static string Any(params string[] options)
	{
		return options[new System.Random().Next(options.Length)];
	}
}






