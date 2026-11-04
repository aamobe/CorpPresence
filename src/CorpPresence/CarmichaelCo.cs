using System;
using System.Collections.Generic;

namespace CorpPresence;

internal sealed class CarmichaelCo : Company
{
	public const string Immunin = "large_purple_injector";

	public const string Phagimycin = "phagimycin_pill";

	public const string Unlicensed = "unlicensed_phagimycin_pill";

	public const string Case = "@immunin_case";

	public const int VanillaStaminaMax = 2;

	public const int SwapMax = 6;

	public static readonly CatalogEntry[] Entries = new CatalogEntry[8]
	{
		new CatalogEntry
		{
			Id = "large_purple_injector",
			Page = "药品"
		},
		new CatalogEntry
		{
			Id = "pure_white_injector",
			Page = "药品"
		},
		new CatalogEntry
		{
			Id = "phagimycin_pill",
			Page = "药品"
		},
		new CatalogEntry
		{
			Id = "smelling_salt",
			Page = "药品"
		},
		new CatalogEntry
		{
			Id = "pink_injector",
			Page = "药品",
			Label = "奥克西莫注射器"
		},
		new CatalogEntry
		{
			Id = "zerochew",
			Page = "药品",
			MinTier = 2
		},
		new CatalogEntry
		{
			Id = "@painblock",
			Page = "药品",
			MinTier = 2,
			Label = "痛觉屏蔽剂"
		},
		new CatalogEntry
		{
			Id = "@immunin_case",
			Page = "药品",
			MinTier = 3,
			WeeklyLimit = 1,
			Bundle = 5,
			Label = "正品免疫宁整箱（5 支）",
			LabelEn = "Genuine Immunivax Case (5)"
		}
	};

	public const int EscapeCoop = 60;

	public const int EscapeDays = 3;

	public const int EscapeWeaponPct = 25;

	public const int CreaturePrice = 500;

	public const int DrawerPrice = 250;

	public const int ReactorPrice = 400;

	public const int HandbookPrice = 50;

	private const string EscapeEventId = "corp_specimen_escape";

	public const int CreatureRespawnDays = 5;

	[ThreadStatic]
	private static bool _escapeSpawning;

	public override string Id => "carmicheal";

	public override string Name => L.T("卡迈克尔制药", "Carmicheal Pharmaceutical");

	public override string CharacterId => "isa";

	public override long PhoneNumber => 22764L;

	public override string[] TierNames => L.T(new string[3] { "登记药房", "指定药房", "合规伙伴" }, new string[3] { "Registered Pharmacy", "Designated Pharmacy", "Compliance Partner" });

	public override string CardFlavor => L.T("白底青字，印着卡迈克尔的十字标和 22764。", "Teal on white, with the Carmicheal cross and 22764.");

	public override string DeskName => L.T("卡迈克尔客服", "Carmicheal Customer Service");

	public override string DeskAsk => L.T("请问办理什么业务？", "How can I help you today?");

	public override string[] FrontDesk => new string[2]
	{
		L.T("卡迈克尔制药客户服务部。请报您的药房登记号。", "Carmicheal Pharmaceutical customer service. Your pharmacy registration number, please."),
		L.T("卡迈克尔制药。本通话可能被录音。", "Carmicheal Pharmaceutical. This call may be recorded.")
	};

	public override string FrontDeskDone => L.T("已登记。明早由专人配送。", "Logged. Our courier delivers tomorrow morning.");

	public static int StaminaGap => Settings.Period("stamina", 10);

	public static int StaminaFee => Services.Fee("carmicheal", "stamina", 500);

	public static int SwapFee => Services.Fee("carmicheal", "swap", 50);

	public static int SwapGap => Settings.Period("swap", 3);

	public override CatalogEntry[] Catalog => Entries;

	private static CompanyState CState => CorpState.Get()?.CompanyOf("carmicheal");

	public static bool Licensed => CState?.Licensed ?? false;

	public static bool ReactorOpen => CorpState.Get()?.Of("isa").SideDone.Contains(7) ?? false;

	public static bool PainBlockOpen()
	{
		return CorpState.Get()?.Of("isa").SideDone.Contains(4) ?? false;
	}

	public static bool StaminaOpen()
	{
		CorpState corpState = CorpState.Get();
		if (corpState == null)
		{
			return false;
		}
		return corpState.Of("isa").Chapter >= 5;
	}

	public static void OnEndDay()
	{
		if (!GameResources.Ready || SaveSlot.CurrentStore == null || !SaveSlot.CanWrite("CorpPresence"))
		{
			TraceEscape("关店资格检查暂停：资源/玩家未就绪或存档不可写");
			return;
		}
		CompanyState cState = CState;
		CharacterState characterState = CorpState.Get()?.Of("isa");
		TraceEscape("关店资格：Chapter=" + characterState?.Chapter + ", Coop=" + cState?.Coop + ", EscapeDay=" + cState?.EscapeDay);
		if (cState == null || characterState == null || cState.EscapeDay >= 0 || characterState.Chapter < 5 || cState.Coop < 60)
		{
			return;
		}
		bool aquaculture = Wilde.Aquaculture;
		TraceEscape("水产养殖解锁=" + aquaculture);
		if (!aquaculture)
		{
			return;
		}
		try
		{
			StoreEvent storeEvent = new StoreEvent
			{
				identifier = "corp_specimen_escape",
				newsName = L.T("卡迈克尔制药收容物逃逸", "Carmicheal Specimen Breach"),
				displayName = L.T("收容物逃逸", "Specimen Breach"),
				newsDescription = L.T("上层区一片慌乱：卡迈克尔制药的实验室有危险生物逃逸。治安部提醒所有居民，见到没见过的生物立即举报。武器价格上涨。", "Panic in the Upper Levels: a dangerous organism has escaped a Carmicheal lab. Security urges all residents to report any unfamiliar creature at once. Weapon prices are up."),
				duration = 3,
				importance = 8,
				eventType = StoreEvent.EventType.NORMALE,
				eventArea = StoreEvent.EventArea.ALL
			};
			storeEvent.negociationDatas.Add(new NegociationData("WEAPON", 25, storeEvent.newsName, storeEvent.displayName));
			StoreStation.Instance.storeEventManager.QueueFuturEvent(storeEvent, 1, IsKnown: true);
			cState.EscapeDay = StoreStation.GetDayCounter() + 1;
			ModLog.Info($"银血线：收容物逃逸新闻排在第 {cState.EscapeDay} 天");
		}
		catch (Exception ex)
		{
			ModLog.Warn("收容物逃逸新闻失败：" + ex.Message);
		}
	}

	public static void OnDayStart()
	{
		if (!GameResources.Ready || SaveSlot.CurrentStore == null || !SaveSlot.CanWrite("CorpPresence"))
		{
			TraceEscape("开店检查暂停：资源/玩家未就绪或公司存档不可写");
			return;
		}
		CompanyState cState = CState;
		int dayCounter = StoreStation.GetDayCounter();
		if (cState == null)
		{
			TraceEscape("开店检查暂停：公司状态不存在");
			return;
		}
		TraceEscape($"开店：Day={dayCounter}, EscapeDay={cState.EscapeDay}, CreatureGiven={cState.CreatureGiven}, Licensed={cState.Licensed}, LostDay={cState.CreatureLostDay}");
		if (cState.EscapeDay < 0)
		{
			TraceEscape("未安排逃逸新闻，不进入发放");
			return;
		}
		if (cState.Licensed)
		{
			TraceEscape("已有养殖许可，按原规则停止逃逸发放/补发");
			return;
		}
		if (!cState.CreatureGiven)
		{
			if (dayCounter < (long)cState.EscapeDay + 1L)
			{
				TraceEscape("尚未到首次出现日：新闻排期日的次日开店才发放");
			}
			else if (SpawnEscapee(L.T("柜台底下有什么东西在动。", "Something is moving under the counter.")))
			{
				cState.CreatureGiven = true;
				TraceEscape("提交：CreatureGiven=True，已确认实物入库");
			}
			else
			{
				TraceEscape("首次发放未完成，CreatureGiven不变，下次开店重试");
			}
			return;
		}
		int num = 0;
		foreach (GameItem item in OwnedCreatures())
		{
			if (!AquacultureHelper.IsDead(item))
			{
				num++;
			}
		}
		TraceEscape($"现有活体={num}");
		if (num > 0)
		{
			cState.CreatureLostDay = -1;
		}
		else if (cState.CreatureLostDay < 0)
		{
			cState.CreatureLostDay = dayCounter;
			TraceEscape("已标记发放但未找到活体：按原规则开始5天补发等待");
		}
		else if ((long)dayCounter - (long)cState.CreatureLostDay >= 5)
		{
			if (SpawnEscapee(L.T("柜台底下又有东西在动。看来逃出来的不止一只。", "Something is moving under the counter again. More than one got out, it seems.")))
			{
				cState.CreatureLostDay = -1;
				TraceEscape("补发提交：已确认实物入库");
			}
			else
			{
				TraceEscape("补发未完成，保留原等待日期");
			}
		}
		else
		{
			TraceEscape($"等待补发：距丢失记录{(long)dayCounter - (long)cState.CreatureLostDay}天，要求5天");
		}
	}

	private static bool EscapeAttached(GameItem item)
	{
		if (item != null && (!(item is GameItemElement gameItemElement) || !gameItemElement.IsDestroyed()))
		{
			return item.parentInventory != null;
		}
		return false;
	}

	private static void TraceEscape(string text)
	{
		try
		{
			ModLog.Info("[三叶蜥发放验证] " + text);
		}
		catch
		{
		}
	}

	private static bool SpawnEscapee(string notice)
	{
		if (_escapeSpawning)
		{
			TraceEscape("忽略重复进入发放");
			return false;
		}
		StoreSession storeSession = StoreSession.Capture();
		if (!storeSession.IsCurrent || !SaveSlot.CanWrite("CorpPresence"))
		{
			TraceEscape("发放暂停：会话失效或存档不可写");
			return false;
		}
		EmporiumEntry instance = EmporiumEntry.Instance;
		if (instance == null || (instance.frontInvinvElement == null && instance.invElement == null))
		{
			TraceEscape("发放暂停：柜台/店铺库存尚未就绪");
			return false;
		}
		_escapeSpawning = true;
		GameItem gameItem = null;
		try
		{
			gameItem = Goods.Make("silverblood");
			if (gameItem == null)
			{
				TraceEscape("生成失败：silverblood工厂返回空，保留发放资格");
				return false;
			}
			TraceEscape("生成成功：" + gameItem.identifier);
			if (!storeSession.IsCurrent)
			{
				TraceEscape("生成后会话改变，停止发放");
				return false;
			}
			ContrabandHelper.InitContrabandItem(gameItem, 3);
			GeneralHelper.SetItemOwned(gameItem);
			if (instance.frontInvinvElement != null && GraphUtils.CanAccept(instance.frontInvinvElement, gameItem))
			{
				GraphUtils.TryAcceptAll(instance.frontInvinvElement, gameItem);
			}
			if (!EscapeAttached(gameItem))
			{
				if (gameItem is GameItemElement gameItemElement && gameItemElement.IsDestroyed())
				{
					TraceEscape("柜台尝试后对象已销毁，保留资格，不重复使用该对象");
					return false;
				}
				TraceEscape("柜台未接收，尝试原版店铺库存回退");
				GraphUtils.EmporiumTryAdd(instance.invElement ?? instance.frontInvinvElement, gameItem);
			}
			if (!EscapeAttached(gameItem))
			{
				TraceEscape("发放失败：原版回退后仍未入库，禁止标记已发放");
				return false;
			}
			if (!storeSession.IsCurrent)
			{
				TraceEscape("入库后会话改变，不提交当前发放标记");
				return false;
			}
			string text;
			if (gameItem.parentInventory == instance.frontInvinvElement)
			{
				text = "落点确认：前柜台";
			}
			else
			{
				text = ((gameItem.parentInventory == instance.invElement) ? "落点确认：店铺主库存" : "落点确认：原版回退接受的其他库存/容器");
			}
			TraceEscape(text);
			try
			{
				SilverBlood.Tell(notice);
			}
			catch (Exception ex)
			{
				TraceEscape("实物已入库，提示显示失败：" + ex.Message);
			}
			return storeSession.IsCurrent;
		}
		catch (Exception ex2)
		{
			bool flag = EscapeAttached(gameItem);
			TraceEscape($"发放异常：{ex2.GetType().Name}: {ex2.Message}；实物已入库={flag}");
			return flag && storeSession.IsCurrent;
		}
		finally
		{
			try
			{
				if (gameItem != null && !EscapeAttached(gameItem) && (!(gameItem is GameItemElement gameItemElement2) || !gameItemElement2.IsDestroyed()))
				{
					gameItem.Destroy();
				}
			}
			catch (Exception ex3)
			{
				TraceEscape("未入库对象回收失败：" + ex3.Message);
			}
			_escapeSpawning = false;
		}
	}

	public static List<GameItem> OwnedCreatures()
	{
		List<GameItem> list = new List<GameItem>();
		try
		{
			EmporiumEntry store = EmporiumEntry.Instance;
			if (store != null)
			{
				// Warehouse extensions can contribute their live items through GetAllItems.
				AddOwnedCreatures(store.GetAllItems(), list);
				// The vanilla owned-items query also includes this separate counter.
				if (store.backInvinvElementCounter != null)
				{
					AddOwnedCreatures(GraphUtils.FindAllChildrenType<GameItem>(store.backInvinvElementCounter), list);
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static void AddOwnedCreatures(List<GameItem> items, List<GameItem> creatures)
	{
		if (items == null) return;
		foreach (GameItem item in items)
		{
			if (SilverBlood.IsCreature(item) && GeneralHelper.IsItemOwned(item) && !creatures.Contains(item))
			{
				creatures.Add(item);
			}
		}
	}

	public static bool HasAdultCreature()
	{
		foreach (GameItem item in OwnedCreatures())
		{
			if (!AquacultureHelper.IsDead(item) && AquacultureHelper.GetStage(item) >= FishStage.Adult)
			{
				return true;
			}
		}
		return false;
	}

	public static void GrantLicense()
	{
		CompanyState cState = CState;
		if (cState == null || cState.Licensed)
		{
			return;
		}
		cState.Licensed = true;
		foreach (GameItem item in OwnedCreatures())
		{
			try
			{
				ContrabandHelper.RemoveContrabandStatus(item);
			}
			catch
			{
			}
		}
		ModLog.Info("银血线：养殖许可已发放");
	}

	public static void OnBloodDrawn(int tubes)
	{
		CompanyState cState = CState;
		if (cState != null)
		{
			cState.BloodDrawn += tubes;
		}
	}

	public static string OrderSpecimen(Company c, string id, int price)
	{
		CompanyState cState = CState;
		if (cState == null)
		{
			return L.T("存档未就绪", "Save not ready");
		}
		if (!cState.Licensed || ((id == "@isomer_reactor" || id == "@sb_handbook") && !ReactorOpen))
		{
			return L.T("还没有开通", "Not available yet");
		}
		string text = Services.OrderBlocked(c);
		if (text != null)
		{
			return text;
		}
		string text2 = Services.Pay(c, price, "specimen");
		if (text2 != null)
		{
			return text2;
		}
		cState.Orders.Add(new Order
		{
			Day = StoreStation.GetDayCounter() + 1,
			Items = new List<string> { id }
		});
		Services.AddCoop(c, (double)price / Settings.CoopCredits, "订购标本");
		ModLog.Info($"标本订单：{id}，{price}₵");
		return null;
	}

	public static GameItem MakeGenuine(string id)
	{
		if (id == "@immunin_case")
		{
			id = "large_purple_injector";
		}
		if (id == "large_purple_injector")
		{
			return PreBuiltItemHelper.CreateRealGenuineInjector();
		}
		return Goods.MakeSpecial(id) ?? DirectoryMaster.Item(id, isOwned: false);
	}

	public static string FakeReason(GameItem item)
	{
		try
		{
			if (item == null)
			{
				return null;
			}
			string identifier = item.identifier;
			if (identifier == "unlicensed_phagimycin_pill")
			{
				return L.T("无证仿制", "Unlicensed copy");
			}
			if (identifier != "large_purple_injector")
			{
				return null;
			}
			if (item.IsTag("ISSUE_LABEL") || item.IsTag("ISSUE_COLOR") || item.IsTag("ISSUE_SERIAL") || item.IsTag("ISSUE_BODY"))
			{
				return L.T("假冒", "Counterfeit");
			}
			if (InsInjectorHelper.IsOnlyIssueRecalled(item))
			{
				return L.T("召回批次", "Recalled batch");
			}
		}
		catch
		{
		}
		return null;
	}

	public static string SwapTarget(GameItem item)
	{
		try
		{
			return (item?.identifier == "unlicensed_phagimycin_pill") ? "phagimycin_pill" : "large_purple_injector";
		}
		catch
		{
			return "large_purple_injector";
		}
	}
}
