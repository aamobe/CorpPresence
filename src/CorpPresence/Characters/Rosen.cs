using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal sealed class Rosen : Character
{
	private const string Sentinel = "@glock_receiver";

	private const string Peacemaker = "@smg";

	private const string Bulwark = "@shotgun";

	private const string Ammo = "heavy_pistol_ammo";

	private const string AmmoAP = "heavy_pistol_ammo_p";

	private const string AmmoNL = "heavy_pistol_ammo_nl";

	public const string Revolver = "@rosen_revolver";

	public const string Rifle = "@rosen_rifle";

	public static string SaleBlocked(string id)
	{
		// Match GunsItemDirectory, including the base revolver and shotgun used by the handmade models.
		var (type, level) = id switch
		{
			Ammo => ("AMMUNITION", 1),
			AmmoAP => ("AMMUNITION", 2),
			Sentinel or Revolver => ("FIREARM", 2),
			Peacemaker or Bulwark or Rifle => ("FIREARM", 3),
			_ => (null, 0)
		};
		if (level == 0 || (PlayerStore.Instance?.ActivePermits != null && PermitHelper.HavePermit(type, level)))
			return null;
		return id switch
		{
			Ammo => L.T("普通实弹需要一级售卖许可。", "Standard live rounds require a Class I ammunition sales permit."),
			AmmoAP => L.T("穿甲弹需要二级售卖许可。", "Armor-piercing rounds require a Class II ammunition sales permit."),
			Sentinel or Revolver => L.T("需要二级枪械售卖许可。", "Requires a Class II firearm sales permit."),
			_ => L.T("需要三级枪械售卖许可。", "Requires a Class III firearm sales permit.")
		};
	}

	public override string Id => "rosen";

	public override string Name => L.T("罗森", "Rosen");

	public override string Faction => "FACTION_SECURITY";

	public override int[] Thresholds => new int[5] { 20, 40, 60, 80, 80 };

	public override int CashNeeded => 1000;

	public override string PortraitRef => "maleSecOff_0";

	public override string[] RefuseIds => new string[3] { "heavy_pistol_ammo", "heavy_pistol_ammo_p", "heavy_pistol_ammo_nl" };

	public override (int Min, int Max) DefaultDailyGap => (Min: 5, Max: 5);

	public override int BuyGoodsNeeded => 1;

	private static string Bye => L.T("保重。", "Take care.");
	private static string AmmoTerms => L.T("橡胶弹不用证。普通实弹看一级，穿甲弹看二级。", "Rubber rounds are over the counter. Live rounds need Class I. Armor-piercing needs Class II.");

	private static int Disc
	{
		get
		{
			if (!Settings.VanillaServices)
			{
				return 30;
			}
			return 20;
		}
	}

	private static string[] Talk => new string[6]
	{
		L.T("哨兵3型不会卡壳。至少宣传上是这样。", "The Sentinel Mk. III doesn't jam. At least, that's what the ads say."),
		L.T("就属治安部用那些橡胶头的软弹最多。报告上写着好看一点。", "Nobody uses more of those rubber-tipped rounds than Security. Looks a little better on the reports."),
		L.T("穿甲弹贵一点。真用得上再买。", "AP rounds cost more. Buy them if you need them."),
		Character.Act(L.T("敲了敲右臂", "taps his right arm"), L.T("每年都得回厂保养一次。", "Back to the factory once a year for maintenance.")),
		L.T("这道疤？演习。谁知道演习上的是实弹。", "This scar? A drill. Who knew they were using live rounds?"),
		L.T("子午线可从来不卖给黑市，我们的活计只是损耗大了一点。", "Meridian never sells to the Black Market. We just lose a little more stock on the job.")
	};

	public override bool IsSide(int chapter)
	{
		return chapter == 5;
	}

	public override bool SideAllowed(int chapter)
	{
		if (chapter == 5)
		{
			return MeridianCo.Coop() >= 60;
		}
		return true;
	}

	public override int CashFor(int chapter)
	{
		if (chapter != 5)
		{
			return CashNeeded;
		}
		return MeridianCo.ForgeFee;
	}

	public override string Portrait(CharacterState s, string kind)
	{
		return "meridian_rosen";
	}

	public override bool DailyBuyer(CharacterState s)
	{
		return true;
	}

	public override bool DailyAccepts(GameItem item)
	{
		return Character.HasTag(item, "MATERIAL");
	}

	public override string[] WantTags(bool daily)
	{
		if (!daily)
		{
			return Array.Empty<string>();
		}
		return new string[1] { "MATERIAL" };
	}

	public override GameItem MakeItem(string id)
	{
		if (SaleBlocked(id) != null) return null;
		bool flag;
		switch (id)
		{
		case "@glock_receiver":
		case "@smg":
		case "@shotgun":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			return MeridianCo.MakeNew(id);
		}
		if (id == "@forge_set")
		{
			return Crates.MakeFilled("crate_meridian", "forge_furnace", "forge_anvil", "forged_billet", "forged_billet", "forged_billet");
		}
		return base.MakeItem(id);
	}

	public override bool IsBuyChapter(int chapter)
	{
		return false;
	}

	public override int BuyGoodsHeld()
	{
		return 0;
	}

	public override bool Accepts(GameItem item)
	{
		return false;
	}

	public override double PriceMultiplier(GameItem item, string kind)
	{
		return 1.0;
	}

	public override Visit Chapter(int chapter)
	{
		switch (chapter)
		{
		case 1:
		{
			string round = SaleBlocked(Ammo) == null ? Ammo : AmmoNL;
			Visit visit = new Visit();
			visit.Lines = new string[3]
			{
				L.T("子午线兵工，罗森。", "Meridian Ballistics. Rosen."),
				Character.Act(L.T("拿出一把哨兵3型给你看", "takes out a Sentinel Mk. III to show you"), ""),
				round == AmmoNL
					? L.T("今天就是过来看看，确认一下你这边的资格。看着还不错。我带了几盒橡胶弹，这个不用证。要吗？", "Just here to look around and check what you're cleared to sell. Looks good so far. I brought a couple of boxes of rubber rounds. No permit needed for those. Interested?")
					: AmmoTerms
			};
			visit.Exit = Bye;
			visit.Discount = Disc;
			visit.Stock = new[] { Sentinel, round, round };
			return LicensedOffer(visit);
		}
		case 2:
		{
			Visit visit = new Visit();
			visit.Lines = new string[4]
			{
				L.T("和平使者，标准军备。枪要按证卖。", "The Peacemaker. Standard issue. Show me the permit if you want one."),
				L.T("（指了指左脸那道疤）我身上绝大部分疤痕都是它留下的。", "(points at the scar on his left cheek) Most of my scars came from one of these."),
				L.T("所以，我知道它有多好用。", "So I know just how well it works."),
				AmmoTerms
			};
			visit.Exit = L.T("保重，再见。", "Take care. See you around.");
			visit.Discount = Disc;
			string round = SaleBlocked(Ammo) == null ? Ammo : AmmoNL;
			visit.Stock = new[] { Peacemaker, round, round };
			return LicensedOffer(visit);
		}
		case 3:
		{
			Visit visit = new Visit();
			visit.Lines = new string[4]
			{
				L.T("嘿。<i>（掏出一把大枪，旁人连忙退开）</i>", "Hey. <i>(pulls out a big gun, sending the bystanders scrambling back)</i>"),
				L.T("「壁垒」，双动式霰弹枪！嘭的一声一切就都结束了。", "The Rampart, a dual-action shotgun! One boom and it's all over."),
				Character.Act(L.T("撇嘴", "curls his lip"), L.T("产线一天能出几百把，每把都一模一样。", "The line can turn out hundreds a day. Every last one looks the same.")),
				Character.Act(L.T("拍了拍枪托", "pats the stock"), L.T("当然，我承认这使维修方便很多。", "It does make repairs a lot easier, I'll give it that."))
			};
			visit.Exit = Bye;
			visit.Discount = Disc;
			visit.Stock = new string[1] { "@shotgun" };
			return LicensedOffer(visit);
		}
		case 5:
			return ForgeLesson();
		default:
		{
			Visit visit = new Visit();
			visit.Lines = new string[4]
			{
				L.T("<i>（点点头）</i>。", "<i>(nods)</i>"),
				L.T("你知道吗，公司的每一把枪都有编号。", "You know, every gun the company makes has a serial number."),
				Character.Act(L.T("打开一个旧木盒", "opens an old wooden case"), L.T("而这两把显然没有。左轮，猎枪，我自己做的，纯手工打造。", "These two clearly don't. A revolver and a hunting rifle. Made them myself, all by hand.")),
				L.T("治安部配不上它们。我相信你能找到配得上的人。", "Security doesn't deserve them. I trust you'll find someone who does.")
			};
			visit.Exit = Character.Act(L.T("摸摸木盒", "runs a hand over the wooden case"), L.T("保养好它们。", "Take care of them."));
			visit.Discount = Disc;
			visit.Stock = new string[2] { "@rosen_revolver", "@rosen_rifle" };
			return LicensedOffer(visit);
		}
		}
	}

	private static Visit LicensedOffer(Visit visit)
	{
		string[] requirements = visit.Stock.Select(SaleBlocked).Where(reason => reason != null).Distinct().ToArray();
		if (requirements.Length == 0) return visit;
		visit.Stock = visit.Stock.Where(id => SaleBlocked(id) == null).ToArray();
		visit.TalkOnly = visit.Stock.Length == 0;
		visit.Lines = visit.TalkOnly
			? visit.Lines.Append(L.T("今天带来的枪得有对应的售卖许可才能卖。你先看看就行。", "You'll need the right sales permit for the guns I brought today. For now, just have a look.")).ToArray()
			: visit.Lines.Concat(requirements).ToArray();
		return visit;
	}

	private static Visit ForgeLesson()
	{
		Visit v = new Visit
		{
			Lines = new string[4]
			{
				L.T("店长，对锻造有兴趣吗？", "Interested in forging, boss?"),
				Character.Act(L.T("把一块钢坯放在柜台上", "sets a steel billet on the counter"), L.T("把高纯度的金属锭塞进炉子里烧一夜，出来的就是这个。", "Stick a high-purity ingot in the forge overnight and this is what comes out.")),
				L.T("我不小心买多了一套…呃不是，我在传播这门手艺。", "I bought one kit too many by mistake… uh, I mean, I'm passing on the craft."),
				L.T($"整套家伙只要{MeridianCo.ForgeFee}₵，还送你三块钢坯，练手足够用了。", L.Translate((System.FormattableString)$"The whole kit's yours for {MeridianCo.ForgeFee}₵. I'll throw in three steel billets. Plenty to practice with."))
			},
			Exit = L.T("保重，兄弟。", "Take care, brother."),
			TalkOnly = true,
			Ask = L.T("学不学？", "Want to learn?")
		};
		v.Choices = new Choice[2]
		{
			new Choice(L.T("学！", "Teach me!"), L.T("好样的！炉子坏了找我订新的。", "That's the spirit! If the forge breaks, you can order a new one from me."), () =>
			{
				if (MeridianCo.PayForgeFee())
				{
					v.Gift = new string[1] { "@forge_set" };
				}
				else
				{
					ModLog.Warn("罗森第 5 章：现金不够，改到面板补办");
				}
			}),
			new Choice(L.T("改天再说吧。", "Maybe another time."), L.T("想好了给我打电话。", "Call me when you've made up your mind."), null)
		};
		return v;
	}

	public override Visit Daily(CharacterState s)
	{
		string[] options = Talk;
		List<string> list = new List<string>();
		list.Add(Engine.Any(L.T("子午线例行补货<i>（哈欠）</i>。", "Meridian. Routine restock. <i>(yawns)</i>"), L.T("罗森。老样子。", "Rosen. Same as usual."), L.T("补货。", "Restock.")));
		list.Add(AmmoTerms);
		list.Add(Engine.Any(options));
		List<string> list2 = list;
		string[] ammoPool = new[] { Ammo, Ammo, AmmoAP, AmmoNL }.Where(id => SaleBlocked(id) == null).ToArray();
		List<string> list3 = Engine.Pick(ammoPool, 3, 5).ToList();
		string[] guns = (s.Chapter >= 3 ? new[] { Sentinel, Peacemaker, Bulwark } : new[] { Sentinel, Peacemaker })
			.Where(id => SaleBlocked(id) == null).ToArray();
		if (guns.Length > 0) list3.Add(Engine.Any(guns));
		Visit visit = new Visit
		{
			Exit = Bye,
			Discount = Disc,
			Both = true,
			Wrong = L.T("只收材料。", "Materials only.")
		};
		if (s.Order != null && SaleBlocked(s.Order) == null)
		{
			list3.Add(s.Order);
			list2.Add(Character.Act(L.T("放下一个木盒", "sets down a wooden case"), L.T("你订的“手工艺品”。", "Here's that “handicraft” you ordered.")));
		}
		if (s.Chapter >= 4)
		{
			var choices = new List<Choice>();
			string reply = L.T("下周给你带来。手工制作，快不了。", "I'll bring it by next week. I make these by hand. Can't rush it.");
			foreach (var (id, label) in new[]
			{
				(Revolver, L.T("要把左轮。", "I'll take a revolver.")),
				(Rifle, L.T("要把猎枪。", "I'll take a hunting rifle."))
			})
			{
				if (SaleBlocked(id) != null) continue;
				choices.Add(new Choice(label, reply, () => { if (SaleBlocked(id) == null) s.Order = id; }));
			}
			if (choices.Count > 0)
			{
				visit.Ask = L.T("要一把？", "Want one?");
				choices.Add(new Choice(L.T("不用。", "No thanks."), L.T("……行。", "…All right."), () => s.Order = null));
				visit.Choices = choices.ToArray();
			}
		}
		visit.Lines = list2.ToArray();
		visit.Stock = list3.ToArray();
		visit.Extra = ammoPool;
		return visit;
	}
}
