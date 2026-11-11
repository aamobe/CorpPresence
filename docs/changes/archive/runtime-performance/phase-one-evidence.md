# R1 · 一期验证进度

2026-11-11。本期仍在进行，按项目 AGENTS.md 的分层汇报要求及总方案第 7 步核查。

原版接入的首批作用域核查见[补丁作用域](hooks-scope.md)，其中明确区分按公司身份限定的入口与有意作用于原版物品的交易规则；全量安装清单尚未完成。

| 检查 | 本轮结果 | 覆盖范围 |
| --- | --- | --- |
| RuntimeLifetimeChecks | 6 项通过 | 重复启停、部分启动失败逆序释放、清理异常隔离、重启、SaveSlot 损坏记录写保护及跨档隔离 |
| GameResourcesChecks | 10 项通过 | 九种语言资源就绪、语言选择等待后恢复 |
| SaveStateChecks | 6 项通过 | 实际 JSON 解码、结构校验、损坏记录拒写、旧瑟琳章节和订单往返 |
| RestorationChecks/State | 36 项通过 | 会话与绑定、报价样品失效、配送箱回调隔离、配送失败重试及堆叠数量 |
| LocalizationChecks | 31 项通过 | 稳定键、回退、格式、菜单缓存、编译资源一致性 |
| OfficialCsvChecks | 三种语言各 1650 条通过 | 安装游戏的 CSV 解析器与转义规则 |

恢复检查直接编译当前 StoreSession、SaveBinding、CatalogEntry、CatalogPricing、Crates、CompanyVisitors.Deliveries、Services.Nightly 和 EscapeePlacement。游戏对象、库存、语言接口及固定价格依赖采用替身。固定价格替身取自当前源码，不能用于证明仓库真实定价正确。

恢复检查入口为 `python tools/checks/RestorationChecks/State/run.py`，要求 PATH 中的 dotnet 指向 .NET 8 或更高 SDK。脚本生成箱体登记数据后执行检查。

源码核对确认 Hooks.InstallEvents 对顾客生成事件使用安装标志，对夜间服务保存委托实例；UninstallEvents 使用对应实例解绑并重置状态。此项目前只有源码证据。

SaveSlot 的实际代码已通过写保护检查，损坏键拒绝覆盖、其他键允许写入、换档后新字典独立、返回受保护字典继续拒写。SaveStateChecks 使用实际 CorpState、SaveStructure、Json 和游戏附带的 Newtonsoft.Json，验证了损坏 JSON、空根对象、空公司集合、空人物记录及空订单明细的读取后拒写，也验证旧瑟琳章节和订单往返。恶意模式校验重载的依赖采用替身，该模式尚未由此验证。

本轮反编译目标构建 26525552 的 SaveManager，确认 DecodeNodesInternal 先从工厂创建物品并恢复 state/modifiedState，接着调用 StorageExpansionHelper.RestoreTabs，再于第二轮遍历按 childItemInventoryNode 挂回子物品。Hooks 的前缀接入该 RestoreTabs；专用工作仓按标签恢复尺寸，集装箱追加页签后恢复过滤规则和名称。此项为目标程序集与 Mod 源码证据。

一期代码侧检查已形成阶段记录，整体结论为部分。尚缺实际启停后的事件次数、旧档格位、订单与剧情进度，以及切换档案后延迟回调和资源引用的游戏验证。上述替身结果不构成这些场景的验收结论。后续可以推进二期代码侧排查，同时保留一期手测待办。
