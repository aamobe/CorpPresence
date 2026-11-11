# 文档总目录

当前发布：[1.1.0 接口版](releases/1.1.0-internal-maintenance.md)，源码提交 `2500a6e`。源码定位和发布操作从[维护指南](maintenance/README.md)进入。

## 规则与流程

| 文档 | 内容 |
| --- | --- |
| [AGENTS.md](../AGENTS.md) | 给助手和维护者的入口：项目档案、命令、硬规则、流程、按情况查文档的目录 |
| [总方案](../总方案.md) | 开发流程的详尽版：各步骤的做法、模板、按领域的设计经验、技术资料。按 AGENTS.md 的目录查对应小节，不必通读 |
| [章程](章程.md) | 本项目不可违反的原则和特有边界 |
| [决策记录](decisions/README.md) | 重要取舍的原因、放弃的方案和代价 |
| [变更](changes/README.md) | 进行中的改动和已归档的实施计划 |
| [更新日志](../CHANGELOG.md) | 写给玩家看的变化；未发布的改动先记在这里 |

目录布局在 2026-11-11 按总方案整理过，原因和范围见 [这条决策](decisions/adopt-plan-layout-for-docs.md)。新增存档结构见[存档数据模型](存档数据模型.md)。尚未建立的文档：日志约定、许可与素材（权利说明目前在根目录 README）。

## 玩家与功能

| 文档 | 内容 |
| --- | --- |
| [游玩流程](../公司增强游玩流程.txt) | 公司来访、电话服务、剧情条件与机器操作 |
| [功能目录](specs/README.md) | 五条工艺线、会员和恶意模式的代码入口 |
| [香水生产线](specs/perfume.md) | 花材化学处理、基底油材料、血袋、采血、基因、模组和扩容 |
| [0.9.9默认难度](specs/default-difficulty-0.9.9.md) | 默认预算、服务强度、收货价、合作度与限购 |

## 维护与发布

[官方接口跟踪](maintenance/official-api.md)汇总游戏官方 API 更新、版本与权限变化。

[接口与物品扩展框架评估](assessments/internal-framework/assessment.md)区分 1.0.0 已有基础、当前增量、验证缺口和后续投入收益。

| 文档 | 内容 |
| --- | --- |
| [维护首页](maintenance/README.md) | 按维护任务选择入口 |
| [源码地图](源码地图.md) | 目录职责、生命周期、数据流与常见修改位置 |
| [分期工程](maintenance/engineering-roadmap.md) | 各期依赖、代码入口、实施顺序和验收条件 |
| [构建与发布](maintenance/build-and-release.md) | 本地依赖、编译、部署、版本核对与发布记录 |
| [兼容边界与回归](maintenance/compatibility-and-regressions.md) | 普通水、身份注册、存档、限购、会员及机器风险 |
| [开发问题复盘与反例目录](retrospective/README.md) | 40个历史案例、处理方法、验证边界及防复发要点 |
| [MOD制作与项目管理指南](mod-development-guide.md) | 开发流程、职责边界、存档/资源约束、任务管理和交付模板 |
| [工具目录](../tools/README.md) | 构建和静态检查脚本、已有诊断程序的用途 |
| [贡献与反馈](../CONTRIBUTING.md) | 修改范围、反馈信息、权利约束 |

## 美术

[美术维护入口](art/README.md)说明素材路径、原生尺寸、缩放和状态差分要求。宣传图与游戏精灵分开验收；当前仓库不收录未发布的立绘改稿。

完整资料见[深空当铺美术规范与参考目录](probably-stolen-art/README.md)，包含物品/机器、人物与撒该风格、缩放防杂色、参考PNG、元数据、统计和辅助工具。

## 版本与历史

- [版本目录](releases/README.md)：当前源码与工坊的对应关系。
- [1.0稳定版](releases/1.0.0.md)：稳定本地化键和无需文件访问权限的官方CSV翻译。
- [0.9.9记录](releases/0.9.9.md)：正式版功能、香水线和会员修复。
- [0.9.8香水线旧说明](releases/archive/perfume-update-0.9.8.md)：保留旧链接，历史版本不代表当前发布基线。
- [0.9.7兼容修复](releases/archive/compatibility-0.9.7.md)、[水系统边界历史记录](releases/archive/water-compatibility.md)：用于追踪旧问题；当前维护约束以兼容回归文档和实际源码为准。

历史更新说明收在 [releases/archive](releases/archive)。不要以旧文件中的版本名决定下一次发布基线。
