# F1 · 第一期实施记录

基线 9b13e40；2026-11-11。用户在零期自动核对后要求开始搭建框架。本轮在现有工作分支实施 T2–T6 的自动部分。

## 执行决策与进度

- Ruling：按用户最新指令进入代码试点，M1–M6 仍待用户手测，不据此宣称零期或一期游戏验收通过。代价：引擎启停和玩法兼容仍需实际确认。
- 使用仓库既有轻量 T1–T6 清单，本文作为持久实施记录；沿用干净的工作分支。
- 预检：T2/T3 的两个布尔只读属性由 T4 捕获；T5 直接使用真实安装器和 RuntimeLifetime，无接口冲突。
- T1 自动基线：远端同步且工作区干净；正式构建 0 警告、0 错误。接口依据零期证据，M1–M6 待测。
- 首次读取审查：Malice partial 类型只有常量数组与安装器字段初始化，不读取玩家、设置或存档；On 属性未被捕获调用。
- T2：ExtensionChecks 先在“module exposes readonly IsInstalled”断言失败，添加只读状态后通过。日志位于本机 `artifacts/phase-one/t2-*`。
- T3：先在“installation warning identifies owner stage and exception type”失败，添加独立异常隔离日志及释放标记后通过；实际 OwnedCallbacks 中注入抛错释放，逆序 3→2→1 继续，三个实际事件租约撤销；重复空卸载保留失败标记，新安装清除旧标记。注入只在测试内反射完成。
- T4：MaintenanceChecks 先在“snapshot exposes module values”失败，接入值对象后通过；覆盖无玩家/存档读取、旧快照独立、只读集合、清理遇错与未安装并存、会话重置保留运行期状态、Unstable 输出未知。
- T5：ExtensionChecks 可直接链接真实 RuntimeLifetime，无需新增检查项目。覆盖等待资源、Continue 安装、模式切换、重复 Stop、第二租约冲突、后续资源阶段抛错、原异常实例保留、重启及外部 owner 保留。已有行为的组合回归首跑通过，未为制造失败改动生产代码。
- 技能执行脚本依赖 Git Bash；本机 Bash 启动因命名对象访问失败，改用 PowerShell 运行同等构建/新 DLL 检查，并在本文保存实际证据，不依赖 Bash 临时台账。

## 替身边界

ExtensionChecks 链接实际 Malice.Install、OwnedCallbacks、ExtensionEvent、Ext 和 RuntimeLifetime；业务回调、日志、诊断聚合为替身。MaintenanceChecks 链接实际 Capture、快照和 Formatter，模块输入为替身。静态字段初始化另经源码审查；这些检查不证明 Unity 资源已实际清理或游戏热停用可行。

首期不改变 Main 装配、支付、存档和剧情。真实清理动作抛错后 OwnedCallbacks 仍会清空动作表，不能自动重试；LastReleaseFailed 是证据，不是恢复机制。

## 独立审查

独立只读审查未发现 Critical / Important 阻塞项。

- Final: minor (deferred)：安装异常用例目前检查类型及消息中的 owner；后续可加强为检查 Subscribe 栈来源，以捕获“重新构造同类型同消息异常”的退化。当前生产代码使用裸 throw，后续资源故障组合用例已检查原异常对象身份。
- Final: Ruling：审查未判定的 Unity 真实清理、官方热停用、M1–M6/P1/P2 和发布就绪保留待用户验证；自动检查不能替代这些证据，当前仅作为开发交付。代价：引擎特有故障仍可能在手测时发现。

## 验收身份

本机运行目录 `artifacts/maintenance-checks/20261111-123024-50ef73c6`。使用零期同一 SDK、Python 和游戏程序集，未修改检查入口。

| 项目 | 结果 |
| --- | --- |
| 统一入口 | 24/24 通过；含双构建、七组替身、构建隔离、冲突负例、还原、本地化与官方扫描 |
| 双构建 | 正式与维护版均 0 警告、0 错误 |
| MaintenanceChecks | 42 条断言通过 |
| ExtensionChecks | 53 条断言通过 |
| 正式版官方扫描 | 2898 个方法；待生效、未知声明、未声明能力均为 0；未拒绝，零权限通过 |
| 维护版官方扫描 | 2900 个方法；待生效、未知声明、未声明能力均为 0；未拒绝，零权限通过 |
| 正式 DLL SHA256 | `92F95246D1156F726D57F51BB380C71E05CC15076115E1F1CEF37592E4C0E634` |
| 维护 DLL SHA256 | `15924F37DB3CFF1436446C695A0261B06F25FC4FED6D35F106EDF5E0DE17F706` |

本轮未启动游戏、部署、生成分发包或上传工坊。P1/P2 与零期 M1–M6 的操作和预期见[验证清单](verify.md)，待用户手测后才能判定一期完整验收。

T6 自动部分完成；M1–M6 与 P1/P2 未执行。最终静态检查及差异空白检查另在提交前运行。
