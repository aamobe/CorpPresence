# 框架跨设备复现与交接

本文为 b1e1dd2 对应的换机记录。后续物品框架改动与当前检查入口见[物品框架实施](item-framework-evidence.md)；本文的 26 步及 DLL 哈希不代表后续产物。

当前换机请使用[总框架手册](../../maintenance/framework.md)与[整合证据](framework-integration-evidence.md)。下文为历史步骤：当前入口已同时扫描正式/维护 DLL；不再等待新物品需求才整合框架。

2026-11-11（Asia/Shanghai）。本轮基于 GitHub `a948375`，在另一台已有游戏和 SDK 的机器上复现；不是全新系统或空依赖缓存验证。用户选择保留旧分支，并基于远端继续。旧分支 github-main 的 `740c067` 未合并，当前工作分支 work/framework-phase-three。

第三期决定见[保留现有实现](phase-three-decision.md)。本轮范围是复现与文档交接，不新增物品、不调整运行行为。新提交不等于工坊更新，工坊身份仍查[发布记录](../../releases/1.1.0-internal-maintenance.md)。

## 下一台机器的执行顺序

1. 检查工作区状态，fetch 后比较本地与远端。干净且只落后才快进；分叉先保留旧分支、确认后从远端建立工作分支，不强推覆盖其他机器成果。
2. 确认 .NET SDK 8 或更高、PowerShell 7、实际可执行的 Python。先分别运行版本命令；WindowsApps 的 python.exe 可能只是占位程序，Get-Command 找得到不代表能执行。
3. 显式配置本机 GameManaged、Dotnet、Python，不把个人路径提交。核对 Steam buildid 与 Assembly-CSharp.dll 的 SHA256；变化时重新核对[官方接口与本机能力](phase-zero-evidence.md)，不能直接套用旧结果。
4. 在仓库内运行下列命令。必要时将 DOTNET_CLI_HOME 指向本地忽略的 artifacts/dotnet-cli。统一入口为每次运行建立独立输出，记录 results.json 与逐步日志。

```powershell
./tools/Check-Maintenance.ps1 -GameManaged '<实际 Managed>' -Dotnet '<实际 dotnet>' -Python '<实际 Python>'
```

5. 统一入口只扫描正式版。将本次维护 DLL 与仓库 manifest.xml 放到独立 artifacts 扫描目录，运行 Check-ModPermissions.ps1，传入同一 GameManaged、Dotnet、该 ModFolder 和 -RequireNone；不得把维护 DLL 放进正式发布目录。
6. 检查入口有改动或需复现其拒绝行为时，使用相同三个依赖参数运行 Check-MaintenanceContract.ps1。缺 SDK、缺 Managed、失败子命令都必须非零退出且不输出成功回执。
7. 汇总源码提交、构建类型、DLL 哈希、检查结果及未测项。原始日志含本机路径，只留 artifacts。游戏验收另记录实际操作，自动检查通过不替代手测。

## 模块接入检查清单

以已实现的 Malice 为例，接入任何后续模块前逐项确认，不要求新增通用模块基类。

- 依赖与时机：Main 的核心阶段或资源阶段；依赖资源必须先就绪，读取报告不得触发初始化。
- 所有权：RuntimeLifetime 管运行期释放，模块持有自身租约；固定 owner，不解除其他监听者。
- 安装与释放：安装全部成功才提交标志；部分失败回滚本轮资源，释放单项失败仍继续后续释放；模式开关与卸载分开。
- 诊断：固定模块和阶段，只读快照；安装状态与清理失败分开，不从计数为零推断所有资源都已释放。
- 测试：真实安装入口、重复安装/停用、局部失败、释放异常、其他监听者保留、重启数量稳定。替身与真实引擎的差异必须记录。
- 游戏：按 P1/P2 验证真实开店、启停、付款与换档，报告记录构建身份；不能复用另一个版本的“通过”。

## 改动到检查的映射

| 改动 | 自动检查入口 | 仍需游戏验证 |
| --- | --- | --- |
| 文档与导航 | Check-Repository.ps1、git diff --check | 无新增游戏要求 |
| 生命周期、事件、诊断 | Check-Maintenance.ps1 中 ExtensionChecks、RuntimeLifetimeChecks、MaintenanceChecks 及构建隔离 | M1–M4、M6、P1/P2 |
| 付款、订单、恢复 | 统一入口中的 PaymentChecks、OrderChecks、SaveStateChecks、RestorationChecks | M3/M5、P2，实际钱包和存档 IO |
| 身份、注册 | 统一入口中的 ItemChecks；基线不能自动覆盖 | I-G1/I-G2，真实工厂及旧库存 |
| 文字与资源 | Check-Localization.ps1；按模块选资源专项工具 | 字体、图像、占格与语言界面 |
| 加工、种植、水产、封装 | 按所属模块选 GameResourcesChecks、CropHealthChecks、AquacultureFlowChecks、SealerChecks 等专项工具，先查看其输入要求 | 加工投入产出、空间不足和保存恢复 |
| 检查脚本 | 对应正常路径及 Check-MaintenanceContract.ps1 的失败契约 | 不因此新增游戏通过结论 |

统一入口的 26 步不是全部专项测试，也不是代码覆盖率。身份基线 90 条不代表 90 种物品已在游戏中生成。

## 本机复现记录

源码 `a948375`；SDK 8.0.426、Python 3.12.14；Steam buildid 26525552；Assembly-CSharp SHA256 `725CB999D019DFF1FF31BED353C7F9BB8D81A7605A15DE1F80F5A3A6CF7B1D05`，与第二期记录一致。

本次统一入口退出 0，26/26 步达到预期结果；其中两个冲突构建预期退出 1，其余步骤退出 0。ItemChecks 的 41 条断言通过，实际身份表 90 条保留基线。缺 SDK、缺 Managed、失败子命令的三项入口拒绝检查通过。

正式和维护构建均 0 警告、0 错误。测试替身工程的警告单独记录：MaintenanceChecks 2、PaymentChecks 2、OrderChecks 4、MangoGiftChecks 2；这些项目成功编译，不能据正式构建结果宣称所有测试工程无警告。本轮无源码修改，未借文档收尾消除替身字段警告。

| 本次产物 | SHA256 | 官方扫描 |
| --- | --- | --- |
| 正式 DLL | `2EAF69D6E9DEAA049C6C0CBBC85D14D4F8A1A3A96BD724DCA7E99E442EF0A11D` | 2898 方法，零权限、无待生效/未知/未声明能力，未拒绝 |
| 维护 DLL | `A7D35A637935CF2F96984DDF95FCCDB3D21C00FF3BCBF4DB5DD59892E159AB7E` | 2900 方法，零权限、无待生效/未知/未声明能力，未拒绝 |

DLL 哈希与另一台机器的第二期记录不同。本轮证明的是同源码、同游戏依赖下检查可复现，不是二进制逐字节复现；未分析哈希差异原因，也未将这些产物替换工坊基线。

原始证据在本地 `artifacts/maintenance-checks/20261111-135920-16ae0efe`，包括 results.json、各步骤日志和 maintenance-scan.log。检查器拒绝证据在本地 artifacts/maintenance-contract-*。新增文档后的仓库检查为 98 份 Markdown、552 个本地链接通过，git diff --check 通过。独立只读审查未发现重要问题；游戏工厂和引擎行为未由此证明。

## 未完成与下一步

- M1/M2：主菜单及存档内请求维护报告，状态合理，连续请求不改变存档与业务。
- M3/M4：保存、切换测试档、切换模式，检查跨档隔离与 owner 数量稳定；模式关闭不等同卸载。
- M5/M6、P1/P2：现金/代金券成功只扣一次，拒绝不扣；真实停启后无重复处理；正式版没有维护热键。
- I-G1/I-G2：普通物品、设备、容器保存重载后身份、数量、库存、改造和槽位保留；普通水及原版物品不被迁移。

完整操作与预期以[验证清单](verify.md)及其 M1–M6 链接为准，全部仍待用户手测。需要测试档副本、确定的正式/维护 DLL 和单独部署授权，本轮未启动游戏或部署。

已有跟踪入口 [Issue #2](https://github.com/aamobe/CorpPresence/issues/2)。本轮未修改 Issue；下次在同一入口补游戏结果，失败记录具体版本、步骤和日志，不通过重置进度掩盖问题。有真实新物品需求后再重开第三期规则评估。
