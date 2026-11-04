using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.Malice;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{

	private static int SolanaNeed(int i)
	{
		if (i >= SolanaModuleTier.Length)
		{
			return 0;
		}
		return SolanaModuleTier[i];
	}

	private static void DrawSolana()
	{
		Theme theme = _theme;
		CompanyState state = State;
		float width = _rect.width;
		float height = _rect.height;
		Rect r = new Rect((width - 560f) / 2f, 8f, 560f, 90f);
		if (theme.Banner != null)
		{
			Ui.Image(r, theme.Banner);
		}
		else
		{
			Ui.Fill(new Rect(60f, 40f, width / 2f - 160f, 1f), theme.Line);
			Ui.Fill(new Rect(width / 2f + 100f, 40f, width / 2f - 160f, 1f), theme.Line);
		}
		GUIStyle bannerTitle = theme.BannerTitle;
		GUI.Label(new Rect(r.x + 192f - 40f, r.y + 52f, 256f, 28f), _company.Name, bannerTitle);
		GUIStyle desk = theme.Desk;
		GUI.Label(new Rect(0f, 102f, width, 22f), _desk, desk);
		Rect rect = new Rect(20f, 130f, 230f, height - 150f);
		Ui.Fill(new Rect(rect.xMax + 10f, rect.y, 1f, rect.height), theme.Line);
		Rect position = new Rect(rect.x + 15f, rect.y, 200f, 146f);
		GUI.Box(position, GUIContent.none, theme.CellBox);
		Texture2D texture2D = Portraits.Texture("solana_seline", "femaleUpper_1");
		if (texture2D != null)
		{
			Ui.Image(new Rect(position.x + 4f, position.y + 4f, position.width - 8f, position.height - 8f), texture2D);
		}
		GUI.Label(new Rect(rect.x, position.yMax + 4f, rect.width, 20f), L.T("专属顾问 · 瑟琳", "Personal Advisor · Seline"), desk);
		GUI.Label(new Rect(rect.x + 10f, position.yMax + 30f, rect.width - 20f, 22f), (L.En ? "Membership: " + TierLabel(state) : "会员：" + TierLabel(state)), theme.Body);
		CoopBar(new Rect(rect.x + 10f, position.yMax + 56f, rect.width - 20f, 8f));
		GUI.Label(new Rect(rect.x + 10f, position.yMax + 66f, rect.width - 20f, 20f), NextTierHint(), theme.Small);
		float num = position.yMax + 96f;
		for (int i = 0; i < SolanaModules.Length; i++)
		{
			bool flag = _company.Tier(state) >= SolanaNeed(i);
			string text = ((i == _module) ? "· " : "  ") + SolanaModules[i] + (flag ? "" : (L.En ? " (" + _company.TierNames[SolanaNeed(i) - 1] + ")" : "（" + _company.TierNames[SolanaNeed(i) - 1] + "）"));
			if (Ui.Button(new Rect(rect.x + 6f, num + (float)(i * 30), rect.width - 12f, 26f), text, (i == _module) ? theme.NavOnStyle : theme.NavStyle))
			{
				_module = i;
			}
		}
		Rect rect2 = new Rect(rect.xMax + 30f, 130f, width - rect.xMax - 50f, height - 150f);
		bool flag2 = _company.Tier(state) >= SolanaNeed(_module);
		GUI.Label(new Rect(rect2.x, rect2.y, rect2.width, 30f), SolanaModules[_module], theme.Title);
		Ui.Fill(new Rect(rect2.x, rect2.y + 34f, rect2.width, 1f), theme.Line);
		Rect rect3 = new Rect(rect2.x, rect2.y + 46f, rect2.width, rect2.height - 46f);
		if (DrawExtraIfSelected(SolanaBase.Length, rect3))
		{
			return;
		}
		if (!flag2)
		{
			GUI.Label(new Rect(rect3.x, rect3.y, rect3.width, 60f), (L.En ? "Exclusive to " + _company.TierNames[SolanaModuleTier[_module] - 1] + "s." : _company.TierNames[SolanaModuleTier[_module] - 1] + "专享。"), theme.Wrap);
			return;
		}
		switch (_module)
		{
		case 0:
			DrawSolanaVip(rect3);
			break;
		case 1:
			DrawSolanaLab(rect3);
			break;
		case 2:
			DrawSolanaGift(rect3);
			break;
		default:
			DrawBrand(rect3);
			break;
		}
	}

	private static void DrawSolanaVip(Rect r)
	{
		Theme theme = _theme;
		CompanyState state = State;
		GUI.Label(new Rect(r.x, r.y, r.width, 90f), (L.En ? $"Solana introduces you to Upper Levels clientele: the day after you book, a Solana VIP with a budget of {SolanaCo.VipBudgetText()} visits your shop, looking for luxury goods, cigarettes and alcohol.\n\n{SolanaCo.VipFee}₵ per booking, paid when you book; one booking every {SolanaCo.VipGap} days." : $"索拉纳为您引荐上层区的客人：预约后第二天会有一位预算在 {SolanaCo.VipBudgetText()} 的索拉纳贵宾来店，想买奢侈品和烟酒。\n\n每次 {SolanaCo.VipFee}₵，预约时支付；每 {SolanaCo.VipGap} 天可预约一次。"), theme.Wrap);
		int num = Services.VipNextDay(state);
		bool flag = Day >= num;
		if (Ui.Button(new Rect(r.x, r.y + 120f, 240f, 34f), flag ? (L.En ? $"Book a VIP ({SolanaCo.VipFee}₵)" : $"预约贵宾（{SolanaCo.VipFee}₵）") : (L.En ? $"Available again on day {num}" : $"第 {num} 天可再预约"), flag ? theme.BtnHotStyle : theme.BtnStyle) & flag)
		{
			Toast(Services.BookVip(_company) ?? L.T("已为您安排，明天专人引荐。", "Arranged. Your VIP arrives tomorrow."));
		}
	}

	private static void DrawSolanaLab(Rect r)
	{
		Theme theme = _theme;
		CompanyState state = State;
		List<string> list = Services.LabListThisWeek(state);
		GUI.Label(new Rect(r.x, r.y, r.width, 40f), (L.En ? $"The lab needs the items below (a new list every {SolanaCo.LabPeriod} days). Book a buyer and they will come by the next day to purchase." : $"实验室这一期需要以下物品（每 {SolanaCo.LabPeriod} 天换一张单子）。预约后，采购员第二天上门收购。"), theme.Wrap);
		float num = (r.width - 20f) / 3f;
		for (int i = 0; i < list.Count; i++)
		{
			Rect position = new Rect(r.x + (float)i * (num + 10f), r.y + 50f, num, 130f);
			GUI.Box(position, GUIContent.none, theme.CellBox);
			string text = ItemName(list[i]);
			GUI.Label(new Rect(position.x + 8f, position.y + 10f, position.width - 16f, 60f), text, theme.Wrap);
			GUI.Label(new Rect(position.x, position.yMax - 40f, position.width, 24f), (L.En ? $"× {SolanaCo.LabMultiplier(list[i]):0.#}" : $"× {SolanaCo.LabMultiplier(list[i]):0.#} 倍"), theme.Price);
		}
		if (state.LabBooked)
		{
			GUI.Label(new Rect(r.x, r.y + 200f, r.width, 24f), L.T("这一期已预约。", "Booked for this list."), theme.Body);
		}
		else if (Ui.Button(new Rect(r.x, r.y + 200f, 200f, 34f), L.T("预约采购员", "Book a buyer"), theme.BtnHotStyle))
		{
			Toast(Services.BookLab(_company) ?? _company.FrontDeskDone);
		}
	}

	private static void DrawSolanaGift(Rect r)
	{
		Theme theme = _theme;
		CompanyState state = State;
		GUI.Label(new Rect(r.x, r.y, r.width, 60f), (L.En ? $"Black Gold Members may claim one bottle of SYNC Neuroactive Perfume every {SolanaCo.GiftPeriod} days, hand-delivered the next day." : $"黑金会员每 {SolanaCo.GiftPeriod} 天可领取一瓶“同步”神经活性香水，次日专人送达。"), theme.Wrap);
		bool flag = Services.GiftReady(state);
		bool enabled = GUI.enabled;
		GUI.enabled = flag;
		if (Ui.Button(new Rect(r.x, r.y + 80f, 200f, 34f), flag ? L.T("领取礼遇", "Claim gift") : L.T("这一期已领取", "Claimed for this period"), theme.BtnHotStyle))
		{
			Toast(Services.ClaimGift(_company) ?? _company.FrontDeskDone);
		}
		GUI.enabled = enabled;
	}

}

