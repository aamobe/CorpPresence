# 公司增强 / CorpPresence

《Probably Stolen》的公司剧情与经营扩展模组。六位公司代表、名片与电话订货、五条工艺线，以及默认关闭的“恶意巨企”模式。

[Steam 创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3828913695) · [游玩流程](share/公司增强游玩流程.txt) · [文档总目录](docs/README.md) · [维护指南](docs/maintenance/README.md)

当前工坊版本为 **1.2.0 — 框架整合与稳定性更新**，发布源码提交 `9ce9689`，标签 `v1.2.0`。已获 SteamCMD 上传成功回执；按用户要求未追加测试，未执行回下载核对。详见[发布记录](docs/releases/1.2.0.md)和[版本目录](docs/releases/README.md)。

## 常用入口

| 要做什么 | 从这里开始 |
| --- | --- |
| 查看剧情、订货与机器操作 | [游玩流程](share/公司增强游玩流程.txt) |
| 查五条产线及对应代码 | [功能目录](docs/specs/README.md) |
| 查香水配方、植物基因和设备属性 | [香水生产线](docs/specs/perfume.md) |
| 找到某个功能的实现位置 | [源码地图](docs/源码地图.md) |
| 查以前踩过的坑与处理方法 | [开发问题复盘](docs/retrospective/README.md) |
| 从立项到维护制作一个MOD | [MOD制作与项目管理指南](docs/mod-development-guide.md) |
| 构建、部署或准备发布 | [构建与发布](docs/maintenance/build-and-release.md) |
| 修改液体、读档、订货或会员条件 | [兼容边界与回归](docs/maintenance/compatibility-and-regressions.md) |
| 制作或导入美术素材 | [美术规范](art/OVERVIEW.md) |
| 查完整美术规则、参考图和分析工具 | [深空当铺美术资料](art/README.md) |
| 查看历史改动和当前发布状态 | [版本目录](docs/releases/README.md) |
| 提交修复或报告问题 | [贡献与反馈](CONTRIBUTING.md) |

## 仓库目录

```text
src/                    C# 源码、嵌入素材与项目文件
manifest.xml            官方加载器清单（随模组发布）
Localization/           官方加载器读取的 en、ru、fr 翻译（随模组发布）
tools/                  构建、导出与检查入口脚本
  checks/               回归检查工程，每个目录一个，不进正式模组
art/                    美术规范、原版参考素材与分析工具
share/                  面向玩家的说明
docs/                   开发与维护文档，入口是 docs/README.md
  源码地图.md           实现位置与调用链
  存档数据模型.md       存档字段与兼容约束
  章程.md               不可违反的项目原则
  specs/                当前玩法、产线与维护规则
  maintenance/          构建发布、兼容回归、框架接入与内部诊断
  localization/         翻译协作说明
  decisions/            已接受与拟议的技术决策
  changes/              进行中的变更；完成的在 changes/archive/
  bugs/                 问题调查与修复记录
  assessments/          值不值得做的评估
  retrospective/        历史问题、反例与证据索引
  releases/             发布记录、文件清单及历史版本
.github/                GitHub 工作流与问题反馈模板
AGENTS.md               项目约定与命令入口
总方案.md              开发流程、设计检查与模板（与 AGENTS.md 同目录使用）
```

每个顶层目录只放一类东西：会随模组发布的在根目录和 `src/`，只在开发时用的在 `tools/`、`art/`、`docs/`，给玩家看的在 `share/`。布局的理由见[这条决策](docs/decisions/reorganize-repository-layout.md)。

目录导航见[源码地图](docs/源码地图.md)和[工具目录](tools/README.md)。本地构建与检查输出位于已忽略的 `artifacts/`。

## 从源码构建

官方接口资料与接入状态见[官方接口跟踪](docs/maintenance/official-api.md)。

需要 **.NET 8 SDK或更高版本**及本地游戏文件。使用游戏自带的官方模组加载接口，入口是 `IMod`；无需为本项目另装BepInEx。

```powershell
./tools/Build.ps1 -GameManaged 'F:/SteamLibrary/steamapps/common/Probably Stolen/Probably Stolen_Data/Managed'
```

脚本只编译，不启动游戏、不部署、不打包、不上传。输出：`artifacts/build/bin/CorpPresence/release/CorpPresence.dll`。构建细节见[维护文档](docs/maintenance/build-and-release.md)。

纯静态检查（PowerShell 7+）：

```powershell
./tools/Check-Repository.ps1
```

## 权利说明

本仓库由 aamobe 公开供阅读和技术交流。公开源码不代表授予重新发布完整模组或分发修改版的许可。若希望分发衍生版本，请先联系作者。游戏及第三方组件的权利归各自权利人所有。

Source is published for review and technical discussion. No permission to redistribute the full mod or distribute modified builds is granted here. Please contact the author about derivative distributions. The game and third-party components remain subject to their respective rights.
