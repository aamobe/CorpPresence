# 公司增强 / CorpPresence

《Probably Stolen》的公司剧情与经营扩展模组。包含公司来访、名片与电话订货、专属商品、自定义生产设备，以及可选的恶意模式。

[Steam 创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3828913695) · [游玩流程](公司增强游玩流程.txt)

## 从源码构建

需要 .NET 8 SDK 和本地安装的《Probably Stolen》。将 `GameManagedDir` 指向游戏安装目录下的 `Probably Stolen_Data/Managed`。

```powershell
dotnet build ./src/CorpPresence.csproj -c Release "-p:GameManagedDir=F:/SteamLibrary/steamapps/common/Probably Stolen/Probably Stolen_Data/Managed"
```

输出为 `src/bin/Release/netstandard2.1/CorpPresence.dll`。将它与仓库根目录的 `manifest.xml` 一同放入游戏的 MOD 目录。若已订阅工坊版，请确保游戏只加载一份相同 MOD ID 的副本。

## 权限与反馈

模组使用游戏的「外部反射」权限，用于连接原版内部接口。反馈 BUG 时请附游戏和 MOD 版本、复现步骤，以及相关日志片段。

## Rights / 权利说明

本仓库由 aamobe 公开供阅读和技术交流。公开源码不代表授予重新发布完整模组或分发修改版的许可。若希望分发衍生版本，请先联系作者。游戏及第三方组件的权利归各自权利人所有。

Source is published for review and technical discussion. No permission to redistribute the full mod or distribute modified builds is granted here. Please contact the author about derivative distributions. The game and third-party components remain subject to their respective rights.
