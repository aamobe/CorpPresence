# 美术分析工具

PowerShell图像脚本使用`System.Drawing`，按Windows环境维护。它们会更新本目录的统计或诊断图，但不修改游戏文件及`src/Assets`。本次整理未运行这些图像生成脚本。

## 已有样本的分析

从仓库根目录执行，脚本默认根据自身位置找到资料根目录；也可传`-Root`指向另一份相同结构的资料。

| 脚本 | 输入 | 输出 |
| --- | --- | --- |
| [Analyze-Sprites.ps1](Analyze-Sprites.ps1) | `evidence/native/Sprite` | 尺寸、主体、Alpha、调色板统计与物品对照 |
| [Build-StyleSheets.ps1](Build-StyleSheets.ps1) | 物品PNG与统计 | 机器/小物件参考页及状态差异 |
| [Measure-Scaling.ps1](Measure-Scaling.ps1) | 固定`scaling-source.png` | 指定采样位置实验，不是通用像素验收器 |
| [Analyze-Portraits.ps1](Analyze-Portraits.ps1) | 人物selected清单及PNG | 人物统计、原尺寸和面部诊断图 |
| [Analyze-Envoy.ps1](Analyze-Envoy.ps1) | 撒该固定副本 | 尺寸/Alpha/颜色及整数复制比较 |

```powershell
./art/tools/Analyze-Sprites.ps1
./art/tools/Build-StyleSheets.ps1
```

重新分析前保留原证据及哈希。不要用新版样本覆盖旧目录后，继续沿用旧结论的日期和数值。

## 可选：重新导出或只读检查游戏

两个.NET工具需要.NET 8 SDK和本地AssetStudio/Mono.Cecil等库，库文件不随仓库上传。通过`AssetStudioDirectory`传绝对路径，不依赖作者的`.tools`目录。

```powershell
$assetStudioDirectory = 'D:/LocalTools/AssetStudio'
dotnet build ./art/tools/ExportPortraits/ExportPortraits.csproj -c Release --artifacts-path ./artifacts/art-tools "-p:AssetStudioDirectory=$assetStudioDirectory"
dotnet build ./art/tools/InspectGame/InspectGame.csproj -c Release --artifacts-path ./artifacts/art-tools "-p:AssetStudioDirectory=$assetStudioDirectory"
```

`ExportPortraits`需要`AssetStudio.dll`、`AssetStudio.Utility.dll`和`SixLabors.ImageSharp.dll`；`InspectGame`需要`Mono.Cecil.dll`。使用与自己游戏资源适配的工具版本；历史输出不证明任意新版库都兼容。

```powershell
dotnet ./artifacts/art-tools/bin/ExportPortraits/release/ExportPortraits.dll $assetStudioDirectory 'D:/Games/Probably Stolen/Probably Stolen_Data/sharedassets1.assets' './artifacts/art-reference-new'
dotnet ./artifacts/art-tools/bin/InspectGame/release/InspectGame.dll 'D:/Games/Probably Stolen/Probably Stolen_Data/Managed/Assembly-CSharp.dll' UISettings GameItemElement GameItem RenderHandler
```

导出使用新的空输出目录，按名称加PathID区分对象；游戏更新后重新确认PathID和选样。程序集检查只读文件并向标准输出打印IL，不启动游戏；完整转储应放忽略的`artifacts`目录，不提交回资料库。

## 本次验证范围

检查了文档本地链接、脚本语法、工具构建、参考PNG复制哈希和运行时源码快照。没有启动游戏，没有重新提取原版资源，也没有重新生成或验收325张参考图。
