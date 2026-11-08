# 人物证据与复现

日期2026-11-05。当前正文为 [PORTRAITS.md](../../PORTRAITS.md)。

## 正式证据

- `selected/manifest.json`：11个资源名、15个Sprite对象；保留PathID、源图集属性、PPU、pivot、SHA256。
- `selected/*__PathID.png`：分别导出的原版样本。使用这些唯一命名的文件，避免同名不同表情覆盖。
- `portrait-metrics.json/csv`：Alpha≥128主体统计与完整Alpha直方图。
- `portraits-native-sheet.png`：完整画布的原尺寸对照，边框不属于资源。
- `native-face-details.png`：4个面部样本的固定区域裁切，4倍最近邻诊断图。
- `native-state-diffs.json`：4组同名不同PathID图像的RGBA／Alpha差异及变化包围盒。
- `envoy/*.png`：当前撒该6档资源的固定副本，作为MOD主风格参考。
- `envoy-metrics.json`：撒该各档尺寸、主体色数、主体包围盒、常见色、Alpha、哈希。
- `envoy-integer-copy-check.json`：n与s系列的精确整数复制比较。
- 原本的`sprite-dict-il.txt`为本地只读IL转储，本仓库不附；正文保留映射使用SpriteEntry.key的分析结论。
- `portrait-loader-source.txt`：本次分析的MOD Portraits.cs快照。
- `provenance.json`：游戏数据、程序集与MOD立绘加载代码来源哈希。

`native/`和`metadata/`是初步名称检索结果，存在同名覆盖风险，只作发现线索，不作为正文中具体人物图的唯一证据。36×36图标、960×540插图、255×351纸张类资源被排除在288×211立绘样本之外。

## 复现分析

在仓库根目录执行：

```powershell
& ./docs/probably-stolen-art/tools/Analyze-Portraits.ps1
& ./docs/probably-stolen-art/tools/Analyze-Envoy.ps1
```

脚本读取固定参考副本，不改游戏或MOD资源。第二个脚本逐像素检查较大档位，耗时高于普通缩略图生成。

## 重取原版参考

`tools/ExportPortraits`使用本地AssetStudio库，只读游戏资源，并以Sprite名称＋PathID保存。[构建配置](../../tools/README.md)完成后调用：

```powershell
dotnet ./artifacts/art-tools/bin/ExportPortraits/release/ExportPortraits.dll `
  'D:/LocalTools/AssetStudio' `
  'D:/Games/Probably Stolen/Probably Stolen_Data/sharedassets1.assets' `
  '新的立绘证据目录'
```

游戏更新后PathID可能变化；重新确认映射与分析脚本中的选样ID，不能将旧PathID当永久API。导出的Sprite名字也不一定是实际对话使用的key。
