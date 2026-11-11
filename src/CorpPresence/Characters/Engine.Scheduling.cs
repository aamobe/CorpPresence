using System;
using System.Collections.Generic;
using System.Linq;
namespace CorpPresence;
internal static partial class Engine
{
	public static void OnDayStart()
	{
		BuyActive = false;
		if (!GameResources.Ready || SaveSlot.CurrentStore == null || !SaveSlot.CanWrite("CorpPresence")) return;
		CorpState corpState = CorpState.Get();
		if (corpState == null)
		{
			return;
		}
		int dayCounter = StoreStation.GetDayCounter();
		StoreSession session = StoreSession.Capture();
		Character[] all = All;
		foreach (Character character in all)
		{
			if (!session.IsCurrent || !SaveSlot.CanWrite("CorpPresence")) return;
			CharacterState characterState = corpState.Of(character.Id);
			if (characterState.PendingDay >= 0 && characterState.PendingDay < dayCounter)
			{
				characterState.PendingDay = dayCounter;
			}
		}
	}

	public static void OnDayEnd()
	{
		if (!GameResources.Ready || SaveSlot.CurrentStore == null || !SaveSlot.CanWrite("CorpPresence")) return;
		CorpState corpState = CorpState.Get();
		if (corpState == null)
		{
			return;
		}
		int dayCounter = StoreStation.GetDayCounter();
		int cash = SaveSlot.CurrentStore.GetCash();
		StoreSession session = StoreSession.Capture();
		Character[] all = All;
		foreach (Character character in all)
		{
			if (!session.IsCurrent || !SaveSlot.CanWrite("CorpPresence")) return;
			try
			{
				ScheduleNext(character, corpState.Of(character.Id), dayCounter, cash);
			}
			catch (Exception ex)
			{
				WarnVisit(character.Name + "预约来访失败：" + ex.Message);
			}
		}
		if (!session.IsCurrent || !SaveSlot.CanWrite("CorpPresence")) return;
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
		string text = (((long)day + 1 - s.LastStoryDay >= Settings.StoryGap(c)) ? DecideStory(c, s, cash) : null);
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


	private static void WarnVisit(string text) { try { ModLog.Warn(text); } catch { } }
	private static void LogVisit(string text) { try { ModLog.Info(text); } catch { } }

	public static void Schedule(StoreClientManager manager)
	{
		if (_scheduling || !GameResources.Ready || manager?.clientStack == null || SaveSlot.CurrentStore == null || !SaveSlot.CanWrite("CorpPresence")) return;
		_scheduling = true;
		try
		{
			StoreSession session = StoreSession.Capture();
			CorpState root = CorpState.Current;
			if (!session.IsCurrent || root == null) return;
			try { CompanyVisitors.Schedule(manager); }
			catch (Exception error) { WarnVisit("公司访客安排失败：" + error.Message); }
			int day = StoreStation.GetDayCounter();
			foreach (Character character in All)
			{
				if (!session.IsCurrent || !SaveSlot.CanWrite("CorpPresence")) return;
				CharacterState state = null;
				try
				{
					state = root.Of(character.Id);
					// 今天排过了就不再排。未到店的预约和未完成的剧情可能仍然保留。
					if (state.PendingDay < 0 || state.PendingDay > day || state.QueuedDay == day) continue;
					string prefix = character.Prefix;
					StoreClient active = SaveSlot.CurrentStore?.currentClientInstance?.GetClientBlueprint();
					bool present = active?.identifier?.StartsWith(prefix, StringComparison.Ordinal) == true;
					if (!present)
						foreach (StoreClient queued in manager.clientStack)
							if (queued?.identifier?.StartsWith(prefix, StringComparison.Ordinal) == true) { present = true; break; }
					if (present) { state.QueuedDay = day; continue; }
					if (!ServiceRetry.Ready(state, UnityEngine.Time.unscaledTime)) continue;
					StoreClient client = Create(character, state.PendingKind);
					if (client == null) throw new InvalidOperationException("人物工厂未返回顾客");
					if (!session.IsCurrent) return;
					int count = manager.clientStack.Count;
					int index = count > 1 ? new System.Random().Next(0, count / 2 + 1) : 0;
					ModHelper.SetClientSource(client, Main.DisplayName);
					manager.clientStack.Insert(index, client);
					state.PendingDay = day;
					state.QueuedDay = day;
					ServiceRetry.Succeeded(state);
					LogVisit($"{character.Name}（{state.PendingKind}）已排进今天的顾客队列，第 {index + 1}/{count + 1} 位");
				}
				catch (Exception error)
				{
					if (state != null) ServiceRetry.Failed(state, UnityEngine.Time.unscaledTime);
					WarnVisit(character.Name + "入队失败，保留预约：" + error.Message);
				}
			}
			if (session.IsCurrent && Malice.Malice.IsInstalled)
			{
				Ext.Fire("ScheduleExtra", () =>
				{
					Malice.Envoy.Schedule(manager);
					Malice.RebelStory.Schedule(manager);
				});
			}
		}
		finally { _scheduling = false; }
	}

}
