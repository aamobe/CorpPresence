# 水培眠苔槽内精灵验收

对应[生产流程的水培眠苔一节](production-flow.md#原料来源水培眠苔)。三个阶段共用 48×64 原生画布和左上原点。接种后留一团紧凑菌苔，扩展中向两侧铺开，成熟时出现更厚的中央菌块与更宽的边缘。所有阶段的接种团像素、托盘接触线和既有收获物保持一致。槽体与灯光由原版设备绘制。

## 原版 100-Rev13 参考

- 水芦苇槽内使用 `hydroreed_p` 单张覆盖图，实际为 48×64，主体包围盒 (1,11)–(46,46)，以 Alpha ≥128 统计。16×16 的 `hydroreed_seed` 是独立库存图。原版 `PlantList.TickHydroreed` 也会把生长中的种子物品切到 `hydroreed_p`。
- 同为菌菇的 `dream_cap_p` 是 48×64，主体包围盒 (1,11)–(44,46)。用于确认托盘接触线和簇状构图。蓝莓使用 48×64 的 `bloomberry_p` 和 `bloomberry_p2`，两个主体都落在 y=46。
- 原版 `MachineHydroponic` 仅在 `STATE_WORKING` 通过 `ItemSpriteModifier` 挂作物图层，偏移 (0,0)，无翻转、旋转或遮罩。工作中仅多年生且处于转熟状态时尝试使用 `_p2`，缺图回到 `_p`。原版 `GameItemElement.UpdateInnateModSprite` 调用 `SetNativeSize`，在默认方向以左上角 (0,1) 为 UI 锚点及 pivot，相对机器底图原点对齐。独立 PNG 不携带 Unity Sprite PPU 或导入 pivot，本次未取得对应原版 Sprite 的 PPU 元数据，接入时应核对与原版覆盖图的显示尺寸一致。占格沿用机器底图。
- 眠苔采用需求中的接种、扩展和可收获三个状态，原版覆盖入口没有直接提供这三段映射。接入时需在阶段判断及可收获状态下提供对应覆盖图。

## 原生像素检查

[可编辑母版](../../../src/CorpPresence/assets/cartel-production/Draw-HydroponicSleepMoss.ps1) 在目标网格叠绘菌簇，最后覆盖三帧共用的接种团。六色来自现有眠苔菌块。所有 PNG 使用二值 Alpha，透明区 RGBA 为 0，未经过缩放或限色。

| PNG | 主体包围盒，左上原点含端点 | 主体 RGB 数 | Alpha 0/255 像素数 | 相对上一帧的 RGBA 变化 |
| --- | --- | ---: | ---: | ---: |
| sleep-moss-hydroponic-inoculated.png | (17,39)–(28,46) | 6 | 2996/76 | 0 |
| sleep-moss-hydroponic-spreading.png | (9,36)–(39,46) | 6 | 2831/241 | 165 |
| sleep-moss-hydroponic-harvestable.png | (4,31)–(44,46) | 6 | 2649/423 | 285 |

三张原生图已直接查看。接种团 76 个不透明像素在三张图上逐像素一致。变化限定在 (4,31)–(44,46)，区外 RGBA 差异为 0。三张图的接触线均为 y=46。从母版重新生成后，三个文件的 SHA-256 与交付文件一致。

接入后在槽内依次查看接种、扩展和可收获，确认贴图贴住同一条托盘线、成熟帧在可收获状态仍显示，并在取走菌块、保留接种材料后回到接种阶段。当前只完成美术与静态检查，未进行编译、部署或游戏内验证。

## 眠苔种子库存精灵

[sleep-moss-seed.png](../../../src/CorpPresence/assets/cartel-production/sleep-moss-seed.png) 对应首访提供、成熟收获返还的可携带接种材料。以原版 100-Rev13 的 `hydroreed_seed` 为结构和纸色参考，并查看 `bloomberry_seed` 与 `fillerweed_seed` 的标签区。三张原版库存图均为 16×16，主体范围 (1,1)–(14,14)，Alpha 3/255 分别为 78/178 像素。新图用浅色封边和小块眠苔绿标记，原生尺寸下可凭纸包外轮廓与裸露的菌块区分。

[可编辑母版](../../../src/CorpPresence/assets/cartel-production/Draw-SleepMossSeed.ps1) 直接输出 16×16 RGBA。实测主体 (1,1)–(14,14)，六色，Alpha 0/255 为 90/166 像素，透明 RGB 为 0。按原版 MOD 取样规则，左上原点 (8,7) 不透明，预期占 1×1 格。独立重绘的 SHA-256 与交付 PNG 一致。接入时仍需验证水培槽收料、库存显示及收获返还使用的资源。
