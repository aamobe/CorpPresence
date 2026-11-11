# 原版契约核验记录

2026-11-12，基于发布源码 `9ce9689` 与原版 Assembly-CSharp SHA256 `725CB999D019DFF1FF31BED353C7F9BB8D81A7605A15DE1F80F5A3A6CF7B1D05`。证据为定向反编译及模组源码阅读，不是游戏实测。原版代码留在仓库外。游戏程序集变化后需重新核对这些入口。

## 保存和标签写入

| 入口 | 已核实行为 | 对新物品的要求 |
| --- | --- | --- |
| GameItem.ModifyTag | action 修改 `state.GetTag`，默认随后 ValidateShapeState | 持久修改走持久状态入口，不直接改展示副本 |
| GameItem.EnableTag/DisableTag | 改 state，默认重新校验 | skipValidate 会延迟刷新，不能把未刷新值当最终状态 |
| GameItem.GetTagReadonly | 从 modifiedState 读取并返回 Clone | 修改返回对象不会提交到物品；读取也不是任意状态写入接口 |
| GameItem 的形状/状态计算 | 以 state.Clone 为起点生成派生状态；最终赋给 modifiedState | modifiedState 是效果叠加后的读取视图，不应成为唯一保存载体 |
| SaveManager.EncodeNode | 保存 state.Clone 和 itemTypes 等字段 | 声称成分可保存前，先确认写入的是 state |
| SaveManager.DecodeNodesInternal | 目录工厂先创建，再用保存值克隆替换 state/modifiedState，继续恢复其他字段 | 工厂默认值不是迁移策略；恢复后再次初始化会清空玩家内容 |

液体 `CURRENT_PART_*` 通过 ModifyTag 写入时具备上述保存路径；仍依赖读档前成分注册、容量及恢复补丁正确。不能由此推导所有新成分、跨版本或缺模组存档都安全。完整液体边界见[液体参考](liquid-systems.md)，工具不能生成这类代码。

## 顾客拒收不是单一 WATER 判断

StoreClient.IsClientBuyingThisItem 首先调用 IsClientRefusingItem。拒收条件包括 NOT_FOR_RESALE、黑名单 ID/分类、禁止违禁品、重复品种、化学纯度不符合接受等级、危险废物、拒收危险水标识，以及奢侈品限制。未上锁且有内容窗口的非 ADV_GUN_TAG 容器，还递归检查内容是否能收购。

IsRefusingLuxuryItem：物品有 LUXURY_ITEM，顾客收购列表包含 WATER，且顾客既不是游客阵营也不是上层阵营时拒收。不是“所有水买家永远优先接受任何混合物”。

通过拒收后依次考虑 ALL_TYPE、明确收购 ID、ALL_TYPE_EXCEPT_CONTRABAND、收购标签和偏好委托。模组售水奖励是另一条入口，不能代替顾客实际收购判据。

## 植物生长时序

HydroponicHelper.ContinueGrowing 在进入时读取旧进度及目标，随后处理营养、污染、耗水、模块、植物 onGrowTick、基因 grow hooks，再按先前读到的目标更新种子显示进度。机器完成判定读机器进度；收获调用 FinishGrowing，再决定多年生下一轮或销毁非多年生种子。

因此[CropHealth](../../../src/CorpPresence/Production/CropHealth.cs)同步种子与机器目标，并在原版回调之后仅对烟草/油花同步种子显示进度。不能把该补丁泛化到所有植物。详细案例见[植物基因与生长](plant-growth-and-genes.md)。

## 养殖时序

AquacultureHelper.TickTank：断电健康损失 → AgeOne → 饥饿/喂食/饥荒 → 排泄与净化 → 供电过滤 → 水环境检查 → 死亡处理 → TryBreed → WanderStock。AgeOne 自身包含饥饿门槛和成长推进，不能简单理解为所有对象每晚固定年龄+1。

原版存在鱼卵、受精、雌雄、繁殖路径；但存在 API 不代表每种模组生物都支持所有路径。银血模组夜间回调只补充自己的血量与适应规则，不再次加龄。详见[养殖阶段与原料转换](aquaculture-stages.md)。

## 风险结论与尚未证明的内容

P0 所需持久写入、顾客拒收、生长回调与养殖所有权入口已核对。没有证明任何未来物品能在游戏中使用，也没有完成每种物种、植物、复杂设备的实现验收。新液体始终退出生成流程；其他复杂类别仅产生待办与人工参考，不伪装为普通静态物品接入。
