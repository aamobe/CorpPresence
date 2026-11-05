using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.Malice;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{

	private static void DrawPastel()
	{
		Theme theme = _theme;
		_ = State;
		float width = _rect.width;
		Rect r = new Rect((width - 560f) / 2f, 10f, 560f, 90f);
		Banner(r);
		GUI.Label(new Rect(r.x + 140f, r.y + 20f, 324f, 50f), _company.Name, theme.Title);
		GUI.Label(new Rect(24f, 106f, width - 80f, 22f), L.T("前台：", "Front desk: ") + _desk, theme.Body);
		Rect position = new Rect(10f, 136f, 168f, 470f);
		GUI.Box(position, GUIContent.none, theme.CellBox);
		GUI.Label(new Rect(position.x + 10f, position.y + 6f, 150f, 22f), L.T("服务", "Services"), theme.Small);
		for (int i = 0; i < PastelModules.Length; i++)
		{
			bool flag = PastelUnlocked(i, out var _);
			Rect r2 = new Rect(position.x + 6f, position.y + 30f + (float)(i * 32), position.width - 12f, 28f);
			string text = ((i == _module) ? "› " : "  ") + PastelModules[i] + (flag ? "" : L.T("（未解锁）", " (locked)"));
			if (Ui.Button(r2, text, (i == _module) ? theme.NavOnStyle : theme.NavStyle))
			{
				_module = i;
			}
		}
		Rect rect = new Rect(186f, 136f, width - 196f, 470f);
		if (!PastelUnlocked(_module, out var why2))
		{
			GUI.Label(new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, 24f), PastelModules[_module], theme.Title);
			GUI.Label(new Rect(rect.x + 10f, rect.y + 50f, rect.width - 20f, 60f), why2, theme.Wrap);
		}
		else if (_module == 0)
		{
			DrawPastelOrder(rect);
		}
		else if (_module == 1)
		{
			DrawPastelDealer(rect);
		}
		else if (_module == 2)
		{
			DrawPastelTraffic(rect);
		}
		else if (!DrawExtraIfSelected(PastelBase.Length, rect))
		{
			DrawBrand(rect);
		}
		DrawPastelFooter(new Rect(10f, 616f, width - 20f, 170f));
	}

	private static bool PastelUnlocked(int module, out string why)
	{
		CompanyState state = State;
		int num = _company.Tier(state);
		why = null;
		if (module == 0 && num < 1)
		{
			why = L.T("签约后可以电话订货。和米洛多做几笔买卖吧。", "Sign with pastel to order by phone. Do a few more deals with Milo.");
		}
		if (module == 1 && num < 1)
		{
			why = L.T("签约后可以加盟成为帕斯泰尔的下层合作商。", "Sign with pastel to join as a Lower Levels dealer.");
		}
		if (module == 2 && !state.DealerJoined)
		{
			why = L.T("先成为下层合作商，帕斯泰尔才会把熟客介绍给你。", "Become a Lower Levels dealer first, and pastel will send its regulars your way.");
		}
		return why == null;
	}

	private static int PastelUnitPrice(CatalogEntry e)
	{
		double num = Company.OrderRate(_company, _company.Tier(State));
		if (e == PastelSpecial())
		{
			num *= 0.9;
		}
		return e.Quote(num);
	}

	private static CatalogEntry PastelSpecial()
	{
		int tier = _company.CatalogTier(State);
		CatalogEntry[] array = PastelCo.Entries.Where((CatalogEntry e) => e.Page == "日用" && e.MinTier <= tier).ToArray();
		if (array.Length != 0)
		{
			return array[Day * 31 % array.Length];
		}
		return null;
	}

	private static void DrawPastelOrder(Rect r)
	{
		Theme theme = _theme;
		CompanyState state = State;
		int num = _company.CatalogTier(state);
		bool flag = PastelCo.HemingwayOpen();
		if (Ui.Button(new Rect(r.x, r.y, 90f, 26f), L.T("日用", "Daily"), (_page == "日用") ? theme.BtnHotStyle : theme.BtnStyle))
		{
			_page = "日用";
		}
		if (Ui.Button(new Rect(r.x + 96f, r.y, 140f, 26f), flag ? L.T("海明威", "Hemingway") : L.T("海明威（未解锁）", "Hemingway (locked)"), (_page == "海明威") ? theme.BtnHotStyle : theme.BtnStyle))
		{
			if (flag)
			{
				_page = "海明威";
			}
			else
			{
				Toast(L.T("米洛调去海明威之后才有这一页", "This page opens once Milo transfers to Hemingway"));
			}
		}
		bool flag2 = PastelCo.CraftDone(1);
		if (Ui.Button(new Rect(r.x + 242f, r.y, 90f, 26f), flag2 ? L.T("工艺", "Craft") : "？？？", (_page == "工艺") ? theme.BtnHotStyle : theme.BtnStyle))
		{
			if (flag)
			{
				_page = "工艺";
			}
			else
			{
				Toast(L.T("米洛调去海明威之后才有这一页", "This page opens once Milo transfers to Hemingway"));
			}
		}
		if (_page == "海明威" || _page == "工艺")
		{
			Ui.Fill(new Rect(r.x, r.y + 32f, r.width, r.height - 32f), Tex.Hex(8730423));
			Ui.Fill(new Rect(r.x + 3f, r.y + 35f, r.width - 6f, 1f), Tex.Hex(12224851));
			Ui.Fill(new Rect(r.x + 3f, r.yMax - 4f, r.width - 6f, 1f), Tex.Hex(12224851));
		}
		bool flag3 = Services.CanOrderToday(_company);
		GUI.Label(new Rect(r.x + 340f, r.y, r.width - 340f, 26f), flag3 ? L.T("今天下单，明天送到", "Order today, delivered tomorrow") : L.T("今天已经下过单了", "Already ordered today"), theme.Small);
		CatalogEntry[] array = PastelCo.Entries.Where((CatalogEntry e) => e.Page == _page).ToArray();
		CatalogEntry catalogEntry = PastelSpecial();
		int num2 = (array.Length + 2) / 3;
		float num3 = (r.width - 18f) / 3f;
		float num4 = Math.Min(118f, (r.height - 40f - (float)(6 * (num2 - 1))) / (float)Math.Max(1, num2));
		for (int num5 = 0; num5 < array.Length; num5++)
		{
			CatalogEntry catalogEntry2 = array[num5];
			Rect rect = new Rect(r.x + 4f + (float)(num5 % 3) * (num3 + 5f), r.y + 40f + (float)(num5 / 3) * (num4 + 6f), num3, num4);
			bool flag4 = catalogEntry2.MinTier > num;
			bool flag5 = !PastelCo.CraftUnlocked(catalogEntry2);
			GUI.Box(rect, GUIContent.none, (catalogEntry2 == catalogEntry && !flag4) ? theme.CellOnBox : theme.CellBox);
			GUI.Label(new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 34f), flag5 ? "？？？" : catalogEntry2.Name(), theme.Wrap);
			if (flag5)
			{
				Cart.Remove(catalogEntry2);
				if (catalogEntry2.Id == "@packaging_machine" && PastelCo.BribePending)
				{
					PastelBribeCell(rect);
				}
				else
				{
					GUI.Label(new Rect(rect.x, rect.y + 44f, rect.width, 22f), "？？？", theme.Center);
				}
				continue;
			}
			if (flag4)
			{
				GUI.Label(new Rect(rect.x, rect.y + 44f, rect.width, 22f), (L.En ? "Unlocks at " + _company.TierNames[catalogEntry2.MinTier - 1] : _company.TierNames[catalogEntry2.MinTier - 1] + "解锁"), theme.Center);
				continue;
			}
			int num6 = PastelUnitPrice(catalogEntry2);
			if (num6 <= 0)
			{
				GUI.Label(new Rect(rect.x, rect.y + 44f, rect.width, 22f), L.T("暂不可用", "Unavailable"), theme.Center);
				Cart.Remove(catalogEntry2);
				continue;
			}
			GUI.Label(new Rect(rect.x, rect.y + 36f, rect.width, 22f), ((catalogEntry2 == catalogEntry) ? L.T("特价 ", "Deal ") : "") + $"{num6}₵", theme.Price);
			int num7 = Math.Min(10, Services.Left(state, catalogEntry2));
			if (catalogEntry2.Limit() > 0)
			{
				GUI.Label(new Rect(rect.x, rect.y + 54f, rect.width, 16f), (L.En ? $"{num7} left {Services.PeriodWord}" : $"{Services.PeriodWord}还剩 {num7}"), theme.Small);
			}
			Cart.TryGetValue(catalogEntry2, out var value);
			value = Ui.Qty(new Rect(rect.x + 6f, rect.yMax - 28f, rect.width - 12f, 24f), value, Math.Min(10, num7), theme.BtnStyle, theme.Center);
			if (value > 0)
			{
				Cart[catalogEntry2] = value;
			}
			else
			{
				Cart.Remove(catalogEntry2);
			}
		}
	}

	private static void PastelBribeCell(Rect cell)
	{
		Theme theme = _theme;
		GUI.Label(new Rect(cell.x + 6f, cell.y + 34f, cell.width - 12f, 22f), L.T("打点主管", "Pay off the supervisor"), theme.Center);
		bool enabled = GUI.enabled;
		GUI.enabled = Cash >= PastelCo.Bribe;
		if (Ui.Button(new Rect(cell.x + 8f, cell.yMax - 30f, cell.width - 16f, 26f), (L.En ? $"Pay {PastelCo.Bribe}₵" : $"补交 {PastelCo.Bribe}₵"), theme.BtnHotStyle))
		{
			Toast(PastelCo.PayBribe(deliver: true) ? L.T("分装机明天送到。", "The packaging machine arrives tomorrow.") : L.T("现金不够", "Not enough cash"));
		}
		GUI.enabled = enabled;
	}

	private static void DrawPastelDealer(Rect r)
	{
		Theme theme = _theme;
		CompanyState state = State;
		GUI.Label(new Rect(r.x + 10f, r.y + 4f, r.width - 20f, 30f), L.T("下层合作商", "Lower Dealer"), theme.Title);
		GUI.Label(new Rect(r.x + 10f, r.y + 40f, r.width - 20f, 120f), L.T("成为帕斯泰尔的特约经销点。\n\n", "Become a pastel partner outlet.\n\n") + (L.En ? $"· Regular rebate: every {PastelCo.RebatePeriod} days, {0.1:P0} of what you sold in pastel products, paid back by the courier.\n" : $"· 定期返点：每 {PastelCo.RebatePeriod} 天，把这段时间你卖出的帕斯泰尔产品按售价的 {0.1:P0} 返现，由送货员带来。\n") + L.T("· 解锁“下层客流”：帕斯泰尔会把下层区的熟客介绍到你店里。", "· Unlocks “Lower Foot Traffic”: pastel sends Lower Levels regulars to your shop."), theme.Wrap);
		if (!state.DealerJoined)
		{
			if (Ui.Button(new Rect(r.x + 10f, r.y + 180f, 220f, 34f), (L.En ? $"Join (one-time {PastelCo.DealerFee}₵)" : $"加盟（一次性 {PastelCo.DealerFee}₵）"), theme.BtnHotStyle))
			{
				Toast(Services.JoinDealer(_company) ?? L.T("欢迎加入帕斯泰尔！", "Welcome to pastel!"));
			}
		}
		else
		{
			int num = Day - state.DealerSinceDay;
			int num2 = PastelCo.RebatePeriod - num % PastelCo.RebatePeriod;
			GUI.Label(new Rect(r.x + 10f, r.y + 180f, r.width - 20f, 24f), (L.En ? $"Dealer for {num} days. Sold {state.WeekSales:0}₵ of pastel products this period." : $"已加盟 {num} 天。本期已卖出 {state.WeekSales:0}₵ 的帕斯泰尔产品。"), theme.Body);
			GUI.Label(new Rect(r.x + 10f, r.y + 208f, r.width - 20f, 24f), (L.En ? $"Expected rebate {state.WeekSales * 0.1:0}₵, settles in {num2} days." : $"预计返点 {state.WeekSales * 0.1:0}₵，{num2} 天后结算。"), theme.Body);
		}
	}

	private static void DrawPastelTraffic(Rect r)
	{
		Theme theme = _theme;
		CompanyState state = State;
		GUI.Label(new Rect(r.x + 10f, r.y + 4f, r.width - 20f, 30f), L.T("下层客流", "Lower Foot Traffic"), theme.Title);
		GUI.Label(new Rect(r.x + 10f, r.y + 40f, r.width - 20f, 100f), (L.En ? $"{PastelCo.TrafficFee}₵ per night. pastel sends Lower Levels regulars over: the next day, 1–2 extra Lower Levels customers come looking for food and daily goods.\n\nPauses automatically when you are short on cash." : $"每晚 {PastelCo.TrafficFee}₵。帕斯泰尔把下层区的熟客介绍过来：第二天会额外来 1–2 位想买食物、日用品的下层区顾客。\n\n现金不够时自动暂停。") + ((Cash < PastelCo.TrafficFee) ? (L.En ? $"\n(You have {Cash}₵, not enough for tonight's fee.)" : $"\n（你现在的现金 {Cash}₵，不够今晚的费用。）") : ""), theme.Wrap);
		if (Ui.Button(new Rect(r.x + 10f, r.y + 160f, 180f, 34f), state.TrafficOn ? L.T("已开启（点击关闭）", "On (click to turn off)") : L.T("开启", "Turn on"), state.TrafficOn ? theme.BtnStyle : theme.BtnHotStyle))
		{
			state.TrafficOn = !state.TrafficOn;
			ModLog.Info("下层客流：" + (state.TrafficOn ? L.T("开启", "Turn on") : "关闭"));
			Toast(state.TrafficOn ? L.T("从今晚开始介绍熟客", "Regulars start coming from tonight") : L.T("已关闭", "Turned off"));
		}
	}

	private static void DrawPastelFooter(Rect r)
	{
		Theme theme = _theme;
		CompanyState state = State;
		GUI.Box(r, GUIContent.none, theme.CellBox);
		Texture2D texture2D = Portraits.Texture(PastelCo.HemingwayOpen() ? "pastel_milo_hemingway" : "pastel_milo", "maleLower_0");
		Rect r2 = new Rect(r.x + 6f, r.y + 6f, 130f, 95f);
		Ui.Fill(r2, Tex.Hex(13223594));
		if (texture2D != null)
		{
			Ui.Image(r2, texture2D);
		}
		GUI.Label(new Rect(r.x + 6f, r.y + 104f, 130f, 22f), L.T("米洛", "Milo"), theme.Center);
		GUI.Label(new Rect(r.x + 144f, r.y + 10f, 300f, 22f), "“" + MiloLines[Day % MiloLines.Length] + "”", theme.Body);
		GUI.Label(new Rect(r.x + 144f, r.y + 40f, 300f, 22f), (L.En ? $"Cooperation {state.Coop}  ·  {TierLabel(state)}" : $"合作度 {state.Coop}  ·  {TierLabel(state)}"), theme.Body);
		CoopBar(new Rect(r.x + 144f, r.y + 66f, 300f, 12f));
		GUI.Label(new Rect(r.x + 144f, r.y + 82f, 300f, 20f), NextTierHint(), theme.Small);
		if (_module != 0 || !PastelUnlocked(0, out var _))
		{
			return;
		}
		int num = Cart.Sum((KeyValuePair<CatalogEntry, int> kv) => PastelUnitPrice(kv.Key) * kv.Value);
		GUI.Label(new Rect(r.xMax - 180f, r.y + 12f, 170f, 22f), (L.En ? $"Total {num}₵" : $"合计 {num}₵"), theme.Price);
		GUI.Label(new Rect(r.xMax - 180f, r.y + 36f, 170f, 22f), (L.En ? $"Cash {Cash}₵" : $"现金 {Cash}₵"), theme.Small);
		bool enabled = num > 0 && num <= Cash && Services.CanOrderToday(_company);
		bool enabled2 = GUI.enabled;
		GUI.enabled = enabled;
		if (Ui.Button(new Rect(r.xMax - 170f, r.y + 64f, 150f, 36f), L.T("下单", "Order"), theme.BtnHotStyle))
		{
			string text = Services.PlaceOrder(_company, Cart, PastelUnitPrice);
			if (text == null)
			{
				Cart.Clear();
				Toast(L.T("已下单！", "Ordered! ") + _company.FrontDeskDone);
			}
			else
			{
				Toast(text);
			}
		}
		GUI.enabled = enabled2;
	}

}

