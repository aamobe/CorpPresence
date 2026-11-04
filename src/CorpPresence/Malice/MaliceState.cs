using System;
using System.Collections.Generic;

namespace CorpPresence.Malice;

internal sealed class MaliceState
{
	private const string Key = "CorpPresence.Malice";
	private SaveBinding _binding;

	public static MaliceState Current;

	public string RunId { get; set; }

	public int Version { get; set; } = 1;

	public bool WasOn { get; set; }

	public Dictionary<string, CorpTerms> Corps { get; set; } = new Dictionary<string, CorpTerms>();

	public int RebelPoints { get; set; }

	public int RebelEarned { get; set; }

	public int FenceDay { get; set; } = -99;

	public bool OdinMet { get; set; }

	public bool OdinArrested { get; set; }

	public EnvoyState Envoy { get; set; } = new EnvoyState();

	public RebelState Rebel { get; set; } = new RebelState();

	public CorpTerms Of(string companyId)
	{
		if (!Corps.TryGetValue(companyId, out var value))
		{
			value = (Corps[companyId] = new CorpTerms());
		}
		return value;
	}

	public static MaliceState Get()
	{
		PlayerStore instance = SaveSlot.CurrentStore;
		if (instance == null)
		{
			return null;
		}
		if (Current != null && Current.RunId == instance.runID)
		{
			if (Current._binding == null) Current._binding = new SaveBinding(instance, Key);
			if (Current._binding.Matches(instance)) return Current;
		}
		Current = null;
		try
		{
			string text = SaveSlot.Read("CorpPresence.Malice");
			if (text != null)
			{
				Current = Json.Read<MaliceState>(text);
				if (Current == null) throw new InvalidOperationException("巨企状态 JSON 为空对象");
				if (Current.RunId == instance.runID) SaveStructure.Validate(Current);
				SaveSlot.AcceptRead(Key);
			}
		}
		catch (Exception ex)
		{
			Current = null;
			SaveSlot.ProtectUnreadable(Key);
			try { ModLog.Warn("巨企数据读取失败，保留原始数据，暂不覆盖该巨企记录：" + ex.Message); } catch { }
		}
		if (Current == null || Current.RunId != instance.runID)
		{
			Current = new MaliceState
			{
				RunId = instance.runID
			};
		}
		MaliceState current = Current;
		if (current.Corps == null)
		{
			Dictionary<string, CorpTerms> dictionary = (current.Corps = new Dictionary<string, CorpTerms>());
		}
		current = Current;
		if (current.Envoy == null)
		{
			EnvoyState envoyState = (current.Envoy = new EnvoyState());
		}
		current = Current;
		if (current.Rebel == null)
		{
			RebelState rebelState = (current.Rebel = new RebelState());
		}
		Current._binding = new SaveBinding(instance, Key);
		return Current;
	}

	public static void Save()
	{
		PlayerStore instance = SaveSlot.CurrentStore;
		if (instance != null && Current != null && !(Current.RunId != instance.runID) && SaveSlot.CanWrite(Key))
		{
			if (Current._binding == null) Current._binding = new SaveBinding(instance, Key);
			if (!Current._binding.Matches(instance)) return;
			SaveSlot.Write("CorpPresence.Malice", Json.Write(Current));
			Current._binding.Refresh(instance);
		}
	}

	public static void Reset()
	{
		Current = null;
	}
}


