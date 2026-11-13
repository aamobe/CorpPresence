# 矿化机阶段覆盖图与腔内微亮

2026-11-13，按用户最后修订将普通矿石两阶段改为竖状矿柱。光盐对象为卡尔特原料 `light_salt_core`（现有物品ID `corp_presence_light_salt_core`），不是光盐析晶。

## 母版和叠加

唯一对齐母版：[mineral-growth-station.png](../machine-redesign-20261113/mineral-growth-station.png)。48×64原生画布，3×4完整逻辑占格，中心pivot `(0.5,0.5)`，100 PPU，Point过滤。母版SHA256在 [validation.json](validation.json)。不适用于其他历史矿化机造型。

四张覆盖图均为完整48×64透明画布，仅含培养物。与母版按左上原点 `(0,0)` 重合；Unity使用相同rect、pivot、PPU与零局部偏移，无需裁边或另加平移。接触线为原生图 `y=37`，共同生长轴约 `x=20`。底部贴住现有培养座，上部向腔内生长。下表包围盒含两端，坐标原点在左上。

|覆盖图|阶段|实际主体包围盒|主体尺寸|
|---|---|---|---|
|[ore-growing.png](ore-growing.png)|普通矿石培养中，短矿柱|`(18,30)–(22,37)`|5×8|
|[ore-mature.png](ore-mature.png)|普通矿石成熟待收获，竖状矿柱|`(17,22)–(23,37)`|7×16|
|[light-salt-core-growing.png](light-salt-core-growing.png)|光盐芯培养中，小晶簇|`(17,31)–(23,37)`|7×7|
|[light-salt-core-mature.png](light-salt-core-mature.png)|光盐芯成熟待收获，完整双核晶簇|`(15,25)–(24,37)`|10×13|

绘制顺序：机器底图 → 所选培养物覆盖图。替换整个覆盖图，不累加培养中与成熟帧。暂停和工作状态可复用同一培养物阶段；它们与底图亮度分开选择。

## 运行时微亮

[mineral-growth-station-running.png](mineral-growth-station-running.png) 是完整机器运行底图，不是培养物覆盖图。运行时使用它替换母版，再叠对应培养物；停止、缺水暂停和成熟待收获时建议恢复母版，是否运行由接入逻辑判断。

仅在腔内 `x=9..29, y=22..44` 的原青色材质像素提亮：`#275654 → #2F756F`、`#2F756F → #45918B`。没有外部光晕，不使用柔光渐变，不改变外壳、培养座或透明轮廓。共403个RGBA像素改变，Alpha差异0，掩膜外差异0。

## 制作与验证

使用内置imagegen生成培养物独立设计，普通矿石参考原版 `ore1`，光盐芯参考现有 `cartel-production/light-salt-core.png`；随后按Alpha包围盒等比落到目标像素预算、限于原版材质色板并放置到统一接触线。原生复查确认连续矿物分面、纵向轮廓、同一底部锚点和腔内边界，未把横向初稿作为最终矿石。

`*.pixels.json` 是可编辑源矩阵，`.`表示透明，其余字符依Palette索引。五图已验证原生尺寸48×64、二值Alpha、透明RGB为0、源矩阵逐像素一致。四个培养物均在腔内声明范围，实际最下行均为37；两张矿石图高度均大于宽度。已查看1×与4×合成对照，视觉效果待用户审核。

过程文件、提示词与复现脚本在项目 `art/evidence/mineral-growth-stages-20261113/`：Build-Stages.ps1 → Inspect-Stages.ps1 → Validate-Stages.ps1。普通矿石读取 `ore-*-vertical-generated.png`；早期横向生成稿不参与构建。逐图统计见 [validation.json](validation.json)，源裁切与放置矩形见 [layout.json](layout.json)。

玩法交接：普通矿石每批1件、光盐芯每批2件，均基础两夜成熟。美术文件不实现计时、数量或收获逻辑。未改生产代码、注册、部署或游戏存档，游戏内显示与状态切换待接入验证。
