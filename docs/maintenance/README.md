# 维护指南

1. 先读[当前发布记录](../releases/1.1.0-internal-maintenance.md)，确认修改所基于的源码。
2. 用[源码地图](../源码地图.md)找入口，并沿调用链检查UI、后端校验和存档。
3. 涉及液体、库存、读档或会员时，读[兼容与回归](compatibility-and-regressions.md)。
4. 完成修改后按[构建与发布](build-and-release.md)验证，并更新受影响文档。

开发中重复遇到的问题，先查[历史问题复盘](../retrospective/README.md)。其中按40个案例区分现场记录、历史复现、已撤回方案和未闭环反馈，供新MOD开发借鉴。

准备新功能或新MOD时，可按[MOD制作与项目管理指南](../mod-development-guide.md)确认环境、职责边界、旧档影响和交付目标，并复用其中的任务卡。

## 按任务查找

[总框架接入与验收](framework.md)统一启动、资源、物品、设备、付款配送、存档、扩展、UI 与诊断的接入契约和回归选择；物品具体字段继续见[物品接入](item-extension.md)。

[框架跨设备交接](../changes/organize-internal-lifecycle/cross-device-handoff.md)提供换机复现步骤、模块接入清单、检查选择表及待游戏验证场景。

[官方接口跟踪](official-api.md)记录官方文档版本、API 更新及本项目验证边界。

只读诊断、三个生命周期事件和真实付款回归已实现并发布。使用方法见[维护手册](internal-maintenance.md)，验证边界见[实施证据](../changes/add-maintenance-interfaces/evidence.md)。后续整理见[待评审方案](../changes/organize-internal-lifecycle/overview.md)。

| 维护任务 | 文档/位置 |
| --- | --- |
| 修改价格、会员门槛、限购 | [源码地图](../源码地图.md#公司与订货) |
| 修改机器、配方或植物 | [功能目录](../specs/README.md)；香水详见[规则](../specs/perfume.md) |
| 处理普通水拒收、旧物品或读档异常 | [兼容与回归](compatibility-and-regressions.md) |
| 增加翻译 | `src/CorpPresence/Presentation/Translations.json`及对应的`L.T`调用 |
| 导入或改动PNG | [美术入口](../art/README.md) |
| 发布GitHub源码或工坊更新 | [构建与发布](build-and-release.md) |

源码和文档共同维护。只改介绍文字，不应顺带调整游戏规则；只整理目录，不应改变已发布物品ID或资源名。
