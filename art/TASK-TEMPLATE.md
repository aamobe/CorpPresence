# 素材任务记录模板

> **首选生图规范已更新：使用下方严格像素周期模板，取代旧提示词骨架。** 生图与正式原生精灵按用户指定的交付阶段执行；最终接入仍按[美术规范](AGENTS.md)检查尺寸、Alpha及像素结构。

复制到具体素材的工作目录，填写后再制作。不确定项先查资源或代码，不通过猜测补全。

## 规格

```text
名称／资源键：
类别：库存物品 / 机器 / 动物 / UI图标 / 其他
交付阶段：原生精灵 / 导入测试
本地 Aseprite／集成检查结果：
原版参考资源及游戏版本哈希：
结构主参考：资源名＋继承的轮廓／视角／部件安排
材质参考：资源名＋外壳／玻璃／标签／指示色
风格对照：STYLE-REFERENCE.md中的类别及与原版并排检查结果
最终原生画布：W×H px
库存网格与形状：
主体包围盒／需保留留白：
锁定区域：
允许改装区域：
PPU / pivot / 纹理路径：
调色板：继承RGB列表＋新增材质色及用途
Alpha：参考实测 / 新作约束
状态：空载、工作等；每个差分的变化掩膜
制作尺寸＝导出尺寸：禁止缩放
机械结构：工作过程、部件用途、连接／遮挡／支撑关系
金属高光：原版取样区域、RGB及像素段排列
预计运行时纹理内存：
```

## 首选生图提示词：严格像素周期（重点）

> **2026-11-12 用户指定：以下通用模板取代原有生成工具提示词骨架，作为物品、机器、布偶等单件像素素材的首选。** 用户最满意的是统一像素方块、平涂、完整轮廓与纯绿背景的生成效果。后续生图沿用这套核心约束，仅替换主体、尺寸、排除内容与设计分支，不擅自加入渐变、纹理、抖动或柔光。

### 忠实重绘模板

```text
Redraw the object from the reference image as true pixel art with a strict pixel period.

Subject: only [SUBJECT]. Completely remove [UNWANTED ELEMENTS]; do not draw them or anything in their place.

Logical resolution: exactly [WIDTH]×[HEIGHT] pixels. Every logical pixel is one exact square block filled uniformly with a single RGB color. Different blocks may use different colors. All blocks are the same size everywhere and sit on one lattice that starts at the top-left corner of the canvas: no half blocks, no smaller blocks for fine detail, no drifting, stretched or rotated grid. Do not add detail finer than one block.

Keep the design of the reference: preserve the silhouette, proportions, pose, colors, major components, and their relative positions. Do not enlarge, move or restyle individual elements. Simplify details that cannot fit within the logical resolution into clear whole-pixel shapes instead of introducing smaller pixels. Render any tiny lettering or symbols as simple blocky marks one block wide; they do not need to be readable.

Flat color fills only. No gradients, no dithering, no noise, no texture.

Background: pure flat #00FF00 everywhere outside the object, including all internal gaps and spaces between separate parts. Every edge is a hard stair-step of whole blocks. No anti-aliasing, no semi-transparent pixels, no glow, no drop shadow, no halo, no white fringe.

Match the viewing angle and orientation of the reference. Center the complete object within the canvas. Keep every part fully visible, with a margin of at least 3 logical pixels on every side.

One object only. No additional objects, scenery, captions, borders, or visible grid lines.
```

### 重新设计分支

用户要求以原版物品为参照重新设计时，将首句换成 `Design a new [SUBJECT] using the reference image as a visual guide, as true pixel art with a strict pixel period.`，并将 `Keep the design of the reference` 整段替换为下段；其余像素、平涂、背景和构图约束保留，不同时要求完全保留原设计。

```text
Use the reference as a guide for visual style, palette, outlines, and level of detail. Create a new design for [SUBJECT], with a distinct silhouette and component arrangement. Keep all details readable at the specified logical resolution.
```

### 填写与交付

- `[SUBJECT]` 填具体主体；没有排除对象时删掉排除句，不保留空占位符。不沿用塔楼、樱花等示例专属部件。
- 尺寸用确定值，不用 `about`。本游戏默认每格16像素：3×3占格对应48×48；96×128只是原示例尺寸，不是所有素材的默认值。
- 默认纯绿背景 `#00FF00`，内部空隙同色。用户指定透明时替换背景要求为真正透明、Alpha仅0或255，并启用工具透明输出；游戏导入的透明PNG与绿底生成原图分别记录。
- 用户只要生图时，按此模板使用图像生成工具交付，并如实标明实际尺寸与网格偏差；不要擅自用手工重绘替换已确认造型。
- 用户要求正式原生精灵或接入游戏时，再按原生画布流程整理与验收；不得仅凭提示词宣称严格网格已达标，也不得将自动缩小认定为验收通过。

## 完成记录

```text
最终PNG及SHA256：
宽高／主体色数／Alpha统计：
原生PNG与参考检查结果（不生成预览）：
机械结构与多色金属高光检查结果：
差分共同区不同像素数：
游戏导入方式与占格路径：
编译结果／部署目标／部署哈希：
实际游戏测试：已观察项、待测项
```

“同尺寸”“同色数”“提示词写了原版风格”都不是最终完成条件。
