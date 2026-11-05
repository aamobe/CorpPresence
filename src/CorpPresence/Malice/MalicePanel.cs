using CorpPresence.UI;
using UnityEngine;

namespace CorpPresence.Malice;

internal static class MalicePanel
{
	private static string _msg;

	private static float _msgUntil;

	private static int Day => StoreStation.GetDayCounter();

	public static void Draw(Company c, Rect r, Theme t)
	{
		CorpTerms corpTerms = MaliceState.Get()?.Of(c.Id);
		CompanyState companyState = Companies.StateOf(c);
		if (corpTerms == null || companyState == null)
		{
			GUI.Label(new Rect(r.x, r.y, r.width, 24f), L.T("存档未就绪", "Save not ready"), t.Small);
			return;
		}
		float y = r.y;
		bool flag = Malice.IsMembership(c);
		GUI.Label(new Rect(r.x, y, r.width, 24f), flag ? L.T("基础会员", "Basic membership") : L.T("订购服务", "Ordering service"), t.Body);
		y += 26f;
		string text;
		bool waived = Malice.SubscriptionWaived(c);
		if (waived)
		{
			text = L.T("已获免订阅费权益，无需开通或续费。商品货款照常结算。", "Subscription fees are waived. No activation or renewal is needed; goods are charged normally.");
		}
		else if (Malice.Subscribed(c))
		{
			text = (L.En ? $"Active through day {corpTerms.SubUntil - 1}." : $"已开通，有效到第 {corpTerms.SubUntil - 1} 天（含）。");
		}
		else if (corpTerms.Subscribed)
		{
			text = (L.En ? $"Expired (paused since day {corpTerms.SubUntil})." : $"已到期（第 {corpTerms.SubUntil} 天起暂停）。") + (Malice.Lapsed(corpTerms) ? L.T("断供超过一周，再续要加复通费。", " Lapsed over a week: reactivation fee applies.") : "");
		}
		else
		{
			text = (flag ? L.T("未开通。会员才能使用索拉纳的服务。", "Not a member. Only members can use Solana's services.") : L.T("未开通。开通后才能电话订货。", "Not subscribed. Subscribe to order by phone."));
		}
		GUI.Label(new Rect(r.x, y, r.width, 40f), text, t.Wrap);
		y += 42f;
		if (!waived)
		{
		int num = Malice.SubscriptionPrice(c);
		string text2;
		if (corpTerms.Subscribed)
		{
			text2 = (L.En ? $"Renew one week ({num}₵)" : $"续费一周（{num}₵）");
		}
		else
		{
			text2 = (flag ? (L.En ? $"Become a basic member ({num}₵ a week)" : $"成为基础会员（{num}₵ 一周）") : (L.En ? $"Subscribe ({num}₵, first week included)" : $"开通（{num}₵，含第一周）"));
		}
		if (Ui.Button(new Rect(r.x, y, 260f, 32f), text2, t.BtnHotStyle))
		{
			Say(Malice.Subscribe(c) ?? L.T("已续上。", "Done."));
		}
		if (Ui.Button(new Rect(r.x + 270f, y, 200f, 32f), (corpTerms.AutoRenew ? "☑ " : "☐ ") + L.T("自动续费", "Auto-renew"), t.BtnStyle))
		{
			corpTerms.AutoRenew = !corpTerms.AutoRenew;
		}
		y += 38f;
		GUI.Label(new Rect(r.x, y, r.width, 40f), L.T("续费每次管 7 天，最多提前续一周，到期前一天提醒。自动续费只在扣完后还够交房租时才扣。", "Each renewal covers 7 days, at most one week ahead; you get a reminder the day before. Auto-renew only charges if you can still pay rent afterwards."), t.Small);
		y += 44f;
		}
		if (!flag)
		{
			Ui.Fill(new Rect(r.x, y, r.width, 1f), t.Line);
			y += 8f;
			GUI.Label(new Rect(r.x, y, r.width, 24f), L.T("目录解锁", "Catalog unlocks"), t.Body);
			y += 26f;
			int num2 = c.Tier(companyState);
			for (int i = 2; i <= 3; i++)
			{
				string text3 = c.TierNames[i - 1];
				string text4;
				if (corpTerms.Unlocked >= i)
				{
					text4 = L.T("已解锁", "Unlocked");
				}
				else
				{
					text4 = ((num2 < i) ? L.T("合作度未到", "Cooperation too low") : (L.En ? $"Can apply ({Malice.ApplyFee[i]}₵)" : $"可申请（{Malice.ApplyFee[i]}₵）"));
				}
				GUI.Label(new Rect(r.x, y, 260f, 28f), text3 + "：" + text4, t.Small);
				if (corpTerms.Unlocked == i - 1 && num2 >= i && Ui.Button(new Rect(r.x + 270f, y, 200f, 28f), (L.En ? $"Apply ({Malice.ApplyFee[i]}₵)" : $"申请（{Malice.ApplyFee[i]}₵）"), t.BtnStyle))
				{
					Say(Malice.ApplyUnlock(c) ?? L.T("已解锁。", "Unlocked."));
				}
				y += 32f;
			}
			GUI.Label(new Rect(r.x, y, r.width, 40f), (L.En ? $"Signed-tier goods need no application. Weekly limits: {Malice.TierLimit[1]} / {Malice.TierLimit[2]} / {Malice.TierLimit[3]} per tier." : $"签约档的货不用申请。每周限量：签约档 {Malice.TierLimit[1]} 份，{c.TierNames[1]} {Malice.TierLimit[2]} 份，{c.TierNames[2]} {Malice.TierLimit[3]} 份。"), t.Small);
			y += 44f;
		}
		if (c.Id == "meridian" && RebelStory.Ch2BribeOpen)
		{
			int num3 = RebelStory.Ch2BribePrice();
			if (Ui.Button(new Rect(r.x, y, 300f, 30f), (L.En ? $"Quiet payment ({num3}₵, a “sample” arrives tomorrow)" : $"私下打点（{num3}₵，明天送“样品”）"), t.BtnHotStyle))
			{
				Say(RebelStory.BribeMeridian() ?? L.T("明天送到。", "Arrives tomorrow."));
			}
			y += 36f;
		}
		if (c.Id == "meridian" && RebelStory.Ch2ReorderOpen)
		{
			if (Ui.Button(new Rect(r.x, y, 340f, 30f), L.T("检查库存并补订炸弹", "Check storage and reorder bomb"), t.BtnHotStyle))
				Say(RebelStory.ReorderBomb() ?? L.T("补订成功，明天送到。", "Replacement ordered. Arrives tomorrow."));
			y += 36f;
		}
		Ui.Fill(new Rect(r.x, y, r.width, 1f), t.Line);
		y += 8f;
		GUI.Label(new Rect(r.x, y, r.width, 24f), (L.En ? $"Vouchers: {corpTerms.Vouchers}₵" : $"代金券余额：{corpTerms.Vouchers}₵"), t.Body);
		y += 26f;
		GUI.Label(new Rect(r.x, y, r.width, 40f), (L.En ? $"Cash spent at {c.Name} returns 1/{10} as vouchers; vouchers are used first. Only valid at {c.Name}." : $"在{c.Name}花的现金返 1/{10} 代金券；付款时先扣代金券。只能在{c.Name}用。"), t.Small);
		if (!string.IsNullOrEmpty(_msg) && Time.unscaledTime < _msgUntil)
		{
			GUI.Label(new Rect(r.x, r.yMax - 26f, r.width, 24f), _msg, t.Body);
		}
	}

	private static void Say(string text)
	{
		_msg = text;
		_msgUntil = Time.unscaledTime + 3f;
	}
}
