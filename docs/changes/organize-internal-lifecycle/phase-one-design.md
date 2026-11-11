# F1 · 第一期详细设计与换机交接

2026-11-11 · 一期实施中 · 关联 GitHub Issue #2。设计基于 8b885d7，本期从 9b13e40 开始实施；自动结果见[F1 · 实施证据](phase-one-evidence.md)。游戏验收仍待完成。

## 本期交付

让维护者知道 Malice 是否完成安装，并从安装失败或释放失败日志定位模块和阶段。复用现有 RuntimeLifetime、OwnedCallbacks 和 ExtensionEvent；两个实际模块证明共性后再考虑抽取。

本轮使用 brainstorming、codebase-design 和 writing-plans 的原则收敛设计。实现前核对[总设计](design.md)、[分期任务](tasks.md)和[验收要求](verify.md)。

## 状态与数据设计

| 候选内部成员 | 语义与更新位置 |
| --- | --- |
| Malice.IsInstalled，bool 只读属性 | 直接返回现有 _installed。只有安装全部成功后为 true；卸载开始时为 false。只读取字段。 |
| Malice.LastReleaseFailed，bool 只读属性 | 表示最近一次实际执行的回调释放是否有异常。开始安装尝试或一次实际释放前清 false，OwnedCallbacks 的报告回调收到释放异常时置 true。重复空卸载保留上一结果。 |
| MaintenanceModuleState，内部只读值对象 | Owner、Installed、ReleaseFailed，使用字符串与布尔值，禁止保留委托、异常和 Unity 对象。 |
| MaintenanceSnapshot.Modules，只读列表 | 捕获时复制值对象，Malice 固定 owner 为 malice。旧快照不受以后启停影响。 |

Installed=false 与 ReleaseFailed=true 可以同时出现。此时报告只能说明安装标志已撤销且清理遇错。重新启动尝试会清除上一尝试的释放标志，故障追溯依靠已有日志。首期不承诺异常清理后的自动恢复。

读取状态不调用 MaliceState.Get、加载方法或惰性玩家入口。Malice 的字段初始化也须纳入首次读取审查；若会触发业务初始化，应将只读状态放到无副作用的独立内部类型中，并在实现评审中说明调整。

MaintenanceDiagnostics.Capture 在原有会话一致性复核内采集模块值。模块属于 Mod 运行期，切换存档不撤销安装状态；业务故障仍按原逻辑隔离。Unstable 快照使用空模块列表，格式化输出“模块状态未知”。空列表不能显示成“模块未安装”。

## 失败与所有权

Malice.Install 捕获安装异常时，先尽力记录固定 owner=malice、stage=install，再执行现有局部 Release，并以 throw 保留原异常。记录函数自身异常全部隔离。局部清理报告的固定阶段为 rollback；正常 Uninstall 释放报告的阶段为 uninstall。阶段只需作用于这次释放调用，避免建立通用状态机。

记录沿用现有 ModLog 警告通路，至少包含模块名、阶段和异常类型。运行期回滚会 Reset 诊断并销毁输入宿主，因此日志承担失败后定位。需要故障聚合时复用 MaintenanceDiagnostics.Record，日志成功与聚合成功互不依赖。

OwnedCallbacks 当前会逐项吞掉释放异常并继续，最后清空释放列表。首期保留该契约，明确记录已丢失的失败释放动作无法自动重试；不把清理遇错写成成功，也不因此阻断其他资源释放。安装重试仅在清理正常的受控场景中证明幂等。

## 文件与调用点

| 文件 | 计划改动 |
| --- | --- |
| src/CorpPresence/Malice/Malice.Install.cs | 提供两个内部只读属性；安装异常阶段日志；释放结果标志和阶段记录 |
| src/CorpPresence/Maintenance/MaintenanceSnapshot.cs | 增加模块值对象和快照字段 |
| src/CorpPresence/Maintenance/MaintenanceDiagnostics.cs | 按需采集模块值；Unknown 路径返回空列表 |
| src/CorpPresence/Maintenance/MaintenanceFormatter.cs | 显示安装提交状态、清理遇错和未知状态 |
| tools/ExtensionChecks/Program.cs、Stubs.cs | 链接真实安装器，断言安装、失败和正常释放状态 |
| tools/MaintenanceChecks/Program.cs、Stubs.cs | 快照只读性、复制与格式化验证 |
| tools/RuntimeLifetimeChecks/Program.cs | 核对真实所有权类与安装器组合测试的依赖可行性，避免复制安装逻辑 |
| docs/maintenance/internal-maintenance.md | 记录字段含义及失败后的可观察证据 |

Main 的既有 Own 调用保持为装配入口。先核对现有测试项目的链接文件和替身冲突，必要时在 tools 下新增仅用于组合验证的项目，并纳入统一检查入口。该选择只在现有项目确实无法容纳真实组合时启用。

## 实现顺序与完成条件

- [ ] T1，基线。核对本机游戏版本及 M1–M6 结果，复核本方案；记录本期可用原生 API。首期只读状态和日志使用现有能力，F9 面板为后续候选。
- [x] T2，状态。增加真实安装器的状态断言，再实现两个只读属性。未安装、成功、正常卸载、重复卸载均满足上表。
- [x] T3，错误路径。复用第二个订阅冲突，证明首个租约撤回、外部租约保留、原异常传播。对真实 OwnedCallbacks 注入释放抛错，证明报告回调置位且后续释放继续；该层替身不能声称已证明真实 Unity 资源清理成功。
- [x] T4，观察。接入值对象和快照，验证旧快照独立、读取不创建玩家或载入状态、Unstable 输出未知。模式开关与换档不能误报为模块卸载。
- [x] T5，组合。使用真实 RuntimeLifetime 和 Malice.Install/Uninstall，覆盖正常两阶段启动、资源阶段失败、重复 Stop、再次成功启用。记录替身边界。
- [x] T6 自动验收。运行统一 24 步入口及新增用例，正式和维护版隔离，P1/P2 另列下项，未执行。编译后直接执行新 DLL；各层证据分别记录。

- [ ] T6 游戏验收：按分期 verify.md 完成 P1/P2，零期 M1–M6 仍待用户回填。

## 风险与停止条件

首期不增加存档字段、游戏规则、周期轮询或运行时依赖。清理异常只提供证据，不新增自动重试和永久锁。若为了状态读取引入业务初始化，或者测试必须复制生产逻辑才能通过，先调整设计并回填理由。按独立提交回退首期即可恢复当前生产基线。

## 换设备后的下一步

先查看 Issue #2 最新进展与 main，执行同步检查，阅读 AGENTS.md、总方案相关章节及本文件。T2–T5 已实施，先阅读实施证据和验证清单，核对本机环境与当前提交。M1–M6 及 P1/P2 游戏实测仍缺少记录，不将一期整体标为完成。

本机 SDK、游戏目录、构建产物、上传缓存、回滚副本和测试存档均不随 GitHub 同步。新设备按实际路径配置，游戏实测由用户操作。发布和跨设备交接依据提交与证据记录确认。
