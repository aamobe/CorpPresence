# GitHub 参考项目

查询于 2026-11-11（北京时间）。精确星数、归档状态及最近推送时间取自 GitHub REST 仓库元数据。最近推送是仓库级信号，可能来自文档或自动化。发行页与源码用于核实维护活动和设计。三者均未归档且为 MIT。筛选依据是能否回答本项目的装配、租约和测试问题。

| 项目 | 星数 | 最近推送（UTC） | 最新发布（UTC） | 来源 |
| --- | ---: | --- | --- | --- |
| VContainer | 3,092 | 2026-10-06 | 1.19.0，2026-07-01 | [仓库](https://github.com/hadashiA/VContainer) · [元数据](https://api.github.com/repos/hadashiA/VContainer) · [发布](https://github.com/hadashiA/VContainer/releases/tag/1.19.0) |
| MessagePipe | 2,015 | 2026-10-28 | 1.8.2，2026-06-08 | [仓库](https://github.com/Cysharp/MessagePipe) · [元数据](https://api.github.com/repos/Cysharp/MessagePipe) · [发布](https://github.com/Cysharp/MessagePipe/releases/tag/1.8.2) |
| NUnit | 2,631 | 2026-11-11 | v5.0.0，2026-09-27 | [仓库](https://github.com/nunit/nunit) · [元数据](https://api.github.com/repos/nunit/nunit) · [发布](https://github.com/nunit/nunit/releases/tag/v5.0.0) |

- **VContainer · Unity 生命周期与模块组织。** `LifetimeScope` 将局部装配交给 `Configure`/安装器，显式释放与 `OnDestroy` 共用 `DisposeCore`。借鉴作用域内集中装配和释放，本项目的作用域是一次 IMod 启用；资源阶段继续由现有 Bootstrap 推进。源码中的场景扫描、层级容器及文档中的 PlayerLoop 接入会与既有宿主和生命周期重叠，本项目没有这些依赖需求。 [LifetimeScope 源码](https://raw.githubusercontent.com/hadashiA/VContainer/1.19.0/VContainer/Assets/VContainer/Runtime/Unity/LifetimeScope.cs) · [生命周期文档](https://vcontainer.hadashikick.jp/scoping/lifetime-overview) · [C# 入口](https://vcontainer.hadashikick.jp/integrations/entrypoint)
- **MessagePipe · 事件所有权。** 官方文档的 `Subscribe` 返回 `IDisposable`，`DisposableBag` 聚合订阅，诊断暴露订阅计数。公司增强已有相同的租约思路，继续用固定 owner 和只读数量查漏，保证其他订阅不受撤销影响。全局发布总线、异步和远程传输、堆栈追踪与 DI 集成都扩大运行时表面。特别是付款委托有返回值和未知结果契约，不能套用通知分发。 [管理订阅与诊断](https://github.com/Cysharp/MessagePipe#managing-subscription-and-diagnostics) · [1.8.2 DisposableBag 源码路径](https://github.com/Cysharp/MessagePipe/blob/1.8.2/src/MessagePipe/DisposableBag.cs)
- **NUnit · 可测试设计。** 参数化用例适合逐个枚举安装失败位置。清理实现先标记 setup 已进入，即使 setup 抛错也可在对应层清理，并单列清理失败。本项目用现有控制台检查链接真实安装实现，通过替身控制原版入口并恢复每个用例状态。项目清理还要求某个释放异常后继续释放其他资源，沿用项目自己的逐项隔离。测试框架的反射和运行器无需进入游戏 DLL。 [SetUpTearDownItem 源码](https://raw.githubusercontent.com/nunit/nunit/v5.0.0/src/NUnitFramework/framework/Internal/Commands/SetUpTearDownItem.cs) · [TestCase](https://docs.nunit.org/articles/nunit/writing-tests/attributes/testcase.html) · [TearDown](https://docs.nunit.org/articles/nunit/writing-tests/attributes/teardown.html)

这些项目只提供设计参照。适用于当前 Mod 的判断来自现有源码与官方 IMod 契约，兼容性最终以项目扫描和实测为准。

## 游戏 Mod 框架补充调研

2026-11-11 通过 GitHub REST 元数据、README 与所列源码核对。以下项目按相关性选取。三个仓库均未归档，最近推送时间为 UTC；推送活动包含文档与维护提交。

| 项目 | 星数 / 最近推送 | 许可核对 | 用于本项目 |
| --- | --- | --- | --- |
| [HugsLib](https://github.com/UnlimitedHugs/RimworldHugsLib) | 304 / 2025-07-13 | API 返回 NOASSERTION，本文不认定为 MIT；采用代码前须单独核对许可全文 | 一期生命周期契约与固定模块日志标识 |
| [Vanilla Expanded Framework](https://github.com/Vanilla-Expanded/VanillaExpandedFramework) | 136 / 2026-11-11 | 根 LICENSE 为 CC BY-NC-ND 4.0；本文仅研究组织原则 | 二、三期按功能组织及变更兼容评估 |
| [R2API](https://github.com/risk-of-thunder/R2API) | 167 / 2026-11-08 | MIT | 三期按需依赖与独立模块职责 |

HugsLib 的 ModBase 明确区分 EarlyInitialize、StaticInitialize、WorldLoaded、MapLoaded、MapDiscarded 等时机，并提供独立日志标识。借鉴点是为每个入口写清触发条件及次数。它还自动实例化派生类型、扫描 Harmony 补丁；这些接入方式依赖 RimWorld 环境，公司增强继续使用官方 IMod 和显式装配。README 的日志上传与设置文件机制也不能直接引入本项目零权限环境。[ModBase 源码](https://github.com/UnlimitedHugs/RimworldHugsLib/blob/master/Source/ModBase.cs) · [项目说明](https://github.com/UnlimitedHugs/RimworldHugsLib/blob/master/README.md)

VEF README 明确作为多个 Vanilla Expanded Mod 的共用库，并警告 1.6 命名空间变化会破坏依赖它的 XML 与 C# Mod。我们据此要求共用代码有真实消费者，迁移列出所有调用点，避免为了目录统一重命名存档相关类型。当前仅核对其组织说明与许可，未开展其存档实现审计。[项目说明](https://github.com/Vanilla-Expanded/VanillaExpandedFramework/blob/main/README.md) · [许可](https://github.com/Vanilla-Expanded/VanillaExpandedFramework/blob/main/LICENSE)

R2API 从 5.0.0 起建议按需引用子模块，文档展示 BepInDependency 依赖声明。可借鉴“功能只依赖自己需要的能力”；公司增强可先落实为清晰的源码职责和显式调用，独立包与 BepInEx 加载机制超出当前范围。本轮核对文档及目录，未将其运行时卸载行为视为已验证依据。[项目说明](https://github.com/risk-of-thunder/R2API/blob/master/README.md)

上述三个项目提供不同游戏生态的经验。具体设计仍须通过公司增强真实入口测试与游戏验收证明。
