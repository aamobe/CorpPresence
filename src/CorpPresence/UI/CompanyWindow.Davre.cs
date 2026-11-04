using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.Malice;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{

	private static void DrawDavre()
	{
		Theme theme = _theme;
		CompanyState state = State;
		float width = _rect.width;
		float height = _rect.height;
		Rect r = new Rect(20f, 8f, 560f, 90f);
		if (theme.Banner != null)
		{
			Ui.Image(r, theme.Banner);
			GUI.Label(new Rect(r.x + 124f, r.y + 16f, 416f, 58f), _company.Name + L.T(" · 供货单", " · Supply Order"), theme.Title);
		}
		else
		{
			Ui.Fill(new Rect(20f, 16f, 560f, 64f), theme.Accent);
			GUI.Label(new Rect(36f, 16f, 520f, 64f), _company.Name + L.T(" · 供货单", " · Supply Order"), theme.InvTitle);
		}
		GUI.Label(new Rect(600f, 40f, width - 640f, 22f), (L.En ? $"Order DAV-{_company.PhoneNumber}   Day {Day}" : $"单号 DAV-{_company.PhoneNumber}\u3000第 {Day} 天"), theme.Small);
		Ui.Fill(new Rect(20f, 100f, width - 40f, 2f), theme.Accent);
		GUI.Label(new Rect(24f, 106f, width - 48f, 22f), L.T("订货中心：", "Order center: ") + _desk, theme.Body);
		Rect position = new Rect(20f, 136f, 220f, height - 156f);
		GUI.Box(position, GUIContent.none, theme.CellBox);
		Rect r2 = new Rect(position.x + 10f, position.y + 10f, 200f, 146f);
		Ui.Fill(r2, Tex.Hex(13483928));
		Texture2D texture2D = Portraits.Texture("davre_doreen", "femaleLower_0");
		if (texture2D != null)
		{
			Ui.Image(new Rect(r2.x + 2f, r2.y + 2f, r2.width - 4f, r2.height - 4f), texture2D);
		}
		GUI.Label(new Rect(position.x + 10f, r2.yMax + 4f, 200f, 20f), L.T("供货专员：多琳", "Supply rep: Doreen"), theme.Small);
		GUI.Label(new Rect(position.x + 10f, r2.yMax + 28f, 200f, 22f), (L.En ? "Tier: " + TierLabel(state) : "档位：" + TierLabel(state)), theme.Body);
		CoopBar(new Rect(position.x + 10f, r2.yMax + 54f, 200f, 8f));
		GUI.Label(new Rect(position.x + 10f, r2.yMax + 64f, 200f, 20f), NextTierHint(), theme.Small);
		float num = r2.yMax + 96f;
		for (int i = 0; i < DavreModules.Length; i++)
		{
			string text = ((i == _module) ? "› " : "  ") + DavreModules[i];
			if (Ui.Button(new Rect(position.x + 6f, num + (float)(i * 32), position.width - 12f, 28f), text, (i == _module) ? theme.NavOnStyle : theme.NavStyle))
			{
				_module = i;
			}
		}
		Rect rect = new Rect(position.xMax + 20f, 136f, width - position.xMax - 40f, height - 156f);
		GUI.Label(new Rect(rect.x, rect.y, rect.width, 30f), DavreModules[_module], theme.HotTitle);
		Ui.Fill(new Rect(rect.x, rect.y + 34f, rect.width, 1f), theme.Ink);
		Rect rect2 = new Rect(rect.x, rect.y + 44f, rect.width, rect.height - 44f);
		if (DrawExtraIfSelected(DavreBase.Length, rect2))
		{
			return;
		}
		if (_company.Tier(state) < 1 && _module < 3)
		{
			GUI.Label(new Rect(rect2.x, rect2.y, rect2.width, 40f), L.T("还没有供货编号。", "No supplier number yet."), theme.Wrap);
			return;
		}
		switch (_module)
		{
		case 0:
			DrawDavreOrder(rect2);
			break;
		case 1:
			DrawDavreLeviathan(rect2);
			break;
		case 2:
			DrawDavreCanning(rect2);
			break;
		default:
			DrawBrand(rect2);
			break;
		}
	}

	private static int DavreUnitPrice(CatalogEntry e)
	{
		return e.Quote(Company.OrderRate(_company, _company.Tier(State)));
	}

	private static void DrawDavreOrder(Rect r)
	{
		Theme theme = _theme;
		int num = _company.CatalogTier(State);
		bool flag = Services.CanOrderToday(_company);
		GUI.Label(new Rect(r.x, r.y, r.width, 20f), flag ? L.T("今天下单，明天到货。", "Order today, delivered tomorrow.") : L.T("今天已经下过单了。", "Already ordered today."), theme.Small);
		float x = r.x;
		float num2 = r.x + 290f;
		float x2 = r.x + 380f;
		float num3 = r.x + 500f;
		float num4 = r.y + 26f;
		Ui.Fill(new Rect(r.x, num4, r.width, 26f), theme.Accent);
		GUI.Label(new Rect(x + 8f, num4, 280f, 26f), L.T("品名", "Item"), theme.InvBody);
		GUI.Label(new Rect(num2, num4, 90f, 26f), L.T("单价", "Unit price"), theme.InvCenter);
		GUI.Label(new Rect(x2, num4, 120f, 26f), L.T("数量", "Qty"), theme.InvCenter);
		GUI.Label(new Rect(num3, num4, r.xMax - num3, 26f), L.T("小计", "Subtotal"), theme.InvCenter);
		num4 += 30f;
		CatalogEntry[] entries = DavreCo.Entries;
		foreach (CatalogEntry catalogEntry in entries)
		{
			Ui.Fill(new Rect(r.x, num4 + 33f, r.width, 1f), theme.Line);
			GUI.Label(new Rect(x + 8f, num4, 280f, 34f), catalogEntry.Name() + Services.LeftText(State, catalogEntry), theme.Body);
			if (catalogEntry.MinTier > num)
			{
				GUI.Label(new Rect(num2, num4, r.xMax - num2, 34f), (L.En ? "Opens at " + _company.TierNames[catalogEntry.MinTier - 1] : _company.TierNames[catalogEntry.MinTier - 1] + "开放"), theme.Small);
				Cart.Remove(catalogEntry);
				num4 += 36f;
				continue;
			}
			int num5 = DavreUnitPrice(catalogEntry);
			if (num5 <= 0)
			{
				GUI.Label(new Rect(num2, num4, r.xMax - num2, 34f), L.T("暂不可用", "Unavailable"), theme.Small);
				Cart.Remove(catalogEntry);
				num4 += 36f;
				continue;
			}
			GUI.Label(new Rect(num2, num4, 90f, 34f), $"{num5}₵", theme.Price);
			Cart.TryGetValue(catalogEntry, out var value);
			value = Ui.Qty(new Rect(x2, num4, 120f, 34f), value, Math.Min(10, Services.Left(State, catalogEntry)), theme.BtnStyle, theme.Center);
			if (value > 0)
			{
				Cart[catalogEntry] = value;
			}
			else
			{
				Cart.Remove(catalogEntry);
			}
			GUI.Label(new Rect(num3, num4, r.xMax - num3, 34f), (value > 0) ? $"{num5 * value}₵" : "—", theme.Center);
			num4 += 36f;
		}
		int num6 = Cart.Sum((KeyValuePair<CatalogEntry, int> kv) => DavreUnitPrice(kv.Key) * kv.Value);
		float num7 = r.yMax - 44f;
		GUI.Label(new Rect(r.x, num7, 200f, 22f), (L.En ? $"Total {num6}₵" : $"合计 {num6}₵"), theme.Price);
		GUI.Label(new Rect(r.x, num7 + 22f, 200f, 20f), (L.En ? $"Cash {Cash}₵" : $"现金 {Cash}₵"), theme.Small);
		bool enabled = GUI.enabled;
		GUI.enabled = (num6 > 0 && num6 <= Cash) & flag;
		if (Ui.Button(new Rect(r.xMax - 170f, num7 + 2f, 160f, 36f), L.T("提交供货单", "Submit order"), theme.BtnHotStyle))
		{
			string text = Services.PlaceOrder(_company, Cart, DavreUnitPrice);
			if (text == null)
			{
				Cart.Clear();
				Toast(_company.FrontDeskDone);
			}
			else
			{
				Toast(text);
			}
		}
		GUI.enabled = enabled;
	}

	private static void DrawDavreCanning(Rect r)
	{
		Theme theme = _theme;
		DavreCo c = (DavreCo)_company;
		GUI.Label(new Rect(r.x, r.y, r.width, 66f), L.T("绞肉机把生肉绞成肉糜、营养果绞成果糊，罐装机每晚把肉糜和果糊封成罐头：合成档出“泥炭”，鲜肉档出鲜肉罐头。", "The grinder turns raw meat into mince and nutrifruit into paste; each night the canner seals mince and paste into cans: Peat on Synthetic, fresh meat on Fresh."), theme.Wrap);
		float num = r.y + 76f;
		if (!DavreCo.CanningOpen && !DavreCo.CanningOffered)
		{
			GUI.Label(new Rect(r.x, num, r.width, 44f), (L.En ? $"Doreen will come to talk once cooperation reaches {60}." : $"合作度到 {60} 后，多琳会上门来谈。"), theme.Wrap);
			return;
		}
		if (!DavreCo.CanningOpen)
		{
			GUI.Label(new Rect(r.x, num, r.width, 44f), (L.En ? $"Doreen already made the offer. {DavreCo.CanningFee}₵ fee for one grinder and one canner, delivered tomorrow." : $"多琳问过你了。手续费 {DavreCo.CanningFee}₵，绞肉机和罐装机各一台，明天送到。"), theme.Wrap);
			float num2 = r.yMax - 44f;
			GUI.Label(new Rect(r.x, num2 + 22f, 200f, 20f), (L.En ? $"Cash {Cash}₵" : $"现金 {Cash}₵"), theme.Small);
			bool enabled = GUI.enabled;
			GUI.enabled = Cash >= DavreCo.CanningFee;
			if (Ui.Button(new Rect(r.xMax - 170f, num2 + 2f, 160f, 36f), (L.En ? $"Sign up ({DavreCo.CanningFee}₵)" : $"补办（{DavreCo.CanningFee}₵）"), theme.BtnHotStyle))
			{
				Toast(Services.MakeUpCanning(c) ?? _company.FrontDeskDone);
			}
			GUI.enabled = enabled;
			return;
		}
		GUI.Label(new Rect(r.x, num, r.width, 20f), L.T("原价，明天到货，不限量。", "List price, delivered tomorrow, no limit."), theme.Small);
		num += 28f;
		(string, string)[] canningMachines = DavreCo.CanningMachines;
		for (int i = 0; i < canningMachines.Length; i++)
		{
			(string, string) tuple = canningMachines[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			Brand.Entry entry = Brand.Info(item);
			int num3 = (int)Goods.ValueOf(item2);
			Rect position = new Rect(r.x, num, 96f, 96f);
			GUI.Box(position, GUIContent.none, theme.CellBox);
			if (entry.Icon != null)
			{
				Ui.Image(new Rect(position.x + 4f, position.y + 4f, 88f, 88f), entry.Icon);
			}
			GUI.Label(new Rect(position.xMax + 16f, num + 14f, 260f, 26f), entry.Name, theme.Body);
			GUI.Label(new Rect(position.xMax + 16f, num + 44f, 200f, 22f), $"{num3}₵", theme.Price);
			bool enabled2 = GUI.enabled;
			GUI.enabled = Cash >= num3;
			if (Ui.Button(new Rect(r.xMax - 170f, num + 30f, 160f, 36f), L.T("订一台", "Order one"), theme.BtnHotStyle))
			{
				Toast(Services.OrderCanningMachine(c, item, num3) ?? _company.FrontDeskDone);
			}
			GUI.enabled = enabled2;
			num += 108f;
		}
		GUI.Label(new Rect(r.x, r.yMax - 22f, 200f, 20f), (L.En ? $"Cash {Cash}₵" : $"现金 {Cash}₵"), theme.Small);
	}

	private static int LeviathanPrice()
	{
		if (_leviathan == null)
		{
			_leviathan = new CatalogEntry
			{
				Id = "@leviathan",
				Page = "利维坦"
			};
		}
		return _leviathan.Quote(Settings.OrderMarkup);
	}

	private static void DrawDavreLeviathan(Rect r)
	{
		Theme theme = _theme;
		CompanyState state = State;
		DavreCo davreCo = (DavreCo)_company;
		if (!DavreCo.LeviathanOpen)
		{
			GUI.Label(new Rect(r.x, r.y, r.width, 60f), L.T("达夫雷自己的净水器，只供给卖水的店。", "Davre's own water purifier, supplied only to shops that sell water."), theme.Wrap);
			return;
		}
		int num = davreCo.LeviathanLimit(state);
		int num2 = DavreCo.LeviathanUsed(state);
		int num3 = LeviathanPrice();
		GUI.Label(new Rect(r.x, r.y, r.width, 66f), L.T("照着海德拉做的。铁锈水、幽灵水都能净化成基础水，沟渠水不行。耐久 600，能净化 60 升。\n", "Modeled on the HydraTech. Purifies Rustwater and Ghostwater to Basewater, but not Gutterflow. 600 durability, good for 60 liters.\n") + (L.En ? $"{num} per restock period (2 from {_company.TierNames[2]}), {num3}₵ each, delivered tomorrow." : $"每个补货周期可订 {num} 台（{_company.TierNames[2]}起 2 台），每台 {num3}₵，明天到货。"), theme.Wrap);
		Texture2D icon = Brand.Info("@leviathan").Icon;
		Rect position = new Rect(r.x, r.y + 80f, 136f, 136f);
		GUI.Box(position, GUIContent.none, theme.CellBox);
		if (icon != null)
		{
			Ui.Image(new Rect(position.x + 4f, position.y + 4f, 128f, 128f), icon);
		}
		GUI.Label(new Rect(position.xMax + 20f, position.y + 10f, 300f, 26f), (L.En ? $"Ordered {Services.PeriodWord}: {num2}/{num}" : $"{Services.PeriodWord}已订 {num2}/{num} 台"), theme.Body);
		GUI.Label(new Rect(position.xMax + 20f, position.y + 40f, 300f, 22f), (L.En ? $"Restocks on day {Services.NextRestockDay}" : $"第 {Services.NextRestockDay} 天补货"), theme.Small);
		float num4 = r.yMax - 44f;
		GUI.Label(new Rect(r.x, num4, 200f, 22f), (L.En ? $"Unit price {num3}₵" : $"单价 {num3}₵"), theme.Price);
		GUI.Label(new Rect(r.x, num4 + 22f, 200f, 20f), (L.En ? $"Cash {Cash}₵" : $"现金 {Cash}₵"), theme.Small);
		bool enabled = GUI.enabled;
		GUI.enabled = num3 > 0 && num2 < num && Cash >= num3;
		if (Ui.Button(new Rect(r.xMax - 170f, num4 + 2f, 160f, 36f), L.T("订一台", "Order one"), theme.BtnHotStyle))
		{
			Toast(Services.OrderLeviathan(davreCo, num3) ?? _company.FrontDeskDone);
		}
		GUI.enabled = enabled;
	}

}

