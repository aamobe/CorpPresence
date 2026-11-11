# 内部维护接口

仅供本项目开发维护；不提供第三方兼容承诺。完整范围见[总方案](../changes/add-maintenance-interfaces/overview.md)。

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
- Bound：当前已有绑定匹配；不等同全面验证了存档。写保护字段独立显示。
- Stale：公司对象与当前字典/原始记录不匹配；Unstable：采集中对象或资源变化，或采集失败。
- 未加载、失配、不稳定和保护中的订单计数显示“未知”，不可理解为零订单。
- 订单数是订单张数；剩余物品条目数是各订单 Items.Count 的和，不推测堆叠数量或现金奖励。
- 缓存和物品注册计数只读现有集合，不代表场景实际物品扫描。
- 故障最多 64 个聚合键，重复计数饱和；不保存异常原文、个人路径或存档 JSON。切换会话和停用清理。

## 当前阶段

只读诊断、维护输入和三个生命周期事件已实现；真实付款检查已接入；统一检查入口已提供，最终验收结果见实施证据。报告仅列出 BeforeSave、DayStart、StateReset 的登记者，其他委托尚未迁移。

登记使用固定 owner 和 Subscribe，返回租约由 OwnedCallbacks.Own 持有。重复 owner 拒绝；分发按登记顺序，新增者下次生效，已撤销者跳过；异常分别记录，重入不递归。安装全部成功才提交标志，失败释放本次租约；模式开关不是卸载，不能用关闭恶意模式推断登记必须消失。

本地检查 `dotnet run --project tools/MaintenanceChecks -p:GameManaged='<游戏 Managed>'` 使用真实存档类型和诊断实现、替身游戏边界。DLL 类型隔离检查使用 `tools/MaintenanceBuildChecks`，参数为正式 DLL、维护 DLL。不能将这些检查当作游戏内验收。

游戏场景及结果记录见[验证清单](../changes/add-maintenance-interfaces/verify.md)。

## 付款测试边界

PaymentChecks 链接正式 Services.Payment、CompanyMoney、SaveSlot、CompanyState、Order 与 Visitor；钱包、音效和公司/恶意模式的加载入口是游戏边界替身。OrderChecks 也链接相同正式付款实现，不再复制 Pay。

扩展返回 null 才走现金，空串代表扩展已成功，非空表示拒绝；抛异常返回状态不确定，禁止补扣。支付后异常没有被证明已回滚。现金原生扣款抛错仍按原契约向上抛出；音效失败不影响成功结算。诊断失败不能影响这些结果。

`dotnet run --project tools/PaymentChecks -p:GameManaged='<游戏 Managed>'` 检查付款契约；`dotnet run --project tools/OrderChecks -p:GameManaged='<游戏 Managed>'` 检查真实付款与订单提交的连接。它们不证明跨档付款持久化、原版存档异常恢复或真实代金券/期限规则，仍需 M5 验收。

## 一次运行完整检查

```powershell
./tools/Check-Maintenance.ps1 -GameManaged '<游戏 Managed>' -Dotnet '<SDK dotnet>' -Python '<Python 解释器>'
```

默认输出在 artifacts/maintenance-checks 的独立运行子目录，可用 OutputRoot 指定本地证据目录。results.json 记录每项命令、退出码和日志；失败立即抛错，不输出 ALL CHECKS PASSED。输出有本机路径，仅留本地忽略目录，不提交。

固定检查包含仓库静态检查、正式/维护构建、诊断/事件/付款/订单/生命周期/存档/芒果回归、DLL 元数据边界、冲突构建、配送恢复、本地化与官方权限扫描。指定 SDK 显式传给所有 dotnet 子检查，包括 Python 恢复检查与权限扫描。正式构建将警告视为错误。

物品定义/身份/基底检查与注册器/RuntimeLifetime 组合测试也在统一入口内；正式和维护 DLL 分别扫描，当前合计 29 步，不再需要补跑第二份扫描。

`Check-MaintenanceContract.ps1` 使用相同三个依赖参数，独立检查错误 SDK、缺游戏引用和失败子命令的退出契约。测试生成的临时脚本仅放 artifacts，不进入正式 DLL。

停用时 Main 的所有权释放清除诊断，Malice 释放其事件租约；切换存档会清除旧会话故障。不要为排错重置业务进度或重发整单。

统一入口对每个检查先 build，再使用同一 SDK 直接运行新产物的绝对路径；不使用带 --artifacts-path 的 dotnet run，以免运行旧默认输出。当前原版付款、存读档、开店和电话回调契约见[契约核对记录](../changes/add-maintenance-interfaces/native-contracts.md)。

正式版保留三个事件每事件一次的安全告警；故障聚合仍逐次记录。日志和诊断器失败均不阻断后续监听者。
# 第一期模块状态

维护报告的 `malice 安装已提交` 只表示 Install 已完成；关闭恶意模式或切换存档不会把它变成未安装。`清理遇错` 表示最近一次实际释放中出现异常，重复空卸载保留此结果，新的安装尝试或实际释放开始时清除。未安装与清理遇错可以同时为真；不能据未安装断言所有资源已回收。

模块值在捕获时复制，只读列表不会随之后启停变化；采集不稳定时显示“模块状态未知”。故障警告以 `malice/install`、`malice/rollback`、`malice/uninstall` 标记阶段并包含异常类型，不写原始异常消息。日志和聚合故障分别隔离。

当前 OwnedCallbacks 释放遇错仍继续并清空释放表，因此失败动作不会自动重试。实现和替身边界见[第一期证据](../changes/organize-internal-lifecycle/phase-one-evidence.md)。

物品扩展的接入与身份基线检查见[新增物品接入清单](item-extension.md)；第二期优先此方向，原版事件模块试点暂缓。

## 物品模块状态

报告的 `items 安装已提交` 表示目录事件订阅已安装；等待资源期间可为真，不能推断全部身份已注册。`items.factories` 是缓存工厂数，`items.registered` 是当前跟踪的所有权条目数，不是全局有效工厂数。报告不读取 Goods.Catalog，不初始化物品或加载纹理。

`items 清理遇错` 表示仍有本次未成功释放的所有权；实际失败条目保留，重复卸载或下一次安装前会重试。若他人已替换该工厂，只清本模块记录，不移除他人工厂。安装前清理仍失败会明确拒绝安装。物品的此语义与 Malice 清空释放表后的失败记录不同。

故障聚合使用固定模块 `items`，阶段 `register/create/release`；异常消息不进入结构化快照。记录失败不阻断业务或后续清理。
