using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CorpPresence;

internal static class Engine
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

	[ThreadStatic]
	private static bool _scheduling;

	public static Character Find(string id)
	{
		Character[] all = All;
		foreach (Character character in all)
		{
			if (character.Id == id)
			{
				return character;
			}
		}
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

	private static StoreClient Create(Character c, string kind)
	{
		CharacterState characterState = StateOf(c);
		bool flag = !kind.StartsWith("ch");
		Visit v = (flag ? c.Daily(characterState) : c.Chapter(int.Parse(kind.Substring(2))));
		StoreClient client = new StoreClient
		{
			identifier = c.Prefix + kind,
			displayName = c.Name,
			clientFaction = c.Faction,
			spriteName = Portraits.Ensure(c.Portrait(characterState, kind), c.PortraitRef),
			clientIntent = (v.TalkOnly ? StoreClient.ClientIntent.DIALOGUE : (v.Both ? StoreClient.ClientIntent.SELLNBUY : (v.Buy ? StoreClient.ClientIntent.BUY : StoreClient.ClientIntent.SELL))),
			dismissable = true
		};
		client.AddBasicDialog();
		if (v.Buy || v.Both)
		{
			StoreClient storeClient = client;
			AddAll(storeClient.clientBuyingTagList ?? (storeClient.clientBuyingTagList = new List<string>()), c.WantTags(flag));
			storeClient = client;
			AddAll(storeClient.clientBuyingIdList ?? (storeClient.clientBuyingIdList = new List<string>()), c.WantIds(flag));
			storeClient = client;
			AddAll(storeClient.clientBlackIdList ?? (storeClient.clientBlackIdList = new List<string>()), c.RefuseIds);
		}
		client.CompleteClientCreation();
		if (v.Buy || v.Both)
		{
			client.SetBudget(Settings.AgentBudget);
			client.willAcceptItem = (flag ? new Func<GameItem, bool>(c.DailyAccepts) : new Func<GameItem, bool>(c.Accepts));
			client.placedWrongItemWhenSellingToDialogue = new Dialogue().SetText(c.Name, v.Wrong);
		}
		if (!v.TalkOnly && !v.Buy)
		{
			client.SetWholesale(v.Discount);
		}
		Dialogue dialogue = new Dialogue().SetText(c.Name, v.Lines[0]);
		Dialogue dialogue2 = dialogue;
		for (int i = 1; i < v.Lines.Length; i++)
		{
			dialogue2 = dialogue2.NextDialogue().SetText(c.Name, v.Lines[i]);
		}
		CharacterVisitSession visitSession = new CharacterVisitSession(c.Id, characterState, client);
		bool done = false;
		Action startTrade = () =>
		{
			if (!(!visitSession.IsCurrent | done))
			{
				if (!done)
				{
					done = true;
					try
					{
						if (!PlaceVisitGifts(c, v.Gift, visitSession))
						{
							done = false;
							return;
						}
					}
					catch (Exception ex)
					{
						done = false;
						WarnVisit("赠品登记失败，保留人物预约：" + ex.Message);
						return;
					}
					try
					{
						long num2 = 0L;
						string[] stock = v.Stock;
						foreach (string id in stock)
						{
							if (!visitSession.IsCurrent)
							{
								return;
							}
							num2 += Place(c, id, owned: false);
						}
						if (!visitSession.IsCurrent)
						{
							return;
						}
						if (v.Extra.Length != 0)
						{
							TopUp(c, v.Extra, num2, v.Stock.Length);
						}
						if (!visitSession.IsCurrent)
						{
							return;
						}
						if (v.Buy || v.Both)
						{
							BuyActive = true;
						}
					}
					catch (Exception ex2)
					{
						WarnVisit("摆货失败：" + ex2.Message);
					}
					if (!visitSession.IsCurrent)
					{
						return;
					}
					MarkVisited(c, kind);
					try
					{
						if (kind.StartsWith("ch"))
						{
							Services.OnChapterDone(c.Id);
						}
					}
					catch (Exception ex3)
					{
						WarnVisit("公司记录失败：" + ex3.Message);
					}
					try
					{
						Services.MaybeGiveCard(c.Id);
					}
					catch (Exception ex4)
					{
						WarnVisit("名片失败：" + ex4.Message);
					}
				}
				if (visitSession.IsCurrent && !v.TalkOnly)
				{
					client.OnIntroduced();
				}
			}
		};
		Action endAction = () =>
		{
			startTrade();
			if (!done || !visitSession.TryFinish())
			{
				return;
			}
			try
			{
				UnityEngine.Object.FindObjectOfType<DialogUIManager>()?.CurrentClientLeave();
			}
			catch (Exception ex)
			{
				WarnVisit("离开失败：" + ex.Message);
			}
		};
		bool flag2 = !string.IsNullOrEmpty(v.Ask) && v.Choices.Length != 0;
		if (flag2)
		{
			Dialogue dialogue3 = dialogue2.NextDialogue().SetText(c.Name, v.Ask);
			bool choiceMade = false;
			Choice[] choices = v.Choices;
			foreach (Choice choice in choices)
			{
				Action then = choice.Then;
				Dialogue dialogue4 = new Dialogue().SetText("PLAYER", choice.Label);
				Dialogue dialogue5 = dialogue4.NextDialogue().SetText(c.Name, choice.Reply);
				if (v.TalkOnly)
				{
					dialogue5.NextDialogue().SetText(c.Name, v.Exit).SetEndAction(endAction);
				}
				else
				{
					dialogue5.SetEndAction(() =>
					{
						startTrade();
					});
				}
				dialogue3.AddChoice(Compat.ChoiceIcon(), choice.Label, "", dialogue4, () =>
				{
					if (!visitSession.IsCurrent | choiceMade | done)
					{
						return;
					}
					choiceMade = true;
					try
					{
						then?.Invoke();
						LogVisit(c.Name + "：玩家选了“" + choice.Label + "”");
					}
					catch (Exception ex)
					{
						WarnVisit("选项处理失败：" + ex.Message);
					}
				});
			}
		}
		else
		{
			dialogue2.SetEndAction(startTrade);
		}
		if (v.TalkOnly && !flag2)
		{
			dialogue2.NextDialogue().SetText(c.Name, v.Exit).SetEndAction(endAction);
		}
		client.mainDialogue = dialogue;
		if (!v.Both)
		{
			client.acceptDealDialogue = Exit(c, v.Exit, visitSession);
		}
		client.allDoneDialogue = Exit(c, v.Exit, visitSession);
		return client;
	}

	private static Dialogue Exit(Character c, string line, CharacterVisitSession visitSession)
	{
		return new Dialogue().SetText(c.Name, line).SetEndAction(() =>
		{
			if (!visitSession.TryFinish())
			{
				return;
			}
			BuyActive = false;
			try
			{
				SaveSlot.CurrentStore.RemoveNotOwnedItemFromTable();
			}
			catch (Exception ex)
			{
				WarnVisit("收回货物失败：" + ex.Message);
			}
			try
			{
				Services.MaybeGiveCard(c.Id);
			}
			catch (Exception ex2)
			{
				WarnVisit("名片失败：" + ex2.Message);
			}
			try
			{
				UnityEngine.Object.FindObjectOfType<DialogUIManager>()?.CurrentClientLeave();
			}
			catch (Exception ex3)
			{
				WarnVisit("离开失败：" + ex3.Message);
			}
		});
	}

	private static void MarkVisited(Character c, string kind)
	{
		CharacterState characterState = StateOf(c);
		if (characterState == null)
		{
			return;
		}
		if (kind.StartsWith("ch") && int.TryParse(kind.Substring(2), out var result))
		{
			if (c.IsSide(result))
			{
				if (!characterState.SideDone.Contains(result))
				{
					characterState.SideDone.Add(result);
				}
			}
			else if (result > characterState.Chapter)
			{
				characterState.Chapter = result;
			}
			characterState.LastStoryDay = StoreStation.GetDayCounter();
		}
		if (kind.StartsWith("daily"))
		{
			characterState.DailyCount++;
		}
		characterState.LastVisitDay = StoreStation.GetDayCounter();
		if (c.HasDaily && characterState.Chapter >= 1)
		{
			characterState.NextDailyDay = characterState.LastVisitDay + c.NextDailyGap();
		}
		if (characterState.PendingKind == kind)
		{
			characterState.PendingDay = -1;
			characterState.PendingKind = null;
		}
		LogVisit($"{c.Name}来访完成：{kind}，进度第 {characterState.Chapter} 章");
	}

	private static void AddAll(List<string> list, string[] values)
	{
		foreach (string item in values)
		{
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
	}

	public static void OnDayStart()
	{
		BuyActive = false;
		if (!GameResources.Ready || SaveSlot.CurrentStore == null || !SaveSlot.CanWrite("CorpPresence"))
		{
			return;
		}
		CorpState corpState = CorpState.Get();
		if (corpState == null)
		{
			return;
		}
		int dayCounter = StoreStation.GetDayCounter();
		StoreSession storeSession = StoreSession.Capture();
		Character[] all = All;
		foreach (Character character in all)
		{
			if (!storeSession.IsCurrent || !SaveSlot.CanWrite("CorpPresence"))
			{
				break;
			}
			CharacterState characterState = corpState.Of(character.Id);
			if (characterState.PendingDay >= 0 && characterState.PendingDay < dayCounter)
			{
				characterState.PendingDay = dayCounter;
			}
		}
	}

	public static void OnDayEnd()
	{
		if (!GameResources.Ready || SaveSlot.CurrentStore == null || !SaveSlot.CanWrite("CorpPresence"))
		{
			return;
		}
		CorpState corpState = CorpState.Get();
		if (corpState == null)
		{
			return;
		}
		int dayCounter = StoreStation.GetDayCounter();
		int cash = SaveSlot.CurrentStore.GetCash();
		StoreSession storeSession = StoreSession.Capture();
		Character[] all = All;
		foreach (Character character in all)
		{
			if (!storeSession.IsCurrent || !SaveSlot.CanWrite("CorpPresence"))
			{
				return;
			}
			try
			{
				ScheduleNext(character, corpState.Of(character.Id), dayCounter, cash);
			}
			catch (Exception ex)
			{
				WarnVisit(character.Name + "预约来访失败：" + ex.Message);
			}
		}
		if (!storeSession.IsCurrent || !SaveSlot.CanWrite("CorpPresence"))
		{
			return;
		}
		try
		{
			ApplyDailyCap(corpState, dayCounter + 1);
		}
		catch (Exception ex2)
		{
			WarnVisit("每日人数上限处理失败：" + ex2.Message);
		}
	}

	private static void ApplyDailyCap(CorpState state, int tomorrow)
	{
		int dailyCap = Settings.DailyCap;
		if (dailyCap <= 0)
		{
			return;
		}
		foreach (var item in (from c in All
			select (c: c, s: state.Of(c.Id)) into x
			where x.s.PendingDay == tomorrow
			orderby (!(x.s.PendingKind?.StartsWith("ch") ?? false)) ? 1 : 0, x.s.LastVisitDay
			select x).ToList().Skip(dailyCap))
		{
			var (character, _) = item;
			item.s.PendingDay = tomorrow + 1;
			LogVisit($"{character.Name}的来访顺延到第 {tomorrow + 1} 天（每天最多 {dailyCap} 位）");
		}
	}

	private static void ScheduleNext(Character c, CharacterState s, int day, int cash)
	{
		if (s.PendingDay >= 0 && s.PendingDay <= day)
		{
			s.PendingDay = day + 1;
		}
		if (s.PendingDay >= 0)
		{
			string pendingKind = s.PendingKind;
			if (pendingKind != null && pendingKind.StartsWith("ch"))
			{
				return;
			}
		}
		string text = (((long)day + 1L - s.LastStoryDay >= Settings.StoryGap(c)) ? DecideStory(c, s, cash) : null);
		if (text != null)
		{
			Book(c, s, text, day + 1, cash);
		}
		else if (s.PendingDay < 0 && c.HasDaily && s.Chapter >= 1)
		{
			if (s.NextDailyDay <= 0)
			{
				s.NextDailyDay = ((s.LastVisitDay >= 0) ? s.LastVisitDay : day) + c.NextDailyGap();
			}
			if (s.NextDailyDay <= day + 1 && (c.DailyBuyer(s) || cash > c.CashNeeded))
			{
				Book(c, s, "daily", day + 1, cash);
			}
		}
	}

	private static void Book(Character c, CharacterState s, string kind, int day, int cash)
	{
		s.PendingDay = day;
		s.PendingKind = kind;
		s.QueuedDay = -1;
		LogVisit($"{c.Name}将于第 {day} 天来访（{kind}；声望 {c.Rep()}，现金 {cash}）");
	}

	private static string DecideStory(Character c, CharacterState s, int cash)
	{
		int num = c.Rep();
		int num2 = c.NextMain(s);
		if (num2 > 0 && num >= c.Thresholds[num2 - 1] && Ready(c, num2, cash))
		{
			return "ch" + num2;
		}
		for (int i = 1; i <= c.Chapters; i++)
		{
			if (SideReady(c, s, i, num, cash))
			{
				return "ch" + i;
			}
		}
		return null;
	}

	private static bool Ready(Character c, int ch, int cash)
	{
		if (!c.IsBuyChapter(ch))
		{
			return cash > c.CashFor(ch);
		}
		return c.BuyGoodsHeld() >= c.BuyGoodsNeeded;
	}

	private static bool SideReady(Character c, CharacterState s, int ch, int rep, int cash)
	{
		if (c.IsSide(ch) && !s.SideDone.Contains(ch) && s.Chapter >= c.SideAfter(ch) && rep >= c.Thresholds[ch - 1] && c.SideAllowed(ch))
		{
			return Ready(c, ch, cash);
		}
		return false;
	}

	private static void WarnVisit(string text)
	{
		try
		{
			ModLog.Warn(text);
		}
		catch
		{
		}
	}

	private static void LogVisit(string text)
	{
		try
		{
			ModLog.Info(text);
		}
		catch
		{
		}
	}

	public static void Schedule(StoreClientManager manager)
	{
		if (_scheduling || !GameResources.Ready || manager?.clientStack == null || SaveSlot.CurrentStore == null || !SaveSlot.CanWrite("CorpPresence"))
		{
			return;
		}
		_scheduling = true;
		try
		{
			StoreSession storeSession = StoreSession.Capture();
			CorpState current = CorpState.Current;
			if (!storeSession.IsCurrent || current == null)
			{
				return;
			}
			try
			{
				CompanyVisitors.Schedule(manager);
			}
			catch (Exception ex)
			{
				WarnVisit("公司访客安排失败：" + ex.Message);
			}
			int dayCounter = StoreStation.GetDayCounter();
			Character[] all = All;
			foreach (Character character in all)
			{
				if (!storeSession.IsCurrent || !SaveSlot.CanWrite("CorpPresence"))
				{
					return;
				}
				CharacterState characterState = null;
				try
				{
					characterState = current.Of(character.Id);
					if (characterState.PendingDay < 0 || characterState.PendingDay > dayCounter || characterState.QueuedDay == dayCounter)
					{
						continue;
					}
					string prefix = character.Prefix;
					StoreClient storeClient = SaveSlot.CurrentStore?.currentClientInstance?.GetClientBlueprint();
					bool flag = storeClient != null && storeClient.identifier?.StartsWith(prefix, StringComparison.Ordinal) == true;
					if (!flag)
					{
						foreach (StoreClient item in manager.clientStack)
						{
							if (item != null && item.identifier?.StartsWith(prefix, StringComparison.Ordinal) == true)
							{
								flag = true;
								break;
							}
						}
					}
					if (flag)
					{
						characterState.QueuedDay = dayCounter;
					}
					else if (ServiceRetry.Ready(characterState, Time.unscaledTime))
					{
						StoreClient storeClient2 = Create(character, characterState.PendingKind);
						if (storeClient2 == null)
						{
							throw new InvalidOperationException("人物工厂未返回顾客");
						}
						if (!storeSession.IsCurrent)
						{
							return;
						}
						int count = manager.clientStack.Count;
						int num = ((count > 1) ? new System.Random().Next(0, count / 2 + 1) : 0);
						manager.clientStack.Insert(num, storeClient2);
						characterState.PendingDay = dayCounter;
						characterState.QueuedDay = dayCounter;
						ServiceRetry.Succeeded(characterState);
						LogVisit($"{character.Name}（{characterState.PendingKind}）已排进今天的顾客队列，第 {num + 1}/{count + 1} 位");
					}
				}
				catch (Exception ex2)
				{
					if (characterState != null)
					{
						ServiceRetry.Failed(characterState, Time.unscaledTime);
					}
					WarnVisit(character.Name + "入队失败，保留预约：" + ex2.Message);
				}
			}
			if (storeSession.IsCurrent && Ext.ScheduleExtra != null)
			{
				Action<StoreClientManager> extra = Ext.ScheduleExtra;
				Ext.Fire("ScheduleExtra", () =>
				{
					extra(manager);
				});
			}
		}
		finally
		{
			_scheduling = false;
		}
	}

	private static StoreClient CurrentBuyer(GameItem item)
	{
		if (!BuyActive || !GameResources.Ready || !Owned(item))
		{
			return null;
		}
		StoreClient storeClient = SaveSlot.CurrentStore?.currentClientInstance?.GetClientBlueprint();
		if (storeClient == null || storeClient.identifier?.StartsWith("corp_", StringComparison.Ordinal) != true || (storeClient.clientIntent != StoreClient.ClientIntent.BUY && storeClient.clientIntent != StoreClient.ClientIntent.SELLNBUY))
		{
			return null;
		}
		return storeClient;
	}

	private static Character FindBuyer(StoreClient client)
	{
		Character[] all = All;
		foreach (Character character in all)
		{
			if (client.identifier.StartsWith(character.Prefix, StringComparison.Ordinal))
			{
				return character;
			}
		}
		return null;
	}

	private static bool AcceptsVisit(Character character, GameItem item, string kind)
	{
		if (!kind.StartsWith("daily", StringComparison.Ordinal))
		{
			return character.Accepts(item);
		}
		return character.DailyAccepts(item);
	}

	public static long? BuyFixedPrice(GameItem item)
	{
		StoreClient storeClient = CurrentBuyer(item);
		if (storeClient == null)
		{
			return null;
		}
		Character character = FindBuyer(storeClient);
		if (character == null)
		{
			return null;
		}
		string kind = storeClient.identifier.Substring(character.Prefix.Length);
		if (!AcceptsVisit(character, item, kind))
		{
			return null;
		}
		return character.FixedPrice(item, kind);
	}

	public static double BuyMultiplier(GameItem item)
	{
		StoreClient storeClient = CurrentBuyer(item);
		if (storeClient == null)
		{
			return 1.0;
		}
		if (storeClient.identifier.StartsWith("corp_solana_lab_", StringComparison.Ordinal))
		{
			return CompanyVisitors.LabMultiplier(storeClient, item);
		}
		Character character = FindBuyer(storeClient);
		if (character == null)
		{
			return 1.0;
		}
		string kind = storeClient.identifier.Substring(character.Prefix.Length);
		if (!AcceptsVisit(character, item, kind))
		{
			return 1.0;
		}
		return character.PriceMultiplier(item, kind);
	}

	private static bool Owned(GameItem item)
	{
		try
		{
			return item?.IsTag("IS_OWNED_TAG") ?? false;
		}
		catch
		{
			return false;
		}
	}

	private static long Place(Character c, string id, bool owned)
	{
		TryPlace(c, id, owned, out var value);
		return value;
	}

	private static bool TryPlace(Character c, string id, bool owned, out long value)
	{
		value = 0L;
		StoreSession storeSession = StoreSession.Capture();
		PlayerStore currentStore = SaveSlot.CurrentStore;
		if (!storeSession.IsCurrent || !SaveSlot.CanWrite("CorpPresence") || CompanyVisitors.CounterFull())
		{
			return false;
		}
		GameItem gameItem = null;
		try
		{
			gameItem = c.MakeItem(id);
			if (gameItem == null || !storeSession.IsCurrent)
			{
				return false;
			}
			bool flag;
			try
			{
				flag = currentStore.TryAddDirectSellingItemToTable(gameItem, owned) || gameItem.parentInventory != null;
			}
			catch (Exception ex)
			{
				WarnVisit(c.Name + "摆货异常：" + ex.Message);
				flag = gameItem.parentInventory != null;
			}
			if (!flag)
			{
				return false;
			}
			try
			{
				value = gameItem.GetCurrentValue(useRetailMarkup: false, withChild: true, includeEvents: false, forceMarkup: false, 0L);
			}
			catch
			{
			}
			return true;
		}
		catch (Exception ex2)
		{
			WarnVisit(c.Name + "摆货失败：" + ex2.Message);
			return false;
		}
		finally
		{
			if (gameItem != null && gameItem.parentInventory == null)
			{
				try
				{
					gameItem.Destroy();
				}
				catch (Exception ex3)
				{
					WarnVisit("未放置物品回收失败：" + ex3.Message);
				}
			}
		}
	}

	private static bool PlaceVisitGifts(Character c, string[] gifts, CharacterVisitSession visit)
	{
		if (gifts.Length == 0)
		{
			return true;
		}
		if (!visit.IsCurrent)
		{
			return false;
		}
		Company company = Companies.ByCharacter(c.Id);
		CompanyState companyState = ((company == null) ? null : Companies.StateOf(company));
		if (companyState?.Orders == null)
		{
			return false;
		}
		Order order = new Order
		{
			Day = StoreStation.GetDayCounter() + 1,
			Items = new List<string>(gifts)
		};
		companyState.Orders.Add(order);
		foreach (string text in gifts)
		{
			if (!visit.IsCurrent)
			{
				return false;
			}
			if (TryPlace(c, text, owned: true, out var _))
			{
				order.Items.Remove(text);
			}
		}
		if (order.Items.Count == 0)
		{
			companyState.Orders.Remove(order);
		}
		else
		{
			LogVisit(c.Name + "的未领取赠品保留为配送订单");
		}
		return true;
	}

	private static void TopUp(Character c, string[] pool, long total, int count)
	{
		StoreSession storeSession = StoreSession.Capture();
		PlayerStore currentStore = SaveSlot.CurrentStore;
		if (!storeSession.IsCurrent || currentStore == null || pool == null || pool.Length == 0)
		{
			return;
		}
		int cash = currentStore.GetCash();
		long num = Math.Min(cash / 4, 4000);
		int num2 = Math.Clamp(3 + cash / 1000, 3, 10) - count;
		if (num2 <= 0 || total >= num || CompanyVisitors.CounterFull())
		{
			return;
		}
		List<(string, long)> list = new List<(string, long)>();
		foreach (string item2 in pool.Distinct())
		{
			if (!storeSession.IsCurrent)
			{
				return;
			}
			list.Add((item2, ValueOf(c, item2)));
		}
		System.Random random = new System.Random();
		while (storeSession.IsCurrent && num2 > 0 && total < num && !CompanyVisitors.CounterFull())
		{
			long num3 = (num - total) / num2;
			int num4 = -1;
			int num5 = -1;
			long num6 = long.MaxValue;
			long num7 = long.MaxValue;
			for (int i = 0; i < list.Count; i++)
			{
				long item = list[i].Item2;
				long num8 = ((item >= num3) ? (item - num3) : (num3 - item));
				if (num4 < 0 || num8 < num6)
				{
					num5 = num4;
					num7 = num6;
					num4 = i;
					num6 = num8;
				}
				else if (num5 < 0 || num8 < num7)
				{
					num5 = i;
					num7 = num8;
				}
			}
			int index = ((num5 < 0 || random.Next(2) == 0) ? num4 : num5);
			total += Place(c, list[index].Item1, owned: false);
			num2--;
		}
	}

	private static long ValueOf(Character c, string id)
	{
		GameItem gameItem = null;
		try
		{
			gameItem = c.MakeItem(id);
			return Math.Max(0L, gameItem?.GetCurrentValue(useRetailMarkup: false, withChild: true, includeEvents: false, forceMarkup: false, 0L) ?? 0);
		}
		catch
		{
			return 0L;
		}
		finally
		{
			if (gameItem != null && gameItem.parentInventory == null)
			{
				try
				{
					gameItem.Destroy();
				}
				catch
				{
				}
			}
		}
	}
}
