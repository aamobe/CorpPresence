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
			GUI.Label(new Rect(r.x + 124f, r.y + 16f, 416f, 58f), _company.Name + L.K("ui.davre.overview.supplyOrder", " · 供货单", " · Supply Order"), theme.Title);
		}
		else
		{
			Ui.Fill(new Rect(20f, 16f, 560f, 64f), theme.Accent);
			GUI.Label(new Rect(36f, 16f, 520f, 64f), _company.Name + L.K("ui.davre.overview.supplyOrder", " · 供货单", " · Supply Order"), theme.InvTitle);
		}
		GUI.Label(new Rect(600f, 40f, width - 640f, 22f), (L.En ? L.K("ui.davre.overview.orderDAVDay", (System.FormattableString)$"Order DAV-{_company.PhoneNumber}   Day {Day}") : $"单号 DAV-{_company.PhoneNumber}\u3000第 {Day} 天"), theme.Small);
		Ui.Fill(new Rect(20f, 100f, width - 40f, 2f), theme.Accent);
		GUI.Label(new Rect(24f, 106f, width - 48f, 22f), L.K("ui.davre.overview.orderCenter", "订货中心：", "Order center: ") + _desk, theme.Body);
		Rect position = new Rect(20f, 136f, 220f, height - 156f);
		GUI.Box(position, GUIContent.none, theme.CellBox);
		Rect r2 = new Rect(position.x + 10f, position.y + 10f, 200f, 146f);
		Ui.Fill(r2, Tex.Hex(13483928));
		Texture2D texture2D = Portraits.Texture("davre_doreen", "femaleLower_0");
		if (texture2D != null)
		{
			Ui.Image(new Rect(r2.x + 2f, r2.y + 2f, r2.width - 4f, r2.height - 4f), texture2D);
		}
		GUI.Label(new Rect(position.x + 10f, r2.yMax + 4f, 200f, 20f), L.K("ui.davre.overview.supplyRepDoreen", "供货专员：多琳", "Supply rep: Doreen"), theme.Small);
		GUI.Label(new Rect(position.x + 10f, r2.yMax + 28f, 200f, 22f), (L.En ? L.K("ui.davre.overview.tier", "Tier: ") + TierLabel(state) : "档位：" + TierLabel(state)), theme.Body);
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
			GUI.Label(new Rect(rect2.x, rect2.y, rect2.width, 40f), L.K("ui.davre.overview.noSupplierNumberYet", "还没有供货编号。", "No supplier number yet."), theme.Wrap);
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
		GUI.Label(new Rect(r.x, r.y, r.width, 20f), flag ? L.K("ui.davre.orders.orderTodayDeliveredTomorrow", "今天下单，明天到货。", "Order today, delivered tomorrow.") : L.K("ui.davre.orders.alreadyOrderedToday", "今天已经下过单了。", "Already ordered today."), theme.Small);
		float num4 = r.y + 26f;
		Rect header = new Rect(r.x, num4, r.width, 26f);
		Ui.Fill(header, theme.Accent);
		DrawOrderHeader(header, theme.InvBody, theme.InvCenter);
		num4 += 30f;
		CatalogEntry[] entries = DavreCo.Entries;
		foreach (CatalogEntry catalogEntry in entries)
		{
			Ui.Fill(new Rect(r.x, num4 + 33f, r.width, 1f), theme.Line);
			var columns = OrderColumns(new Rect(r.x, num4, r.width, 34f));
			Rect status = new Rect(columns.Unit.x, num4, columns.ListPrice.x - columns.Unit.x, 34f);
			DrawOrderItem(columns.Item, catalogEntry, State, theme);
			GUI.Label(columns.ListPrice, ItemListPrice(ItemListValue(catalogEntry)), theme.Center);
			if (catalogEntry.MinTier > num)
			{
				GUI.Label(status, (L.En ? L.K("ui.davre.orders.tierRequired", (System.FormattableString)$"Opens at {_company.TierNames[catalogEntry.MinTier - 1]}") : _company.TierNames[catalogEntry.MinTier - 1] + "开放"), theme.Small);
				Cart.Remove(catalogEntry);
				num4 += 36f;
				continue;
			}
			int num5 = DavreUnitPrice(catalogEntry);
			if (num5 <= 0)
			{
				GUI.Label(status, L.K("ui.davre.orders.unavailable", "暂不可用", "Unavailable"), theme.Small);
				Cart.Remove(catalogEntry);
				num4 += 36f;
				continue;
			}
			GUI.Label(columns.Unit, $"{num5}₵", theme.Price);
			Cart.TryGetValue(catalogEntry, out var value);
			value = Ui.Qty(columns.Quantity, value, Math.Min(10, Services.Left(State, catalogEntry)), theme.BtnStyle, theme.Center);
			if (value > 0)
			{
				Cart[catalogEntry] = value;
			}
			else
			{
				Cart.Remove(catalogEntry);
			}
			GUI.Label(columns.Subtotal, (value > 0) ? $"{num5 * value}₵" : "—", theme.Center);
			num4 += 36f;
		}
		int num6 = Cart.Sum((KeyValuePair<CatalogEntry, int> kv) => DavreUnitPrice(kv.Key) * kv.Value);
		float num7 = r.yMax - 44f;
		GUI.Label(new Rect(r.x, num7, 200f, 22f), (L.En ? L.K("ui.davre.orders.total", (System.FormattableString)$"Total {num6}₵") : $"合计 {num6}₵"), theme.Price);
		int cashDue = DrawCheckoutBalance(new Rect(r.x, num7 + 22f, r.width - 170f, 20f), num6);
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && num6 > 0 && Services.CanAfford(cashDue) && flag;
		if (Ui.Button(new Rect(r.xMax - 170f, num7 + 2f, 160f, 36f), L.K("ui.davre.orders.submitOrder", "提交供货单", "Submit order"), theme.BtnHotStyle))
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
		GUI.Label(new Rect(r.x, r.y, r.width, 66f), L.K("ui.davre.canning.pressGrindProcessRawMeat", "绞肉机按按钮加工生肉、营养果和合格鱼片。罐装机过夜制作泥炭或鲜肉罐头。封装机有蟹肉棒、鱼罐头和繁星果糊三档。", "Press Grind to process raw meat, nutrifruit or suitable fish fillets. The canner makes Peat or fresh meat cans overnight. The sealer has Crab Stick, Canned Fish and Galaxy Blend modes."), theme.Wrap);
		float num = r.y + 76f;
		if (!DavreCo.CanningOpen && !DavreCo.CanningOffered)
		{
			GUI.Label(new Rect(r.x, num, r.width, 44f), (L.En ? L.K("ui.davre.canning.doreenWillComeTalkOnce", (System.FormattableString)$"Doreen will come to talk once cooperation reaches {60}.") : $"合作度到 {60} 后，多琳会上门来谈。"), theme.Wrap);
			return;
		}
		if (!DavreCo.CanningOpen)
		{
			int fee = DavreCo.CanningFee;
			GUI.Label(new Rect(r.x, num, r.width, 44f), (L.En ? L.K("ui.davre.canning.doreenAlreadyMadeOfferFee", (System.FormattableString)$"Doreen already made the offer. {fee}₵ fee for one grinder and one canner, delivered tomorrow.") : $"多琳问过你了。手续费 {fee}₵，绞肉机和罐装机各一台，明天送到。"), theme.Wrap);
			float num2 = r.yMax - 44f;
			int cashDue = DrawCheckoutBalance(new Rect(r.x, num2 + 22f, r.width - 170f, 20f), fee);
			bool enabled = GUI.enabled;
			GUI.enabled = enabled && Services.CanAfford(cashDue);
			if (Ui.Button(new Rect(r.xMax - 170f, num2 + 2f, 160f, 36f), (L.En ? L.K("ui.davre.canning.signUp", (System.FormattableString)$"Sign up ({fee}₵)") : $"补办（{fee}₵）"), theme.BtnHotStyle))
			{
				Toast(Services.MakeUpCanning(c) ?? _company.FrontDeskDone);
			}
			GUI.enabled = enabled;
			return;
		}
		GUI.Label(new Rect(r.x, num, r.width, 20f), L.K("ui.davre.canning.listPriceDeliveredTomorrowNo", "原价，明天到货，不限量。", "List price, delivered tomorrow, no limit."), theme.Small);
		num += 28f;
		(string, string)[] canningMachines = DavreCo.CanningMachines;
		float rowHeight = Mathf.Min(108f, (r.yMax - 26f - num) / canningMachines.Length);
		float iconSize = Mathf.Max(0f, rowHeight - 12f);
		for (int i = 0; i < canningMachines.Length; i++)
		{
			(string, string) tuple = canningMachines[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			Brand.Entry entry = Brand.Info(item);
			int num3 = (int)Goods.ValueOf(item2);
			Rect position = new Rect(r.x, num, iconSize, iconSize);
			GUI.Box(position, GUIContent.none, theme.CellBox);
			if (entry.Icon != null)
			{
				Ui.Image(new Rect(position.x + 4f, position.y + 4f, iconSize - 8f, iconSize - 8f), entry.Icon);
			}
			GUI.Label(new Rect(position.xMax + 16f, num + 4f, 260f, 26f), entry.Name, theme.Body);
			GUI.Label(new Rect(position.xMax + 16f, num + 30f, 200f, 22f), $"{num3}₵", theme.Price);
			GUI.Label(new Rect(r.xMax - 170f, num + 48f, 160f, 22f), ItemListPrice(num3, labeled: true), theme.Center);
			int cashDue = DrawVoucherDetails(new Rect(position.xMax + 16f, num + 52f, r.width - iconSize - 186f, 20f), num3);
			bool enabled2 = GUI.enabled;
			GUI.enabled = enabled2 && Services.CanAfford(cashDue);
			if (Ui.Button(new Rect(r.xMax - 170f, num + 10f, 160f, 36f), L.K("ui.davre.canning.orderOne", "订一台", "Order one"), theme.BtnHotStyle))
			{
				Toast(Services.OrderCanningMachine(c, item, num3) ?? _company.FrontDeskDone);
			}
			GUI.enabled = enabled2;
			num += rowHeight;
		}
		GUI.Label(new Rect(r.x, r.yMax - 22f, 200f, 20f), (L.En ? L.K("ui.davre.canning.cash", (System.FormattableString)$"Cash {Cash}₵") : $"现金 {Cash}₵"), theme.Small);
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
			GUI.Label(new Rect(r.x, r.y, r.width, 60f), L.K("ui.davre.purifier.davreSOwnWaterPurifier", "达夫雷自己的净水器，只供给卖水的店。", "Davre's own water purifier, supplied only to shops that sell water."), theme.Wrap);
			return;
		}
		int num = davreCo.LeviathanLimit(state);
		int num2 = DavreCo.LeviathanUsed(state);
		int num3 = LeviathanPrice();
		GUI.Label(new Rect(r.x, r.y, r.width, 66f), L.K("ui.davre.purifier.modeledHydraTechPurifiesRustwaterGhostwater", "照着海德拉做的。铁锈水、幽灵水都能净化成基础水，沟渠水不行。耐久 600，能净化 60 升。\n", "Modeled on the HydraTech. Purifies Rustwater and Ghostwater to Basewater, but not Gutterflow. 600 durability, good for 60 liters.\n") + (L.En ? L.K("ui.davre.purifier.perRestockPeriodEachDelivered", (System.FormattableString)$"{num} per restock period (2 from {_company.TierNames[2]}), {num3}₵ each, delivered tomorrow.") : $"每个补货周期可订 {num} 台（{_company.TierNames[2]}起 2 台），每台 {num3}₵，明天到货。"), theme.Wrap);
		Texture2D icon = Brand.Info("@leviathan").Icon;
		Rect position = new Rect(r.x, r.y + 80f, 136f, 136f);
		GUI.Box(position, GUIContent.none, theme.CellBox);
		if (icon != null)
		{
			Ui.Image(new Rect(position.x + 4f, position.y + 4f, 128f, 128f), icon);
		}
		GUI.Label(new Rect(position.xMax + 20f, position.y + 10f, 240f, 26f), (L.En ? L.K("ui.davre.purifier.quotaUsed", (System.FormattableString)$"Ordered {Services.PeriodWord}: {num2}/{num}") : $"{Services.PeriodWord}已订 {num2}/{num} 台"), theme.Body);
		GUI.Label(new Rect(position.xMax + 20f, position.y + 40f, 300f, 22f), (L.En ? L.K("ui.davre.purifier.restocksDay", (System.FormattableString)$"Restocks on day {Services.NextRestockDay}") : $"第 {Services.NextRestockDay} 天补货"), theme.Small);
		GUI.Label(new Rect(r.xMax - 170f, position.y + 10f, 160f, 26f), ItemListPrice(ItemListValue(_leviathan), labeled: true), theme.Center);
		float num4 = r.yMax - 44f;
		GUI.Label(new Rect(r.x, num4, 200f, 22f), (L.En ? L.K("ui.davre.purifier.unitPrice", (System.FormattableString)$"Unit price {num3}₵") : $"单价 {num3}₵"), theme.Price);
		int cashDue = DrawCheckoutBalance(new Rect(r.x, num4 + 22f, r.width - 170f, 20f), num3);
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && num3 > 0 && num2 < num && Services.CanAfford(cashDue);
		if (Ui.Button(new Rect(r.xMax - 170f, num4 + 2f, 160f, 36f), L.K("ui.davre.purifier.orderOne", "订一台", "Order one"), theme.BtnHotStyle))
		{
			Toast(Services.OrderLeviathan(davreCo, num3) ?? _company.FrontDeskDone);
		}
		GUI.enabled = enabled;
	}

}

