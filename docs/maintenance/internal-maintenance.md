# 内部维护接口

仅供本项目开发维护；不提供第三方兼容承诺。完整范围见[总方案](../changes/archive/add-maintenance-interfaces/overview.md)。

当前全系统接入、所有权与回归选择见[总框架手册](framework.md)；历史维护接口方案保留为设计来源。

## 构建和请求报告

```powershell
dotnet build src/CorpPresence.csproj -c Release --artifacts-path artifacts/regular -p:GameManaged='<游戏 Managed>'
dotnet build src/CorpPresence.csproj -c Release --artifacts-path artifacts/maintenance -p:GameManaged='<游戏 Managed>' -p:Maintenance=true
```

第二份版本带 `-maintenance`。仅维护版中，按 Ctrl+Shift+F10 输出一次 `[维护]` 报告到已有日志；按住不重复，最短间隔 1 秒。主菜单等待资源时由 ResourceBootstrap 接收，资源就绪后由 Host 接收，存档内也可请求；不创建 UI，不写诊断文件或存档。部署和启动游戏需另行授权。

维护模式不得与 LocalTestFunds 或 WaterTrace 同时构建；正式 DLL 不含 MaintenanceConsole。两份产物必须分目录，不能用维护 DLL 发布工坊。

## 解读报告

- NoStore：尚无玩家实例；NotLoaded：存在玩家，但公司数据尚未载入。
- `Late` 表示原版读档后期事件。`库存恢复` 区分完整解码、恢复不完整、解码失败和未验证。报告记录失败库存、物品 ID、原因及缺失的容器槽位；未分类异常的原始详情见原版日志。
- 已识别的公司物品在原版解码时创建失败，可走原版同 ID 占位路径。占位物品保留普通存档字段和所有权，运行状态使用原版空物品状态。库存拒收和子槽无法恢复都会触发同一次读档提醒。排除故障后以原档重读恢复。
- `公司送货和来访暂停=True` 表示当前会话发现了解码失败。原版仍可读写。成功重读并确认库存解码完整后恢复调度。
- Bound：当前已有绑定匹配；不等同全面验证了存档。写保护字段独立显示。
- Stale：公司对象与当前字典/原始记录不匹配；Unstable：采集中对象或资源变化，或采集失败。
- 未加载、失配、不稳定和保护中的订单计数显示“未知”，不可理解为零订单。
- 订单数是订单张数；剩余物品条目数是各订单 Items.Count 的和，不推测堆叠数量或现金奖励。
- 缓存和物品注册计数只读现有集合，不代表场景实际物品扫描。
- 运行期和存档会话各最多 64 个聚合键，每层保留最早 16 个及最近 48 个，重复计数饱和；快照合并两层，最多 128 条。不保存异常原文、个人路径或存档 JSON。无玩家实例时的故障及明确调用 RecordRuntime 的补丁安装故障属于运行期；进档、换档和资源更新不清除它们。会话层仍在换档、字典或资源代次变化时清除；Reset 清除两层。

## 当前阶段

只读诊断、维护输入和三个生命周期事件已实现；真实付款检查已接入；统一检查入口已提供，最终验收结果见实施证据。报告仅列出 BeforeSave、DayStart、StateReset 的登记者，其他委托尚未迁移。

登记使用固定 owner 和 Subscribe，返回租约由 OwnedCallbacks.Own 持有。重复 owner 拒绝；分发按登记顺序，新增者下次生效，已撤销者跳过；异常分别记录，重入不递归。安装全部成功才提交标志，失败释放本次租约；模式开关不是卸载，不能用关闭恶意模式推断登记必须消失。

本地检查 `dotnet run --project tools/checks/MaintenanceChecks -p:GameManaged='<游戏 Managed>'` 使用真实存档类型和诊断实现、替身游戏边界。DLL 类型隔离检查使用 `tools/checks/MaintenanceBuildChecks`，参数为正式 DLL、维护 DLL。不能将这些检查当作游戏内验收。

游戏场景及结果记录见[验证清单](../changes/archive/add-maintenance-interfaces/verify.md)。

## 付款测试边界

PaymentChecks 链接正式 Services.Payment、CompanyMoney、SaveSlot、CompanyState、Order 与 Visitor；钱包、音效和公司/恶意模式的加载入口是游戏边界替身。OrderChecks 也链接相同正式付款实现，不再复制 Pay。

扩展返回 null 才走现金，空串代表扩展已成功，非空表示拒绝；抛异常返回状态不确定，禁止补扣。支付后异常没有被证明已回滚。现金原生扣款抛错仍按原契约向上抛出；音效失败不影响成功结算。诊断失败不能影响这些结果。

`dotnet run --project tools/checks/PaymentChecks -p:GameManaged='<游戏 Managed>'` 检查付款契约；`dotnet run --project tools/checks/OrderChecks -p:GameManaged='<游戏 Managed>'` 检查真实付款与订单提交的连接。它们不证明跨档付款持久化、原版存档异常恢复或真实代金券/期限规则，仍需 M5 验收。

## 一次运行完整检查

```powershell
./tools/Check-Maintenance.ps1 -GameManaged '<游戏 Managed>' -Dotnet '<SDK dotnet>' -Python '<Python 解释器>'
```

默认输出在 artifacts/maintenance-checks 的独立运行子目录，可用 OutputRoot 指定本地证据目录。results.json 记录每项命令、退出码和日志；失败立即抛错，不输出 ALL CHECKS PASSED。输出有本机路径，仅留本地忽略目录，不提交。

固定检查包含仓库静态检查、正式/维护构建、诊断/事件/付款/订单/生命周期/存档/芒果回归、DLL 元数据边界、冲突构建、配送恢复、本地化与官方权限扫描。指定 SDK 显式传给所有 dotnet 子检查，包括 Python 恢复检查与权限扫描。正式构建将警告视为错误。

物品定义/身份/基底检查与注册器/RuntimeLifetime 组合测试也在统一入口内；正式和维护 DLL 分别扫描，实际步数以该次入口输出为准，不再需要补跑第二份扫描。

`Check-MaintenanceContract.ps1` 使用相同三个依赖参数，独立检查错误 SDK、缺游戏引用和失败子命令的退出契约。测试生成的临时脚本仅放 artifacts，不进入正式 DLL。

停用时 Malice 释放其事件租约，运行期诊断保留至下一次启用；切换存档会清除旧会话故障。不要为排错重置业务进度或重发整单。

统一入口对每个检查先 build，再使用同一 SDK 直接运行新产物的绝对路径；不使用带 --artifacts-path 的 dotnet run，以免运行旧默认输出。当前原版付款、存读档、开店和电话回调契约见[契约核对记录](../changes/archive/add-maintenance-interfaces/native-contracts.md)。

正式版保留三个事件每事件一次的安全告警；故障聚合仍逐次记录。日志和诊断器失败均不阻断后续监听者。
# 第一期模块状态

维护报告的 `malice 安装已提交` 只表示 Install 已完成；关闭恶意模式或切换存档不会把它变成未安装。`清理遇错` 表示释放中出现异常，失败后保留此结果，并拒绝再次安装；重复卸载不重放可能部分完成的释放动作。未安装与清理遇错可以同时为真；不能据未安装断言所有资源已回收。

模块值在捕获时复制，只读列表不会随之后启停变化；采集不稳定时显示“模块状态未知”。故障警告以 `malice/install`、`malice/rollback`、`malice/uninstall` 标记阶段并包含异常类型，不写原始异常消息。日志和聚合故障分别隔离。

OwnedCallbacks 释放遇错仍继续其他动作，完成后关闭集合并保留 ReleaseFailed，禁止再次向该集合获取资源。RuntimeLifetime 的通用释放失败进入 Faulted；同一运行期拒绝重启，避免重复获取残留资源。清理动作可能已部分成功，因此不盲目重试；应停止启用并重启游戏，保留日志定位根因。Malice 将自己的失败向运行期传播。当前修复见[异常闭环](../changes/archive/organize-internal-lifecycle/closure-fixes.md)，[F1 · 第一期证据](../changes/archive/organize-internal-lifecycle/phase-one-evidence.md)保留为历史记录。

物品扩展的接入与身份基线检查见[新增物品接入清单](item-extension.md)；第二期优先此方向，原版事件模块试点暂缓。

## 物品模块状态

报告的 `items 安装已提交` 表示目录事件订阅已安装，等待资源期间也可为真。`items.factories` 是缓存工厂数，`items.registered` 是当前跟踪的所有权条目数，`items.resolvable` 是原版全局目录仍能解析到同一个自有委托的数量。Unity 场景对象销毁后，原版仍可能持有该目录的托管字典。报告通过只读查询计数，不调用工厂、不初始化物品或加载纹理。

`save.session_revision` 在返回主菜单、开始新游戏或读档切换时推进，用来区分旧回调与当前存档会话。资源代次仍表示模组资源的启停。会话切换清除旧档故障，保留模组运行期的工厂注册和清理故障。

物品相关故障按阶段、原因和物品 ID 聚合。优先看 `items/register` 的冲突和注册拒绝、`items/resolve` 的本地或全局条目缺失及工厂被替换、`items/create` 的空返回、异常和身份不匹配。配送与发卡的 `placement` 阶段表示柜台放置未成功，绞肉机还区分成品槽拒绝和扣电失败。低热量入储是正常加工结果。

`items 清理遇错` 表示仍有本次未成功释放的所有权；实际失败条目保留，重复卸载或下一次安装前会重试。若他人已替换该工厂，只清本模块记录，不移除他人工厂。安装前清理仍失败会明确拒绝安装。物品的此语义与 Malice 清空释放表后的失败记录不同。

故障聚合使用固定模块 `items`，阶段 `register/create/release`；异常消息不进入结构化快照。记录失败不阻断业务或后续清理。

## 宿主运行任务

可选模块的逐帧任务由 RuntimeUpdates 返回租约，Malice 安装时登记 `malice.envoy`，卸载时撤回；Host 不直接引用 Envoy。任务异常仍走 RuntimeTask，遍历中撤回的任务立即跳过，新登记任务下次更新生效。换档、卸载及失败回滚清理 Envoy 的延时/待发送通知；模式开关不等同模块卸载。

Host 的 RuntimeTask 每次执行失败都记录模块 `runtime`、阶段为源码中的固定任务名、代码 `task_failed`。一分钟日志限流只影响文字输出，不减少报告中的异常计数；成功执行不产生故障记录，原执行顺序和重试频率不变。

换档清除旧会话故障后，即使文字日志仍在限流，新异常也会进入新会话报告。诊断保留异常类型与受限方法链，不保存异常消息、路径或异常对象；任务重试不是业务事务回滚，查看报告不会自动修复失败任务。实施证据见[运行任务诊断](../changes/archive/organize-internal-lifecycle/runtime-task-diagnostics.md)。

Hooks 内层 Guard 已接入 HookGuard：已捕获的异常记录为 `hooks/<固定阶段>/stage_failed`，后续阶段仍可运行，外层 RuntimeTask 不会重复记录这次已处理异常。故障报告没有自动修复语义；读档修复仍为单次尝试，不重放可能部分完成的修复操作。其他业务内自行捕获的异常仍需按模块审查，不能据此宣称所有 catch 都已进入报告。

## 扩展点与补丁安装故障

Main 通过 RegisterLifetime 在下一次启用开始时清空诊断；停用及失败回滚不再清空刚产生的故障。资源代次改变仍会清除会话层，运行期安装失败保留。完全回滚后维护热键宿主可能已销毁，不能保证仍可用热键查询；内存中的快照可供调试，原始启动错误仍由现有启动日志输出。

Ask/Fire 每次失败记录 `ext/<扩展点>/callback_failed`；文字日志每个扩展点在一个 Mod 启用周期内只警告一次，且只写异常类型。日志或诊断失败不改变回退值；付款 Charge 的独立结果语义不变。Main 的正常启停重置节流及三个 ExtensionEvent 的告警标志，不清理其他模块订阅。单独切换恶意模式不代表新的 Mod 启用周期。

Hooks 的统一 Patch 入口和养殖占位补丁经 PatchInstallation 记录：目标缺失为 `target_missing`，安装异常为 `patch_failed`，异常仍抛给原有回滚。`hooks.installed` 统计这些入口成功完成的安装调用数（同一目标的前后缀算一次），不是全局 Harmony 补丁总量，不包括其他模块自有安装器或他人补丁；只有 UnpatchAll 成功后归零。可选兼容探测未选择安装不算失败。计数不能证明补丁已在游戏里执行。

本轮裁决、证据和未完成项见[框架评审修复](../bugs/framework-review/assessment.md)，合并后的[游戏验收表](../bugs/framework-review/manual-verification.md)可用于一次测试交接。
## 正式版主动报告入口（开发中）

在模组管理中打开公司增强的设置，点击底部“生成诊断报告”。无需点击“应用”，也不改变存档或配置；成功后界面显示“报告已写入 Player.log”。失败时显示失败反馈，不伪装成成功。报告使用现有 Unity 日志，不新增文件或网络权限。

库存恢复失败并暂停公司送货与来访时，游戏会自动把这次读档的失败摘要写入 `Player.log`。搜索 `[读档恢复失败]` 可找到库存、可识别的物品 ID 和失败原因；退出到主菜单后，仍可在本次运行的日志里找到该条记录。

该入口正式和维护构建均提供，不依赖逐帧 Host 或组合键；维护版仍保留 Ctrl+Shift+F10。报告区分“运行期”和“当前存档”故障；存档采集不稳定时仍保留运行期故障，会话字段保持未知。

目前原版 API 已核对、生产编译通过；窗口实际排版、点击、语言切换及权限扫描留待最终统一验收。此前维护热键间歇无报告的根因仍待定位，不以新增入口替代根因结论。
