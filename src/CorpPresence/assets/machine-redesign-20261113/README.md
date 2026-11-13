# 六台机器原生精灵审核版

2026-11-13。来源为本轮六张新设计生成稿，未读取更早旧稿。独立目录保存，未修改运行时资源引用。

|机器|PNG|画布|逻辑占格|
|---|---|---|---|
|破碎机|[mineral-crusher.png](mineral-crusher.png)|48×48|3×3|
|筛分机|[powder-sieve.png](powder-sieve.png)|48×48|3×3|
|烧结站|[sintering-station.png](sintering-station.png)|48×48|3×3|
|矿化机|[mineral-growth-station.png](mineral-growth-station.png)|48×64|3×4|
|酒精制备站|[alcohol-preparation-station.png](alcohol-preparation-station.png)|48×48|3×3|
|双胆反应槽|[twin-vessel-reactor.png](twin-vessel-reactor.png)|64×48|4×3|

每张 `.pixels.json` 是完整可编辑像素矩阵：`.`表示透明，0–D对应 Palette 索引。PNG与矩阵一致。规格见 [sprite-specs.json](sprite-specs.json)，统计见 [validation.json](validation.json)。PPU与pivot是接入规格，PNG自身不存储Unity Sprite元数据。

制作：完整源画布保持比例，以每个目标像素内7×7样本的调色板投票生成初稿（绿底单独分类），然后按明确坐标修整筛孔、立柱、散热条、料口、液窗与仪表。未采用单点取样自动交付。工具和生成源见项目 `art/evidence/redesign-20261113/` 的 Build-Native.ps1、Finish-Native.ps1 与六张 generated PNG。

视觉复查：在1×与2×最近邻下查看六张；顶盖、底座、外壳保持连续色块，筛孔和散热条使用规则间距，保留结构性单像素亮点。原生尺寸与Alpha已实测，视觉风格待用户审核。

占格：保持表中完整矩形逻辑占格，不由Alpha重新构造。酒精站、破碎机、双胆槽部分角格中心透明，中心采样掩码已如实记录；接入时必须沿用既有Shape定义。未改游戏代码，未编译、部署或进行游戏内验证。
