# 素材任务记录模板

> 当前执行规则（2026-11-13）—新精灵直接生成独立文件并原样保存，按同目录 AGENTS.md 的“直接生成精灵”验收。禁止预览稿或高清合图再缩小交付。以下转换方法仅限用户明确授权的既有贴图编辑任务。

> **首选生图规范已更新：使用下方严格像素周期模板，取代旧提示词骨架。** 生图与正式原生精灵按用户指定的交付阶段执行；正式精灵按素材需求选工具，在最终原生尺寸制作。按[美术规范](AGENTS.md)直接读取原生 PNG 和像素数据，检查尺寸、Alpha 和像素结构。旧图板和缩放记录用于历史参考或问题诊断。

复制到具体素材的工作目录，填写后再制作。不确定项先查资源或代码，不通过猜测补全。

## 规格

```text
名称／资源键：
类别：库存物品 / 机器 / 动物 / UI图标 / 其他
交付阶段：原生精灵 / 导入测试
制作工具／可编辑源文件格式：
原版参考资源及游戏版本哈希：
设计范围：忠实重绘 / 原版改装 / 完全重新设计；明确禁止沿用的旧稿
结构主参考：精确资源键＋借鉴的视角／体量；重新设计另写新的部件安排
观察方向与光源分别记录：正面边方向、顶面深度、浅侧缘、受光与遮挡
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
直接生成并原样保存；分别记录目标尺寸与实际输出尺寸
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

Match the viewing angle and orientation of the reference. Keep the complete object within the canvas. Use the specified native bounding box, anchor and margins: [BOUNDING BOX / ANCHOR / MARGINS]. Do not shrink the object to impose a universal margin.

One object only. No additional objects, scenery, captions, borders, or visible grid lines.
```

### 重新设计分支

用户要求以原版物品为参照重新设计时，将首句换成 `Design a new [SUBJECT] using the reference image as a visual guide, as true pixel art with a strict pixel period.`，并将 `Keep the design of the reference` 整段替换为下段；其余像素、平涂、背景约束保留，包围盒和锚点按任务填写。完全重新设计时不载入被排除的旧稿或复用旧绘图坐标；从功能重新组织轮廓和部件，不同时要求保留原设计。

```text
Use the reference as a guide for visual style, palette, outlines, and level of detail. Create a new design for [SUBJECT], with a distinct silhouette and component arrangement. Keep all details readable at the specified logical resolution.
```

### 填写与交付

- `[BOUNDING BOX / ANCHOR / MARGINS]` 填已确认的原生包围盒、锚点和留白；没有锁定数据时先查参考，不统一强加3像素边距。机器提示词另写观察方向与光源，颜色按材质色组及面积组织，避免整机统一色相替换。
- `[SUBJECT]` 填具体主体；没有排除对象时删掉排除句，不保留空占位符。不沿用塔楼、樱花等示例专属部件。
- 尺寸用确定值，不用 `about`。本游戏默认每格16像素：3×3占格对应48×48；96×128只是原示例尺寸，不是所有素材的默认值。
- 默认纯绿背景 `#00FF00`，内部空隙同色。用户指定透明时替换背景要求为真正透明、Alpha仅0或255，并启用工具透明输出；游戏导入的透明PNG与绿底生成原图分别记录。
- 用户只要生图时，按此模板使用图像生成工具交付，并如实标明实际尺寸与网格偏差；不要擅自用手工重绘替换已确认造型。
- 正式精灵按直接生成规则验收；尺寸或网格不符时重新生成或说明限制，转换和重绘须有用户另行授权。

## 完成记录

```text
最终PNG及SHA256：
宽高／主体色数／Alpha统计：
原生PNG与同倍率参考检查结果（诊断图单独存放）：
机械结构、色块面积、色相过渡与局部高光检查结果：
差分共同区不同像素数：
游戏导入方式与占格路径：
编译结果／部署目标／部署哈希：
实际游戏测试：已观察项、待测项
```

“同尺寸”“同色数”“提示词写了原版风格”都不是最终完成条件。
