using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.Malice;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{

	private static void DrawEnvoy()
	{
		Theme theme = _theme;
		EnvoyState envoyState = MaliceState.Get()?.Envoy;
		float width = _rect.width;
		Ui.Fill(new Rect(20f, 20f, width - 40f, 56f), theme.Ink);
		GUI.Label(new Rect(36f, 20f, width - 80f, 56f), _company.Name, theme.InvTitle);
		GUI.Label(new Rect(24f, 90f, width - 48f, 22f), L.T("撒该：", "Zacchaeus: ") + _desk, theme.Body);
		Ui.Fill(new Rect(20f, 116f, width - 40f, 1f), theme.Line);
		if (envoyState == null)
		{
			return;
		}
		Rect rect = new Rect(30f, 130f, width - 60f, _rect.height - 150f);
		float y = rect.y;
		GUI.Label(new Rect(rect.x, y, rect.width, 24f), L.T("援助", "Aid"), theme.Title);
		y += 30f;
		string text = envoyState.Aid switch
		{
			"active" => L.T($"扶持期：第 {envoyState.AidStart + Envoy.AidWeeks * 7} 天之前，帕斯泰尔和卡迈克尔的订购服务不能断。", $"Support period: keep Pastel and Carmicheal subscribed until day {envoyState.AidStart + Envoy.AidWeeks * 7}."),
			"repay5" => L.T($"还款中：还剩 {envoyState.RepayLeft} 期，每期 {Envoy.Installment}₵，第 {envoyState.RepayDue} 天他来收。交不上会受罚。", $"Repaying: {envoyState.RepayLeft} installments of {Envoy.Installment}₵ left; he collects on day {envoyState.RepayDue}. Missing one brings a penalty."),
			"plan6" => L.T($"还款计划：还剩 {envoyState.RepayLeft} 期，每期 {Envoy.Installment}₵，第 {envoyState.RepayDue} 天他来收。", $"Repayment plan: {envoyState.RepayLeft} installments of {Envoy.Installment}₵ left; he collects on day {envoyState.RepayDue}.") + ((envoyState.LateWeeks > 0) ? L.T($"已拖 {envoyState.LateWeeks} 周，巨企订货 +{Envoy.LateOrderSurchargePercent(envoyState.LateWeeks)}%。", $" {envoyState.LateWeeks} week(s) late: corporate orders +{Envoy.LateOrderSurchargePercent(envoyState.LateWeeks)}%.") : ""),
			"punished" => L.T("惩罚中：所有公司服务暂停（反抗军除外），每天少一位顾客。", "Penalty: all company services are suspended (except the Resistance), and one fewer customer comes each day."), 
			"done" => L.T("已结清。", "Settled."), 
			"declined" => L.T("你拒绝了援助。", "You declined the aid."), 
			_ => L.T("无。", "None."), 
		};
		GUI.Label(new Rect(rect.x, y, rect.width, 44f), text, theme.Wrap);
		y += 48f;
		if (envoyState.Aid == "punished")
		{
			int weeks = Envoy.RepaymentPlanWeeks(envoyState);
			string label = L.T($"改签：每周 {Envoy.Installment}₵，共 {weeks} 期（含违约费）", $"Reschedule: {weeks} × {Envoy.Installment}₵ (incl. penalty)");
			if (Ui.Button(new Rect(rect.x, y, 360f, 34f), label, theme.BtnHotStyle))
				Toast(Envoy.AgreePlan() ?? _company.FrontDeskDone);
			y += 42f;
		}
		if (envoyState.Ch2 != "")
		{
			Ui.Fill(new Rect(rect.x, y, rect.width, 1f), theme.Line);
			y += 8f;
			GUI.Label(new Rect(rect.x, y, rect.width, 24f), L.T("服务费", "Service fee"), theme.Title);
			y += 30f;
			string text2 = envoyState.Ch2 switch
			{
				"refused" => L.T("你拒绝了他。巨企的订货暂不开放，直到你交钱或收下礼物并办成。", "You turned him down. Corporate ordering stays closed until you pay or take the gift and deliver it."), 
				Envoy.BombPending => L.T("礼物等待领取。请留出库存空间，领取成功后才开始计时。", "The gift is waiting to be claimed. Make room in the shop. The timer starts once you receive it."),
				"bomb" => (L.En ? $"Book Odin through the Resistance Tasks page and deliver the gift by day {envoyState.BombDeadline}. The deadline pauses while he is unavailable." : $"在反抗军“任务”页预约奥丁，于第 {envoyState.BombDeadline} 天（含）之前交礼物；奥丁不可用时暂停期限。"), 
				"bombdone" => L.T("礼物送到了。巨企订购服务不收订阅费。", "Gift delivered. Corporate ordering services are subscription-free."), 
				"malice" => (Day < envoyState.ChannelUntil
					? L.T($"渠道费计提至第 {envoyState.ChannelUntil - 1} 天（含）。", $"Channel fees accrue through day {envoyState.ChannelUntil - 1}.")
					: L.T("渠道费已停止计提。", "Channel fees have stopped accruing.")) + " "
					+ L.T($"待结算 {envoyState.ChannelFee}₵，第 {envoyState.ChannelDue} 天起安排收款。", $"{envoyState.ChannelFee}₵ pending. Collection is scheduled from day {envoyState.ChannelDue}."),
				_ => L.T("已结清。", "Settled."), 
			};
			GUI.Label(new Rect(rect.x, y, rect.width, 44f), text2, theme.Wrap);
			y += 48f;
			if (envoyState.Ch2 == "refused")
			{
				if (Ui.Button(new Rect(rect.x, y, 220f, 34f), (L.En ? $"Pay {1500}₵" : $"付 {1500}₵"), theme.BtnHotStyle))
				{
					Toast(Envoy.PayServiceFee() ?? _company.FrontDeskDone);
				}
				if (Ui.Button(new Rect(rect.x + 230f, y, 220f, 34f), L.T("收下礼物", "Take the gift"), theme.BtnStyle))
				{
					Toast(Envoy.AcceptBomb() ?? L.T("礼物已入库，交付期限开始。", "The gift is in your shop. The delivery timer has started."));
				}
				y += 42f;
			}
			else if (envoyState.Ch2 == Envoy.BombPending)
			{
				if (Ui.Button(new Rect(rect.x, y, 220f, 34f), L.T("领取礼物", "Claim the gift"), theme.BtnHotStyle))
					Toast(Envoy.AcceptBomb() ?? L.T("礼物已入库，交付期限开始。", "The gift is in your shop. The delivery timer has started."));
				y += 42f;
			}
		}
		if (envoyState.Ch3 == "joined")
		{
			Ui.Fill(new Rect(rect.x, y, rect.width, 1f), theme.Line);
			y += 8f;
			GUI.Label(new Rect(rect.x, y, rect.width, 24f), L.T("各办各事", "Each to their own"), theme.Title);
			y += 30f;
			GUI.Label(new Rect(rect.x, y, rect.width, 44f), (L.En ? $"Pay Zacchaeus {3000}₵, then 1000₵ to make peace on the Resistance's line: the Resistance is back in touch (no more underground buyers) and your corporate perks stay." : $"给撒该 {3000}₵，再在反抗军的电话里花 1000₵ 和解：反抗军恢复联系（不再有地下买家），巨企的待遇照旧。"), theme.Wrap);
			y += 48f;
			if (!envoyState.BribedEnvoy && Ui.Button(new Rect(rect.x, y, 260f, 34f), (L.En ? $"Pay off Zacchaeus ({3000}₵)" : $"打点撒该（{3000}₵）"), theme.BtnHotStyle))
			{
				Toast(Envoy.BribeForReconcile() ?? _company.FrontDeskDone);
			}
			if (envoyState.BribedEnvoy)
			{
				GUI.Label(new Rect(rect.x, y, rect.width, 24f), L.T("已打点。", "Paid."), theme.Small);
			}
		}
	}

	private static void DrawRebel()
	{
		Theme theme = _theme;
		CompanyState state = State;
		MaliceState maliceState = MaliceState.Get();
		float width = _rect.width;
		float height = _rect.height;
		new Rect(20f, 8f, 560f, 90f);
		Ui.Fill(new Rect(20f, 20f, 420f, 56f), theme.Ink);
		GUI.Label(new Rect(36f, 20f, 400f, 56f), _company.Name, theme.InvTitle);
		GUI.Label(new Rect(600f, 40f, width - 640f, 22f), (L.En ? $"{_company.PhoneNumber}   Day {Day}" : $"{_company.PhoneNumber}\u3000第 {Day} 天"), theme.Small);
		Ui.Fill(new Rect(20f, 100f, width - 40f, 2f), theme.Ink);
		GUI.Label(new Rect(24f, 106f, width - 48f, 22f), L.T("奥丁：", "Odin: ") + _desk, theme.Body);
		Rect position = new Rect(20f, 136f, 190f, height - 156f);
		GUI.Box(position, GUIContent.none, theme.CellBox);
		GUI.Label(new Rect(position.x + 10f, position.y + 8f, 170f, 22f), (L.En ? "Standing: " + TierLabel(state) : "身份：" + TierLabel(state)), theme.Body);
		GUI.Label(new Rect(position.x + 10f, position.y + 32f, 170f, 20f), (L.En ? $"Trust {state.Coop}" : $"信任度 {state.Coop}"), theme.Small);
		CoopBar(new Rect(position.x + 10f, position.y + 54f, 170f, 8f));
		GUI.Label(new Rect(position.x + 10f, position.y + 64f, 170f, 20f), NextTierHint(), theme.Small);
		GUI.Label(new Rect(position.x + 10f, position.y + 88f, 170f, 22f), (L.En ? $"Support points {maliceState?.RebelPoints ?? 0}" : $"助力点 {maliceState?.RebelPoints ?? 0}"), theme.Body);
		GUI.Label(new Rect(position.x + 10f, position.y + 110f, 170f, 20f), (L.En ? $"Total earned {maliceState?.RebelEarned ?? 0}" : $"累计 {maliceState?.RebelEarned ?? 0}"), theme.Small);
		string[] rebelModules = RebelModules;
		if (_module >= rebelModules.Length)
		{
			_module = 0;
		}
		float num = position.y + 140f;
		for (int i = 0; i < rebelModules.Length; i++)
		{
			string text = ((i == _module) ? "› " : "  ") + rebelModules[i];
			if (Ui.Button(new Rect(position.x + 6f, num, position.width - 12f, 28f), text, (i == _module) ? theme.NavOnStyle : theme.NavStyle))
			{
				_module = i;
			}
			num += 32f;
		}
		Rect rect = new Rect(position.xMax + 20f, 136f, width - position.xMax - 40f, height - 156f);
		GUI.Label(new Rect(rect.x, rect.y, rect.width, 30f), rebelModules[_module], theme.HotTitle);
		Ui.Fill(new Rect(rect.x, rect.y + 34f, rect.width, 1f), theme.Ink);
		Rect r = new Rect(rect.x, rect.y + 44f, rect.width, rect.height - 44f);
		if (Rebels.Arrested)
		{
			DrawRescue(r);
		}
		else if (Rebels.Cut)
		{
			EnvoyState envoyState = maliceState?.Envoy;
			GUI.Label(new Rect(r.x, r.y, r.width, 80f), L.T("你加入了巨企，反抗军和你断了联系。\n花 1000₵ 和解（还要在撒该那边打点 3000₵）：反抗军恢复联系，但不再给你安排地下买家。", "You joined the corporations and the Resistance cut you off.\nPay 1000₵ to make peace (and pay off Zacchaeus 3000₵ on his line): the Resistance is back in touch, but no more underground buyers."), theme.Wrap);
			if (envoyState != null && envoyState.ReconciledRebels)
			{
				GUI.Label(new Rect(r.x, r.y + 90f, r.width, 24f), envoyState.Gone ? L.T("已付。明天开店时恢复联系。", "Paid. Back in touch from tomorrow's opening.") : L.T("已付。等撒该那边也打点好。", "Paid. Waiting on Zacchaeus's side."), theme.Small);
			}
			else if (Ui.Button(new Rect(r.x, r.y + 90f, 240f, 34f), L.T("和解（1000₵）", "Make peace (1000₵)"), theme.BtnHotStyle))
			{
				Toast(Envoy.ReconcileWithRebels() ?? _company.FrontDeskDone);
			}
		}
		else if (_company.Tier(state) < 1)
		{
			GUI.Label(new Rect(r.x, r.y, r.width, 40f), L.T("没人认识你。", "Nobody knows you."), theme.Wrap);
		}
		else if (_module == 0)
		{
			DrawSpacerOrder(r, "军械");
		}
		else if (rebelModules[_module] == L.T("任务", "Tasks"))
		{
			DrawRebelTasks(r);
		}
		else
		{
			DrawRebelFence(r);
		}
	}

	private static void DrawRebelFence(Rect r)
	{
		Theme theme = _theme;
		MaliceState maliceState = MaliceState.Get();
		GUI.Label(new Rect(r.x, r.y, r.width, 84f), (L.En ? $"Book an underground buyer for tomorrow: takes anything, pays cash at retail +10%, doesn't carry much. Selling to him won't raise Security's investigation.\nOne booking every {5} days for {50} support points; book early for {25} more per day early, up to {150}." : $"约一位地下买家明天上门：什么都收，按零售价加 10% 付现钱，带的钱不多。卖给他的东西不会让治安部的调查进度上涨。\n每 {5} 天可以约一次，花 {50} 助力点；等不及就紧急预约，每提前一天多花 {25} 点，最多 {150} 点。"), theme.Wrap);
		int num = Rebels.FenceCost();
		bool flag = num > 50;
		GUI.Label(new Rect(r.x, r.y + 96f, r.width, 22f), (L.En ? $"Booking now: {num} points" + (flag ? " (early)" : "") : $"现在预约：{num} 点" + (flag ? "（紧急预约）" : "")), theme.Price);
		GUI.Label(new Rect(r.x, r.y + 120f, r.width, 22f), L.T($"助力点余额 {maliceState?.RebelPoints ?? 0}。向反抗军出售或订货，每笔每满 {Rebels.PointDivisor}₵ 得 1 点。", $"Support points: {maliceState?.RebelPoints ?? 0}. Earn 1 point per {Rebels.PointDivisor}₵ on each sale or order."), theme.Small);
		bool enabled = GUI.enabled;
		GUI.enabled = (maliceState?.RebelPoints ?? 0) >= num;
		if (Ui.Button(new Rect(r.x, r.y + 152f, 240f, 34f), (L.En ? $"Book ({num} points)" : $"预约（{num} 点）"), theme.BtnHotStyle))
		{
			Toast(Rebels.BookFence() ?? _company.FrontDeskDone);
		}
		GUI.enabled = enabled;
	}

	private static void DrawRescue(Rect r)
	{
		Theme theme = _theme;
		RebelState rebelState = MaliceState.Get()?.Rebel;
		GUI.Label(new Rect(r.x, r.y, r.width, 64f), (L.En ? $"Odin has been arrested; orders and the fence network are suspended.\nTo get him out, the Resistance needs {350}₵ worth of medical supplies and weapons. Call for a pickup when ready; someone comes the next day. You can hand them over in several trips (no payment)." : $"奥丁被捕了，订货和地下销赃网络都停了。\n要救他出来，反抗军需要价值 {350}₵ 的医药和武器。准备好了就呼叫提货，第二天有人来取，可以分几次交（交东西不给钱）。"), theme.Wrap);
		GUI.Label(new Rect(r.x, r.y + 72f, r.width, 22f), (L.En ? $"Delivered: {rebelState?.RescueValue ?? 0} / {350}₵" : $"已交：{rebelState?.RescueValue ?? 0} / {350}₵"), theme.Price);
		if (Ui.Button(new Rect(r.x, r.y + 104f, 240f, 34f), L.T("呼叫提货", "Call for pickup"), theme.BtnHotStyle))
		{
			Toast(RebelStory.CallPickup() ?? L.T("明天有人来取。", "Someone will come tomorrow."));
		}
	}

	private static void DrawRebelTasks(Rect r)
	{
		Theme theme = _theme;
		MaliceState maliceState = MaliceState.Get();
		RebelState rebelState = maliceState?.Rebel;
		if (rebelState != null)
		{
			string text = rebelState.Active switch
			{
				1 => (L.En ? $"Odin wants a bottle of poison, or {100}₵ worth of Dream Dust and Phanta Pills. Delivered so far: {rebelState.Ch1Value}₵." : $"奥丁要一瓶毒药，或者价值 {100}₵ 的梦尘和芬太片。已交梦尘、芬太片 {rebelState.Ch1Value}₵。"), 
				2 => (rebelState.Ch2Route == "bribe") ? L.T("已经打点了子午线：炸弹送到后，奥丁会来买。", "Meridian has been paid off: once the bomb arrives, Odin will come for it.") : (L.En ? $"Odin wants explosives and Meridian heavy firepower: call Meridian and pay them off on the Corporate Terms page ({RebelStory.Ch2BribePrice()}₵), or sell {1000}₵ of weapons through the underground buyer ({rebelState.Ch2FenceWeapons}₵ so far)." : $"奥丁要炸药和子午线的重火力：打子午线的电话，在“巨企合约”页打点（{RebelStory.Ch2BribePrice()}₵）；或者通过地下买家卖出 {1000}₵ 的武器（已卖 {rebelState.Ch2FenceWeapons}₵）。"), 
				3 => (L.En ? $"The crackdown left many wounded. Odin comes weekly for medical supplies: {1200}₵ in total ({rebelState.Ch3Medical}₵ so far)." : $"清缴之后伤员很多。奥丁每周来收医疗品，累计 {1200}₵（已卖 {rebelState.Ch3Medical}₵）。"), 
				4 => rebelState.Joined ? (L.En ? $"Odin comes weekly for weapons and medical supplies: {3000}₵ in total ({rebelState.Ch4Sales}₵ so far)." : $"奥丁每周来收武器和医疗品，累计 {3000}₵（已卖 {rebelState.Ch4Sales}₵）。") : L.T("奥丁邀请你加入反抗军。想好了就叫他来。", "Odin has invited you to join the Resistance. Call him over when you've decided."), 
				5 => (L.En ? $"The Mentor is negotiating with the corporations. Each corporation at 100 cooperation sends a Coin of Fate ({maliceState.Envoy.Coins.Count} / 5)." : $"导师在替你和巨企谈。合作度满 100 的巨企会派人送命运硬币（已收 {maliceState.Envoy.Coins.Count} / 5）。"), 
				_ => (rebelState.Done >= 5) ? L.T("你是下层区的代表。", "You are the Lower Levels' representative.") : (L.En ? $"No task right now. Keep dealing with the Resistance ({maliceState.RebelEarned} support points earned)." : $"暂时没有任务。多和反抗军打交道（助力点累计 {maliceState.RebelEarned}）。"), 
			};
			GUI.Label(new Rect(r.x, r.y, r.width, 90f), text, theme.Wrap);
			float y = r.y + 100f;
			if (RebelStory.PickupOpen && Ui.Button(new Rect(r.x, y, 240f, 34f), L.T("呼叫提货", "Call for pickup"), theme.BtnHotStyle))
			{
				Toast(RebelStory.CallPickup() ?? L.T("明天有人来取。", "Someone will come tomorrow."));
			}
			if (RebelStory.CanCallOdin && Ui.Button(new Rect(r.x, y, 240f, 34f), L.T("呼叫奥丁", "Call Odin"), theme.BtnHotStyle))
			{
				Toast(RebelStory.CallOdin() ?? L.T("他明天来。", "He'll come tomorrow."));
			}
		}
		if (Envoy.CanCallBombOdin && Ui.Button(new Rect(r.x, r.y + 150f, 360f, 34f), L.T("预约奥丁收取撒该的礼物", "Book Odin to collect Zacchaeus's gift"), theme.BtnHotStyle))
			Toast(Envoy.CallBombOdin() ?? L.T("奥丁明天来收礼物。", "Odin will collect the gift tomorrow."));
	}

}

