# 公司增强分期工程

后续授权补充：本次用户明确允许暂不进行游戏实测。手测与运行性能数据保留为未验证，不作为本次发布前等待项；不得宣称已经实测通过。该授权不免除代码审查、本地检查和发布物核验。

当前交付目标补充：全部分期工作完成并核验后，将审阅过的源码上传 GitHub、发布物更新到既定创意工坊项目；取得上传成功回执后核对发布文件哈希，完成发布核验。各期尚未完成的源码、回归及游戏验证项仍是前置条件，上传授权不代表跳过验收。发布沿用构建与发布规范，不额外生成未要求的压缩安装包。

审阅基线为本地 `fix/envoy-subscription-grace` 分支的 `33744ab` 与现有工作区改动，公开工坊基线为 1.0.1。当前主工程约 160 个 C# 文件、2.95 万行，包含尚未发布的仓库、水培水质和日常来访改动。分期按功能依赖与可验收的改动组织，交付仍采用同一个 `CorpPresence` Mod ID 和一个主程序集。源码定位见[源码地图](../源码地图.md)，旧档承诺见[存档数据模型](../存档数据模型.md)。

| 期次 | 工程 | 主要依赖 | 可独立验收的结果 |
| --- | --- | --- | --- |
| E1 | 运行底座、原版接入与存档兼容 | 目标版本的游戏程序集与官方接口 | 启停、读写档、物品身份与补丁作用域有可复现基线 |
| E2 | 人物来访与公司交易 | 一 | 到店登记、剧情推进、目录、扣款和配送可按链路验证 |
| E3 | 五条生产线与机器基础 | 一；涉及交易的入口依赖二 | 五条产线可分别跑通，普通水、原版鱼缸和原版机器通过回归 |
| E4 | 专用工作仓与大型集装箱仓库 | 一、二、三 | 两个仓库的订购、扩容、收纳、分类和旧档读写可完整验证 |
| E5 | 恶意模式、订阅与阵营剧情 | 一、二；全功能联调使用三、四 | 普通模式与恶意模式的门槛、扣费和剧情各自可验收 |
| E6 | 界面、资源、本地化与整体验收 | 前五期；各期同时更新对应文案 | 面板交互、缓存、四语、权限扫描和本地测试版核验 |

## E1 · 一期　运行底座、原版接入与存档兼容

- [Main.cs](../../src/CorpPresence/Main.cs) 通过 `RuntimeLifetime` 先注册核心身份和补丁，再等 `ResourceBootstrap` 确认资源就绪。资源阶段安装植物、液体、事件、恶意模式和 `Host`。梳理每个注册入口对应的解绑及部分启动失败的回滚顺序。
- [Hooks.cs](../../src/CorpPresence/Integration/Hooks.cs) 同时承担读写档、顾客、电话、输入、物品显示、养殖和化学接入。先列出每条钩子的原版方法、所有者、作用对象和退出路径。顾客生成、物品目录及夜间服务已有官方事件，保留已有迁移成果。其余补丁逐条核对目标版本和物品 ID 限定。
- [Goods.cs](../../src/CorpPresence/Items/Goods.cs) 同时做定义汇集、工厂、贴图、旧占格恢复和用品行为。先按调用职责分开审查这些路径，注册键、资源键和公开调用保持稳定。[ItemRegistry](../../src/CorpPresence/Items/ItemRegistry.cs) 是独立身份的注册入口。
- [CorpState](../../src/CorpPresence/Persistence/CorpState.cs)、`SaveSlot`、`StoreSession`、`LoadedItemRepair` 对应业务状态、存档读写、当前会话和延迟修复。原版解码物品标签之后、挂回子库存之前恢复仓库页签；延迟修复用于已完成读档的物品，两者的顺序不同。
- 验收用现有启动语言检查、状态恢复诊断和游戏手测。覆盖启动失败回滚、停用再启用、切换存档、旧物品身份和库存槽位，以及损坏 Mod 数据的写入保护。

## E2 · 二期　人物来访与公司交易

- [Engine.Scheduling](../../src/CorpPresence/Characters/Engine.Scheduling.cs) 管预约与入队，[Engine.Dialogues](../../src/CorpPresence/Characters/Engine.Dialogues.cs) 管到店、对话和剧情结算。日常到店计数与剧情走完才推进的两个时点分别验证。只排队未到店、到店后立即请离、交易完成和换档都要覆盖。
- 电话与公司面板调用 [CatalogEntry](../../src/CorpPresence/Companies/CatalogEntry.cs) 报价，经 [Services.Orders](../../src/CorpPresence/Companies/Services.Orders.cs) 复核门槛、金额和数量，再扣款登记。[CompanyVisitors.Deliveries](../../src/CorpPresence/Companies/CompanyVisitors.Deliveries.cs) 负责配送，实际摆放成功后才移除已交付条目。
- 分开列出滚动/周期额度、存档永久额度、实物或待配送订单占用的额度。子午线订购锻造设备和卡迈克尔订购标本有独立付款路径，也纳入失败语义核对。博士仓库商品的固定价及规则在四期按真实场景复核。
- 验收覆盖普通与恶意模式的同一目录在可用、暂停、余额不足和超量时的表现，配送覆盖柜台满、部分成功、次日重试和重读。订购成功应只扣一次，失败时没有扣款或占用额度。

## E3 · 三期　五条生产线与机器基础

| 产线 | 主要入口 | 回归重点 |
| --- | --- | --- |
| 帕斯泰尔烟草 | [Tobacco](../../src/CorpPresence/Production/Tobacco.cs)、`PastelCo`、`Milo` | 种植、采收、卷烟、封装、印章与成品身份 |
| 达夫雷食品与水产 | [Canning](../../src/CorpPresence/Production/Canning.cs)、[Canning.Seafood](../../src/CorpPresence/Production/Canning.Seafood.cs)、`RatWheelGenerator`、`DavreCo` | 允许的投料、封装耗材、鼠笼与电池；出料满时的消耗保护 |
| 子午线锻造 | [Forge](../../src/CorpPresence/Production/Forge.cs)、`Rosen`、`MeridianCo` | 金属纯度、坯料、三种成品和锻造设备解锁 |
| 卡迈克尔银血 | [SilverBlood](../../src/CorpPresence/Production/SilverBlood.cs) 及同名前缀文件、`CarmichaelCo` | 原版负责的养殖进度、三叶蜥额外占位、液体活化与反应釜 |
| 索拉纳香水 | [Perfume](../../src/CorpPresence/Production/Perfume.cs) 及同名前缀文件、`SolanaCo`、`Seline` | 油花与多年生进度、基底油扣量、红蓝血袋、跨机器每日采血次数 |

- [CropHealth](../../src/CorpPresence/Production/CropHealth.cs) 是烟草和油花的共同入口。两种作物联测原版矿物质、污染物判定后的成熟进度、收获档位及多年生归零。
- [ProductionOutput](../../src/CorpPresence/Production/ProductionOutput.cs)、`MachineUpgrade`、`MachineAppearance` 分别承担出料、升级和外观。共同验收顺序为判断空间、提交产物和消耗、刷新外观。每条工艺线单独验收，公共实现只在确认调用约定一致后收敛。
- 原版普通水、普通鱼缸、原版机器与普通顾客交易作为独立对照。香水和银血虽然都接入温度反应，仍按各自成分和范围分别验收。

## E4 · 四期　专用工作仓与大型集装箱仓库

- [ProductionWorkBay](../../src/CorpPresence/Production/ProductionWorkBay.cs) 管五条产线页、分类收纳和容量扩容；`MachineUpgrade` 接金属锭。核对实际配方与允许清单，共用物品可放入适用页，仓体首选页满时拒收。详细规则见[专用工作仓](../specs/production-work-bay.md)。
- [CargoWarehouse](../../src/CorpPresence/Production/CargoWarehouse.cs) 与 [CargoWarehouse.Sorting](../../src/CorpPresence/Production/CargoWarehouse.Sorting.cs) 管 24×24 页、追加页、自动分类、控制器与补购库存检查；`CargoSortWindow`、`CargoTabRename` 分别负责勾选界面及原版改名入口。页签名称、过滤规则、页顺序和仓内物品分别验收。
- 两个仓库共用 `StorageExpansionHelper.RestoreTabs` 的接入，但恢复规则不同。把接入分派与两个仓各自的恢复实现放在明确位置，同时保留原版仓的路径及现有图节点顺序。
- 博士私人目录、付款和配送由二期的交易链承接。控制器补购需同时检查店内实物、嵌套库存、配送箱及待配送订单，结账前复查。锁定最常见的两个边界：控制器不能进入集装箱的页格，拖到仓体仍能配置；新增页追加到末尾。
- 验收按[集装箱仓库场景清单](../changes/cargo-warehouse/verify.md)逐项执行，另检查专用工作仓原有五页在满页和读档时的行为。

## E5 · 五期　恶意模式、订阅与阵营剧情

- [Malice](../../src/CorpPresence/Malice/Malice.cs) 经 `Ext` 安装或替换目录档位、限购、费用、价格、交易、来访和电话行为。按回调列出默认值、安装者和卸载恢复路径，普通模式使用默认行为。
- [Envoy](../../src/CorpPresence/Malice/Envoy.cs) 管撒该来访与扶持金、复查、还款等过程。[RebelStory](../../src/CorpPresence/Malice/RebelStory.cs) 管反抗军剧情与相关事件。优先按存档状态和触发时点分块审查两份长文件，保留已开始的剧情、还款及旧档数据。
- 验收覆盖自动续订先于宽限期复查、过期转还款、已经还款的旧档、索拉纳会员例外、阵营来访和公司功能在关闭模式后的回归。索拉纳的目录例外和订阅/剧情暂停属于不同门槛。

## E6 · 六期　界面、资源、本地化与整体验收

- [CompanyWindow](../../src/CorpPresence/UI/CompanyWindow.cs)、`InputShield`、`CargoSortWindow` 核对开关窗、拖拽与输入拦截。补齐游戏内实际字号、中文字体、缩放、关闭和键盘操作检查。
- [Portraits](../../src/CorpPresence/Presentation/Portraits.cs)、`Tex`、`CardView`、`CatalogPricing` 按资源归属测打开、关闭、换档、换语言和停用。持续占用以反复操作后的对象数、纹理或缓存趋势为证据；逐帧任务以当前调用和必要性审查。
- 官方 CSV 与稳定键随各期更新，最后用仓库检查、本地化脚本、真实游戏 CSV 解析、Release 构建和官方权限扫描合并验收。正式构建确认不带本地 F1 测试键。发布与部署按当次授权办理。

## 执行起点

1. 记录当前构建对应的源码与游戏版本，保留这批工作区改动。先从一期的接入清单和两种仓库的读档顺序开始取证。
2. 二期和三期可先各自梳理用例，涉及交易或共享原版入口的变更按依赖合入。四期开始前应有交易和生产规则可对照。
3. 每期先检查当前行为，随后按小改动分别编译和验证。阶段交付记录源码、静态检查、替身检查和游戏实测各做到哪一层。
