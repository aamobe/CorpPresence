# 装备、武器、电池与模块

遵循[共用契约](common-contract.md)，首版仅需求参考。基底自带的枪械、电池行为不能伪装为普通商品。

案例链：[Goods.Definitions](../../../src/CorpPresence/Items/Goods.Definitions.cs)的rosen_rifle→Goods.Build→[RifleMods.Attach](../../../src/CorpPresence/Items/RifleMods.cs)限制配件槽并绑定叠图。当前步枪拒绝MOD_STOCK_TAG和MOD_MAGAZINE_TAG；这不是全部武器的通用兼容表。电池显示由Goods.RefreshBattery与Catalog.TryBatteryState关联空/半/满图。机器升级参考[MachineUpgrade](../../../src/CorpPresence/Production/MachineUpgrade.cs)，不是武器改装的同义入口。

字段表：耐久的当前/上限/损耗单位，弹药型号与数量，能量当前/容量/每次耗电，装配槽的角色而非仅索引，叠加规则，状态精灵和占格，拆卸后保存哪些改造。尚未核实的弹药/伤害字段保持待查，不生成猜测API。

| 转换 | 守卫与写入者 | 消耗/输出 |
| --- | --- | --- |
| 新建→可装配 | 原版初始化，模组仅限制自己的槽 | 不自动赠送额外模块 |
| 安装→有效果 | 身份和兼容谓词、原版许可都通过 | 物品转移归原版；不克隆模块 |
| 使用→损耗 | 原版武器/电池规则 | 弹药/电量/耐久仅一个写入者 |
| 拆卸/重载 | 重绑叠图与许可，恢复现有库存 | 不重置电量或覆盖改造 |

获取通过目录；使用和装配走原版与定向回调；出售前检查违禁/价值规则；卸载只释放自有叠图资源。反例：每次Attach套一层许可回调导致重复判断；读档重建槽位索引导致模块丢失。

[示例](../../../tools/ItemAuthoring/examples/equipment.json)列六栏待办。代码检查兼容矩阵、空电/满电边界、绑定幂等、恢复后改造保留；必要实机为一次装卸、一次消耗和重载，不要求穷举所有原版配件。
