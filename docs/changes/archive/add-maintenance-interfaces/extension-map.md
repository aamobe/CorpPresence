# 扩展字段清单

下表基于 ccc80c0 核查，已更新三事件的实施结果。三个事件是 readonly 实例，初始无订阅；其余委托初始 null。当前登记/赋值者为 Malice.Install，OwnedCallbacks 逆序释放（Own 撤销对应租约；Replace 仅在仍拥有时恢复旧值）。

| 字段 / 声明 | 调用位置 | 缺省与错误策略 | 钱货影响 |
| --- | --- | --- | --- |
| `BeforeSave: ExtensionEvent` | Hooks.cs | 无订阅为空操作；Publish 按登记顺序逐项隔离异常，拒绝同事件重入 | 直接或间接业务副作用；不得当作纯查询 |
| `DayStart: ExtensionEvent` | Hooks.cs | 无订阅为空操作；Publish 按登记顺序逐项隔离异常，拒绝同事件重入 | 直接或间接业务副作用；不得当作纯查询 |
| `StateReset: ExtensionEvent` | Hooks.cs | 无订阅为空操作；Publish 按登记顺序逐项隔离异常，拒绝同事件重入 | 直接或间接业务副作用；不得当作纯查询 |
| `OrderBlocked: Func<Company, string>` | Services.Orders.cs | 无扩展/异常返回 null（不阻挡）；Ask 捕获 | 返回值影响门槛、价格或展示；非诊断入口 |
| `OrderFactor: Func<Company, double>` | Company.cs | 无扩展/异常倍率 1.0；Ask 捕获 | 返回值影响门槛、价格或展示；非诊断入口 |
| `CatalogTier: Func<Company, CompanyState, int, int>` | Company.cs | 无扩展/异常保留原 tier；Ask 捕获 | 返回值影响门槛、价格或展示；非诊断入口 |
| `Limit: Func<CatalogEntry, int?>` | CatalogEntry.cs | 无扩展/异常 null，采用原额度；Ask 捕获 | 返回值影响门槛、价格或展示；非诊断入口 |
| `ServicePrice: Func<Company, string, int, int>` | Services.cs | 无扩展/无公司/异常 basePrice；扩展结果取非负；Ask 捕获 | 返回值影响门槛、价格或展示；非诊断入口 |
| `Charge: Func<Company, int, string, string>` | Services.Payment.cs | null 现金；空串成功不补扣；非空拒绝；异常不确定，不补扣 | 直接或间接业务副作用；不得当作纯查询 |
| `ExtraModule: Func<Company, string>` | CompanyWindow.cs | 无扩展/异常 null，不附加菜单；Ask 捕获 | 返回值影响门槛、价格或展示；非诊断入口 |
| `DrawExtraModule: Action<Company, Rect, Theme>` | CompanyWindow.cs | 无扩展不绘制；异常 Ask 返回 false（调用者仍视为已选额外页） | 直接或间接业务副作用；不得当作纯查询 |
| `SpawnVisitor: Func<Company, string, StoreClient>` | CompanyVisitors.cs | 直接调用；无扩展或返回 null 视为取消/完成，异常由外层调度捕获并保留预约 | 直接或间接业务副作用；不得当作纯查询 |
| `SoldToClient: Action<StoreClient, GameItem, int>` | Services.cs | 无扩展不调用；Fire 整体捕获，不撤回已发生交易 | 直接或间接业务副作用；不得当作纯查询 |
| `BuyFactor: Func<GameItem, double>` | Hooks.cs | 无扩展/异常倍率 1.0；Ask 捕获 | 返回值影响门槛、价格或展示；非诊断入口 |
| `ScheduleExtra: Action<StoreClientManager>` | Engine.Scheduling.cs | 无扩展不调用；Fire 整体捕获，不回滚已排访客 | 直接或间接业务副作用；不得当作纯查询 |
| `CallDialog: Func<Company, Dialogue>` | Phone.cs | 无扩展/异常 null，走普通公司对话；Ask 捕获 | 直接或间接业务副作用；不得当作纯查询 |
| `ClientBuys: Func<StoreClient, GameItem, bool>` | SilverBlood.Medicines.cs | 无扩展不加接受条件；异常 false；Ask 捕获 | 返回值影响门槛、价格或展示；非诊断入口 |
| `CustomerReported: Action<string, int>` | Hooks.cs | 无扩展不调用；Fire 整体捕获，不撤回原举报 | 直接或间接业务副作用；不得当作纯查询 |
| `LuckBonus: Func<int>` | Hooks.cs | 无扩展/异常 0；Ask 捕获 | 返回值影响门槛、价格或展示；非诊断入口 |
| `LockVanillaDifficulty: Func<bool>` | Settings.cs | 无扩展沿用配置；异常 false；Ask 捕获 | 返回值影响门槛、价格或展示；非诊断入口 |
| `ForceOrderLimits: Func<bool>` | Settings.cs | 无扩展沿用配置；异常 false；Ask 捕获 | 返回值影响门槛、价格或展示；非诊断入口 |
| `ForceRestockWeeks: Func<int>` | Settings.cs | 无扩展/异常 0，沿用原周期；Ask 捕获 | 返回值影响门槛、价格或展示；非诊断入口 |

## 诊断只读来源

版本 Main.Version；资源 GameResources.Ready/Generation；已有玩家 SaveSlot.CurrentStore；保护状态 SaveSlot.CanWrite；绑定新增无更新查询 SaveBinding.MatchesReadonly；订单从 CorpState.Current.Companies[].Orders 和 Items.Count 读取。缓存计数由 Portraits、CatalogPricing、ItemRegistry 各自提供，只读 Count。

Ruling：原 IsBoundTo 调用的 Matches 在原 modData 为空时会收养新字典；诊断用单独的无更新查询，不改变原业务绑定行为。错误代价：诊断可能保守报告 Stale，但不能隐式绑定存档。
