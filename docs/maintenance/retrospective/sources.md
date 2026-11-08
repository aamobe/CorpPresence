# 复盘证据索引

[返回问题总汇](README.md)

本次先读取历史报告、当前源码与版本说明，再整理案例。没有重新运行历史检查、启动游戏、改玩家存档或制造新的故障。下列“历史复现/实测”均指原材料明确记录的当时结果。

一部分早期材料只在作者本地历史工作目录保留，不在当前GitHub仓库；这里保留文档名、时代和证据摘要，使公开读者知道结论来自哪里。没有把本机路径、个人日志或存档复制进仓库。不能因此宣称公开读者已经能独立重跑全部历史测试。

| 编号 | 材料 | 可支持的结论与限制 |
| --- | --- | --- |
| S01 | 0.3.3.2《AUDIT-2026-10-21.md》、0.3.4《RELEASE-0.3.4.md》 | 实际旧闭包的null工厂丢单、失败价格缓存复现；逐件交付和返点处理。早期名片补发有用户日志结论，但完整初始根因材料不足。材料仅本地保留。 |
| S02 | 0.4.2《说明.md》、0.4.3与0.4.4《修复说明.md》 | 滚动限额、分类列表与状态标签同步、人物独立剧情/日常。旧测试以隔离原生接口为主，不是对应游戏端到端实测。材料仅本地保留。 |
| S03 | 0.8.4《检查报告.md》《卖水与更新兼容检查.md》 | 正式Mono环境、保存问题尚未定位、原版水壶滤芯/分类拒收、第三方提前访问异常。明确不能确认所有卡死的唯一根因。材料仅本地保留。 |
| S04 | 0.9.0《启动分阶段与回滚修复说明.md》 | 静态水名/液体过早初始化、资源就绪、失败回滚、用户冷启动矩阵和补丁顺序边界。合启记录不能代表全部组合玩法。材料仅本地保留。 |
| S05 | 0.9.1《修复与改动说明》、第三期《交易摆货》《联动收尾》 | 整合后生命周期、赠品保留、日常报价与收购资格一致、排程/对话隔离。保留实际Unity验收限制。材料仅本地保留。 |
| S06 | 《存档安全检查与优化》《第四期-A-存档换档》《第四期-B-资源拥有权》 | 惰性单例、同run会话替换、显式null、外来回调误删和多库存清理，含旧/新代码历史复现。材料仅本地保留。 |
| S07 | 第二期《付款与预约》《订单记账与分配优化》《收货》《配送回调安全》《换货返厂》《夜间结算与跨天保护》 | 付款前准备、结果不确定不重扣、已成功副作用与重试、盲盒固定、签收归属和夜间隔离。没有通用事务回滚。材料仅本地保留。 |
| S08 | 《银血原版成长修正说明》《三叶蜥发放验证补丁说明》及0.9.1整合说明 | 原生成长所有权、Fits越界和发放确认；不能证明玩家全部旧档卡死由该入口导致。材料仅本地保留。 |
| S09 | 《生产中断保护与订货准备说明》《原版流程优先说明》 | 前一份提出持久暂停候选，后一份明确撤回。必须按后来的决定记录，不能把两个方案同时推荐。材料仅本地保留；当前[兼容代码](../../../src/CorpPresence/Production/ProductionCompatibility.cs)可查。 |
| S10 | 《发电笼下部穿透与旧占格修复》、素材AGENTS/SCALING分析 | 现场UI穿透、保存占格与新图不符、缩图采样和防杂色规范。个人存档不公开；美术参考图、元数据和工具已另行[整理归档](../../probably-stolen-art/README.md)，游戏IL完整转储仍不公开。 |
| S11 | [版本归档](../../releases/archive)、[0.9.7兼容记录](../../compatibility-0.9.7.md)、本地0.9.8fix3说明 | 博士箱体漏注册、堆叠交付、订阅费、价格标签、许可、采购清单、批量空间、Rev12入口和正式版状态图。更新说明记录处理范围，不自动提供全链路实测证据。 |
| S12 | 本地《香水线代码追踪与买水兼容检查》、[当前香水规则](../../features/perfume.md)与Perfume/Items源码 | ammo_box缺失现场警告及具体独立Shell路径；油材料身份分离、扩容、采血、基因迁移。警告没有逐物品ID，不能全部归因香水；静态审计不证明普通水完整回归。 |
| S13 | 本地《索拉纳会员香水目录解锁修复》、[0.9.9发布记录](../../releases/0.9.9.md)、Malice/CatalogTier源码 | 黑金反馈、无申请入口的目录门槛冲突、源码修复和编译/上传回执。未完成该次回下载和真实游戏验收。 |
| S14 | [版本目录](../../releases/README.md)、[发布源码快照](../../releases/0.9.9-source-manifest.json)、提交f990dac/f6ffc41 | GitHub0.9.8与工坊0.9.9曾分叉；整理后按发布基线同步。快照文本有LF规范化，二进制按原字节，不应混同为上传DLL逐字节复现。 |
| S15 | 2026-11-07至11-08本次开发会话中的截图、操作结果与明确反馈 | 框选重启恢复但未定位、利润口径返工、图片占位符导致破图、构建缺SDK/路径错误。属于会话证据，未附个人截图或完整聊天；网页配图最终实测未完成。 |

## 当前代码的检查入口

- 启动/资源：[Main](../../../src/CorpPresence/Main.cs)、[GameResources](../../../src/CorpPresence/Runtime/GameResources.cs)、[RuntimeLifetime](../../../src/CorpPresence/Runtime/RuntimeLifetime.cs)、[OwnedCallbacks](../../../src/CorpPresence/Runtime/OwnedCallbacks.cs)。
- 存档：[SaveBinding](../../../src/CorpPresence/Persistence/SaveBinding.cs)、[SaveStructure](../../../src/CorpPresence/Persistence/SaveStructure.cs)、[Cleanup](../../../src/CorpPresence/Persistence/Cleanup.cs)。
- 订单：[Services.Orders](../../../src/CorpPresence/Companies/Services.Orders.cs)、[CompanyVisitors.Deliveries](../../../src/CorpPresence/Companies/CompanyVisitors.Deliveries.cs)、[CatalogEntry](../../../src/CorpPresence/Companies/CatalogEntry.cs)、[OrderContents](../../../src/CorpPresence/Companies/OrderContents.cs)。
- 物品：[ItemTags](../../../src/CorpPresence/Items/ItemTags.cs)、[ItemIdentity](../../../src/CorpPresence/Items/ItemIdentity.cs)、[Goods](../../../src/CorpPresence/Items/Goods.cs)。
- 香水/模式：[Perfume](../../../src/CorpPresence/Production/Perfume.cs)、[Malice](../../../src/CorpPresence/Malice/Malice.cs)、[CompanyWindow.Solana](../../../src/CorpPresence/UI/CompanyWindow.Solana.cs)。

## 如何追加证据

保留问题版本与复现条件；写清是观察、日志、代码、替身复现还是游戏实测。后续修复、撤回或反证应追加在原案例下，不删除早期结论后假装从未走过弯路。没有证据的推断明确标为待确认，不为凑案例补写根因。
