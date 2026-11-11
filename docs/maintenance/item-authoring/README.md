# 物品开发参考

配套[开发工具](../../../tools/ItemAuthoring/README.md)与[既定方案](../../superpowers/plans/2026-11-12-item-authoring-tool.md)。所有类别先读[共用接入契约](common-contract.md)，再看本类状态、字段和失败边界。进度与证据见[实施记录](implementation-ledger.md)。

完整结果见[验收与评估](evaluation.md)：规范化是主要价值，单次计时不代表普遍速度承诺。

| 类别 | 参考与限制 |
| --- | --- |
| 1 普通商品/固体材料 | [普通商品](ordinary.md)：首版唯一自动接入类别 |
| 2 消耗品/药剂/工具 | [使用与持续效果](consumable.md) |
| 3 装备/武器/电池/模块 | [装配与能量](equipment.md) |
| 4 容器/箱体/背包 | [容量与恢复](container.md) |
| 5 生产设备 | [周期、批次和失败结算](machine.md) |
| 6 种子与植物 | [植物](plant.md)及[基因与生长专题](plant-growth-and-genes.md) |
| 7 收获物/化学原料 | [产量与加工资格](plant-produce.md)；涉及成分则退出工具 |
| 8 液体成分/混合物 | [仅人工参考](liquid.md)；工具拒绝 |
| 9 液体容器 | [仅人工参考](liquid-container.md)；工具拒绝 |
| 10 养殖活体/物种 | [养殖](aquaculture.md)及[阶段专题](aquaculture-stages.md) |
| 11 死亡水产/整鱼/切片/肉糜 | [原料转换与验收](seafood-raw.md) |
| 12 水产加工品/食品 | [食品能力和生产](food.md) |
| 13 其他生物/逃逸 | [独立生物边界](creature.md) |
| 14 卡片/凭证/任务奖励 | [领取与唯一性](credential.md) |
| 15 多状态/组合物品 | [状态转换与守恒](multistate.md) |
| 液体与液体容器 | [液体系统参考](liquid-systems.md)：原版水扩展与独立液体系统分开；水系统列为最高兼容风险；新液体禁止工具设计或生成，必须走独立开发流程 |
| 原版契约 | [持久状态、顾客拒收、生长与养殖入口](native-contracts.md) |
| 植物 | [基因与生长专题](plant-growth-and-genes.md)：进度时序、基因迁移、健康影响 |
| 养殖 | [阶段与原料转换专题](aquaculture-stages.md)：原版状态归属、活死和加工边界 |

液体三个专题：[成分](liquid-components.md)、[容器](liquid-containers.md)、[保存与交易](liquid-persistence-trade.md)。混合类别只要涉及液体设计/操作，也受液体禁区约束。
