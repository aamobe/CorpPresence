using System;
using System.Collections.Generic;

namespace CorpPresence;

internal sealed class CorpState
{
	private const string Key = "CorpPresence";
	private SaveBinding _binding;

	internal bool IsBoundReadonly(PlayerStore store) => _binding != null && _binding.MatchesReadonly(store);

	internal bool IsBoundTo(PlayerStore store) => _binding != null && _binding.Matches(store);

	public string RunId { get; set; }

	public int MangoGiftStatus { get; set; }

	public Dictionary<string, CharacterState> Characters { get; set; } = new Dictionary<string, CharacterState>();

	public CharacterState Seline { get; set; }

	public MedState Meds { get; set; } = new MedState();

	public Dictionary<string, CompanyState> Companies { get; set; } = new Dictionary<string, CompanyState>();

	public static CorpState Current { get; set; }

	public CompanyState CompanyOf(string id)
	{
		if (!Companies.TryGetValue(id, out var value))
		{
			value = (Companies[id] = new CompanyState());
		}
		return value;
	}

	public CharacterState Of(string id)
	{
		if (Seline != null && !Characters.ContainsKey("seline"))
		{
			Characters["seline"] = Seline;
		}
		Seline = null;
		if (!Characters.TryGetValue(id, out var value))
		{
			value = (Characters[id] = new CharacterState());
		}
		return value;
	}

	public static CorpState Get()
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
		Current = Load(instance) ?? new CorpState
		{
			RunId = instance.runID
		};
		Current._binding = new SaveBinding(instance, Key);
		return Current;
	}

	private static CorpState Load(PlayerStore store)
	{
		try
		{
			string text = SaveSlot.Read("CorpPresence");
			if (text == null)
			{
				return null;
			}
			CorpState corpState = Json.Read<CorpState>(text);
			if (corpState == null) throw new InvalidOperationException("公司状态 JSON 为空对象");
			if (corpState.RunId == store.runID) SaveStructure.Validate(corpState);
			SaveSlot.AcceptRead(Key);
			return (corpState?.RunId == store.runID) ? corpState : null;
		}
		catch (Exception ex)
		{
			SaveSlot.ProtectUnreadable(Key);
			try { ModLog.Warn("公司数据读取失败，保留原始数据，暂不覆盖该公司记录：" + ex.Message); } catch { }
			return null;
		}
	}

	public static void Save()
	{
		PlayerStore instance = SaveSlot.CurrentStore;
		if (instance != null && Current != null && !(Current.RunId != instance.runID) && SaveSlot.CanWrite(Key))
		{
			if (Current._binding == null) Current._binding = new SaveBinding(instance, Key);
			if (!Current._binding.Matches(instance)) return;
			SaveSlot.Write("CorpPresence", Json.Write(Current));
			Current._binding.Refresh(instance);
		}
	}
}


