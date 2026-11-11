# 内部维护接口设计

状态：拟议，等待用户设立目标后实施。总范围见 [overview.md](overview.md)。对应[提案](proposal.md)。

## 已有机制与实际风险

- `Integration/Ext.cs` 是 internal 类，提供 Action/Func 字段及 Fire/Ask。Ask 的默认回退不能用于已经发生扣款的调用。
- `Runtime/OwnedCallbacks.cs` 已支持 Add/Replace 和逆序释放。保留它承担未迁移字段的所有权，不重新建立通用插件容器。
- `CorpState.Get()` 会加载或创建状态，`CompanyOf()` 会补齐公司记录；`Companies.StateOf()` 经过这些路径。维护采集不得调用它们。使用 `CorpState.Current`、新增纯查询绑定入口和已有字典只读枚举。现有 `IsBoundTo(store)` 调用 `SaveBinding.Matches`，后者可能收养新建字典；实施时新增 `SaveBinding.MatchesReadonly(store)` 与 `CorpState.IsBoundReadonly(store)`，不改变原业务 Matches 行为。
- `SaveSlot.CurrentStore` 已避免访问不存在的单例；`CanWrite` 只反映当前已知写保护，不证明原始存档已经成功解码。诊断必须分别展示“绑定/加载状态”和“已知写保护状态”。
- `StoreSession`、`SaveBinding`、`GameResources.Generation` 已有会话与资源代次机制，应复用。不能把 runID 相同当作同一份存档对象。
- `Services.Pay` 已区分扩展未接管（null）、接管成功（空串）、业务拒绝（非空串）及抛异常（结果不确定）。测试必须保留这些区别。

## 文件与接口

所有新增类型保持 internal，生产目标 netstandard2.1/C# 12，不增加包依赖。

| 文件（相对项目根目录） | 职责 |
| --- | --- |
| `src/CorpPresence/Maintenance/MaintenanceSnapshot.cs` | 不可变快照及状态枚举 |
| `src/CorpPresence/Maintenance/MaintenanceDiagnostics.cs` | 主线程按需采集、会话复核、故障队列 |
| `src/CorpPresence/Maintenance/MaintenanceFormatter.cs` | 纯文本格式化，不读取游戏状态 |
| `src/CorpPresence/Maintenance/MaintenanceConsole.cs` | 仅 MAINTENANCE 构建中的本地按键入口 |
| `src/CorpPresence/Integration/ExtensionEvent.cs` | 三个无参生命周期事件共用的具名登记、释放、分发 |
| `src/CorpPresence/Companies/Services.Payment.cs` | 从 Services.cs 原样移出的 Pay，仍属于 Services partial 类 |
| `tools/MaintenanceChecks/`、`tools/ExtensionChecks/`、`tools/PaymentChecks/` | 直接编译相应真实代码，替换游戏边界 |
| `tools/Check-Maintenance.ps1` | 串联必要检查、保存本地摘要，失败即非零退出 |

### 快照与故障记录

拟定入口：`MaintenanceDiagnostics.Capture(): MaintenanceSnapshot`、`Record(string source, string stage, string code, Exception error): void`、`Reset(): void`；`MaintenanceFormatter.Format(MaintenanceSnapshot snapshot): string`。

快照字段：`Version:string`、`ResourcesReady:bool`、`Generation:long`、`SaveStatus:SaveDiagnosticStatus`、`CorpWriteProtected:bool?`、`MaliceWriteProtected:bool?`、`PendingOrders:int?`、`PendingItemUnits:long?`、`Counters:IReadOnlyList<MaintenanceCounter>`、`Faults:IReadOnlyList<MaintenanceFault>`。SaveDiagnosticStatus 为 NoStore、NotLoaded、Bound、Stale、Unstable；未加载/失配/不稳定时订单数量为 null，不能显示 0。

MaintenanceCounter 为 Name:string、Value:long；MaintenanceFault 为 Source、Stage、Code、ExceptionType、Detail（string）及 Count:int。快照另含 Registrations:IReadOnlyList<MaintenanceRegistration>，记录 EventName:string 与 Owner:string；T2 返回空集合并标明未接入，T4 接入三个事件。类型只提供 get-only 属性，构造时复制集合并封装为只读；不携带游戏对象、原始存档标识或委托。

缓存计数从所属模块获得：`Portraits.AppendDiagnostics(ICollection<MaintenanceCounter> target)`、`CatalogPricing.AppendDiagnostics(...)`、`ItemRegistry.AppendDiagnostics(...)`。只读当前集合 Count；不加载图片、不生成报价样品、不调用注册工厂。ItemRegistry 计数表示登记数量，不冒充全场景实际物品数量。不得在 Capture 中扫描库存判断控制器存在。

采集前后复核当前 store、modData 引用、资源代次及 CorpState 的绑定；过程中变化返回 Unstable。业务故障队列以弱会话引用隔离，Record 和 Capture 都检查会话；无存档阶段可记录启动错误，进入新会话先清理旧队列。诊断常量见提案容量表。

只记录预定义阶段/错误代码、异常类型以及不带路径和行号的托管调用链（声明类型、方法名）；不复制原始 Message、Data、存档 JSON 或 Exception.ToString。现有日志策略不在本次全局改写。诊断及日志输出自身异常吞在诊断边界，业务不受影响。

### 生命周期事件

`ExtensionEvent(string name)`；`IDisposable Subscribe(string owner, Action callback)`；`void Publish()`；`IReadOnlyList<string> Owners { get; }`；`void Clear()`。

Ext 的 BeforeSave、DayStart、StateReset 改为只读 ExtensionEvent 实例。owner 使用代码内固定标识，例如 `malice`，不接受玩家姓名。一个事件内相同 owner 重复注册抛 InvalidOperationException，空 owner/null callback 在登记前拒绝。按登记顺序执行；分发使用当次订阅快照，但执行前复核 token 仍有效；订阅回调期间换档/停用后不得继续使用已撤销项。

事件重入拒绝并诊断；每个监听者分别捕获异常，后续监听者继续。Subscribe 返回的一次性租约由安装者保存，重复 Dispose 无影响。给 OwnedCallbacks 增加 `Own(IDisposable lease): void` 接入已有逆序释放；不改变 Replace 的旧行为。Malice 安装失败时释放本次租约，成功后才置安装完成标志。

付款、价格、门槛等其他 Func 字段先保留原签名和异常策略。不得用 Publish 的继续执行语义处理收费，不新增优先级系统或通用结果合并器。Owners 查询只支持已迁移事件；诊断明确标注其他字段未迁移，不给出虚假的完整注册清单。

### 付款测试入口

保留 `Services.Pay(Company c, int amount, string what): string`；只将其独立为 partial 文件，便于 PaymentChecks 和 OrderChecks 链接真实实现。CompanyMoney、付款边界和必要保存保护链接真实源码，Unity/玩家余额采用替身。不为测试增加正式运行时可切换钱包、不公开可写公司状态。

PaymentChecks 覆盖 null/空串/拒绝/抛异常四种扩展返回，实际扣款后异常不补扣。需要的诊断阶段为 `payment.check`、`payment.extension`、`payment.cash`，结果不确定用固定代码记录；记录不能改变现有返回文本或支付次数。保留历史限制：这不能证明跨档付款持久化或任意原版接口已提交后异常能够回滚。

## 九项设计核查（拟定约束，实施后必须复核）

| 门禁 | 设计回答 |
| --- | --- |
| 所有权 | 快照只含副本；队列有上限；订阅由租约持有、安装者逆序释放；停用清队列 |
| 边界 | 不新增原版全局补丁；仅内部既有调用点；调试触发器仅维护构建 |
| 存档 | 不新增/重排 ID、字段、槽位或标签；不写诊断入存档；无迁移 |
| 不可逆 | 诊断不扣款、不发货、不修档；付款重排禁止，保留结果不确定语义 |
| 门槛 | 不改变目录、订阅、限购和 UI 校验；报告缺失信息不解释成放行 |
| 简单性 | 首批只迁移三个无参事件；不做第三方 API、依赖注入容器或内容框架 |
| 性能 | 查询按需；缓存读 Count；故障容量固定；无新增场景扫描；按键入口只做按下沿判断 |
| 权限 | 不新增文件/网络/线程/动态代码接口，最终仍用实际 ModScanner 核对零权限 |
| 授权与来源 | 使用现有代码与依赖；无新素材；本次仅写方案，后续开发授权不等于部署/发布授权 |

## 停止扩大范围的条件

若某个计数只能通过全场景扫描获得，首版省略该计数并说明。若付款提取要求改业务顺序，先缩小到原方法直接链接测试。发现真实缺陷先登记独立修复任务，不混进等价重构。定向修复功能只有出现已确认根因、可重复故障与幂等操作时另立提案。
