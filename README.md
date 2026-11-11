# 公司增强 / CorpPresence

《Probably Stolen》的公司剧情与经营扩展模组。六位公司代表、名片与电话订货、五条工艺线，以及默认关闭的“恶意巨企”模式。

[Steam 创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3828913695) · [游玩流程](公司增强游玩流程.txt) · [文档总目录](docs/README.md) · [维护指南](docs/maintenance/README.md)

当前版本为 **1.0.1**。恶意模式下，撒该在发现扶持金要求的订阅失效后会给一天补订时间。详见[当前版本记录](docs/releases/1.0.1.md)和[翻译维护说明](docs/localization/README.md)。

## 常用入口

| 要做什么 | 从这里开始 |
| --- | --- |
| 查看剧情、订货与机器操作 | [游玩流程](公司增强游玩流程.txt) |
| 查五条产线及对应代码 | [功能目录](docs/specs/README.md) |
| 查香水配方、植物基因和设备属性 | [香水生产线](docs/specs/perfume.md) |
| 找到某个功能的实现位置 | [源码地图](docs/源码地图.md) |
| 查以前踩过的坑与处理方法 | [开发问题复盘](docs/retrospective/README.md) |
| 从立项到维护制作一个MOD | [MOD制作与项目管理指南](docs/mod-development-guide.md) |
| 构建、部署或准备发布 | [构建与发布](docs/maintenance/build-and-release.md) |
| 修改液体、读档、订货或会员条件 | [兼容边界与回归](docs/maintenance/compatibility-and-regressions.md) |
| 制作或导入美术素材 | [美术规范](docs/art/README.md) |
| 查完整美术规则、参考图和分析工具 | [深空当铺美术资料](docs/probably-stolen-art/README.md) |
| 查看历史改动和当前发布状态 | [版本目录](docs/releases/README.md) |
| 提交修复或报告问题 | [贡献与反馈](CONTRIBUTING.md) |

## 仓库目录

```text
src/                    模组源码、嵌入素材与项目文件
docs/
  features/             产线和玩法规则
  maintenance/          源码定位、构建发布与兼容约束
  releases/             当前版本、发布证据与历史更新说明
  art/                  像素素材和状态差分规范
tools/                  构建、静态检查与已有诊断工具
.github/ISSUE_TEMPLATE/ 问题反馈模板
manifest.xml            官方模组加载器清单
公司增强游玩流程.txt    玩家操作说明
```

## 从源码构建

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
