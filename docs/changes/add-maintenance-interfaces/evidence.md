# 实施证据

基线 ccc80c0；SDK 8.0.426；Assembly-CSharp SHA256 725CB999D019DFF1FF31BED353C7F9BB8D81A7605A15DE1F80F5A3A6CF7B1D05。已有文档改动为本方案，原样保留。远端此前已核对并推送芒果分支，当前创建 feat/internal-maintenance。

## 决策记录

- 使用现有 checkout 新工作分支；用户授权继续现有方案，不另建 worktree。
- 现有方案为中文 T1–T6 清单，不符合技能脚本的 Task 标题解析；按同一任务顺序在本证据文件记录任务、基线、命令和结果，不改造计划只为适配脚本。
- T1→T2 只读绑定、T2→T3 报告、T2→T4 故障记录、T4→T2 登记快照、T2→T5 支付诊断、T1–T5→T6 检查命令均采用 design.md 签名；无接口冲突。

## 用户调整为先出总方案

暂停接口实施。尚未修改生产代码；创建的 MaintenanceChecks 草稿已移动到本地忽略目录，不能视为正式检查入口。已观察到的红测试仅证明类型尚不存在，不证明新接口实现。任务清单保持未勾选。后续用户设立目标时重新核对环境与源码。

## 已运行的既有基线（不计作新接口完成）

在用户调整为先写总方案之前启动的检查现已结束：

| 命令 | 结果 | 范围 |
| --- | --- | --- |
| dotnet run --project tools/RuntimeLifetimeChecks/Checks.csproj | 退出 0，13 项通过 | 现有生命周期与回调所有权替身 |
| dotnet run --project tools/SaveStateChecks/Checks.csproj -p:GameManaged=<Managed> | 退出 0，6+4+6 项通过 | 当前存档、会话、芒果领取状态 |
| dotnet run --project tools/OrderChecks/Checks.csproj | 退出 0 | 当前订单入口，仍使用旧付款替身 |
| python tools/RestorationChecks/State/run.py | 退出 0，37 项通过 | 当前配送恢复 |
| tools/Check-Localization.ps1 -GameManaged <Managed> | 成功 | 1661 键及游戏 CSV 解析 |
| tools/Build.ps1 -GameManaged <Managed> -Dotnet <SDK> | 成功，0 警告、0 错误 | 当前正式源码，不含新接口 |
| tools/Check-Repository.ps1 | 成功，77 文档、401 链接 | 总方案落地后的文档静态检查 |

本次未启动游戏、未部署、未发布。待用户游戏验证场景仅是未来接口验收规划。当前只有方案文档未提交、未推送；无需用户额外决定，下一步由用户设立目标。新增发现为只读绑定查询可能产生内部更新，以及测试必须核对原生回调顺序，均已写入总方案。

## T1 完成：基线与约定

1. 完成扩展字段清单，逐项列明调用者、默认/异常语义、释放和副作用；只读诊断来源和付款四态已明确。生产源码保持 ccc80c0。
2. 已重新核对远端：main=1f33188，芒果分支=ccc80c0，与本地相符；remote.origin.fetch 仅包含 main，因此通过 ls-remote 核对芒果分支。上述基线检查日志仍在本地，源码无变化；文档检查再次通过。没有游戏/发布验证。
3. 本期没有新增游戏功能；后续主菜单无存档报告、在档只读和跨档场景按 verify.md 验证。
4. 方案与 T1 文档作工作分支本地提交，不推送，不合并 main。
5. 无待用户决定事项；可以进入 T2。
6. 记录纯查询绑定缺口和原生回调契约风险；保持接口签名集中于 design.md。

## T2：只读快照与有界故障记录

基线 da881aa。新增 MaintenanceSnapshot/Diagnostics/Formatter，三个缓存模块提供 Count 查询；SaveBinding/CorpState 新增只读绑定观察，原业务 Matches 不变；Main 停用释放诊断记录。

- 红测试：新增检查因 MaintenanceCounter 等未实现而编译失败，随后实现。最终 MaintenanceChecks 26 项通过，链接真实 SaveSlot、SaveBinding、CorpState、状态结构与 JSON；玩家单例替身模拟懒创建，缓存边界提供计数/换档故障注入。
- 覆盖无存档/坏记录不修复、过期绑定、字典/状态/资源代次中途变化、不可变快照、64 项淘汰、重复饱和、超长键、去路径调用链、异常吞吐、进出存档清理及停用重置。
- SaveStateChecks 6+4+6 项通过；正式构建 0 警告、0 错误；官方扫描 2880 方法，Pending/Unknown/undeclared=0，Refused=False，零权限通过。
- 故障输出器异常及按键节流由 T3 的维护触发器检查覆盖；T2 当前没有输出器，Capture 的缓存采集异常已覆盖。本地异常调用链不等于全部原生故障可追踪。
- 游戏待测：M1/M2/M3，尤其主菜单报告不创建存档、在档查询后钱货不变、跨档不带旧记录。未启动游戏、未部署或发布。
- 本期本地提交，不推送；无用户待决定事项。诊断状态不等同完整存档解码成功，写保护与绑定分别显示。

## T3：维护触发器与构建隔离

基线 14c6a67。新增 MAINTENANCE 专用 Console，Host 仅条件编译时调用；维护版版本 1.1.0-maintenance，正式版 1.1.0。冲突开关在编译期拒绝。新增只读 PE 元数据检查，不执行游戏程序集。

- 首批按键检查在无实现时因 MaintenanceConsole 缺失失败；实现后 MaintenanceChecks 共 32 项通过（T2 26 + T3 6）。覆盖不自动输出、左右修饰键组合、按住不重复、1 秒节流、输出抛异常、F10 单键无效。
- 构建边界检查先对两份旧正式 DLL 运行，正确失败于缺少维护类型；分目录正式/维护构建后均 0 警告、0 错误，元数据类型和版本检查通过。
- WaterTrace/LocalTestFunds 两种冲突组合各退出 1，日志明确为 CheckMaintenanceFlags 拒绝。初次日志查找命令参数错误，不能作为通过证据；已修正为命名参数并重新执行确认。
- 未运行游戏或发布；游戏待验为 M1/M2/M6：主菜单手动请求、在档查询只读、正式版组合键无入口。
- 本地提交，不推送；无待决定事项。维护使用方式、报告未知状态及替身边界写入 internal-maintenance.md。
- T3 判定完成，可进入 T4；游戏未验不影响本地源码交付，但不能报告游戏实测通过。

## T4：三个生命周期事件迁移

基线 6db6d7a。新增 ExtensionEvent 与 OwnedCallbacks.Own；Ext 的 StateReset/BeforeSave/DayStart 为 readonly 事件。Hooks 三处各 Publish 一次，保留原顺序；其余 Func 不改。

- 新事件测试先因缺少实现失败；实现后事件顺序、重复 owner、快照新增/撤销、重入、独立释放、Clear 和诊断抛错隔离通过。
- 先迁移 StateReset：生命周期 13 项、存档 6+4+6 项和正式编译通过，再迁移其余两事件。
- 真实 Malice 安装代码移至同一 partial 类 Malice.Install.cs，以便直接链接测试，不复制安装逻辑。原先先置 _installed 的实现，在第二项注册冲突后泄漏首项订阅；测试复现失败，再改为局部 OwnedCallbacks、成功后赋值标志、失败逆序释放。全部安装测试转绿。
- ExtensionChecks 共 19 项，MaintenanceChecks 共 34 项，RuntimeLifetimeChecks 13 项、SaveStateChecks 6+4+6 项通过；正式构建 0 警告、0 错误。维护事件快照独立，Capture 不分发事件。静态检索无旧三字段赋值/Fire 调用。
- Ruling：拆出 Malice.Install.cs 是为了链接真实安装路径，范围仅搬迁安装/卸载和所有权字段；业务方法留原处。代价是新增一个 partial 文件，避免测试复制实现的更高维护风险。
- 游戏待验 M3/M4：切档和模式切换不重复每日处理，实际停用/重启后登记不累加。仅关闭恶意模式不等于卸载，验证清单已澄清。
- 本地提交，不推送、部署或发布；无用户待决定事项。可进入 T5。游戏证据仍为空，不将替身检查称为游戏通过。

## T5：正式付款入口进入回归

基线 6c64949。Services.Pay 移至 Services.Payment.cs，提取时逐字对比方法正文一致；随后仅在原拒绝/扩展异常分支添加固定诊断，不改返回文本、L.K 键、支付顺序或次数。

- PaymentChecks 直接链接 Pay、CompanyMoney、SaveSlot 和公司数据类型。先验证原行为，再增加诊断期待，失败于缺少 payment.extension:uncertain；补诊断后 21 项通过。
- 四种扩展返回、扩展先扣后抛、无存档、两类保护记录、零费用检查、不完整公司数据、余额不足、音效异常、原生扣款后异常和诊断器异常均覆盖。原生扣款异常仍向上抛且不重试；不声称已回滚。
- OrderChecks 删除替身 Pay；Payment 测试入口仅映射到 Ext.Charge，扣款计数来自钱包边界。原先模拟 Pay 抛出的异常用例改为真实 Pay 返回不确定；正常/拒绝/重入/报价异常/换档用例及先扣后抛新用例通过，不以改正式行为迎合旧替身。
- SaveStateChecks 6+4+6 项通过；Release 0 警告、0 错误。首次 PaymentChecks 编译缺少 Newtonsoft 引用，已按现有 GameManaged 引入真实依赖后运行，不把环境错误当红测试。
- 游戏待验 M5：普通现金、恶意模式代金券、拒绝时余额与额度；跨档付款持久化和真实存档异常恢复不在替身证据内。未运行游戏、未部署/发布。
- 本地提交、不推送；无待用户决定事项。测试边界与结果不确定语义已写入维护手册。可进入 T6。
