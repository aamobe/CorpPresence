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
			Factory = MakeGenuine,
			Page = "药品"
		},
		new CatalogEntry
		{
			Id = "pure_white_injector",
			Factory = MakeGenuine,
			Page = "药品"
		},
		new CatalogEntry
		{
			Id = "phagimycin_pill",
			Factory = MakeGenuine,
			Page = "药品"
		},
		new CatalogEntry
		{
			Id = "smelling_salt",
			Factory = MakeGenuine,
			Page = "药品"
		},
		new CatalogEntry
		{
			Id = "pink_injector",
			Factory = MakeGenuine,
			Page = "药品",
			Label = "奥克西莫注射器"
		},
		new CatalogEntry
		{
			Id = "zerochew",
			Factory = MakeGenuine,
			Page = "药品",
			MinTier = 2
		},
		new CatalogEntry
		{
			Id = "@painblock",
			Factory = MakeGenuine,
			Page = "药品",
			MinTier = 2,
			Label = "痛觉屏蔽剂"
		},
		new CatalogEntry
		{
			Id = "@immunin_case",
			Factory = MakeGenuine,
			Page = "药品",
			MinTier = 3,
			WeeklyLimit = 1,
			Bundle = 5,
			Label = "正品免疫宁整箱（5 支）",
			LabelKey = "companies.catalog.genuineImmunivaxCase5.name",
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

	public override string Id => "carmicheal";

	public override string Name => L.K("companies.carmichael.name.carmichealPharmaceutical", "卡迈克尔制药", "Carmicheal Pharmaceutical");

	public override string CharacterId => "isa";

	public override long PhoneNumber => 22764L;

	public override string[] TierNames => new string[3] { L.K("companies.carmichael.tierNames.registeredPharmacy", "登记药房", "Registered Pharmacy"), L.K("companies.carmichael.tierNames.designatedPharmacy", "指定药房", "Designated Pharmacy"), L.K("companies.carmichael.tierNames.compliancePartner", "合规伙伴", "Compliance Partner") };

	public override string CardFlavor => L.K("companies.carmichael.cardFlavor.tealWhiteWithCarmichealCross", "白底青字，印着卡迈克尔的十字标和 22764。", "Teal on white, with the Carmicheal cross and 22764.");

	public override string DeskName => L.K("companies.carmichael.deskName.carmichealCustomerService", "卡迈克尔客服", "Carmicheal Customer Service");

	public override string DeskAsk => L.K("companies.carmichael.deskAsk.howCanIHelpToday", "请问办理什么业务？", "How can I help you today?");

	public override string[] FrontDesk => new string[2]
	{
		L.K("companies.carmichael.frontDesk.carmichealPharmaceuticalCustomerServicePharmacy", "卡迈克尔制药客户服务部。请报您的药房登记号。", "Carmicheal Pharmaceutical customer service. Your pharmacy registration number, please."),
		L.K("companies.carmichael.frontDesk.carmichealPharmaceuticalThisCallMay", "卡迈克尔制药。本通话可能被录音。", "Carmicheal Pharmaceutical. This call may be recorded.")
	};

	public override string FrontDeskDone => L.K("companies.carmichael.frontDeskDone.loggedOurCourierDeliversTomorrow", "已登记。明早由专人配送。", "Logged. Our courier delivers tomorrow morning.");

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
		if (!GameResources.Ready || SaveSlot.CurrentStore == null || !SaveSlot.CanWrite("CorpPresence")) { TraceEscape("关店资格检查暂停：资源/玩家未就绪或存档不可写"); return; }
		CompanyState cState = CState;
		CharacterState characterState = CorpState.Get()?.Of("isa");
		TraceEscape("关店资格：Chapter=" + characterState?.Chapter + ", Coop=" + cState?.Coop + ", EscapeDay=" + cState?.EscapeDay);
		if (cState == null || characterState == null || cState.EscapeDay >= 0 || characterState.Chapter < 5 || cState.Coop < 60)
		{
			return;
		}
		bool aquaculture = Wilde.Aquaculture;
		TraceEscape("水产养殖解锁=" + aquaculture);
		if (!aquaculture) return;
		try
		{
			StoreEvent storeEvent = new StoreEvent
			{
				identifier = "corp_specimen_escape",
				newsName = L.K("companies.carmichael.onEndDay.carmichealSpecimenBreach", "卡迈克尔制药收容物逃逸", "Carmicheal Specimen Breach"),
				displayName = L.K("companies.carmichael.onEndDay.specimenBreach", "收容物逃逸", "Specimen Breach"),
				newsDescription = L.K("companies.carmichael.onEndDay.panicUpperLevelsDangerousOrganism", "上层区一片慌乱：卡迈克尔制药的实验室有危险生物逃逸。治安部提醒所有居民，见到没见过的生物立即举报。武器价格上涨。", "Panic in the Upper Levels: a dangerous organism has escaped a Carmicheal lab. Security urges all residents to report any unfamiliar creature at once. Weapon prices are up."),
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

	[ThreadStatic] private static bool _escapeSpawning;

	public static void OnDayStart()
	{
		if (!GameResources.Ready || SaveSlot.CurrentStore == null || !SaveSlot.CanWrite("CorpPresence"))
		{ TraceEscape("开店检查暂停：资源/玩家未就绪或公司存档不可写"); return; }
		CompanyState state = CState;
		int day = StoreStation.GetDayCounter();
		if (state == null) { TraceEscape("开店检查暂停：公司状态不存在"); return; }
		TraceEscape($"开店：Day={day}, EscapeDay={state.EscapeDay}, CreatureGiven={state.CreatureGiven}, Licensed={state.Licensed}, LostDay={state.CreatureLostDay}");
		if (state.EscapeDay < 0) { TraceEscape("未安排逃逸新闻，不进入发放"); return; }
		if (state.Licensed) { TraceEscape("已有养殖许可，按原规则停止逃逸发放/补发"); return; }
		if (!state.CreatureGiven)
		{
			if ((long)day < (long)state.EscapeDay + 1) { TraceEscape("尚未到首次出现日：新闻排期日的次日开店才发放"); return; }
			if (SpawnEscapee())
			{ state.CreatureGiven = true; TraceEscape("提交：CreatureGiven=True，已确认实物入库"); }
			else TraceEscape("首次发放未完成，CreatureGiven不变，下次开店重试");
			return;
		}
		int alive = 0;
		foreach (GameItem creature in OwnedCreatures()) if (!AquacultureHelper.IsDead(creature)) alive++;
		TraceEscape($"现有活体={alive}");
		if (alive > 0) state.CreatureLostDay = -1;
		else if (state.CreatureLostDay < 0)
		{ state.CreatureLostDay = day; TraceEscape("已标记发放但未找到活体：按原规则开始5天补发等待"); }
		else if ((long)day - state.CreatureLostDay >= 5)
		{
			if (SpawnEscapee())
			{ state.CreatureLostDay = -1; TraceEscape("补发提交：已确认实物入库"); }
			else TraceEscape("补发未完成，保留原等待日期");
		}
		else TraceEscape($"等待补发：距丢失记录{(long)day - state.CreatureLostDay}天，要求5天");
	}

	private static bool EscapeAttached(GameItem item)
	{
		return item != null && !(item is GameItemElement element && element.IsDestroyed()) && item.parentInventory != null;
	}

	private static void TraceEscape(string text)
	{
		try { ModLog.Info("[三叶蜥发放验证] " + text); } catch { }
	}

	private static bool SpawnEscapee()
	{
		if (_escapeSpawning) { TraceEscape("忽略重复进入发放"); return false; }
		StoreSession session = StoreSession.Capture();
		if (!session.IsCurrent || !SaveSlot.CanWrite("CorpPresence")) { TraceEscape("发放暂停：会话失效或存档不可写"); return false; }
		var store = EmporiumEntry.Instance;
		if (store == null || (store.invElement == null && store.backInvinvElement == null)) { TraceEscape("发放暂停：主库存/后柜台尚未就绪"); return false; }
		_escapeSpawning = true;
		GameItem item = null;
		try
		{
			item = Goods.Make("silverblood");
			if (item == null) { TraceEscape("生成失败：silverblood工厂返回空，保留发放资格"); return false; }
			TraceEscape("生成成功：" + item.identifier);
			if (!session.IsCurrent) { TraceEscape("生成后会话改变，停止发放"); return false; }
			ContrabandHelper.InitContrabandItem(item, 3);
			GeneralHelper.SetItemOwned(item);
			if (!EscapeePlacement.TryPlace(store, item)) { TraceEscape("可存档库存未接收，保留发放资格"); return false; }
			if (!session.IsCurrent) { TraceEscape("入库后会话改变，不提交当前发放标记"); return false; }
			TraceEscape(ReferenceEquals(item.parentInventory, store.invElement) ? "落点确认：店铺主库存" : "落点确认：后柜台");
			try
			{
				var ui = StoreUIManager.Instance;
				var notifier = ReputationUIManager.Instance;
				if (ui != null && notifier != null)
				{
					ui.Notify(L.K("companies.carmichael.spawnEscapee.somethingMovingInsideCheckMain", "店里有动静。看看主库存和后柜台。", "Something is moving inside. Check the main inventory and the back counter."));
					notifier.notifierTimer += 2f;
				}
			}
			catch (Exception error) { TraceEscape("实物已入库，提示显示失败：" + error.Message); }
			return session.IsCurrent;
		}
		catch (Exception error)
		{
			bool attached = EscapeePlacement.IsSaved(store, item);
			TraceEscape($"发放异常：{error.GetType().Name}: {error.Message}；实物已入库={attached}");
			return attached && session.IsCurrent;
		}
		finally
		{
			try
			{
				if (item != null && !EscapeAttached(item) && !(item is GameItemElement element && element.IsDestroyed())) item.Destroy();
			}
			catch (Exception error) { TraceEscape("未入库对象回收失败：" + error.Message); }
			_escapeSpawning = false;
		}
	}
	public static List<GameItem> OwnedCreatures()
	{
		List<GameItem> list = new List<GameItem>();
		try
		{
			List<GameItem> list2 = EmporiumEntry.Instance?.GetAllOwnedItems();
			if (list2 != null)
			{
				foreach (GameItem item in list2)
				{
					if (SilverBlood.IsCreature(item))
					{
						list.Add(item);
					}
				}
			}
		}
		catch
		{
		}
		return list;
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
			return L.K("companies.carmichael.orderSpecimen.saveNotReady", "存档未就绪", "Save not ready");
		}
		if (!cState.Licensed || ((id == "@isomer_reactor" || id == "@sb_handbook") && !ReactorOpen))
		{
			return L.K("companies.carmichael.orderSpecimen.notAvailableYet", "还没有开通", "Not available yet");
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
		try { ModLog.Info($"标本订单：{id}，{price}₵"); } catch { }
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
				return L.K("companies.carmichael.fakeReason.unlicensedCopy", "无证仿制", "Unlicensed copy");
			}
			if (identifier != "large_purple_injector")
			{
				return null;
			}
			if (item.IsTag("ISSUE_LABEL") || item.IsTag("ISSUE_COLOR") || item.IsTag("ISSUE_SERIAL") || item.IsTag("ISSUE_BODY"))
			{
				return L.K("companies.carmichael.fakeReason.counterfeit", "假冒", "Counterfeit");
			}
			if (InsInjectorHelper.IsOnlyIssueRecalled(item))
			{
				return L.K("companies.carmichael.fakeReason.recalledBatch", "召回批次", "Recalled batch");
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


