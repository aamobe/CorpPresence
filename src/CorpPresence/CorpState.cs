using System;
using System.Collections.Generic;

namespace CorpPresence;

internal sealed class CorpState
{
	private const string Key = "CorpPresence";

	private SaveBinding _binding;

	public string RunId { get; set; }

	public Dictionary<string, CharacterState> Characters { get; set; } = new Dictionary<string, CharacterState>();

	public CharacterState Seline { get; set; }

	public MedState Meds { get; set; } = new MedState();

	public Dictionary<string, CompanyState> Companies { get; set; } = new Dictionary<string, CompanyState>();

	public static CorpState Current { get; set; }

	internal bool IsBoundTo(PlayerStore store)
	{
		if (_binding != null)
		{
			return _binding.Matches(store);
		}
		return false;
	}

	public CompanyState CompanyOf(string id)
	{
		if (!Companies.TryGetValue(id, out var value))
		{
			return Companies[id] = new CompanyState();
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
			return Characters[id] = new CharacterState();
		}
		return value;
	}

	public static CorpState Get()
	{
		PlayerStore currentStore = SaveSlot.CurrentStore;
		if (currentStore == null)
		{
			return null;
		}
		if (Current != null && Current.RunId == currentStore.runID)
		{
			if (Current._binding == null)
			{
				Current._binding = new SaveBinding(currentStore, "CorpPresence");
			}
			if (Current._binding.Matches(currentStore))
			{
				return Current;
			}
		}
		Current = Load(currentStore) ?? new CorpState
		{
			RunId = currentStore.runID
		};
		Current._binding = new SaveBinding(currentStore, "CorpPresence");
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
			if (corpState == null)
			{
				throw new InvalidOperationException("公司状态 JSON 为空对象");
			}
			if (corpState.RunId == store.runID)
			{
				SaveStructure.Validate(corpState);
			}
			SaveSlot.AcceptRead("CorpPresence");
			return (corpState?.RunId == store.runID) ? corpState : null;
		}
		catch (Exception ex)
		{
			SaveSlot.ProtectUnreadable("CorpPresence");
			try
			{
				ModLog.Warn("公司数据读取失败，保留原始数据，暂不覆盖该公司记录：" + ex.Message);
			}
			catch
			{
			}
			return null;
		}
	}

	public static void Save()
	{
		PlayerStore currentStore = SaveSlot.CurrentStore;
		if (currentStore != null && Current != null && !(Current.RunId != currentStore.runID) && SaveSlot.CanWrite("CorpPresence"))
		{
			if (Current._binding == null)
			{
				Current._binding = new SaveBinding(currentStore, "CorpPresence");
			}
			if (Current._binding.Matches(currentStore))
			{
				SaveSlot.Write("CorpPresence", Json.Write(Current));
				Current._binding.Refresh(currentStore);
			}
		}
	}
}
