using System;
using CorpPresence.Malice;
using System.Text;

namespace CorpPresence;

internal static class StoryGuide
{
	public static string[] Build(Company company)
	{
		if (company.Id == "envoy")
		{
			return WithPrerequisites(EnvoyPages(), L.T("共同前提：恶意模式开启，撒该未离场；开店时检查。", "All chapters: Malice mode on, envoy not gone; checked at opening."));
		}
		if (company.Id == "rebel")
		{
			return WithPrerequisites(RebelPages(), L.T("共同前提：恶意模式开启，已认识奥丁，未被捕且未断线；开店时检查。助力点门槛按累计获得计算，不是当前余额。", "All chapters: Malice mode on; Odin met, not arrested or cut off; checked at opening. Support thresholds use lifetime earned points, not your current balance."));
		}
		Character character = Engine.Find(company.CharacterId);
		if (character == null)
		{
			return new string[1] { L.T("此公司没有独立章节。", "This company has no independent chapters.") };
		}
		int[] thresholds = character.Thresholds;
		string[] array = new string[thresholds.Length];
		for (int i = 1; i <= thresholds.Length; i++)
		{
			bool flag = character.IsSide(i);
			int num = (flag ? character.SideAfter(i) : character.PrevMain(i));
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(L.T(string.Format("第 {0} 章（{1}）", i, flag ? "支线" : "主线"), string.Format("Chapter {0} ({1})", i, flag ? "side story" : "main story")));
			stringBuilder.AppendLine(L.T("预约条件（须同时满足）：", "Booking requirements (all required):"));
			stringBuilder.AppendLine(L.T($"• {Faction(character)}声望 ≥ {thresholds[i - 1]}。", $"• {Faction(character)} reputation ≥ {thresholds[i - 1]}."));
			stringBuilder.AppendLine((num == 0) ? L.T("• 无前置章节。", "• No previous chapter required.") : L.T($"• 主线进度达到第 {num} 章；本章尚未完成。", $"• Main progress at least chapter {num}; this chapter not completed."));
			if (character.IsBuyChapter(i))
			{
				stringBuilder.AppendLine(L.T($"• 店内持有符合收购要求的物品 ≥ {character.BuyGoodsNeeded} 件；本章不检查现金门槛。", $"• Hold at least {character.BuyGoodsNeeded} qualifying item(s) in the shop; no cash gate for this chapter."));
			}
			else
			{
				stringBuilder.AppendLine(L.T($"• 现金 > {character.CashFor(i)}₵（恰好相等不满足）。", $"• Cash > {character.CashFor(i)}₵ (equality does not qualify)."));
			}
			string text = Extra(character.Id, i);
			if (text.Length > 0)
			{
				stringBuilder.AppendLine(text);
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(L.T($"关店时检索，满足后预约次日。按预计来访日计算，距该人物上次剧情至少 {Settings.StoryGap(character)} 天（当前设置）。已有剧情预约先完成，主线优先于支线；不同人物独立判断。", $"Checked at closing for the next day. The planned visit must be at least {Settings.StoryGap(character)} day(s) after this character's last story (current setting). Existing story bookings are handled first; main stories take priority over side stories. Characters are checked independently."));
			stringBuilder.AppendLine(L.T("这是触发条件说明，不会替你完成任务、补发物品或改变存档。", "This guide does not complete tasks, grant items, or change your save."));
			array[i - 1] = stringBuilder.ToString();
		}
		return array;
	}

	private static string[] WithPrerequisites(string[] pages, string requirements)
	{
		for (int i = 0; i < pages.Length; i++)
		{
			ref string reference = ref pages[i];
			reference = reference + "\n\n" + requirements;
		}
		return pages;
	}

	private static string Faction(Character c)
	{
		if (c.Id == "mentor")
		{
			return L.T("黑市/革命军/下层区三者最高值（恶意模式仅黑市）", "highest of Black Market/Revolution/Lower Levels (Black Market only in Malice mode)");
		}
		return c.Faction switch
		{
			"FACTION_UPPER_LEVEL" => L.T("上层区", "Upper Levels"), 
			"FACTION_SECURITY" => L.T("治安部", "Security"), 
			"FACTION_LOWER_LEVEL" => L.T("下层区", "Lower Levels"), 
			_ => c.Faction, 
		};
	}

	private static string Extra(string id, int ch)
	{
		switch (id)
		{
		case "doreen":
			switch (ch)
			{
			case 2:
				return L.T("• 物品：便携净水器 portable_water_purifier。", "• Item: portable_water_purifier.");
			case 3:
				return L.T("• 净水商开局，或已解锁原版水商功能。无需先完成罐装支线。", "• Water Merchant start, or vanilla water trading unlocked. Canning is not a prerequisite.");
			case 4:
				return L.T("• 达夫雷合作度 ≥ 60。\n• 养鼠开局，或解锁 RETIRED_RANCHER（养鼠相关升级）。\n• 不要求先完成第3章净水支线。交谈后拒绝或现金不足，可在电话“罐装设备”页补办。", "• Davre cooperation ≥ 60.\n• Rancher start, or RETIRED_RANCHER unlocked.\n• The chapter 3 water side story is not required. If declined or payment fails, sign up later on the Canning Equipment phone page.");
			}
			break;
		case "isa":
			switch (ch)
			{
			case 4:
				return L.T("• 物品：突触注射器 black_injector；按物品标识计数。", "• Item: black_injector; counted by item identifier.");
			case 6:
				return L.T("• 卡迈克尔已记录发放三叶蜥（CreatureGiven）。\n• 当前持有活着的成年或更高阶段三叶蜥。主线需先达到第5章。", "• Carmicheal has recorded the creature grant (CreatureGiven).\n• Currently hold a living creature at Adult stage or above. Main progress must have reached chapter 5.");
			case 7:
				return L.T("• 已获卡迈克尔银血采血许可（Licensed）。\n• 累计采血记录 BloodDrawn > 0。", "• Carmicheal blood-drawing license granted (Licensed).\n• Recorded blood drawn (BloodDrawn) > 0.");
			}
			break;
		case "milo":
			switch (ch)
			{
			case 6:
				return L.T("• 水培开局，或解锁 RETIRED_FARMER（水培相关升级）。\n• 第6～8章只要求主线达到第3章，不必等主线第5章。", "• Farmer start, or RETIRED_FARMER unlocked.\n• Chapters 6–8 require main chapter 3, not chapter 5.");
			case 7:
				return L.T("• 已完成卷烟工艺第一阶段（CraftDone(1)）。\n• 已累计售出散烟 ≥ 10（LooseSold）。现金门槛使用当前打点费用。", "• Rolling craft stage 1 completed (CraftDone(1)).\n• Loose cigarettes sold ≥ 10 (LooseSold). The cash gate uses the current bribe fee.");
			case 8:
				return L.T("• 已完成打点（Bribed）。\n• 已累计制作烟包 ≥ 6（PacksMade）。", "• Bribe paid (Bribed).\n• Cigarette packs made ≥ 6 (PacksMade).");
			}
			break;
		case "rosen":
			if (ch != 5)
			{
				break;
			}
			return L.T("• 子午线合作度 ≥ 60。现金门槛使用当前锻造设备手续费。", "• Meridian cooperation ≥ 60. The cash gate uses the current forge equipment fee.");
		case "seline":
			if (ch != 2)
			{
				break;
			}
			return L.T("• 需2件可收购奢侈品；排除索拉纳拒收名单：护肤霜、神经活性香水、费洛蒙香水及本模组面膜、蜡烛（含点燃）、精华。", "• Two accepted luxury items; excluded IDs: skincare_cream, neuroactive_perfume, pheromone_perfume, corp_presence_mask, corp_presence_candle, corp_presence_candle_lit, corp_presence_elixir.");
		}
		return "";
	}

	private static string[] EnvoyPages()
	{
		return new string[4]
		{
			L.T("第1章：扶持金\n\n恶意模式开启；撒该未离场、扶持金尚未处理。\n现金首次 >3000 后记下日期；此后现金 <2500，或距该日期满4天，才会预约。\n同时必须拿到帕斯泰尔和卡迈克尔两家联系方式（公司启用且已发名片）。\n接受后需连续订阅两家公司21天；断订转为每周800、共5周的还款。开店时检查；待排队的旧预约也检查联系方式。", "Chapter 1: Aid\n\nMalice mode on; envoy not gone; aid not handled.\nFirst cash >3000 records the day. Booking requires cash <2500 later, or four days since that record.\nBoth Pastel and Carmicheal must be active with their business cards granted.\nAcceptance requires both subscriptions for 21 days; a lapse creates five weekly payments of 800. Checked at opening, including pending bookings."),
			L.T("第2章：服务费与礼物\n\n第1章状态为扶持金完成或拒绝；本章未处理；现金 >6000。\n可支付服务费、接受礼物或拒绝。\n礼物期限14天，只能交给从反抗军电话“任务”页专门预约来的奥丁；普通奥丁、其他剧情奥丁及地下买家不能代收。预约次日到达，截止日当天不可再预约次日；被捕、断线或黑名单时不能交付。", "Chapter 2: Service fee and gift\n\nAid completed or declined; chapter 2 not handled; cash >6000.\nPay the service fee, accept the gift, or refuse.\nThe gift has a 14-day deadline. Only Odin specifically booked from the Resistance phone Tasks page can take it. Ordinary/story Odin and fences cannot. He arrives next day; do not book on the deadline itself. Arrest, cutoff and blacklist block delivery."),
			L.T("第3章：加入巨企\n\n第2章已付费、礼物已交付，或渠道费阶段已结束。\n已经买下店铺产权（IsPropertyPaid）；本章尚未处理。\n开店时预约。加入和拒绝会进入不同后续分支；加入会影响反抗军联系。", "Chapter 3: Join the corporations\n\nChapter 2 paid, gift delivered, or channel-fee period ended.\nShop property purchased (IsPropertyPaid); chapter 3 not handled.\nBooked at opening. Joining and refusing lead to different routes; joining affects Resistance contact."),
			L.T("第4章：导师来信与硬币\n\n已经加入巨企；本章未处理；现金 ≥20000。\n开店时登记导师来信，需回拨商人联合电话继续。\n进入沉默分支后，未成为代理人时，帕斯泰尔、索拉纳、卡迈克尔、子午线、达夫雷各自合作度 ≥100，会分别预约尚未领取的命运硬币。", "Chapter 4: Mentor's message and coins\n\nCorporations joined; chapter 4 not handled; cash ≥20000.\nOpening records the message; call the Merchant Union to continue.\nOn the silent route, while not an agent, Pastel, Solana, Carmicheal, Meridian and Davre each send their unclaimed Fate Coin at cooperation ≥100.")
		};
	}

	private static string[] RebelPages()
	{
		return new string[5]
		{
			L.T("反抗军第1章\n\n恶意模式开启，已认识奥丁，未被捕且未断线。原版反抗军电话为正常或冷却状态后会登记认识并开放本模组反抗军电话。\n尚无已完成/进行中的反抗军章节，累计获得助力点 ≥50。\n开店时预约；任务交一瓶毒药，或累计价值100的梦尘/芬太片。", "Resistance chapter 1\n\nMalice mode on; Odin met, not arrested or cut off. A Regular/Cooldown vanilla Resistance phone records the introduction and unlocks this mod's phone.\nNo completed/active Resistance chapter; total support points earned ≥50.\nBooked at opening. Deliver poison, or 100 worth of Dream Dust/Phanta Pills."),
			L.T("反抗军第2章\n\n第1章完成，当前没有进行中章节，累计获得助力点 ≥300。\n可打点子午线取得炸弹再交给任务买家奥丁，或通过地下买家累计售出价值1000的武器。\n此章的子午线炸弹与撒该的礼物是两种不同任务物品，不能混交。", "Resistance chapter 2\n\nChapter 1 completed, no active chapter, total support points earned ≥300.\nBribe Meridian and sell its bomb to the chapter's Odin buyer, or sell 1000 worth of weapons through the underground buyer.\nThis bomb is distinct from Zacchaeus's gift; they are not interchangeable."),
			L.T("反抗军第3章\n\n第2章完成，当前没有进行中章节，累计获得助力点 ≥500。\n开店时启动；奥丁按任务周期来收医疗品，累计售出金额达到1200完成。\n仍要求已认识奥丁、未被捕、未断线。", "Resistance chapter 3\n\nChapter 2 completed, no active chapter, total support points earned ≥500.\nStarts at opening. Sell 1200 worth of medical supplies to the scheduled Odin buyer.\nOdin must still be met, not arrested and not cut off."),
			L.T("反抗军第4章\n\n第3章完成，当前没有进行中章节，尚未记录加入邀请；撒该第3章已选择拒绝巨企。\n邀请后可从反抗军任务页呼叫奥丁。加入后，向任务奥丁累计售出武器/医疗品达到3000，会安排突袭剧情，再按剧情日期推进。", "Resistance chapter 4\n\nChapter 3 completed, no active chapter, invitation not yet recorded; corporations refused in envoy chapter 3.\nAfter invitation, call Odin from the Tasks page. After joining, sell 3000 worth of weapons/medical supplies to the quest buyer to book the raid; subsequent progression follows its scheduled day."),
			L.T("反抗军第5章\n\n第4章完成，第5章尚未启动，现金 ≥16000；仍需奥丁已认识、未被捕、未断线。\n启动后，帕斯泰尔、索拉纳、卡迈克尔、子午线、达夫雷各自合作度 ≥100，分别安排未领取的命运硬币。\n五家公司硬币都登记领取后，才完成最终代表资格。", "Resistance chapter 5\n\nChapter 4 completed, chapter 5 not started, cash ≥16000; Odin still met, not arrested and not cut off.\nAfter activation, Pastel, Solana, Carmicheal, Meridian and Davre each book their unclaimed Fate Coin at cooperation ≥100.\nAll five coins must be recorded as received to complete the representative qualification.")
		};
	}
	public static int CurrentChapter(Company company)
	{
		if (company.Id == "envoy")
		{
			EnvoyState envoyState = MaliceState.Get()?.Envoy;
			if (envoyState == null)
			{
				return 0;
			}
			if (envoyState.Aid != "done" && envoyState.Aid != "declined")
			{
				return 1;
			}
			if (envoyState.Ch2 != "paid" && envoyState.Ch2 != "bombdone" && envoyState.Ch2 != "maliceover")
			{
				return 2;
			}
			if (string.IsNullOrEmpty(envoyState.Ch3))
			{
				return 3;
			}
			if (!(envoyState.Ch3 == "joined") || envoyState.Agent)
			{
				return 0;
			}
			return 4;
		}
		if (company.Id == "rebel")
		{
			RebelState rebelState = MaliceState.Get()?.Rebel;
			if (rebelState != null)
			{
				if (rebelState.Active <= 0)
				{
					if (rebelState.Done >= 5)
					{
						return 0;
					}
					return rebelState.Done + 1;
				}
				return rebelState.Active;
			}
			return 0;
		}
		Character character = Engine.Find(company.CharacterId);
		CorpState corpState = CorpState.Get();
		if (character == null || corpState == null)
		{
			return 0;
		}
		if (!corpState.Characters.TryGetValue(character.Id, out var value))
		{
			value = ((character.Id == "seline") ? corpState.Seline : null) ?? new CharacterState();
		}
		string pendingKind = value.PendingKind;
		if (value.PendingDay >= 0 && pendingKind != null && pendingKind.StartsWith("ch", StringComparison.Ordinal) && int.TryParse(pendingKind.Substring(2), out var result) && result > 0 && result <= character.Chapters)
		{
			return result;
		}
		int num = character.NextMain(value);
		if (num > 0)
		{
			return num;
		}
		for (int i = 1; i <= character.Chapters; i++)
		{
			if (character.IsSide(i) && value.Chapter >= character.SideAfter(i) && !value.SideDone.Contains(i))
			{
				return i;
			}
		}
		return 0;
	}
}
