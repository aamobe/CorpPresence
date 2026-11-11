# 内置模块状态与接口清理记录

2026-11-12。用于[完整框架方案](../superpowers/plans/2026-11-12-integrated-framework.md) B1 的实施，不是对外扩展 API。

## 现有状态归属

| 状态/资源 | 作用域与所有者 | 退出/切档行为 |
| --- | --- | --- |
| Main 装配、Harmony 补丁 | 启用期，RuntimeLifetime 先登记释放再获取 | 逆序释放；补丁由当前 Harmony ID 撤回；失败隔离，不假装已安全清理 |
| Host、ResourceBootstrap | 启用期，由 Main 创建与销毁 | 不随普通场景自动卸载；实际存活状态/热键缺报告仍需定位 |
| 物品工厂和注册订阅 | 启用期，ItemRegistry | 仅撤回自身仍拥有的工厂；失败保留可重试记录；资源就绪后激活 |
| 恶意模式生命周期事件、Envoy 更新任务 | 启用期，Malice.Install 的租约集合 | 卸载撤回自身租约，不移除其他订阅者；模式设置开关不等于模块卸载 |
| Envoy 通知队列 | 存档期，Envoy | StateReset、卸载、安装回滚清空 |
| 公司/恶意模式持久记录 | 当前存档，CorpState / MaliceState 及 SaveBinding | 各自读取和校验；换档重新绑定；损坏记录写保护 |
| 估价样品与配送临时绑定 | 由 CatalogPricing / Crates 管理 | Main 卸载及 Hooks 换档路径释放或重置 |
| 运行期故障 | 当前启用期，MaintenanceDiagnostics | 不随换档消失；下一次启用重置；不稳定快照保留 |
| 会话故障 | 当前存档及绑定代次，MaintenanceDiagnostics | 换档丢弃；不稳定采集不把旧值当成当前状态 |
| 有限物品 Sprite 缓存 | 进程级，Goods/Cards；原版 UI 也持有引用 | 当前无安全解绑协议，不在停用时贸然销毁；这不是动态资源热重载契约 |
| 订货购物车/分类/分页 | 当前打开窗口，OrderPageState | 标签切换保留；打开、关闭及卸载重置；换档调用关闭路径 |
| 品牌选择、换货/翻新预览、其他页面状态 | CompanyWindow，尚未全部迁移 | 仍待 B3 后续清理，不宣称所有页面已归属 |

此表覆盖当前改动涉及的入口，不宣称所有静态状态均已审计。新增状态必须指定作用域和退出路径；不要求所有状态进入同一个全局注册表。

## 已移除的子模组间接层

以下十三个 Ext 单槽字段只有 Malice.Install 一个生产赋值者，已删除字段、Replace 安装样板和无用 Forced 辅助函数。调用直接指向内置业务，继续以 Malice.IsInstalled 控制启用期边界；Ext.Ask 只作为已有异常保护，不再承载可替换实现。

| 原字段 | 当前调用位置 | 未安装/关闭/失败语义 |
| --- | --- | --- |
| ForceOrderLimits | Settings.OrderLimits | 未安装不强制，已安装按模式设置；异常回退 false |
| ForceRestockWeeks | Settings.RestockWeeks | 未安装或模式关闭使用原配置；模式开启强制1周；异常回退0后沿用配置 |
| LockVanillaDifficulty | Settings.LockedVanilla | 未安装不锁定，已安装按模式设置；异常回退 false |
| OrderFactor | Company.OrderRate | 未安装不叠加；仅 Applies 公司取 Envoy 倍率；异常回退1 |
| BuyFactor | Hooks.Trade | 未安装不叠加；模式开启取两项反抗军倍率乘积；异常回退1 |
| LuckBonus | Hooks.Effects | 未安装不增加；已安装直接调用 Envoy；异常回退0 |
| OrderBlocked | Services.OrderBlocked | 未安装或公司为空不阻止；异常回退 null；业务限制仍由 Malice 判断 |
| CatalogTier | Company.CatalogTier | 未安装及异常沿用合作度档位；会员公司例外仍由 Malice 判断 |
| Limit | CatalogEntry.Limit | 显式 WeeklyLimit 优先；未安装及异常无覆盖，继续原有普通限购规则 |
| ServicePrice | Services.Fee | 未安装或公司不存在返回原价；已安装沿用异常回退原价及非负钳制 |
| Charge | Services.Pay / Malice.VoucherDeduction | 未安装不调用模块且不预览代金券；已安装保留三态返回、异常不补扣现金 |

安装未提交或卸载开始后不再进入这些业务查询；三个生命周期事件与更新租约仍按原顺序安装/撤回。变更不增加新的同义策略包装。

## 其余六个单槽字段的迁移顺序

| 字段 | 实际边界 | 处理要求 |
| --- | --- | --- |
| SpawnVisitor | 预约重试 | 必须保留异常与 null 的区别：null取消预约，异常保留重试；不得用返回null的通用保护吞掉异常 |
| ScheduleExtra / SoldToClient / CustomerReported | 剧情调度与交易通知 | 按原调用顺序迁移，并保留现有故障隔离和模式判断 |
| CallDialog / ClientBuys | 电话和收货 | 保留原空值/false回退及短路顺序，再移除安装样板 |

这些字段是待迁移项目，不以暂时仍存在作为永久保留理由。三个具名生命周期事件和 RuntimeUpdates 有实际多订阅、撤回及异常隔离职责，继续保留。

## 存档校验的取舍

MaliceState 的专用结构校验已从 Persistence/SaveStructure 移回 MaliceState，仍在实际解码后、AcceptRead 前调用，错误消息和条件保持不变。这里已有明确的唯一调用者，不再为“让模块登记校验”新增一层全局委托。公司结构由 SaveStructure 负责，恶意模式结构由所属模块负责，避免基础层反向认识特使字段。

实现后生产编译通过；相关业务和存档回归仍安排在最终统一测试，不把编译成功当作行为等价证明。


## 目录报价数据归属

固定报价现在由所属公司的 CatalogEntry 定义提供，通用 BaseValue、ListValue、Quote、EventFactor 不再按印章、工位、仓库或盲盒 ID 分支。

- FixedBaseValue：可选的基础估值，当前仅无实体的盲盒指定500。普通物品仍创建受生命周期管理的样品估值，保留就绪与失败保护。
- FixedListValue：可选的目录单位总标价（若有 Bundle，也表示整组价格），指定后不叠加事件和标签加价。印章400、工位800、仓库15000、扩页2000、控制器3000、盲盒500；常量由对应业务维护。
- IgnoreOrderRate：印章明确不参与公司倍率。盲盒仍由 Spacer 页面选择1倍，其他固定标价保留 Quote 的倍率与非法倍率检查，不扩大现有免倍率范围。
- 固定标价不替代基础估值；印章和设备仍需正常样品与资源就绪。没有配置时继续现有取样、事件、标签和整组估价逻辑。

仓库 UI、后端强制成交价、物品定义共用价格常量；目录初始化只引用 const，不调用带 Companies.All 静态依赖的 CargoWarehouse 方法，避免初始化环。订单后端按 ID 校验解锁与额度的逻辑尚未迁移，不能仅信任可构造的目录对象来放行。CatalogEntry.Make 的特殊创建路径和页面内特殊倍率仍待后续按职责整理。

本批生产和链接真实 CatalogEntry 的 RestorationChecks 工程编译通过，未执行测试。最终验收需对照固定商品标价、公司倍率、盲盒与印章例外、普通物品事件价及实际扣款。


## 内置付款处理契约

Services.Pay 完成存档和公司状态检查后，仅在 Malice 已安装且公司非空时调用 Malice.Charge。Charge 是内置业务入口，不是可替换插件槽；代金券预览以相同安装状态及 Applies 判断，不再比较委托身份。

返回 null 表示未处理，继续原现金流程；空字符串表示付款完成，不再扣现金；非空字符串表示拒绝，返回原因。任何异常都表示结果不确定，记录故障并停止，不补扣现金。保留原 payment.extension 诊断阶段名，以免旧日志筛选规则失效。实际业务中的代金券优先、余额预检、现金扣款、扣券与返券顺序未变。

必要定向验证执行真实 Services.Payment 和 Services.Orders、替身游戏/恶意模式边界：PaymentChecks 的23项检查通过；OrderChecks 通过付款拒绝、成功单扣、重入、报价/付款异常、数量/额度拒绝及会话变化场景。这不代表真实 Malice 代金券业务或游戏实测已完成，集中验收仍须覆盖混合支付、纯现金、纯代金券及模式开关。


## 内置合约页与订货页面状态

合约页标签由 CompanyWindow.WithTerms 直接检查 Malice.IsInstalled / Applies，绘制由 DrawTermsIfSelected 直接调用 MalicePanel.Draw。删除 ExtraModule 与 DrawExtraModule 单槽和安装样板，不再提供无使用方的插件页面入口；异常保护和稳定本地化键保留。

OrderPageState 集中拥有购物车、帕斯泰尔分类、梅里迪安配件分页以及联盟订货分页/分页键，各订货页面直接访问状态单元，不保留同义转发字段。同一窗口内的标签切换继续共享购物车。Open 重置整组订货状态，Close 立即清空并重置分页，换档的既有 Close 路径因此清理旧档条目引用；ReleaseUi 的 finally 也执行完整重置。原先打开时清购物车但可能保留分页，现在统一回到第一页。

没有改动窗口位置、拖动、主题与其他页面选择/缓存的作用域。品牌、换货、翻新及特殊报价对象仍待后续归属，不把本批订货状态迁移当作整个 B3 完成。生产编译通过，游戏验收留到最后：标签切换保留购物车，关闭重开/换公司/正常双档切换清空，分页归零，恶意模式合约标签及绘制正常。
