# 公司增强内部框架分期方案

2026-11-12 起的当前执行入口为[完整内部框架开发方案](../../superpowers/plans/2026-11-12-integrated-framework.md)。用户明确要求开发期间不做非必要测试，最后统一测试；必要的故障定位和高风险定向验证按新方案执行。下述分期为历史过程，尚未验收的内容不自动视为通过。

上一轮执行的是[总框架整合](framework-integration-plan.md)，将物品接入现有生命周期、诊断、统一检查和[全系统模块契约](../../maintenance/framework.md)。结果见[整合证据](framework-integration-evidence.md)。以下分期记录保留历史范围，未测游戏场景仍待验收。

最新续接：用户确认先完成物品框架，再提交物品需求，并依托原框架搭建。见[框架实施记录](item-framework-evidence.md)和[接入清单](../../maintenance/item-extension.md)。此前的[F3 · 保留现有实现决定](phase-three-decision.md)与[换机记录](cross-device-handoff.md)保留为历史证据；前期游戏验收继续待测。

2026-11-11 的[完整评估](../../assessments/internal-framework/assessment.md)核对接口与框架的实际收益、1.0.0 对照、成本及未验证项，供跨设备继续前参考。

2026-11-11 · 评审草案 · 基线 `feat/internal-maintenance` / `2500a6e`

先验收现有维护接口，再用恶意模式试点模块级诊断和完整启停验证。2026-11-11 用户明确物品扩展优先，第二期改为物品接入流程和自动检查。第三期依据真实物品接入需求决定共用规则。

| 文档 | 内容 |
| --- | --- |
| [提案](proposal.md) | 维护缺口、路线取舍与前置条件 |
| [调研](research.md) | 三个 GitHub 项目的证据、借鉴点和适用边界 |
| [设计](design.md) | 模块选择、所有权、故障语义与接口门槛 |
| [增量规格](specs/lifecycle.md) | 可验证的内部维护要求 |
| [任务](tasks.md) | 分期迁移、独立成果、停止和回退条件 |
| [验证](verify.md) | 本次证据范围、后续自动检查和手测 |

前轮交付见[总览](../add-maintenance-interfaces/overview.md)、[实施证据](../add-maintenance-interfaces/evidence.md)、[维护手册](../../maintenance/internal-maintenance.md)、[前轮任务](../add-maintenance-interfaces/tasks.md)、[前轮设计](../add-maintenance-interfaces/design.md)、[前轮提案](../add-maintenance-interfaces/proposal.md)与[原版契约](../add-maintenance-interfaces/native-contracts.md)。工程历史见[分期核查](../runtime-performance/checkpoint.md)和[复盘目录](../../retrospective/README.md)。

## 首期细化

2026-11-11 已补充[F1 · 第一期详细设计与换机交接](phase-one-design.md)。定义安装提交状态、释放失败标志、只读快照字段、错误传播及 T1–T6 实现顺序。一期已开始实施，代码及检查结果见[F1 · 实施证据](phase-one-evidence.md)，游戏验收仍待完成。

## 总体推进入口

[内部框架建设总方案](master-plan.md)统一零至四期的目标、进入条件、范围、参考项目、验收和回退安排。此前任务编号继续沿用，新增第四期用于文档与交接收敛。

## 第零期现场记录

环境、实际 API 可用性和自动检查见[F0 · 第零期证据](phase-zero-evidence.md)。游戏验收继续按 M1–M6 逐项记录。

## 第二期当前入口

[F2 · 物品接入计划与证据](phase-two-items.md) · [新增物品接入清单](../../maintenance/item-extension.md)。原版顾客/夜间事件试点暂缓。
