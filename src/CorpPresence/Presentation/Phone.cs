using System;
using System.Collections.Generic;
using CorpPresence.UI;

namespace CorpPresence;

internal static class Phone
{
	private const int Regular = 4;

	private const int NotResponding = 1;

	private const int Supplier = 1;

	private static readonly HashSet<long> Warned = new HashSet<long>();
	private static long _callGeneration;

	public const string Shunned = "LLAVOIDED";

	public static void Register(Dictionary<long, StorePhoneClient> dict)
	{
		if (dict == null || Settings.CleanupForUninstall)
		{
			return;
		}
		Company[] all = Companies.All;
		foreach (Company company in all)
		{
			if (dict.ContainsKey(company.PhoneNumber))
			{
				StorePhoneClient storePhoneClient = dict[company.PhoneNumber];
				if (Owns(storePhoneClient, company))
				{
					Sync(company, storePhoneClient);
				}
				else
				{
					WarnTaken(company, storePhoneClient);
				}
				continue;
			}
			StorePhoneClient storePhoneClient2 = new StorePhoneClient
			{
				phoneClientType = StorePhoneClient.PhoneClientType.Supplier,
				displayName = company.Name,
				locID = LocId(company),
				dialogFuncId = "",
				cooldownDuration = 0,
				currentCooldown = 0
			};
			Sync(company, storePhoneClient2);
			dict.Add(company.PhoneNumber, storePhoneClient2);
		}
	}

	public static string LocId(Company c)
	{
		return "corp_phone_" + c.Id;
	}

	public static bool Owns(StorePhoneClient client, Company c)
	{
		if (client != null)
		{
			return client.locID == LocId(c);
		}
		return false;
	}

	private static void WarnTaken(Company c, StorePhoneClient other)
	{
		if (Warned.Add(c.PhoneNumber))
		{
			ModLog.Warn(string.Format("号码 {0} 已被别的模组占用（{1}），{2}的电话不登记，以免改动对方的电话", c.PhoneNumber, other?.displayName ?? other?.locID ?? "未知", c.Name));
		}
	}

	public static bool IsOurs(long number, Company c)
	{
		Dictionary<long, StorePhoneClient> dictionary = SaveSlot.CurrentStore?.PhoneClientDict;
		if (dictionary != null && dictionary.ContainsKey(number))
		{
			return Owns(dictionary[number], c);
		}
		return true;
	}

	public static bool Available(Company c) =>
		c.Active && (c is JacksonContact || (Companies.StateOf(c)?.CardGiven ?? false));

	private static void Sync(Company c, StorePhoneClient client)
	{
		bool flag = Available(c);
		client.phoneState = ((!flag) ? StorePhoneClient.PhoneState.NotResponding : StorePhoneClient.PhoneState.Regular);
		client.dialedBefore = flag;
	}

	public static void Activate(Company c)
	{
		try
		{
			Dictionary<long, StorePhoneClient> dictionary = SaveSlot.CurrentStore?.PhoneClientDict;
			if (dictionary != null)
			{
				if (!dictionary.ContainsKey(c.PhoneNumber))
				{
					Register(dictionary);
				}
				else if (Owns(dictionary[c.PhoneNumber], c))
				{
					Sync(c, dictionary[c.PhoneNumber]);
				}
				else
				{
					WarnTaken(c, dictionary[c.PhoneNumber]);
				}
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("电话登记失败：" + ex.Message);
		}
	}

	public static void WarnIfShunned(long number)
	{
		Company company = Companies.ByNumber(number);
		if (company != null)
		{
			CompanyState companyState = Companies.StateOf(company);
			if (companyState != null && companyState.CardGiven && StoreReputation.IsPerkUnlocked("LLAVOIDED"))
			{
				StoreUIManager.Instance?.Notify(L.T(company.Name + "拒接了你的电话：你在下层区“狗嫌人厌”，供货商都不愿和你来往。", company.Name + L.Translate(" won't take your call: you're a pariah in the Lower Levels, and no supplier wants to deal with you.")));
			}
		}
	}

	public static int RemoveAll()
	{
		Dictionary<long, StorePhoneClient> dictionary = SaveSlot.CurrentStore?.PhoneClientDict;
		if (dictionary == null)
		{
			return 0;
		}
		int num = 0;
		Company[] all = Companies.All;
		foreach (Company company in all)
		{
			if (dictionary.ContainsKey(company.PhoneNumber) && Owns(dictionary[company.PhoneNumber], company) && dictionary.Remove(company.PhoneNumber))
			{
				num++;
			}
		}
		return num;
	}

	public static void RegisterCurrent()
	{
		try
		{
			Register(SaveSlot.CurrentStore?.PhoneClientDict);
		}
		catch (Exception ex)
		{
			ModLog.Warn("电话登记失败：" + ex.Message);
		}
	}

	public static Dialogue CallDialog(Company c)
	{
		long generation = unchecked(++_callGeneration);
		Func<Company, Dialogue> custom = Ext.CallDialog;
		Dialogue dialogue = ((custom == null) ? null : Ext.Ask("CallDialog", () => custom(c), null));
		if (dialogue != null)
		{
			return dialogue;
		}
		StoreSession session = StoreSession.Capture();
		bool SessionCurrent() => generation == _callGeneration && session.IsCurrent;
		bool CallCurrent() => SessionCurrent() && Owns(PhoneUIManager.Instance?.currentPhoneClient, c);
		bool opened = false;
		bool hungUp = false;
		string deskName = c.DeskName;
		string[] frontDesk = c.FrontDesk;
		string text = frontDesk[new Random().Next(frontDesk.Length)];
		string text2 = (string.IsNullOrEmpty(c.DeskAsk) ? text : (text + "\n" + c.DeskAsk));
		Dialogue dialogue2 = new Dialogue().SetText(deskName, text2);
		string browse = c is JacksonContact ? L.T("看看机器和配件", "Machines and parts") : L.T("浏览服务", "Browse services");
		Dialogue dialogue3 = new Dialogue().SetText("PLAYER", browse);
		Dialogue dialogue4 = new Dialogue().SetText("PLAYER", L.T("挂断", "Hang up"));
		dialogue3.NextDialogue().SetText(deskName, c is JacksonContact ? L.T("目录发给你了，看看有没有用得上的。", "Here's the list. See if anything looks useful.") : L.T("好的，这就为您打开服务目录。", "Of course. Opening our service catalog now.")).SetStartAction(() =>
		{
			if (opened || !CallCurrent()) return;
			opened = true;
			try
			{
				CompanyWindow.Open(c);
			}
			catch (Exception arg)
			{
				try { ModLog.Warn($"打开面板失败：{arg}"); } catch { }
			}
			if (!CallCurrent()) return;
			try
			{
				PhoneUIManager.Instance?.CloseUI();
			}
			catch
			{
			}
			if (!SessionCurrent()) return;
			try
			{
				PhoneUIManager.Instance?.OnPhoneDialogEnd(playEndSound: false);
			}
			catch
			{
			}
		});
		dialogue4.NextDialogue().SetText(deskName, c is JacksonContact ? L.T("行，有需要再打来。", "All right. Call me if you need anything.") : L.T("感谢来电，再见。", "Thank you for calling. Goodbye.")).SetEndAction(() =>
		{
			if (hungUp || !CallCurrent()) return;
			hungUp = true;
			try
			{
				PhoneUIManager.Instance?.OnPhoneDialogEnd();
			}
			catch
			{
			}
		});
		dialogue2.AddChoice(Compat.ChoiceIcon(), browse, "", dialogue3);
		dialogue2.AddChoice(Compat.ChoiceIcon(), L.T("挂断", "Hang up"), "", dialogue4);
		return dialogue2;
	}

	public static Action GuardCall(Company c, Action action)
	{
		StoreSession session = StoreSession.Capture();
		long generation = _callGeneration;
		bool done = false;
		return () =>
		{
			if (done || generation != _callGeneration || !session.IsCurrent || !Owns(PhoneUIManager.Instance?.currentPhoneClient, c)) return;
			done = true;
			action();
		};
	}
}

