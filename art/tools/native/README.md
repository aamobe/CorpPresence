# 原生像素诊断与转换工具

Windows + PowerShell 7，使用本机System.Drawing与C#编译，不安装外部依赖、不联网、不启动游戏。输出使用仓库外工作目录；转换和编辑重放拒绝覆盖现有输出。实验说明见[原生流程](../../NATIVE-WORKFLOW.md)，完整输入见[矿化机样例](../../examples/mineral-native/README.md)。

## 入口

```powershell
# 复现已提交样例；选择一个不存在的输出目录
./art/tools/native/Test-NativeWorkflow.ps1 -OutputDirectory '<新工作目录>'

# 其他素材转换：先确认此任务允许原生整理
./art/tools/native/Convert-Sprite.ps1 -InputPath '<原稿.png>' -OutputPath '<新草稿.png>' -Width 48 -Height 64 -PalettePath './art/examples/mineral-native/palette.json' -Method center

# 只读分析；报告会包含本机绝对路径，不直接提交该报告
./art/tools/native/Measure-Sprites.ps1 -InputPaths '<素材.png>' -LogicalWidth 48 -LogicalHeight 64 -PalettePath './art/examples/mineral-native/palette.json' -OutputPath '<报告.json>'

# 重放明确记录的像素修改；before不匹配则拒绝
./art/tools/native/Replay-PixelEdits.ps1 -InputPath '<原生基稿.png>' -EditsPath '<修改清单.json>' -OutputPath '<新输出.png>'
```

Convert支持center（中心像素）、majority（每格中心60%色板众数）、elastic（实验性有界全局切格）。固定RGB欧氏最近邻、Alpha<128透明/其他255、无抖动、无自动裁切；这些是本工具参数，不是所有素材的通用最佳选择。转换不能补回源图不存在的结构。只有elastic使用Radius（默认0.4格，最大0.45），切线及参数写入输出旁的conversion.json。

## 诊断指标的范围

- Alpha及精确色板命中在原始像素上统计，主体阈值为128。
- 格内主色占比在内存中映射到色板后计算，整画布零相位均匀切格。它不表示原图已经单色平涂，大面积背景及更小格子会抬高分数。
- 边缘分数以RGB距离≥30或穿越Alpha128作边缘，搜索64种相位、容差±0.1格；与格内主色统计不共享拟合相位。1像素原生网格得分必然为1。
- 包围盒和四连通组件也按Alpha≥128统计；SmallComponents4指面积≤2。单组件不能证明安装关系正确，孤立功能灯也不应机械删除。
- 没有自动裁切、局部语义区域拟合或审美评分。elastic是本地动态规划基线，不是Pixel Snapper完整复现；允许的格宽变化会造成局部形变。

## 验证范围

测试包含已知4倍格点、错误网格、两个孤立像素、原版炉子8倍复制恢复RGBA完全一致，以及固定矿化机输入重放。测试还核对PNG尺寸、色板、Alpha掩膜、像素修改净值、错误版本拒绝及manifest哈希。

通过仅证明这批确定性步骤与提交样例一致。不能证明模型直出、任意图无损恢复、材质审美或引擎渲染。修改源稿后仍应实际从Aseprite导出并逐像素比较。C#类加载在当前PowerShell进程中，改源码后用新进程重跑以避免复用旧类型。
