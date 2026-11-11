# 公司增强内部框架分期方案

2026-11-11 · 评审草案 · 基线 `feat/internal-maintenance` / `2500a6e`

先验收现有维护接口，再用恶意模式试点模块级诊断和完整启停验证。第二个模块选原版事件接入作为候选。两个实际调用方证明规则相同后，才考虑提取一处内部共用逻辑。

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

2026-11-11 已补充[第一期详细设计与换机交接](phase-one-design.md)。定义安装提交状态、释放失败标志、只读快照字段、错误传播及 T1–T6 实现顺序。状态仍为设计草案，生产代码尚未实施。

## 总体推进入口

[内部框架建设总方案](master-plan.md)统一零至四期的目标、进入条件、范围、参考项目、验收和回退安排。此前任务编号继续沿用，新增第四期用于文档与交接收敛。

## 第零期现场记录

环境、实际 API 可用性和自动检查见[第零期证据](phase-zero-evidence.md)。游戏验收继续按 M1–M6 逐项记录。
