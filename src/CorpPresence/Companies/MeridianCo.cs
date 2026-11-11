using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal sealed class MeridianCo : Company
{
	public const int ForgeCoop = 60;

	public const string ForgeSet = "@forge_set";

	public static readonly (string Id, string Goods)[] ForgeMachines = new (string, string)[2]
	{
		("@forge_furnace", "forge_furnace"),
		("@forge_anvil", "forge_anvil")
	};

	public static readonly CatalogEntry[] Entries = new CatalogEntry[6]
	{
		new CatalogEntry
		{
			Id = "@glock_receiver",
			Page = "制式",
			Label = "哨兵3型"
		},
		new CatalogEntry
		{
			Id = "@smg",
			Page = "制式",
			Label = "“和平使者”重型冲锋枪"
		},
		new CatalogEntry
		{
			Id = "@shotgun",
			Page = "制式",
			MinTier = 2,
			Label = "“壁垒”双动式霰弹枪"
		},
		new CatalogEntry
		{
			Id = "heavy_pistol_ammo",
			Page = "制式"
		},
		new CatalogEntry
		{
			Id = "heavy_pistol_ammo_nl",
			Page = "制式"
		},
		new CatalogEntry
		{
			Id = "heavy_pistol_ammo_p",
			Page = "制式",
			MinTier = 2
		}
	};

	public static readonly CatalogEntry[] Parts = new CatalogEntry[16]
	{
		new CatalogEntry
		{
			Id = "rds",
			Page = "配件"
		},
		new CatalogEntry
		{
			Id = "silencer2",
			Page = "配件"
		},
		new CatalogEntry
		{
			Id = "compensator",
			Page = "配件"
		},
		new CatalogEntry
		{
			Id = "barrel",
			Page = "配件"
		},
		new CatalogEntry
		{
			Id = "laser2",
			Page = "配件"
		},
		new CatalogEntry
		{
			Id = "grip",
			Page = "配件"
		},
		new CatalogEntry
		{
			Id = "bipod",
			Page = "配件"
		},
		new CatalogEntry
		{
			Id = "stock",
			Page = "配件"
		},
		new CatalogEntry
		{
			Id = "short_stock",
			Page = "配件"
		},
		new CatalogEntry
		{
			Id = "trigger_group",
			Page = "配件"
		},
		new CatalogEntry
		{
			Id = "match_barrel",
			Page = "配件"
		},
		new CatalogEntry
		{
			Id = "grip_gel",
			Page = "配件"
		},
		new CatalogEntry
		{
			Id = "ammo_display",
			Page = "配件"
		},
		new CatalogEntry
		{
			Id = "internal_springs",
			Page = "配件"
		},
		new CatalogEntry
		{
			Id = "mag_10mm_ext",
			Page = "配件"
		},
		new CatalogEntry
		{
			Id = "mag_10mm_smg_box",
			Page = "配件"
		}
	};

	private static readonly CatalogEntry[] AllEntries = Entries.Concat(Parts).ToArray();

	public const int RefurbishOwn = 60;

	public const int RefurbishOther = 120;

	public const int RefurbishMax = 3;

	public const int RefurbishTier = 2;

	private static readonly string[] Ladder = new string[5] { "poor", "fair", "good", "excellent", "new" };

	private static readonly string[] LadderName = new string[5] { "很差", "一般", "良好", "优秀", "全新" };

	private static readonly string[] LadderTextKeys = { "companies.meridian.condition.poor", "companies.meridian.condition.fair", "companies.meridian.condition.good", "companies.meridian.condition.excellent", "companies.meridian.condition.new" };

	private static readonly string[] LadderNameEn = new string[5] { "Poor", "Fair", "Good", "Excellent", "New" };

	public override string Id => "meridian";

	public override string Name => L.K("companies.meridian.name.meridianBallistics", "子午线兵工", "Meridian Ballistics");

	public override string CharacterId => "rosen";

	public override long PhoneNumber => 63743L;

	public override string[] TierNames => new string[3] { L.K("companies.meridian.tierNames.classILicense", "一级许可", "Class I License"), L.K("companies.meridian.tierNames.classIILicense", "二级许可", "Class II License"), L.K("companies.meridian.tierNames.classIIILicense", "三级许可", "Class III License") };

	public override string CardFlavor => L.K("companies.meridian.cardFlavor.redBlackSingleMeridianLine", "黑底红字，一道经线标记，63743。背面印着：返厂服务，终身有效。", "Red on black, a single meridian line, 63743. On the back: Factory service, for life.");

	public override string DeskName => L.K("companies.meridian.deskName.meridianOrderDesk", "子午线订货处", "Meridian Order Desk");

	public override string DeskAsk => "";

	public override string[] FrontDesk => new string[1] { L.K("companies.meridian.frontDesk.meridianBallisticsOrderDeskLicense", "子午线兵工，订货处。报许可编号。", "Meridian Ballistics, order desk. License number.") };

	public override string FrontDeskDone => L.K("companies.meridian.frontDeskDone.loggedTomorrow", "登记了。明天到。", "Logged. Tomorrow.");

	public static int ForgeFee => Services.Fee("meridian", "forge", 600);

	public static bool ForgeOpen => StateOf()?.ForgeOwned ?? false;

	public static bool ForgeOffered => CorpState.Get()?.Of("rosen").SideDone.Contains(5) ?? false;

	public static double PartsRate => Settings.OrderMarkup;

	public override CatalogEntry[] Catalog => AllEntries;

	public static int RefurbishGap => Settings.Period("refurbish", 3);

	private static CompanyState StateOf()
	{
		return CorpState.Get()?.CompanyOf("meridian");
	}

	public static int Coop()
	{
		return StateOf()?.Coop ?? 0;
	}

	public static bool PayForgeFee()
	{
		CompanyState companyState = StateOf();
		if (companyState == null || Services.Pay(Companies.Find("meridian"), ForgeFee, "forge") != null)
		{
			return false;
		}
		companyState.ForgeOwned = true;
		try { ModLog.Info($"锻造整套工具 {ForgeFee}₵ 已付"); } catch { }
		return true;
	}

	public static string MakeUpForge()
	{
		CompanyState companyState = StateOf();
		if (companyState == null)
		{
			return L.K("companies.meridian.makeUpForge.saveNotReady", "存档未就绪", "Save not ready");
		}
		if (companyState.ForgeOwned)
		{
			return L.K("companies.meridian.makeUpForge.alreadyDone", "已经办过了", "Already done");
		}
		if (!ForgeOffered)
		{
			return L.K("companies.meridian.makeUpForge.notAvailableYet", "还没有开通", "Not available yet");
		}
		if (!PayForgeFee())
		{
			return L.K("companies.meridian.makeUpForge.notEnoughCash", "现金不够", "Not enough cash");
		}
		companyState.Orders.Add(new Order
		{
			Day = StoreStation.GetDayCounter() + 1,
			Items = new List<string> { "@forge_furnace", "@forge_anvil", "@forged_billet", "@forged_billet", "@forged_billet" }
		});
		return null;
	}

	public static string OrderForgeMachine(Company c, string id, int price)
	{
		CompanyState companyState = StateOf();
		if (companyState == null)
		{
			return L.K("companies.meridian.orderForgeMachine.saveNotReady", "存档未就绪", "Save not ready");
		}
		if (!companyState.ForgeOwned)
		{
			return L.K("companies.meridian.orderForgeMachine.notAvailableYet", "还没有开通", "Not available yet");
		}
		string text = Services.OrderBlocked(c);
		if (text != null)
		{
			return text;
		}
		string text2 = Services.Pay(c, price, "forge_machine");
		if (text2 != null)
		{
			return text2;
		}
		companyState.Orders.Add(new Order
		{
			Day = StoreStation.GetDayCounter() + 1,
			Items = new List<string> { id }
		});
		Services.AddCoop(c, (double)price / Settings.CoopCredits, "订购锻造设备");
		try { ModLog.Info($"锻造设备订单：{id}，{price}₵"); } catch { }
		return null;
	}

	public static GameItem MakeNew(string id)
	{
		if (id.StartsWith("@"))
		{
			id = id.Substring(1);
		}
		GameItem gameItem = DirectoryMaster.Item(id, isOwned: false);
		try
		{
			GunHelper.SetEquipmentCondition(gameItem, ItemFeatureList.EquipmentCondition.New);
		}
		catch
		{
		}
		return gameItem;
	}

	public static int ConditionOf(GameItem item)
	{
		try
		{
			ItemFeature itemFeature = item?.FindItemFeatureByCategory("CATEGORY_EQUIPMENT_CONDITION");
			if (itemFeature == null)
			{
				return -1;
			}
			string text = itemFeature.realCondition?.identifier ?? itemFeature.identifier;
			return Array.IndexOf(Ladder, (text ?? "").Replace("equipment_condition_", ""));
		}
		catch
		{
			return -1;
		}
	}

	public static string ConditionName(int level)
	{
		if (level < 0 || level >= LadderName.Length)
		{
			return "?";
		}
		return L.K(LadderTextKeys[level], LadderName[level], LadderNameEn[level]);
	}

	public static bool CanRefurbish(GameItem item)
	{
		int num = ConditionOf(item);
		if (num >= 0)
		{
			return num < 3;
		}
		return false;
	}

	public static int RefurbishFee(GameItem item)
	{
		bool ownMake = false;
		try
		{
			ownMake = item.GetTagReadonly("manufacturer")?.GetString() == "meridian";
		}
		catch
		{
		}
		return RefurbishFee(ownMake);
	}

	public static int RefurbishFee(bool ownMake) => Services.Fee("meridian", "refurbish", ownMake ? RefurbishOwn : RefurbishOther);

	public static GameItem MakeRefurbished(string entry)
	{
		string[] array = entry.Split('|');
		if (array.Length != 2 || !int.TryParse(array[1], out var result))
		{
			return null;
		}
		GameItem gameItem = DirectoryMaster.Item(array[0], isOwned: false);
		if (gameItem == null)
		{
			return null;
		}
		ItemFeatureList.EquipmentCondition condition = result switch
		{
			1 => ItemFeatureList.EquipmentCondition.Fair, 
			2 => ItemFeatureList.EquipmentCondition.Good, 
			_ => ItemFeatureList.EquipmentCondition.Excellent, 
		};
		try
		{
			GunHelper.SetEquipmentCondition(gameItem, condition);
		}
		catch (Exception ex)
		{
			ModLog.Warn("翻新成色设置失败：" + ex.Message);
		}
		return gameItem;
	}
}
