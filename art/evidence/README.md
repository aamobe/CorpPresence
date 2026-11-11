# 证据索引与复现

人物立绘与撒该风格证据单独收录在 [portraits/README.md](portraits/README.md)，不与以下物品统计混算。

- `native/Sprite/*.png`：直接导出的137张样本，保留原始Alpha，不作为修过的素材发布。
- `metadata/Sprite/*.json`：14个代表样本的Sprite元数据。
- `metadata/Texture2D/*.json`：6个相关图集的纹理属性，不含整个图集图像。
- `sprite-metrics.json/csv`：主体阈值Alpha≥128的颜色、包围盒、Alpha分布等；JSON含单张SHA256。
- `native-contact-sheet.png`：4倍最近邻对照。
- `style-machines.png`、`style-small.png`：按用途精选的原版1×与4×对照，每张附主体调色板，按像素使用数从多到少排列。
- `style-state-diffs.json`：电池与炉子的原版状态差分像素统计。
- `scaling-source.png`：跑轮概念生成双图的固定实验副本。
- `scaling-phase-results.json`、`scaling-phase-comparison.png`：取样位置敏感性实验。
- `game-render-il.txt`是原本的本地游戏类IL核对输出，不随GitHub资料同步。需要时可用InspectGame在本地重新生成，正文保留当时分析结论。
- `provenance.json`：源文件与实验输入哈希。

在仓库根目录执行：

```powershell
& ./art/tools/Analyze-Sprites.ps1
& ./art/tools/Measure-Scaling.ps1
& ./art/tools/Build-StyleSheets.ps1
```

上述脚本只更新证据统计和诊断图，不改游戏文件或MOD资产。

原版PNG提取命令示例（工具与游戏路径需替换为自己的安装位置；输出必须用新目录）：

```powershell
& 'D:/LocalTools/AssetStudio/AssetStudio.CLI.exe' `
  'D:/Games/Probably Stolen/Probably Stolen_Data/resources.assets' `
  '新的证据目录' --game Normal --types Sprite `
  --names '^(animal_cage|rat.*|.*battery.*|.*injector.*|.*cigarette.*|.*rifle.*|.*revolver.*|.*bottle.*|.*furnace.*|gibber|portable_water_purifier|.*tilapia.*|.*meat_fish.*|AQUACULTURE.*)$' --silent
```

元数据使用相同工具的 `--export_type JSON` 和 `--types Sprite`／`--types Texture2D`。PNG计数137与工具导出项计数不完全相同，以磁盘PNG和附带哈希为准。

`tools/InspectGame`使用本地Mono.Cecil只读程序集并输出IL；依赖配置和构建见[工具说明](../tools/README.md)。编译好后：

```powershell
dotnet ./artifacts/art-tools/bin/InspectGame/release/InspectGame.dll `
  'D:/Games/Probably Stolen/Probably Stolen_Data/Managed/Assembly-CSharp.dll' `
  UISettings GameItemElement GameItem RenderHandler
```

更新游戏后重新读取元数据和IL，不只替换PNG而保留旧的参数结论。
