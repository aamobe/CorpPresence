using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.Malice;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{
	private static int _bloodRecoveryCount;

	private static void DrawCarmichael()
	{
		Theme theme = _theme;
		CompanyState state = State;
		float width = _rect.width;
		float height = _rect.height;
		Rect r = new Rect(20f, 8f, 560f, 90f);
		if (theme.Banner != null)
		{
			Ui.Image(r, theme.Banner);
			GUI.Label(new Rect(r.x + 120f, r.y + 16f, 420f, 58f), _company.Name + L.K("ui.carmichael.overview.pharmaceuticalOrderForm", " · 药品订货单", " · Pharmaceutical Order Form"), theme.Title);
		}
		else
		{
			Ui.Fill(new Rect(36f, 34f, 36f, 12f), theme.Accent);
			Ui.Fill(new Rect(48f, 22f, 12f, 36f), theme.Accent);
			GUI.Label(new Rect(88f, 18f, 520f, 44f), _company.Name + L.K("ui.carmichael.overview.pharmaceuticalOrderForm", " · 药品订货单", " · Pharmaceutical Order Form"), theme.Title);
		}
		GUI.Label(new Rect(width - 260f, 30f, 220f, 22f), (L.En ? L.K("ui.carmichael.overview.formCPDay", (System.FormattableString)$"Form CP-{_company.PhoneNumber}   Day {Day}") : $"表单 CP-{_company.PhoneNumber}\u3000第 {Day} 天"), theme.Small);
		Ui.Fill(new Rect(20f, 96f, width - 40f, 2f), theme.Accent);
		Ui.Fill(new Rect(20f, 100f, width - 40f, 1f), theme.Line);
		GUI.Label(new Rect(24f, 104f, width - 48f, 22f), L.K("ui.carmichael.overview.service", "客服：", "Service: ") + _desk, theme.Body);
		Rect position = new Rect(20f, 134f, 220f, height - 154f);
		GUI.Box(position, GUIContent.none, theme.CellBox);
		Rect r2 = new Rect(position.x + 10f, position.y + 10f, 200f, 146f);
		Ui.Fill(r2, Tex.Hex(13688022));
		Texture2D texture2D = Portraits.Texture("carmicheal_isa", "femaleUpper_1");
		if (texture2D != null)
		{
			Ui.Image(new Rect(r2.x + 2f, r2.y + 2f, r2.width - 4f, r2.height - 4f), texture2D);
		}
		GUI.Label(new Rect(position.x + 10f, r2.yMax + 4f, 200f, 20f), L.K("ui.carmichael.overview.handledByIsaLangdonMarket", "经办：伊莎·兰登（市场规范部）", "Handled by: Isa Langdon (Market Compliance)"), theme.Small);
		GUI.Label(new Rect(position.x + 10f, r2.yMax + 28f, 200f, 22f), (L.En ? L.K("ui.carmichael.overview.shop", "Your shop: ") + TierLabel(state) : "贵店：" + TierLabel(state)), theme.Body);
		CoopBar(new Rect(position.x + 10f, r2.yMax + 54f, 200f, 8f));
		GUI.Label(new Rect(position.x + 10f, r2.yMax + 64f, 200f, 20f), NextTierHint(), theme.Small);
		float num = r2.yMax + 96f;
		for (int i = 0; i < CarmModules.Length; i++)
		{
			string text = ((i == _module) ? "› " : "  ") + CarmModules[i];
			if (Ui.Button(new Rect(position.x + 6f, num + (float)(i * 32), position.width - 12f, 28f), text, (i == _module) ? theme.NavOnStyle : theme.NavStyle))
			{
				if (i == 3 && _module != 3 && CarmichaelCo.Licensed)
					_bloodRecoveryCount = Services.BloodRecoveryCount();
				_module = i;
			}
		}
		Rect rect = new Rect(position.xMax + 20f, 134f, width - position.xMax - 40f, height - 154f);
		GUI.Label(new Rect(rect.x, rect.y, rect.width, 30f), CarmModules[_module], theme.Title);
		Ui.Fill(new Rect(rect.x, rect.y + 34f, rect.width, 1f), theme.Line);
		Rect rect2 = new Rect(rect.x, rect.y + 44f, rect.width, rect.height - 44f);
		if (DrawTermsIfSelected(CarmBase.Length, rect2))
		{
			return;
		}
		if (_company.Tier(state) < 1 && _module < 3)
		{
			GUI.Label(new Rect(rect2.x, rect2.y, rect2.width, 60f), L.K("ui.carmichael.overview.shopNotRegisteredYetDeal", "贵店尚未登记。和伊莎多打几次交道吧。", "Your shop is not registered yet. Deal with Isa a few more times."), theme.Wrap);
			return;
		}
		switch (_module)
		{
		case 0:
			DrawCarmOrder(rect2);
			break;
		case 1:
			DrawCarmStamina(rect2);
			break;
		case 2:
			DrawCarmSwap(rect2);
			break;
		case 3:
			DrawCarmSpecimen(rect2);
			break;
		default:
			DrawBrand(rect2);
			break;
		}
	}

	private static int CarmUnitPrice(CatalogEntry e)
	{
		double rate = Company.OrderRate(_company, _company.Tier(State));
		return e.Quote(rate);
	}

	private static string CarmLocked(CatalogEntry e) => _company.OrderEntryError(e, State);

	private static void DrawCarmOrder(Rect r)
	{
		Theme theme = _theme;
		CompanyState state = State;
		bool flag = Services.CanOrderToday(_company);
		GUI.Label(new Rect(r.x, r.y, r.width, 20f), flag ? L.K("ui.carmichael.orders.orderTodayDeliveredTomorrowOne", "今天下单，明天送到。每天一单。", "Order today, delivered tomorrow. One order per day. ") : L.K("ui.carmichael.orders.alreadyOrderedToday", "今天已经下过单了。", "Already ordered today. "), theme.Small);
		float num3 = r.y + 28f;
		Rect header = new Rect(r.x, num3, r.width, 26f);
		Ui.Fill(header, Tex.Hex(13886431));
		DrawOrderHeader(header, theme.Body, theme.Center);
		num3 += 30f;
		CatalogEntry[] entries = CarmichaelCo.Entries;
		foreach (CatalogEntry catalogEntry in entries)
		{
			Rect rect = new Rect(r.x, num3, r.width, 34f);
			var columns = OrderColumns(rect);
			Rect status = new Rect(columns.Unit.x, num3, columns.ListPrice.x - columns.Unit.x, 34f);
			Ui.Fill(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), theme.Line);
			int val = Math.Min(10, Services.Left(state, catalogEntry));
			DrawOrderItem(columns.Item, catalogEntry, state, theme);
			GUI.Label(columns.ListPrice, ItemListPrice(ItemListValue(catalogEntry)), theme.Center);
			string text2 = CarmLocked(catalogEntry);
			if (text2 != null)
			{
				GUI.Label(status, text2, theme.Small);
				Orders.Cart.Remove(catalogEntry);
				num3 += 36f;
				continue;
			}
			int num4 = CarmUnitPrice(catalogEntry);
			if (num4 <= 0)
			{
				GUI.Label(status, L.K("ui.carmichael.orders.unavailable", "暂不可用", "Unavailable"), theme.Small);
				Orders.Cart.Remove(catalogEntry);
				num3 += 36f;
				continue;
			}
			GUI.Label(columns.Unit, $"{num4}₵", theme.Price);
			Orders.Cart.TryGetValue(catalogEntry, out var value);
			value = Ui.Qty(columns.Quantity, value, Math.Min(10, val), theme.BtnStyle, theme.Center);
			if (value > 0)
			{
				Orders.Cart[catalogEntry] = value;
			}
			else
			{
				Orders.Cart.Remove(catalogEntry);
			}
			GUI.Label(columns.Subtotal, (value > 0) ? $"{num4 * value}₵" : "—", theme.Center);
			num3 += 36f;
		}
		int num5 = Orders.Cart.Sum((KeyValuePair<CatalogEntry, int> kv) => CarmUnitPrice(kv.Key) * kv.Value);
		float num6 = r.yMax - 44f;
		GUI.Label(new Rect(r.x, num6, 200f, 22f), (L.En ? L.K("ui.carmichael.orders.total", (System.FormattableString)$"Total {num5}₵") : $"合计 {num5}₵"), theme.Price);
		int cashDue = DrawCheckoutBalance(new Rect(r.x, num6 + 22f, r.width - 170f, 20f), num5);
		bool enabled2 = GUI.enabled;
		GUI.enabled = enabled2 && num5 > 0 && Services.CanAfford(cashDue) && flag;
		if (Ui.Button(new Rect(r.xMax - 170f, num6 + 2f, 160f, 36f), L.K("ui.carmichael.orders.submitOrder", "提交订货单", "Submit order"), theme.BtnHotStyle))
		{
			string text3 = Services.PlaceOrder(_company, Orders.Cart, CarmUnitPrice);
			if (text3 == null)
			{
				Orders.Cart.Clear();
				Toast(_company.FrontDeskDone);
			}
			else
			{
				Toast(text3);
			}
		}
		GUI.enabled = enabled2;
	}

	private static void DrawCarmStamina(Rect r)
	{
		Theme theme = _theme;
		CompanyState state = State;
		bool flag = CarmichaelCo.StaminaOpen();
		int num = Services.StaminaNextDay(state);
		Rect position = new Rect(r.x, r.y, r.width, 250f);
		GUI.Box(position, GUIContent.none, theme.CellBox);
		float num2 = position.x + 16f;
		float width = position.width - 32f;
		float num3 = position.y + 12f;
		GUI.Label(new Rect(num2, num3, width, 26f), L.K("ui.carmichael.stamina.requestFormStaminaBoosterUnreleased", "申请单 · 体力增强剂（未上市）", "Request Form · Stamina Booster (unreleased)"), theme.Body);
		num3 += 32f;
		Ui.Fill(new Rect(num2, num3, width, 1f), theme.Line);
		num3 += 8f;
		GUI.Label(new Rect(num2, num3, width, 22f), L.K("ui.carmichael.stamina.requestingPharmacyShop", "申请药房：贵店", "Requesting pharmacy: your shop"), theme.Body);
		num3 += 26f;
		GUI.Label(new Rect(num2, num3, width, 22f), L.K("ui.carmichael.stamina.handlerIsaLangdonMarketCompliance", "经办人：伊莎·兰登（市场规范部）", "Handler: Isa Langdon (Market Compliance)"), theme.Body);
		num3 += 26f;
		GUI.Label(new Rect(num2, num3, width, 22f), L.K("ui.carmichael.stamina.drugCost", "药品费用：0₵", "Drug cost: 0₵"), theme.Body);
		num3 += 26f;
		GUI.Label(new Rect(num2, num3, 90f, 24f), L.K("ui.carmichael.stamina.fee", "手续费：", "Fee:"), theme.Body);
		GUI.Label(new Rect(num2 + 70f, num3, 120f, 24f), $"{CarmichaelCo.StaminaFee}₵", theme.Price);
		num3 += 30f;
		GUI.Label(new Rect(num2, num3, width, 22f), (L.En ? L.K("ui.carmichael.stamina.noteSpecialRequestPerDays", (System.FormattableString)$"Note: special request, 1 per {CarmichaelCo.StaminaGap} days. Hand-delivered the next day.") : $"备注：特别申请，每 {CarmichaelCo.StaminaGap} 天限 1 支。次日专人配送。"), theme.Small);
		Rect position2 = new Rect(position.xMax - 150f, position.y + 54f, 120f, 48f);
		Color hot = theme.Hot;
		Ui.Fill(new Rect(position2.x, position2.y, position2.width, 2f), hot);
		Ui.Fill(new Rect(position2.x, position2.yMax - 2f, position2.width, 2f), hot);
		Ui.Fill(new Rect(position2.x, position2.y, 2f, position2.height), hot);
		Ui.Fill(new Rect(position2.xMax - 2f, position2.y, 2f, position2.height), hot);
		GUI.Label(position2, L.K("ui.carmichael.stamina.specialStamp", "特别申请", "SPECIAL"), theme.Price);
		float num4 = position.yMax + 16f;
		if (!flag)
		{
			GUI.Label(new Rect(r.x, num4, r.width, 44f), L.K("ui.carmichael.stamina.thisNewProductHasnT", "新品尚未向贵店配发。伊莎·兰登为贵店争取到配额后开放。", "This new product hasn't been allocated to your shop yet. It opens once Isa Langdon secures you a quota."), theme.Wrap);
			return;
		}
		bool flag2 = Day >= num;
		GUI.Label(new Rect(r.x, num4, r.width, 22f), flag2 ? L.K("ui.carmichael.stamina.currentQuotaAvailable", "本期配额：可申领。", "Current quota: available.") : (L.En ? L.K("ui.carmichael.stamina.nextAvailableDayDaysLeft", (System.FormattableString)$"Next available: day {num} ({num - Day} days left)") : $"下次可申领：第 {num} 天（还有 {num - Day} 天）"), theme.Body);
		int fee = CarmichaelCo.StaminaFee;
		int cashDue = DrawVoucherDetails(new Rect(r.x + 270f, num4 + 40f, r.width - 270f, 20f), flag2 ? fee : 0);
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && flag2 && Services.CanAfford(cashDue);
		if (Ui.Button(new Rect(r.x, num4 + 32f, 260f, 36f), flag2 ? (L.En ? L.K("ui.carmichael.stamina.submitRequestFee", (System.FormattableString)$"Submit request (fee {fee}₵)") : $"提交申请（手续费 {fee}₵）") : L.K("ui.carmichael.stamina.batchNotReady", "批次未到", "Batch not ready"), theme.BtnHotStyle))
		{
			Toast(Services.RequestStamina(_company) ?? L.K("ui.carmichael.stamina.requestAcceptedHandDeliveredTomorrow", "申请已受理。明早由专人配送。", "Request accepted. Hand-delivered tomorrow morning."));
		}
		GUI.enabled = enabled;
	}

	private static void ScanFakes()
	{
		if (Swap.ScannedAt >= 0f && Time.unscaledTime - Swap.ScannedAt < 1f)
		{
			return;
		}
		Swap.ScannedAt = Time.unscaledTime;
		Swap.Rows.Clear();
		List<GameItem> list = Services.FakesOwned();
		Swap.TotalCount = list.Count;
		foreach (GameItem item2 in list.Take(6))
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
			Swap.Rows.Add((item, CarmichaelCo.FakeReason(item2), ItemName(CarmichaelCo.SwapTarget(item2))));
		}
	}

	private static void DrawCarmSpecimen(Rect r)
	{
		Theme theme = _theme;
		if (!CarmichaelCo.Licensed)
		{
			GUI.Label(new Rect(r.x, r.y, r.width, 40f), "？？？", theme.Wrap);
			return;
		}
		GUI.Label(new Rect(r.x, r.y, r.width, 44f), L.K("ui.carmichael.specimens.breedingLicenseFileKeepSilverblood", "养殖许可在册。银血三叶蜥养在循环水养殖系统里，成年后能采血；离开水也不会死，只是不积血。原价，明天送到，不限量。", "Breeding license on file. Keep Silverblood Trilobites in an aquaculture system; adults can be bled. Out of water they survive but build no blood. List price, delivered tomorrow, no limit."), theme.Wrap);
		DrawBloodRecovery(new Rect(r.x, r.y + 48f, r.width, 96f));
		float num = r.y + 154f;
		List<(string, int)> list = new List<(string, int)>
		{
			("@silverblood", 500),
			("@blood_drawer", 250)
		};
		if (CarmichaelCo.ReactorOpen)
		{
			list.Add(("@isomer_reactor", 400));
			list.Add(("@sb_handbook", 50));
		}
		foreach (var item3 in list)
		{
			string item = item3.Item1;
			int item2 = item3.Item2;
			Brand.Entry entry = Brand.Info(item);
			Rect position = new Rect(r.x, num, 56f, 56f);
			GUI.Box(position, GUIContent.none, theme.CellBox);
			if (entry.Icon != null)
			{
				Ui.Image(new Rect(position.x + 4f, position.y + 4f, 48f, 48f), entry.Icon);
			}
			GUI.Label(new Rect(position.xMax + 12f, num + 3f, r.width - 242f, 26f), entry.Name, theme.Body);
			GUI.Label(new Rect(position.xMax + 12f, num + 30f, 120f, 22f), $"{item2}₵", theme.Price);
			int cashDue = DrawVoucherDetails(new Rect(position.xMax + 140f, num + 30f, r.width - 366f, 22f), item2);
			bool enabled = GUI.enabled;
			GUI.enabled = enabled && Services.CanAfford(cashDue);
			if (Ui.Button(new Rect(r.xMax - 170f, num + 10f, 160f, 36f), L.K("ui.carmichael.specimens.orderOne", "订一份", "Order one"), theme.BtnHotStyle))
			{
				Toast(CarmichaelCo.OrderSpecimen(_company, item, item2) ?? _company.FrontDeskDone);
			}
			GUI.enabled = enabled;
			num += 64f;
		}
		GUI.Label(new Rect(r.x, r.yMax - 22f, 200f, 20f), (L.En ? L.K("ui.carmichael.specimens.cash", (System.FormattableString)$"Cash {Cash}₵") : $"现金 {Cash}₵"), theme.Small);
	}

	private static void DrawBloodRecovery(Rect r)
	{
		var state = State;
		if (state == null) return;
		var theme = _theme;
		int nextDay = Services.BloodRecoveryNextDay(state);
		int payout = _bloodRecoveryCount * Services.BloodRecoveryPrice;
		GUI.Box(r, GUIContent.none, theme.CellBox);
		GUI.Label(new Rect(r.x + 8f, r.y + 4f, r.width - 16f, 22f), L.K("ui.carmichael.bloodRecovery.silverBloodRecovery", "银血样品回收", "Silver Blood Recovery"), theme.Body);
		GUI.Label(new Rect(r.x + 8f, r.y + 26f, r.width - 16f, 34f),
			L.K("ui.carmichael.bloodRecovery.fullUnalteredSamplesEachUp", $"完整原配方样品 {Services.BloodRecoveryPrice}₵/支，最多 {Services.BloodRecoveryLimit} 支，冷却 {Services.BloodRecoveryGap} 天。", (System.FormattableString)$"Full, unaltered samples: {Services.BloodRecoveryPrice}₵ each, up to {Services.BloodRecoveryLimit}, every {Services.BloodRecoveryGap} days."), theme.Wrap);
		string summary = Day < nextDay
			? L.K("ui.carmichael.bloodRecovery.availableAgainDay", $"第 {nextDay} 天可再次回收", (System.FormattableString)$"Available again on day {nextDay}")
			: L.K("ui.carmichael.bloodRecovery.samplesPayout", $"本次 {_bloodRecoveryCount} 支 · 到账 {payout}₵", (System.FormattableString)$"{_bloodRecoveryCount} samples · {payout}₵ payout");
		GUI.Label(new Rect(r.x + 8f, r.y + 63f, r.width - 170f, 25f), summary, theme.Price);
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && Day >= nextDay && _bloodRecoveryCount > 0;
		if (Ui.Button(new Rect(r.xMax - 150f, r.y + 59f, 142f, 30f), L.K("ui.carmichael.bloodRecovery.confirmRecovery", "确认回收", "Confirm recovery"), theme.BtnHotStyle))
		{
			Toast(Services.RequestBloodRecovery(_company, _bloodRecoveryCount));
			_bloodRecoveryCount = Services.BloodRecoveryCount();
		}
		GUI.enabled = enabled;
	}

	private static void DrawCarmSwap(Rect r)
	{
		Theme theme = _theme;
		CompanyState state = State;
		ScanFakes();
		GUI.Label(new Rect(r.x, r.y, r.width, 64f), (L.En ? L.K("ui.carmichael.swap.drugsSoldUnderCarmichealName", (System.FormattableString)$"Drugs sold under the Carmicheal name without authorization are recalled by the company and replaced one for one with the genuine article: counterfeit Immunivax, recalled Immunivax batches, and unlicensed Phagimycin. Expired genuine products are not included.\nOne request every {CarmichaelCo.SwapGap} days, up to {6} items each time, {CarmichaelCo.SwapFee}₵ per item.") : $"市面上冒用卡迈克尔名义的药品，由公司回收，按 1 换 1 替换为正品：假冒的免疫宁、召回批次的免疫宁、无证噬菌霉素。过期的正品不在此列。\n每 {CarmichaelCo.SwapGap} 天可申请一次，每次最多 {6} 件，每件手续费 {CarmichaelCo.SwapFee}₵。"), theme.Wrap);
		float num = r.y + 74f;
		Ui.Fill(new Rect(r.x, num, r.width, 26f), Tex.Hex(13886431));
		GUI.Label(new Rect(r.x + 8f, num, 240f, 26f), L.K("ui.carmichael.swap.problemDrugsShop", "贵店持有的问题药品", "Problem drugs in your shop"), theme.Body);
		GUI.Label(new Rect(r.x + 250f, num, 110f, 26f), L.K("ui.carmichael.swap.issue", "问题", "Issue"), theme.Center);
		GUI.Label(new Rect(r.x + 360f, num, r.width - 360f, 26f), L.K("ui.carmichael.swap.replaceWith", "替换为", "Replace with"), theme.Center);
		num += 30f;
		if (Swap.Rows.Count == 0)
		{
			GUI.Label(new Rect(r.x + 8f, num, r.width, 30f), L.K("ui.carmichael.swap.noneFound", "没有找到。", "None found."), theme.Small);
			num += 34f;
		}
		foreach (var swapRow in Swap.Rows)
		{
			GUI.Label(new Rect(r.x + 8f, num, 240f, 30f), swapRow.Name, theme.Body);
			GUI.Label(new Rect(r.x + 250f, num, 110f, 30f), swapRow.Reason, theme.Price);
			GUI.Label(new Rect(r.x + 360f, num, r.width - 360f, 30f), L.K("ui.carmichael.swap.genuine", "正品", "Genuine ") + swapRow.Target, theme.Center);
			Ui.Fill(new Rect(r.x, num + 30f, r.width, 1f), theme.Line);
			num += 32f;
		}
		if (Swap.TotalCount > Swap.Rows.Count)
		{
			GUI.Label(new Rect(r.x + 8f, num, r.width, 22f), (L.En ? L.K("ui.carmichael.swap.moreSwapThemNextTime", (System.FormattableString)$"{Swap.TotalCount - Swap.Rows.Count} more; swap them next time.") : $"另有 {Swap.TotalCount - Swap.Rows.Count} 件，下次再换。"), theme.Small);
		}
		int num2 = Services.SwapNextDay(state);
		bool flag = Day >= num2;
		int num3 = Swap.Rows.Count * CarmichaelCo.SwapFee;
		float num4 = r.yMax - 44f;
		GUI.Label(new Rect(r.x, num4, 300f, 22f), flag ? (L.En ? L.K("ui.carmichael.swap.itemsThisTimeFee", (System.FormattableString)$"{Swap.Rows.Count} items this time, fee {num3}₵") : $"本次 {Swap.Rows.Count} 件，手续费 {num3}₵") : (L.En ? L.K("ui.carmichael.swap.nextRequestDay", (System.FormattableString)$"Next request: day {num2}") : $"下次可申请：第 {num2} 天"), flag ? theme.Price : theme.Body);
		int cashDue = DrawCheckoutBalance(new Rect(r.x, num4 + 22f, r.width - 170f, 20f), flag ? num3 : 0);
		bool enabled = GUI.enabled;
		GUI.enabled = enabled && flag && Swap.Rows.Count > 0 && Services.CanAfford(cashDue);
		if (Ui.Button(new Rect(r.xMax - 170f, num4 + 2f, 160f, 36f), L.K("ui.carmichael.swap.requestSwap", "申请换货", "Request swap"), theme.BtnHotStyle))
		{
			string text = Services.RequestSwap(_company);
			Swap.ScannedAt = -1f;
			if (text == null)
			{
				text = L.K("ui.carmichael.swap.problemDrugsCollectedGenuineReplacements", "问题药品已收回。明早送来正品。", "Problem drugs collected. Genuine replacements arrive tomorrow morning.");
			}
			Toast(text);
		}
		GUI.enabled = enabled;
	}

}

