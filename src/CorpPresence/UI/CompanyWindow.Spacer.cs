using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.Malice;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{

	private static string SpacerTitle(int i)
	{
		return SpacerServices[i];
	}

	private static void DrawSpacer()
	{
		Theme theme = _theme;
		CompanyState state = State;
		float width = _rect.width;
		float height = _rect.height;
		Rect r = new Rect(20f, 8f, 560f, 90f);
		if (theme.Banner != null)
		{
			Ui.Image(r, theme.Banner);
			GUI.Label(new Rect(r.x + 120f, r.y + 16f, 420f, 58f), _company.Name, theme.Title);
		}
		else
		{
			Ui.Fill(new Rect(20f, 20f, 420f, 56f), theme.Ink);
			GUI.Label(new Rect(36f, 20f, 400f, 56f), _company.Name, theme.InvTitle);
		}
		GUI.Label(new Rect(600f, 40f, width - 640f, 22f), (L.En ? L.K("ui.spacer.overview.dayHeading", (System.FormattableString)$"{_company.PhoneNumber}   Day {Day}") : $"{_company.PhoneNumber}\u3000第 {Day} 天"), theme.Small);
		Ui.Fill(new Rect(20f, 100f, width - 40f, 2f), theme.Ink);
		GUI.Label(new Rect(24f, 106f, width - 48f, 22f), L.K("ui.spacer.overview.mentor", "导师：", "The Mentor: ") + _desk, theme.Body);
		Rect position = new Rect(20f, 136f, 190f, height - 156f);
		GUI.Box(position, GUIContent.none, theme.CellBox);
		GUI.Label(new Rect(position.x + 10f, position.y + 8f, 170f, 22f), (L.En ? L.K("ui.spacer.overview.node", "Node: ") + TierLabel(state) : "节点：" + TierLabel(state)), theme.Body);
		GUI.Label(new Rect(position.x + 10f, position.y + 32f, 170f, 20f), (L.En ? L.K("ui.spacer.overview.trust", (System.FormattableString)$"Trust {state.Coop}") : $"信任度 {state.Coop}"), theme.Small);
		CoopBar(new Rect(position.x + 10f, position.y + 54f, 170f, 8f));
		GUI.Label(new Rect(position.x + 10f, position.y + 64f, 170f, 20f), NextTierHint(), theme.Small);
		float num = position.y + 96f;
		for (int i = 0; i < SpacerModuleCount; i++)
		{
			string text = ((i == _module) ? "› " : "  ") + SpacerTitle(i);
			if (Ui.Button(new Rect(position.x + 6f, num, position.width - 12f, 28f), text, (i == _module) ? theme.NavOnStyle : theme.NavStyle))
			{
				_module = i;
			}
			num += 32f;
		}
		Rect rect = new Rect(position.xMax + 20f, 136f, width - position.xMax - 40f, height - 156f);
		GUI.Label(new Rect(rect.x, rect.y, rect.width, 30f), SpacerTitle(_module), theme.HotTitle);
		Ui.Fill(new Rect(rect.x, rect.y + 34f, rect.width, 1f), theme.Ink);
		Rect rect2 = new Rect(rect.x, rect.y + 44f, rect.width, rect.height - 44f);
		if (!DrawTermsIfSelected(SpacerPages.Length, rect2))
		{
			if (_company.Tier(state) < 1)
			{
				GUI.Label(new Rect(rect2.x, rect2.y, rect2.width, 40f), L.K("ui.spacer.overview.nobodyKnows", "没人认识你。", "Nobody knows you."), theme.Wrap);
			}
			else if (SpacerPages[_module] == "阵线" && _company.Tier(state) < 2)
			{
				GUI.Label(new Rect(rect2.x, rect2.y, rect2.width, 40f), (L.En ? L.K("ui.spacer.overview.tierRequired", (System.FormattableString)$"Only at {_company.TierNames[1]}.") : _company.TierNames[1] + "才有。"), theme.Wrap);
			}
			else if (SpacerPages[_module] == "目录")
			{
				DrawBrand(rect2);
			}
			else if (SpacerPages[_module] != null)
			{
				DrawSpacerOrder(rect2, SpacerPages[_module]);
			}
			else
			{
				DrawSpacerMutual(rect2);
			}
		}
	}

	private static int SpacerUnitPrice(CatalogEntry e) => _company.OrderUnitPrice(e, State);

	private static void DrawSpacerOrder(Rect r, string page)
	{
		Theme theme = _theme;
		int num = _company.CatalogTier(State);
		bool flag = Services.CanOrderToday(_company);
		string text = page switch
		{
			"阵线" => L.K("ui.spacer.orders.madeBySeveralOutfitsTogether", "几家一起出的。", "Made by several outfits together."), 
			"药房" => L.K("ui.spacer.orders.noLicense", "没有许可证。", "No license."), 
			"军械" => L.K("ui.spacer.orders.conditionNotGuaranteed", "成色不保证。", "Condition not guaranteed."), 
			_ => L.K("ui.spacer.orders.freighterSurplusPlenty", "货船余货，量大。", "Freighter surplus. Plenty of it."), 
		};
		GUI.Label(new Rect(r.x, r.y, r.width, 20f), (flag ? L.K("ui.spacer.orders.orderTodayHereTomorrow", "今天下单，明天到。", "Order today, here tomorrow. ") : L.K("ui.spacer.orders.alreadyOrderedToday", "今天已经下过单了。", "Already ordered today. ")) + text, theme.Small);
		float num4 = r.y + 26f;
		Rect header = new Rect(r.x, num4, r.width, 24f);
		Ui.Fill(header, theme.Accent2);
		DrawOrderHeader(header, theme.InvBody, theme.InvCenter);
		num4 += 28f;
		IEnumerable<CatalogEntry> source;
		if (!(_company is RebelCo))
		{
			source = SpacerCo.PageEntries(page);
		}
		else
		{
			IEnumerable<CatalogEntry> entries = RebelCo.Entries;
			source = entries;
		}
		List<CatalogEntry> list = source.ToList();
		float num5 = 52 + ((page == "阵线") ? 176 : 0);
		int num6 = Math.Max(1, (int)((r.yMax - num5 - num4) / 33f));
		if (list.Count > num6)
		{
			num6 = Math.Max(1, (int)((r.yMax - num5 - 40f - num4) / 33f));
		}
		int num7 = (list.Count + num6 - 1) / num6;
		if (Orders.PageKey != _company.Id + page)
		{
			Orders.PageKey = _company.Id + page;
			Orders.Page = 0;
		}
		Orders.Page = Math.Max(0, Math.Min(Orders.Page, num7 - 1));
		if (num7 > 1)
		{
			float num8 = r.xMax - 150f;
			float y = r.yMax - 84f;
			if (Ui.Button(new Rect(num8, y, 34f, 30f), "‹", theme.BtnStyle) && Orders.Page > 0)
			{
				Orders.Page--;
			}
			GUI.Label(new Rect(num8 + 38f, y, 70f, 30f), $"{Orders.Page + 1} / {num7}", theme.Center);
			if (Ui.Button(new Rect(num8 + 112f, y, 34f, 30f), "›", theme.BtnStyle) && Orders.Page < num7 - 1)
			{
				Orders.Page++;
			}
		}
		var prices = new Dictionary<CatalogEntry, int>();
		int QuotedUnitPrice(CatalogEntry item)
		{
			if (!prices.TryGetValue(item, out int price))
				prices[item] = price = SpacerUnitPrice(item);
			return price;
		}
		foreach (CatalogEntry item in list.Skip(Orders.Page * num6).Take(num6))
		{
			Ui.Fill(new Rect(r.x, num4 + 31f, r.width, 1f), theme.Line);
			var columns = OrderColumns(new Rect(r.x, num4, r.width, 32f));
			Rect status = new Rect(columns.Unit.x, num4, columns.ListPrice.x - columns.Unit.x, 32f);
			DrawOrderItem(columns.Item, item, State, theme);
			GUI.Label(columns.ListPrice, ItemListPrice(ItemListValue(item)), theme.Center);
			string entryError = _company.OrderEntryError(item, State);
			if (entryError != null)
			{
				GUI.Label(status, entryError, theme.Small);
				Orders.Cart.Remove(item);
				num4 += 33f;
				continue;
			}
			int num9 = QuotedUnitPrice(item);
			if (num9 <= 0)
			{
				GUI.Label(status, L.K("ui.spacer.orders.unavailable", "暂不可用", "Unavailable"), theme.Small);
				Orders.Cart.Remove(item);
				num4 += 33f;
				continue;
			}
			GUI.Label(columns.Unit, $"{num9}₵", theme.Price);
			Orders.Cart.TryGetValue(item, out var value);
			value = Ui.Qty(columns.Quantity, value, Math.Min(10, Services.Left(State, item)), theme.BtnStyle, theme.Center);
			if (value > 0)
			{
				Orders.Cart[item] = value;
			}
			else
			{
				Orders.Cart.Remove(item);
			}
			GUI.Label(columns.Subtotal, (value > 0) ? $"{(long)num9 * value}₵" : "—", theme.Center);
			num4 += 33f;
		}
		if (page == "阵线")
		{
			DrawSpacerCredits(new Rect(r.x, num4 + 16f, r.width, 160f));
		}
		int num10 = 0;
		bool validTotal = Orders.Cart.All(kv => OrderAmounts.TryAdd(ref num10, QuotedUnitPrice(kv.Key), kv.Value, kv.Key.Bundle, out _));
		float num11 = r.yMax - 44f;
		GUI.Label(new Rect(r.x, num11, 260f, 22f), !validTotal ? "—" : (L.En ? L.K("ui.spacer.orders.totalAllPages", (System.FormattableString)$"Total {num10}₵ (all pages)") : $"合计 {num10}₵（各页合计）"), theme.Price);
		int cashDue = validTotal ? DrawCheckoutBalance(new Rect(r.x, num11 + 22f, r.width - 150f, 20f), num10) : 0;
		bool enabled2 = GUI.enabled;
		GUI.enabled = enabled2 && validTotal && num10 > 0 && Services.CanAfford(cashDue) && flag;
		if (Ui.Button(new Rect(r.xMax - 150f, num11 + 2f, 140f, 36f), L.K("ui.spacer.orders.order", "下单", "Order"), theme.BtnHotStyle))
		{
			string text2 = Services.PlaceOrder(_company, Orders.Cart, QuotedUnitPrice);
			if (text2 == null)
			{
				Orders.Cart.Clear();
				Toast(_company.FrontDeskDone);
			}
			else
			{
				Toast(text2);
			}
		}
		GUI.enabled = enabled2;
	}

	private static void DrawSpacerCredits(Rect r)
	{
		Theme theme = _theme;
		GUI.Box(r, GUIContent.none, theme.CellBox);
		GUI.Label(new Rect(r.x + 12f, r.y + 4f, r.width - 24f, 22f), L.K("ui.spacer.credits.techCredits", "技术来源", "Tech credits"), theme.Small);
		float num = r.y + 28f;
		(string, string)[] techCredits = SpacerCo.TechCredits;
		for (int i = 0; i < techCredits.Length; i++)
		{
			var (text, text2) = techCredits[i];
			GUI.Label(new Rect(r.x + 16f, num, 180f, 24f), text, theme.Body);
			GUI.Label(new Rect(r.x + 200f, num, r.width - 216f, 24f), text2, theme.Body);
			num += 26f;
		}
	}

	private static void DrawSpacerMutual(Rect r)
	{
		Theme theme = _theme;
		CompanyState state = State;
		if (_company.Tier(state) < 2)
		{
			GUI.Label(new Rect(r.x, r.y, r.width, 40f), (L.En ? L.K("ui.spacer.mutualAid.tierRequired", (System.FormattableString)$"Only at {_company.TierNames[1]}.") : _company.TierNames[1] + "才有。"), theme.Wrap);
			return;
		}
		GUI.Label(new Rect(r.x, r.y, r.width, 90f), (L.En ? L.K("ui.spacer.mutualAid.spacersDocksAreShortFood", (System.FormattableString)$"Spacers at the docks are short on food, water and medicine. With this on, the Alliance points people your way every night: the next day, 1–2 extra Spacers come by to buy whatever they need.\n\n{SpacerCo.MutualFee}₵ per night. Stops automatically when you are short on cash.") : $"码头上的星际客缺吃、缺水、缺药。开着这个，联盟每晚把人往你这儿指：第二天多来 1–2 位星际客，缺什么买什么。\n\n每晚 {SpacerCo.MutualFee}₵。现金不够时自动停。"), theme.Wrap);
		if (Ui.Button(new Rect(r.x, r.y + 110f, 200f, 34f), state.TrafficOn ? L.K("ui.spacer.mutualAid.clickTurnOff", "开着（点击关掉）", "On (click to turn off)") : L.K("ui.spacer.mutualAid.enable", "开", "Turn on"), state.TrafficOn ? theme.BtnStyle : theme.BtnHotStyle))
		{
			state.TrafficOn = !state.TrafficOn;
			ModLog.Info("街坊互助：" + (state.TrafficOn ? "开启" : "关闭"));
			Toast(state.TrafficOn ? L.K("ui.spacer.mutualAid.startingTonight", "今晚开始。", "Starting tonight.") : L.K("ui.spacer.mutualAid.stopped", "停了。", "Stopped."));
		}
	}

}

