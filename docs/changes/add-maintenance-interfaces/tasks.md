# 内部维护与扩展入口分期实施方案

> **执行说明：** 实施时使用 `executing-plans` 按任务推进；若用户另选代理分工，则使用 `subagent-driven-development`。勾选只记录实际完成，不能用计划替代证据。

**目标：** 让维护者能安全查询状态、定位失败、管理现有生命周期扩展，并在仓库内复跑真实付款回归。

**架构：** 复用现有会话、资源生命周期和回调释放机制；先打通只读诊断，再迁移三个事件。付款保持原调用契约，仅拆分文件便于直接测试，避免一次改写整个 Ext。

**技术栈：** 官方 IMod + Unity Mono + Harmony；生产 netstandard2.1/C# 12；本地检查 .NET SDK 8 或更高及 PowerShell 7。

**规格：** [proposal.md](proposal.md)。**接口及九门核查：** [design.md](design.md)。**游戏清单：** [verify.md](verify.md)。

状态：目标已生效，按 T1→T6 实施；进度和实际证据见 evidence.md。总体入口见 [overview.md](overview.md)。

## 全局约束

- 维护接口仅供本项目内部；不承诺第三方类型、方法和数据格式兼容。
- 保持当前物品 ID、存档字段、槽位、标签、本地化键、价格和限购；不增加诊断持久化。
- 只读采集不得调用 CorpState.Get/CompanyOf/Companies.StateOf、生成物品或报价、调用写入和修复路径。
- 不新增线程、网络、动态程序集或通用文件读写；保持零权限目标。
- 正式 DLL 不含维护按键、测试资金或测试驱动；诊断核心不依赖维护按键才能编译。
- 每期独立可编译、可验证；每期结束写六项总结，阶段间有阻塞则先处理，不用累计测试数量代替覆盖说明。
- 不自动启动游戏、部署、打包、上传或合并 main；这次计划不续用上一版的发布授权。
- 当前 1.1.0 源码快照不覆盖。生产源码开始变化后，CheckReleaseBaseline 不再作为每期必须全绿的检查，不为通过而改旧快照。

## 顺序与交付

| 期次 | 任务 | 交付及价值 | 进入下一期的条件 |
| --- | --- | --- | --- |
| 一期：基线与约定 | T1 | 回调清单、验证基线、诊断字段表；明确哪些只是查询、哪些可能扣款 | 现有检查结果已记；所有 Ext 字段有分类和失败语义 |
| 二期：只读诊断 | T2、T3 | 一次按键得到可解释的状态报告；无存档和错误状态也可查询 | 不产生存档/资源副作用；正式/维护构建分离可证 |
| 三期：事件登记 | T4 | 三个生命周期事件有归属、可撤销、可查询 | 顺序、重入、失败安装和释放检查通过，无重复触发 |
| 四期：付款回归 | T5 | 正式付款代码进入可重复测试；失败位置可诊断 | 原支付语义保持，扣款后异常不会补扣，订单回归通过 |
| 五期：整体收尾 | T6 | 单一检查命令、维护手册、完整阶段报告 | 自动检查/审查通过；游戏场景逐项交接，证据分层 |

依赖为 T1 → T2 → T3 → T4 → T5 → T6。各期可以单独交付源码和文档；不要求完成全部才允许评估收益。

## 审查重点

1. 主菜单或损坏存档下查询触发懒加载、写保护解除或状态创建：T2 检查对象/字典/原始数据不变。
2. 换档仍报告旧订单，或快照持有游戏对象：T2 检查绑定与快照独立性。
3. 回调分发中安装、撤销、异常和重入导致重复执行：T4 固定事件语义并测试。
4. 已扣款后异常被当成“未支付”再次扣款：T5 用真实 Pay 和真实支付适配层验证。
5. 诊断刷屏、无界增长或正式构建混入调试键：T2 检查容量，T3/T6 检查双构建及类型。

## T1 · 建立基线和扩展清单

文件：新增本目录 `evidence.md`、`extension-map.md`；读取 `Integration/Ext.cs`、`Runtime/OwnedCallbacks.cs`、`Malice/Malice.cs`、`Companies/Services.cs`、`Persistence/CorpState.cs`、`SaveSlot.cs` 和所有 Ext 调用点。不开新生产接口。

- [x] 开工核对远端、当前分支及工作区，使用合适的工作分支；保留已有改动。记录实际提交、游戏程序集哈希与 SDK 版本，不把本机路径写进提交。
- [x] 对 Ext 的每个字段列出：声明类型、写入者、调用者、缺省值、错误策略、释放方式、有无钱货副作用。首批迁移明确只含 BeforeSave、DayStart、StateReset。
- [x] 执行现有仓库检查、Release 构建、RuntimeLifetimeChecks、SaveStateChecks、OrderChecks、RestorationChecks/State 和本地化检查；逐项记录命令/退出码/范围，既有失败不能隐去。
- [x] 对照设计的快照字段逐项定位只读来源；支付返回值 null/空串/非空串/异常列成契约表。将发现的额外业务改动单独列出，不暗中纳入本任务。
- [x] 写一期六项总结并进行本地文档提交；进入 T2 前不存在未定义的必需接口。

## T2 · 打通只读快照和有界故障记录

新建：设计列出的 MaintenanceSnapshot、MaintenanceDiagnostics、MaintenanceFormatter 文件；`tools/MaintenanceChecks/Checks.csproj`、`Program.cs`、`Stubs.cs`。修改：`Presentation/Portraits.cs`、`Companies/CatalogPricing.cs`、`Items/ItemRegistry.cs` 的只读计数；`Main.cs` 停用清理。其他读取以现有类型为准。

产出接口：design.md 中 Capture/Record/Reset/Format 和三个 AppendDiagnostics。输入：真实 SaveSlot/SaveBinding/CorpState 的已有状态；不得用会生成记录的访问器。无需外部文件读写。

- [ ] 编写失败测试 `CaptureWithoutStoreDoesNotCreatePlayer`、`CaptureDoesNotLoadOrRepairState`、`StaleBindingHasUnknownOrderCount`：验证无单例创建、无 Get/Write 调用；坏 JSON 原文及保护集合不变，过期绑定计数为 null。
- [ ] 编写 `SnapshotDoesNotExposeLiveCollections`、`SessionChangeDiscardsCounts`：修改原状态不改变已返回快照；采集期间换 store/modData/资源代次返回 Unstable。先运行新项目，确认因缺实现失败而非环境错误。
- [ ] 实现最小快照和缓存计数；用真实状态类型连接测试，游戏对象才用替身。pending 单位数为订单明细剩余条目数之和，使用 long，不把订单张数当件数。
- [ ] 编写并运行失败测试 `FaultBufferEvictsAt64`、`DuplicateFaultSaturates`、`FaultsResetAcrossStores`、`DiagnosticFailureDoesNotEscape`；实现聚合队列、脱敏调用链、会话清理和不抛出记录边界。包含超长内容、无异常对象及诊断输出器抛异常场景。
- [ ] 运行 `dotnet run --project tools/MaintenanceChecks/Checks.csproj`、Release 构建及相关存档检查；检查无新增全场景查找、无持久化键。提交本任务，证据注明替身边界。

## T3 · 提供可使用的维护触发器

新建：`src/CorpPresence/Maintenance/MaintenanceConsole.cs`。修改：`src/CorpPresence.csproj`、`Runtime/Host.cs`。新建说明：`docs/maintenance/internal-maintenance.md`（本任务开始时创建，最终 T6 补齐）。

接口：`MaintenanceConsole.Tick(): void` 仅 `#if MAINTENANCE` 编译；调用 T2 的 Capture 和 Format。设置 MSBuild `Maintenance=true` 定义 MAINTENANCE；版本使用 `-maintenance` 后缀。与 LocalTestFunds/WaterTrace 同时启用时构建报错，避免维护报告混淆构建身份。

- [ ] 添加编译检查：默认 Release 不应出现 MaintenanceConsole；维护构建应出现；冲突开关应失败。新增实现前先确认检查可因缺少维护类型失败。
- [ ] 实现 Ctrl+Shift+F10 按下沿请求，至少间隔 1 秒；只在明确维护构建中接入 Host，不改玩家设置、不做定时采集、不创建新 UI。复用 Unity 输入接口并检查按键状态组合。
- [ ] 格式化后向现有 ModLog 输出固定前缀 `[维护]` 的报告；输出失败不影响游戏，正常启动不自动输出全量报告。未绑定数据标为未知，报告不声称完整库存扫描。
- [ ] 分开输出目录构建默认 Release 和 Maintenance=true 版本，枚举类型确认边界；用替身验证按住不重复触发、节流和输出异常。人工检查见 verify.md M1/M2。
- [ ] 写二期总结：快照覆盖字段、未知字段、两个构建身份、诊断容量和待手测项；本地提交。

## T4 · 迁移三个生命周期事件

新建：`Integration/ExtensionEvent.cs`；`tools/ExtensionChecks/Checks.csproj`、`Program.cs`。修改：`Integration/Ext.cs`、`Runtime/OwnedCallbacks.cs`、`Malice/Malice.cs`、BeforeSave/DayStart/StateReset 的实际调用点（T1 清单逐项列出后执行）；维护快照补已迁移事件拥有者计数和报告。

接口：使用 design.md 定义的 ExtensionEvent 全部方法；OwnedCallbacks.Own(IDisposable)。其余 Func 字段及调用策略不变。

- [ ] 新增并先运行失败测试：同名 owner 拒绝且旧订阅不变；登记 A/B 顺序为 A→B；A 异常仍执行 B；A 中登记 C 本轮不执行 C；A 中撤销 B 本轮跳过 B；同事件重入不递归；Dispose 两次无副作用；Clear 后旧租约不能移除新订阅。
- [ ] 实现具名事件及租约管理，复用 T2 记录来源和错误代码；只在安装/卸载时改变登记。测试中诊断器失败也不能阻止后续订阅。
- [ ] 先迁移 StateReset 并运行现有生命周期/存档检查，再迁移 BeforeSave、DayStart；移除对应旧字段赋值和 Fire 包装，确保每个原调用点只分发一次。模式切换和存档切换不应意外清空运行期订阅。
- [ ] Malice 安装用局部租约集准备，失败释放已安装项，完成后才置 _installed；测试第二项登记失败后的计数、正常卸载、再次安装及第三方无关租约保留（这里只用测试订阅，不提供第三方功能）。
- [ ] 运行 ExtensionChecks、RuntimeLifetimeChecks、SaveStateChecks 和 Release 构建；核对顺序仍为原每日顺序、诊断不触发 Publish。写三期总结并提交。

## T5 · 让付款检查执行真实入口

新建：`Companies/Services.Payment.cs`；`tools/PaymentChecks/Checks.csproj`、`Program.cs`、`Stubs.cs`。修改：`Companies/Services.cs` 仅移出 Pay；`tools/OrderChecks/Checks.csproj`、`Stubs.cs`、`Program.cs` 移除替身 Pay、改链接真实 Payment 文件；诊断只添加在已存在的失败分支。

接口：保持 `Services.Pay(Company c, int amount, string what): string`；消费 T2 的 Record。链接真实 CompanyMoney、SaveSlot 和收费返回判定；游戏余额、日期、库存及其他不相关依赖使用替身。不导出新付款公共接口。

- [ ] 将现有付款四种扩展结果写成表驱动用例：null 走现金一次；空串不补扣；非空错误不补扣；抛异常返回不确定且不补扣。再覆盖扩展先扣款再抛异常，最终扣款次数仍为一次。
- [ ] 先原样移动 Pay 并让测试链接它，检查移动前后方法正文一致；不要为了制造红测试改变现有行为。新增诊断行为另写失败测试，再接入 Record，保持返回值不变。
- [ ] 测试无存档、保护中的公司/恶意模式记录、零费用仍检查存档、余额不足、支付音效异常、诊断异常；证明未知付款结果不是“已回滚”。
- [ ] OrderChecks 移除 Pay 替身后复跑正常订单、失败订单、重入、报价换档和异常退出恢复；核对订单数、扣款次数、额度及合作度。若依赖缺口出现，只替换游戏边界，不重新复制正式方法。
- [ ] 运行 PaymentChecks、OrderChecks、SaveStateChecks、Release 构建；用 diff 检查支付顺序和 L.K 键保持。写四期总结，将跨档付款持久化等未覆盖项原样留在清单，本地提交。

## T6 · 统一运行与交付

新建：`tools/Check-Maintenance.ps1`。修改：`tools/README.md`、`docs/maintenance/internal-maintenance.md`、本目录 evidence/verify；对应主索引补链接。计划的维护说明不取代 AGENTS 和总方案。

脚本参数：`-GameManaged <目录> -Dotnet <SDK可执行文件> -Python <解释器>`；可选 `-OutputRoot <本地输出目录>`，默认 artifacts/maintenance-checks。所有子检查使用指定 SDK；脚本不把参数写回仓库。未来生成的检查与日志只保留在忽略目录。

- [ ] 先验证错误 SDK、缺游戏引用及子检查非零返回时总脚本失败，不出现“全部通过”；检查列表固定，不靠扫描并执行未知工程。
- [ ] 串联仓库检查、Release 构建、MaintenanceChecks、ExtensionChecks、PaymentChecks、OrderChecks、RuntimeLifetimeChecks、SaveStateChecks、RestorationChecks/State、本地化及最终官方权限扫描。临时扫描目录仅放当前 DLL 和 manifest，不生成分发压缩包。
- [ ] 检查脚本输出测试名称、退出码和证据层级；扫描须 Pending=0、Unknown=0、undeclared=0、Refused=False，正式构建零警告/错误。其他生产线未改则不为凑数量全量重跑。
- [ ] 最终差异审查重点为诊断副作用、回调遗漏/重复、收费默认回退、测试代码排除；回填 verify 的实际层级，不把未执行的游戏场景勾为通过。
- [ ] 更新维护文档：如何构建维护版、请求报告、解释未知状态、复跑回归、卸载清理；把实际诊断字段及事件清单写回源码地图。新存档字段应为零，如有变化需先重开设计核查。
- [ ] 写五期总结与总交付报告，逐项列出未提交/未推送、用户待决策和文档发现；本地提交。发布、版本号和主分支合并在后续明确授权范围内处理。

## 命令约定

现有命令从项目根目录执行；GameManaged 传入实际安装目录，dotnet 必须为 SDK：

```powershell
./tools/Check-Repository.ps1
./tools/Build.ps1 -GameManaged '<游戏Managed>' -Dotnet '<SDK dotnet>'
dotnet run --project tools/RuntimeLifetimeChecks/Checks.csproj
dotnet run --project tools/SaveStateChecks/Checks.csproj
dotnet run --project tools/OrderChecks/Checks.csproj
python tools/RestorationChecks/State/run.py
./tools/Check-Localization.ps1 -GameManaged '<游戏Managed>' -Dotnet '<SDK dotnet>' -CompiledAssembly './artifacts/build/bin/CorpPresence/release/CorpPresence.dll'
```

MaintenanceChecks、ExtensionChecks、PaymentChecks 和 Check-Maintenance.ps1 是上述任务将创建的入口，现在不存在，不能标记为已运行。测试框架不引入新 NuGet 包，沿用仓库控制台检查方式。

## 每期总结模板与方案自查

每期在 evidence.md 追加：①改了什么及涉及文件；②源码/静态/编译/替身/游戏/发布六层证据；③具体游戏场景和预期；④未提交及未推送；⑤需要用户决定的事项；⑥文档发现。写明本期是否可进入下一期，失败项的下一步是什么。

方案自查：R1/R2→T2，R3→T2/T3，R4/R5→T4，R6→T5，R7→T6；五项审查重点均有任务和用例。接口签名统一在 design.md 定义，任务不得另造同义接口。默认建议顺序执行，复用上下文；目前不需要为写计划或独立文件数量而拆多代理。
