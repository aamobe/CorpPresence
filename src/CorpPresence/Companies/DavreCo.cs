using System;

namespace CorpPresence;

internal sealed class DavreCo : Company
{
	public static readonly CatalogEntry[] Entries = new CatalogEntry[6]
	{
		new CatalogEntry
		{
			Id = "processed_meat",
			Page = "食品"
		},
		new CatalogEntry
		{
			Id = "cup_noodle",
			Page = "食品"
		},
		new CatalogEntry
		{
			Id = "@tablets",
			Page = "净水",
			Label = "一瓶净水药片",
			LabelEn = "Bottle of Water Tablets"
		},
		new CatalogEntry
		{
			Id = "@strips",
			Page = "净水",
			Label = "水质检测试剂盒",
			LabelEn = "Water Test Kit"
		},
		new CatalogEntry
		{
			Id = "uv_filter",
			Page = "净水",
			MinTier = 2
		},
		new CatalogEntry
		{
			Id = "aquascan",
			Page = "净水",
			MinTier = 2
		}
	};

	public const string Leviathan = "@leviathan";

	public const int CanningCoop = 60;

	public const string CanningSet = "@canning_set";

	public static readonly (string Id, string Goods)[] CanningMachines =
	{
		("@meat_grinder", "meat_grinder"),
		("@canning_machine", "canning_machine"),
		(Canning.SeafoodSealerOrder, "seafood_sealer")
	};

	private const int GhostSample = 9700;

	private const int RustSample = 9266;

	public override string Id => "davre";

	public override string Name => L.T("达夫雷食品", "Davre Food");

	public override string CharacterId => "doreen";

	public override long PhoneNumber => 32873L;

	public override string[] TierNames => L.T(new string[3] { "普通供货", "区域供货", "长期合约" }, new string[3] { "Standard Supplier", "Regional Supplier", "Long-Term Contract" });

	public override string CardFlavor => L.T("红底米色的达夫雷标志，32873。", "The Davre logo, beige on red. 32873.");

	public override string DeskName => L.T("达夫雷订货中心", "Davre Order Center");

	public override string DeskAsk => "";

	public override string[] FrontDesk => L.T(new string[2] { "达夫雷食品，订货中心。请报供货编号。", "达夫雷。罐头和药片，今天都有。" }, new string[2] { "Davre Food, order center. Supplier number, please.", "Davre. Cans and tablets, all in stock today." });

	public override string FrontDeskDone => L.T("已排单，明天到货。", "Scheduled. Arriving tomorrow.");

	public override CatalogEntry[] Catalog => Entries;

	public static bool LeviathanOpen => CorpState.Get()?.Of("doreen").SideDone.Contains(3) ?? false;

	public static int CanningFee => Services.Fee("davre", "canning", 500);

	public static bool CanningOpen => StateOf()?.CanningOwned ?? false;

	public static bool CanningOffered => CorpState.Get()?.Of("doreen").SideDone.Contains(4) ?? false;

	public static GameItem MakePrebuilt(string id)
	{
		try
		{
			GameItem result;
			if (id == "@tablets")
			{
				result = PreBuiltItemHelper.FullWaterPurificationTabletBox();
			}
			else
			{
				result = ((!(id == "@strips")) ? null : PreBuiltItemHelper.FullWaterTestStripBox());
			}
			return result;
		}
		catch (Exception ex)
		{
			ModLog.Warn("达夫雷整瓶货生成失败：" + ex.Message);
			return null;
		}
	}

	public int LeviathanLimit(CompanyState s)
	{
		if (Tier(s) < 3)
		{
			return 1;
		}
		return 2;
	}

	public static int LeviathanUsed(CompanyState s)
	{
		if (!s.LimitedBought.TryGetValue("leviathan@" + Services.PeriodKey, out var value))
		{
			return 0;
		}
		return value;
	}

	private static CompanyState StateOf()
	{
		return CorpState.Get()?.CompanyOf("davre");
	}

	public static int Coop()
	{
		return StateOf()?.Coop ?? 0;
	}

	public static bool PayCanningFee()
	{
		CompanyState companyState = StateOf();
		if (companyState == null || Services.Pay(Companies.Find("davre"), CanningFee, "canning") != null)
		{
			return false;
		}
		companyState.CanningOwned = true;
		ModLog.Info($"罐装设备手续费 {CanningFee}₵ 已付");
		return true;
	}

	public static bool IsWaterMerchant()
	{
		try
		{
			if (PlayerStore.Instance != null && PlayerStore.Instance.IsWaterMerchantStart())
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return NetworkUpgrade.IsWaterTraderUnlocked();
		}
		catch
		{
			return false;
		}
	}

	private static string Grade(int purity)
	{
		ItemCondition conditionFromPurity = WaterFeatureHelper.GetConditionFromPurity(purity);
		string text = conditionFromPurity?.identifier;
		if (text == null)
		{
			if (conditionFromPurity == null)
			{
				return null;
			}
			text = conditionFromPurity.display;
		}
		return text;
	}

	public static bool IsGutter(GameItem container)
	{
		try
		{
			int waterPurity = WaterHelper.GetWaterPurity(container);
			if (waterPurity >= 9700)
			{
				return false;
			}
			string text = Grade(waterPurity);
			int num;
			if (text != null)
			{
				if (!(text == Grade(9700)))
				{
					num = ((text == Grade(9266)) ? 1 : 0);
					if (num == 0)
					{
						goto IL_0048;
					}
				}
				else
				{
					num = 1;
				}
				goto IL_005e;
			}
			num = 0;
			goto IL_0048;
			IL_005e:
			return num == 0;
			IL_0048:
			ModLog.Info($"利维坦拒绝净化：纯度 {waterPurity}（{text}）");
			goto IL_005e;
		}
		catch (Exception ex)
		{
			ModLog.Warn("判断水质失败：" + ex.Message);
			return false;
		}
	}
}
