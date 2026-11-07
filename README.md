# 公司增强 / CorpPresence

《Probably Stolen》的公司剧情与经营扩展模组。包含公司来访、名片与电话订货、专属商品、自定义生产设备，以及可选的恶意模式。

[Steam 创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3828913695) · [游玩流程](公司增强游玩流程.txt)

当前源码为 **0.9.8 香水线更新**，版本号继续使用0.9.8；保留此前fix2修复。详见[香水线更新说明](docs/perfume-update-0.9.8.md)。

索拉纳金卡电话“香水工艺”开放油花种植、手动温浸、基底油材料分离、血袋提取和香水合成。支持采血限制、植物基因及机器扩容；普通水仍沿用原版处理。

## 从源码构建

需要 .NET 8 SDK 和本地安装的《Probably Stolen》。将 `GameManaged` 指向游戏安装目录下的 `Probably Stolen_Data/Managed`。

```powershell
dotnet build ./src/CorpPresence.csproj -c Release "-p:GameManaged=F:/SteamLibrary/steamapps/common/Probably Stolen/Probably Stolen_Data/Managed"
```

输出为 `src/bin/Release/netstandard2.1/CorpPresence.dll`。将 DLL 和仓库根目录的 `manifest.xml` 放入 `Mods/CorpPresence`。若已订阅工坊版，请确保游戏只加载一份相同 MOD ID 的副本。

## 权限与反馈

当前源码已移除 Neon Undertow 专用兼容钩子。启用 Neon Undertow 不会阻止公司增强启动。

反馈 BUG 时请附游戏和模组版本、复现步骤以及相关日志片段。更新时请同时替换 DLL 和清单。液体相关改动见 [兼容边界](docs/water-compatibility.md)，此前的堆叠配送和回收范围修复见 [0.9.7 兼容修复说明](docs/compatibility-0.9.7.md)。

## Rights / 权利说明

本仓库由 aamobe 公开供阅读和技术交流。公开源码不代表授予重新发布完整模组或分发修改版的许可。若希望分发衍生版本，请先联系作者。游戏及第三方组件的权利归各自权利人所有。

Source is published for review and technical discussion. No permission to redistribute the full mod or distribute modified builds is granted here. Please contact the author about derivative distributions. The game and third-party components remain subject to their respective rights.
