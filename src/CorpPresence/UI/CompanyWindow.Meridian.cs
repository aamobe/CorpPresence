using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.Malice;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{

	private static void DrawMeridian()
	{
		Theme theme = _theme;
		CompanyState state = State;
		float width = _rect.width;
		float height = _rect.height;
		Rect r = new Rect(20f, 8f, 560f, 90f);
		if (theme.Banner != null)
		{
			Ui.Image(r, theme.Banner);
			GUI.Label(new Rect(r.x + 120f, r.y + 16f, 420f, 58f), _company.Name + L.K("ui.meridian.overview.ordnanceRequisitionForm", " · 军械申领表", " · Ordnance Requisition Form"), theme.InvTitle);
		}
		else
		{
			Ui.Fill(new Rect(20f, 16f, 560f, 64f), theme.Ink);
			Ui.Fill(new Rect(20f, 80f, 560f, 4f), theme.Hot);
			GUI.Label(new Rect(36f, 16f, 520f, 64f), _company.Name + L.K("ui.meridian.overview.ordnanceRequisitionForm", " · 军械申领表", " · Ordnance Requisition Form"), theme.InvTitle);
		}
		GUI.Label(new Rect(600f, 40f, width - 640f, 22f), (L.En ? L.K("ui.meridian.overview.formMRDDay", (System.FormattableString)$"Form MRD-{_company.PhoneNumber}   Day {Day}") : $"表 MRD-{_company.PhoneNumber}\u3000第 {Day} 天"), theme.Small);
		Ui.Fill(new Rect(20f, 100f, width - 40f, 2f), theme.Ink);
		GUI.Label(new Rect(24f, 106f, width - 48f, 22f), L.K("ui.meridian.overview.orderDesk", "订货处：", "Order desk: ") + _desk, theme.Body);
		Rect position = new Rect(20f, 136f, 220f, height - 156f);
		GUI.Box(position, GUIContent.none, theme.CellBox);
		Rect r2 = new Rect(position.x + 10f, position.y + 10f, 200f, 146f);
		Ui.Fill(r2, Tex.Hex(12104616));
		Texture2D texture2D = Portraits.Texture("meridian_rosen", "maleSecOff_0");
		if (texture2D != null)
		{
			Ui.Image(new Rect(r2.x + 2f, r2.y + 2f, r2.width - 4f, r2.height - 4f), texture2D);
		}
		GUI.Label(new Rect(position.x + 10f, r2.yMax + 4f, 200f, 20f), L.K("ui.meridian.overview.handledByRosenStationManager", "经办：罗森（驻站负责人）", "Handled by: Rosen (Station Manager)"), theme.Small);
		GUI.Label(new Rect(position.x + 10f, r2.yMax + 28f, 200f, 22f), (L.En ? L.K("ui.meridian.overview.license", "License: ") + TierLabel(state) : "许可：" + TierLabel(state)), theme.Body);
		CoopBar(new Rect(position.x + 10f, r2.yMax + 54f, 200f, 8f));
		GUI.Label(new Rect(position.x + 10f, r2.yMax + 64f, 200f, 20f), NextTierHint(), theme.Small);
		float num = r2.yMax + 96f;
		for (int i = 0; i < MeridianModules.Length; i++)
		{
			string text = ((i == _module) ? "› " : "  ") + MeridianModules[i];
			if (Ui.Button(new Rect(position.x + 6f, num + (float)(i * 32), position.width - 12f, 28f), text, (i == _module) ? theme.NavOnStyle : theme.NavStyle))
			{
				_module = i;
			}
		}
		Rect rect = new Rect(position.xMax + 20f, 136f, width - position.xMax - 40f, height - 156f);
		GUI.Label(new Rect(rect.x, rect.y, rect.width, 30f), MeridianModules[_module], theme.HotTitle);
		Ui.Fill(new Rect(rect.x, rect.y + 34f, rect.width, 1f), theme.Ink);
		Rect rect2 = new Rect(rect.x, rect.y + 44f, rect.width, rect.height - 44f);
		if (DrawTermsIfSelected(MeridianBase.Length, rect2))
		{
			return;
		}
		if (_company.Tier(state) < 1 && _module < 3)
		{
			GUI.Label(new Rect(rect2.x, rect2.y, rect2.width, 40f), L.K("ui.meridian.overview.noLicense", "没有许可。", "No license."), theme.Wrap);
			return;
		}
		switch (_module)
		{
		case 0:
			DrawMeridianOrder(rect2, MeridianCo.Entries, L.K("ui.meridian.overview.orderTodayHereTomorrowFactory", "今天下单，明天到。全新出厂。", "Order today, here tomorrow. Factory new."));
			break;
		case 1:
		{
			int num2 = (MeridianCo.Parts.Length + 8 - 1) / 8;
			Orders.PartsPage = Math.Max(0, Math.Min(Orders.PartsPage, num2 - 1));
			if (Ui.Button(new Rect(rect2.xMax - 150f, rect2.y - 40f, 32f, 26f), "‹", theme.BtnStyle) && Orders.PartsPage > 0)
			{
				Orders.PartsPage--;
			}
			GUI.Label(new Rect(rect2.xMax - 116f, rect2.y - 40f, 80f, 26f), $"{Orders.PartsPage + 1}/{num2}", theme.Center);
			if (Ui.Button(new Rect(rect2.xMax - 34f, rect2.y - 40f, 32f, 26f), "›", theme.BtnStyle) && Orders.PartsPage < num2 - 1)
			{
				Orders.PartsPage++;
			}
			DrawMeridianOrder(rect2, MeridianCo.Parts.Skip(Orders.PartsPage * 8).Take(8).ToArray(), (L.En ? L.K("ui.meridian.overview.vanillaAttachmentsSteadySupplyX", (System.FormattableString)$"Vanilla attachments, steady supply, at {MeridianCo.PartsRate:0.0}x value. Counts as the same daily order as standard issue.") : $"原版配件，稳定供货，按价值的 {MeridianCo.PartsRate:0.0} 倍。和制式订货算同一天的一单。"));
			break;
		}
		case 2:
			DrawMeridianRefurb(rect2);
			break;
		case 3:
			DrawMeridianForge(rect2);
			break;
		default:
			DrawBrand(rect2);
			break;
		}
	}

	private static int MeridianUnitPrice(CatalogEntry e)
	{
		return e.Quote((Array.IndexOf(MeridianCo.Parts, e) >= 0) ? MeridianCo.PartsRate : Company.OrderRate(_company, _company.Tier(State)));
	}

	private static void DrawMeridianOrder(Rect r, CatalogEntry[] entries, string note)
		=> DrawEquipmentOrder(r, entries, note, MeridianUnitPrice, L.K("ui.meridian.orders.submitRequisition", "提交申领", "Submit requisition"));

	private static void DrawEquipmentOrder(Rect r, CatalogEntry[] entries, string note, Func<CatalogEntry, int> unitPrice, string submitLabel)
	{
		Theme theme = _theme;
		int num = _company.CatalogTier(State);
		bool flag = Services.CanOrderToday(_company);
		GUI.Label(new Rect(r.x, r.y, r.width, 20f), flag ? note : L.K("ui.meridian.equipmentOrder.alreadyOrderedToday", "今天已经下过单了。", "Already ordered today."), theme.Small);
		float num4 = r.y + 26f;
		Rect header = new Rect(r.x, num4, r.width, 26f);
		Ui.Fill(header, theme.Ink);
		DrawOrderHeader(header, theme.InvBody, theme.InvCenter);
		num4 += 30f;
		foreach (CatalogEntry catalogEntry in entries)
		{
			Ui.Fill(new Rect(r.x, num4 + 33f, r.width, 1f), theme.Line);
			var columns = OrderColumns(new Rect(r.x, num4, r.width, 34f));
			Rect status = new Rect(columns.Unit.x, num4, columns.ListPrice.x - columns.Unit.x, 34f);
			DrawOrderItem(columns.Item, catalogEntry, State, theme);
			GUI.Label(columns.ListPrice, ItemListPrice(ItemListValue(catalogEntry)), theme.Center);
			string blocked = _company is MeridianCo ? Rosen.SaleBlocked(catalogEntry.Id) : null;
			if (_company is JacksonContact && CargoWarehouse.Price(catalogEntry.Id) > 0 && !CargoWarehouse.Unlocked())
				blocked = CargoWarehouse.UnlockHint();
			if (blocked == null && catalogEntry.MinTier > num)
				blocked = L.En ? L.K("ui.meridian.equipmentOrder.tierRequired", (System.FormattableString)$"Opens at {_company.TierNames[catalogEntry.MinTier - 1]}") : _company.TierNames[catalogEntry.MinTier - 1] + "开放";
			if (blocked != null)
			{
				GUI.Label(status, blocked, theme.Small);
				Orders.Cart.Remove(catalogEntry);
				num4 += 36f;
				continue;
			}
			int num5 = unitPrice(catalogEntry);
			if (num5 <= 0)
			{
				GUI.Label(status, L.K("ui.meridian.equipmentOrder.unavailable", "暂不可用", "Unavailable"), theme.Small);
				Orders.Cart.Remove(catalogEntry);
				num4 += 36f;
				continue;
			}
			GUI.Label(columns.Unit, $"{num5}₵", theme.Price);
			Orders.Cart.TryGetValue(catalogEntry, out var value);
			value = Ui.Qty(columns.Quantity, value, Math.Min(10, Services.Left(State, catalogEntry)), theme.BtnStyle, theme.Center);
			if (value > 0)
			{
				Orders.Cart[catalogEntry] = value;
			}
			else
			{
				Orders.Cart.Remove(catalogEntry);
			}
			GUI.Label(columns.Subtotal, (value > 0) ? $"{num5 * value}₵" : "—", theme.Center);
			num4 += 36f;
		}
		int num6 = Orders.Cart.Sum((KeyValuePair<CatalogEntry, int> kv) => unitPrice(kv.Key) * kv.Value);
		float num7 = r.yMax - 44f;
		GUI.Label(new Rect(r.x, num7, 200f, 22f), (L.En ? L.K("ui.meridian.equipmentOrder.total", (System.FormattableString)$"Total {num6}₵") : $"合计 {num6}₵"), theme.Price);
		int cashDue = DrawCheckoutBalance(new Rect(r.x, num7 + 22f, r.width - 170f, 20f), num6);
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && num6 > 0 && Services.CanAfford(cashDue) && flag;
		if (Ui.Button(new Rect(r.xMax - 170f, num7 + 2f, 160f, 36f), submitLabel, theme.BtnHotStyle))
		{
			string text = Services.PlaceOrder(_company, Orders.Cart, unitPrice);
			if (text == null)
			{
				Orders.Cart.Clear();
				Toast(_company.FrontDeskDone);
			}
			else
			{
				Toast(text);
			}
		}
		GUI.enabled = enabled;
	}

	private static void ScanRefurb()
	{
		if (Refurbishment.ScannedAt >= 0f && Time.unscaledTime - Refurbishment.ScannedAt < 1f)
		{
			return;
		}
		Refurbishment.ScannedAt = Time.unscaledTime;
		Refurbishment.Rows.Clear();
		List<GameItem> list = Services.RefurbishCandidates();
		Refurbishment.TotalCount = list.Count;
		foreach (GameItem item2 in list.Take(3))
		{
			string item;
			try
			{
				item = item2.GetDisplayName();
			}
			catch
			{
				item = item2.identifier;
			}
			int num = MeridianCo.ConditionOf(item2);
			Refurbishment.Rows.Add((item, MeridianCo.ConditionName(num), MeridianCo.ConditionName(num + 1), MeridianCo.RefurbishFee(item2)));
		}
	}

	private static void DrawMeridianForge(Rect r)
	{
		Theme theme = _theme;
		GUI.Label(new Rect(r.x, r.y, r.width, 66f), L.K("ui.meridian.forging.precisionForgeTurnsHighPurity", "精锻熔炉每晚把高纯度的金属锭炼成钢胚；锻台用钢胚锻刀，淬火的水越干净，刀的成色越好。", "The precision forge turns high-purity ingots into billets overnight; the anvil forges blades from billets, and the cleaner the quench water, the better the finish."), theme.Wrap);
		float num = r.y + 76f;
		if (!MeridianCo.ForgeOpen && !MeridianCo.ForgeOffered)
		{
			GUI.Label(new Rect(r.x, num, r.width, 44f), (L.En ? L.K("ui.meridian.forging.rosenWillComeTeachOnce", (System.FormattableString)$"Rosen will come to teach you once cooperation reaches {60}.") : $"合作度到 {60} 后，罗森会上门来教。"), theme.Wrap);
			return;
		}
		if (!MeridianCo.ForgeOpen)
		{
			int fee = MeridianCo.ForgeFee;
			GUI.Label(new Rect(r.x, num, r.width, 44f), (L.En ? L.K("ui.meridian.forging.rosenAlreadyCameByFull", (System.FormattableString)$"Rosen already came by. The full kit is {fee}₵: precision forge and anvil, plus 3 billets, delivered tomorrow.") : $"罗森来教过了。整套工具 {fee}₵：精锻熔炉、锻台，附 3 块钢胚，明天送到。"), theme.Wrap);
			float num2 = r.yMax - 44f;
			int cashDue = DrawCheckoutBalance(new Rect(r.x, num2 + 22f, r.width - 170f, 20f), fee);
			bool enabled = GUI.enabled;
			GUI.enabled = enabled && Services.CanAfford(cashDue);
			if (Ui.Button(new Rect(r.xMax - 170f, num2 + 2f, 160f, 36f), (L.En ? L.K("ui.meridian.forging.buyKit", (System.FormattableString)$"Buy kit ({fee}₵)") : $"补办（{fee}₵）"), theme.BtnHotStyle))
			{
				Toast(MeridianCo.MakeUpForge() ?? _company.FrontDeskDone);
			}
			GUI.enabled = enabled;
			return;
		}
		GUI.Label(new Rect(r.x, num, r.width, 20f), L.K("ui.meridian.forging.orderThroughRosenListPrice", "找罗森订。原价，明天到，不限量。", "Order through Rosen. List price, here tomorrow, no limit."), theme.Small);
		num += 28f;
		(string, string)[] forgeMachines = MeridianCo.ForgeMachines;
		for (int i = 0; i < forgeMachines.Length; i++)
		{
			(string, string) tuple = forgeMachines[i];
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
			GUI.Label(new Rect(r.xMax - 170f, num + 68f, 160f, 22f), ItemListPrice(num3, labeled: true), theme.Center);
			int cashDue = DrawVoucherDetails(new Rect(position.xMax + 16f, num + 66f, r.width - 282f, 20f), num3);
			bool enabled2 = GUI.enabled;
			GUI.enabled = enabled2 && Services.CanAfford(cashDue);
			if (Ui.Button(new Rect(r.xMax - 170f, num + 30f, 160f, 36f), L.K("ui.meridian.forging.orderOne", "订一台", "Order one"), theme.BtnHotStyle))
			{
				Toast(MeridianCo.OrderForgeMachine(_company, item, num3) ?? _company.FrontDeskDone);
			}
			GUI.enabled = enabled2;
			num += 108f;
		}
		GUI.Label(new Rect(r.x, r.yMax - 22f, 200f, 20f), (L.En ? L.K("ui.meridian.forging.cash", (System.FormattableString)$"Cash {Cash}₵") : $"现金 {Cash}₵"), theme.Small);
	}

	private static void DrawMeridianRefurb(Rect r)
	{
		Theme theme = _theme;
		CompanyState state = State;
		if (_company.Tier(state) < 2)
		{
			GUI.Label(new Rect(r.x, r.y, r.width, 40f), (L.En ? L.K("ui.meridian.refurbishment.tierRequired", (System.FormattableString)$"Opens at {_company.TierNames[1]}.") : _company.TierNames[1] + "开放。"), theme.Wrap);
			return;
		}
		ScanRefurb();
		int ownFee = MeridianCo.RefurbishFee(ownMake: true);
		int otherFee = MeridianCo.RefurbishFee(ownMake: false);
		GUI.Label(new Rect(r.x, r.y, r.width, 44f), L.K("ui.meridian.refurbishment.factoryRefurbishmentGuaranteedOneCondition", $"厂家返修，保证成功，一次升一档，最高到“优秀”。子午线的枪每把 {ownFee}₵，别家的 {otherFee}₵。每 {MeridianCo.RefurbishGap} 天一次，每次最多 {MeridianCo.RefurbishMax} 把，第二天送回。", (System.FormattableString)$"Factory refurbishment, guaranteed: one condition grade up each time, up to “Excellent”. {ownFee}₵ per Meridian gun, {otherFee}₵ for other makes. Once every {MeridianCo.RefurbishGap} days, up to {MeridianCo.RefurbishMax} guns each time, returned the next day."), theme.Wrap);
		float num = r.y + 54f;
		Ui.Fill(new Rect(r.x, num, r.width, 26f), theme.Ink);
		GUI.Label(new Rect(r.x + 8f, num, 260f, 26f), L.K("ui.meridian.refurbishment.gun", "枪", "Gun"), theme.InvBody);
		GUI.Label(new Rect(r.x + 270f, num, 160f, 26f), L.K("ui.meridian.refurbishment.condition", "成色", "Condition"), theme.InvCenter);
		GUI.Label(new Rect(r.x + 430f, num, r.width - 430f, 26f), L.K("ui.meridian.refurbishment.fee", "费用", "Fee"), theme.InvCenter);
		num += 30f;
		if (Refurbishment.Rows.Count == 0)
		{
			GUI.Label(new Rect(r.x + 8f, num, r.width, 30f), L.K("ui.meridian.refurbishment.noGunsRefurbish", "没有能翻新的枪。", "No guns to refurbish."), theme.Small);
			num += 34f;
		}
		foreach (var refurbRow in Refurbishment.Rows)
		{
			GUI.Label(new Rect(r.x + 8f, num, 260f, 32f), refurbRow.Name, theme.Body);
			GUI.Label(new Rect(r.x + 270f, num, 160f, 32f), refurbRow.From + " → " + refurbRow.To, theme.Center);
			GUI.Label(new Rect(r.x + 430f, num, r.width - 430f, 32f), $"{refurbRow.Fee}₵", theme.Price);
			Ui.Fill(new Rect(r.x, num + 32f, r.width, 1f), theme.Line);
			num += 34f;
		}
		if (Refurbishment.TotalCount > Refurbishment.Rows.Count)
		{
			GUI.Label(new Rect(r.x + 8f, num, r.width, 22f), (L.En ? L.K("ui.meridian.refurbishment.moreSendThemNextTime", (System.FormattableString)$"{Refurbishment.TotalCount - Refurbishment.Rows.Count} more; send them next time.") : $"另有 {Refurbishment.TotalCount - Refurbishment.Rows.Count} 把，下次再送。"), theme.Small);
		}
		int num2 = Services.RefurbishNextDay(state);
		bool flag = Day >= num2;
		int num3 = Refurbishment.Rows.Sum(((string Name, string From, string To, int Fee) x) => x.Fee);
		float num4 = r.yMax - 44f;
		GUI.Label(new Rect(r.x, num4, 300f, 22f), flag ? (L.En ? L.K("ui.meridian.refurbishment.gunsThisTime", (System.FormattableString)$"{Refurbishment.Rows.Count} guns this time, {num3}₵") : $"本次 {Refurbishment.Rows.Count} 把，{num3}₵") : (L.En ? L.K("ui.meridian.refurbishment.nextDropOffDay", (System.FormattableString)$"Next drop-off: day {num2}") : $"下次可送：第 {num2} 天"), flag ? theme.Price : theme.Body);
		int cashDue = DrawCheckoutBalance(new Rect(r.x, num4 + 22f, r.width - 170f, 20f), flag ? num3 : 0);
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && flag && Refurbishment.Rows.Count > 0 && Services.CanAfford(cashDue);
		if (Ui.Button(new Rect(r.xMax - 170f, num4 + 2f, 160f, 36f), L.K("ui.meridian.refurbishment.sendRefurbishment", "送厂翻新", "Send for refurbishment"), theme.BtnHotStyle))
		{
			string text = Services.RequestRefurbish(_company);
			Refurbishment.ScannedAt = -1f;
			if (text == null)
			{
				text = L.K("ui.meridian.refurbishment.receivedBackTomorrow", "收到。明天送回。", "Received. Back tomorrow.");
			}
			Toast(text);
		}
		GUI.enabled = enabled;
	}

}

