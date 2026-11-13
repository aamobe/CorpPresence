# 粉末工艺新增素材交接

2026-11-13，采用本轮修订规格。只交付美术，未改生产逻辑、注册、资源调用或部署。此前六台机器素材保持原状。

|文件|名称|原生尺寸|完整矩形占格|
|---|---|---|---|
|[silicon-plate.png](silicon-plate.png)|硅板|32×32|2×2|
|[electronic-substrate.png](electronic-substrate.png)|电子基材（替代原“稀有金属触点”需求）|32×16|2×1|
|[electronic-blueprint.png](electronic-blueprint.png)|普通电子元件装配蓝图|16×16|1×1|
|[crafting-station.png](crafting-station.png)|综合工艺台|48×48|3×3|
|[crafting-station-running.png](crafting-station-running.png)|综合工艺台通用运行差分|48×48|3×3|

铜线复用原版物品 `wire`：`Items/items_construction` 图集的 `wire`。成品复用原版物品 `common_electronic`：`Items/items_material` 图集的 `electronic1`（物品ID与精灵名不同）。本目录没有这两项的替代图。

## 参考与制作

工艺台采用原版 `purifier`／`furnace` 的正前上方浅俯视、灰蓝金属、紫暗边与暖灰亮部；蓝图借鉴 `manual1` 的青色纸面与浅色标记；半成品结合原版 `electronic1` 的材质色。补焊片、双铆钉、替换接头表达改装质感，避免随机磨损碎点。

采用内置 imagegen 生成四项独立设计，再在规定原生网格整理。完整生成画布等比映射，每个目标像素以7×7样本进行绿底分类和调色板投票；该结果仅作初稿。随后在可编辑像素矩阵上清理硅板侧缘与杂色、补全基材轮廓并统一接点、重画蓝图线路符号，整理工艺台示意图、台面和补焊片。运行图从同一原生母版派生。未使用更早被否定的机器稿。

源稿、完整提示词、加工与验证脚本位于项目 `art/evidence/powder-crafting-20261113/`。顺序为 Rasterize-Drafts.ps1、Finish-Items.ps1、Finish-Station.ps1、Inspect-Native.ps1、Validate-Native.ps1。运行前需保证对应 generated PNG 存在。

各 `.pixels.json` 为可编辑源文件：`.`透明，0–F对应Palette索引。全部PNG已逐像素匹配源矩阵；Palette仅定义可用色，实际用色见验证结果。

## 原生与差分验证

五图均为Alpha 0/255，透明RGB为0；颜色数依次为8、9、7、12、12。已查看1×与4×最近邻对照，检查连续大平面、边缘、安装孔、三格备料盘、夹具和手柄连接；最终视觉效果待用户审核。

工艺台运行帧仅改变26个RGBA像素：中央夹具 `(16,22,6,4)` 和前工作灯 `(33,35,1,2)`，格式为左上原点 `x,y,width,height`。Alpha差异0，掩膜外RGBA差异0。手柄、画布原点、轮廓、蓝图夹与台面不移动。

接入规格：Point过滤、100 PPU、中心pivot `(0.5,0.5)`；这些是Unity接入参数，PNG本身不保存该元数据。五项占格按上表显式定义。工艺台顶部部分角格透明，中心Alpha采样为 `100/111/111`，**不得据此缩减3×3逻辑占格**。运行时切图保留原Shape。

实测数据、SHA256和变化掩膜见 [validation.json](validation.json)。尚未进行游戏内显示、拖拽、旋转或状态切换验证；源码接入由粉末工艺任务负责。
