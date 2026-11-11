# 液体读档分类修复

2026-11-12，基于d16f9d8。用户要求先处理分类方案审查发现的危险代码，本轮只修复静态分类覆盖液体动态分类这一条路径。

## 根因与改动

银血和异构蛋白瓶定义Types为MEDICAL。新建灌装会通过原版OnLiquidChanged设置非空容器的WATER，但读档后的ItemTags.AfterDecode调用Repair，再由Apply清除全部商品分类并只写回静态定义，遗漏原版空/满容器分类。

ItemTags.Apply现在在静态分类完成后，仅当对象具有LIQUID_CONTAINER_TAG时调用WaterHelper.UpdateContainerGoodType。该原版方法按照容器基础价格标签及实际空满状态选择WATER/HOUSEHOLD_GOOD；不按water成分是否非零猜测分类，不硬编码所有液体永远WATER。Apply的生产调用位于自有Goods.Build及有已知身份定义的Repair，没有对原版全局液体方法打补丁。

不调用InitLiquidContainerItem或OnLiquidChanged，避免重新填充、清空成分、重新估价或改变已保存鉴定状态。创建与读档共用此修复。不会改变原版纯度、顾客偏好或模组售水奖励条件。

## 证据

原版方法已按当前游戏程序集反编译核对，SHA256为725CB999D019DFF1FF31BED353C7F9BB8D81A7605A15DE1F80F5A3A6CF7B1D05。

新增ItemTagChecks链接实际ItemTags。修复前第一项按预期失败：native classification must be restored once；修复后8项全部通过：装有银血、空异构瓶、无water但有其他成分、重复修复、新建Apply、普通物品、原版/外部容器隔离、嵌套AfterDecode。对state/modifiedState中的成分、容量、温度与unitValue检查未改写。证据在本机artifacts/item-tags-red.log及item-tags-green.log。

WaterHelper为调用边界替身，返回预定空/满分类，不复制原版比例或分类实现；这些检查证明真实ItemTags正确委托分类并保留状态，不等于原版Unity代码实机执行通过。检查已接入统一维护和便携入口。

## 最小游戏验收（未执行）

部署获准后，用正常测试档记录一瓶有内容银血的各成分量、等级及商品分类，保存重读，确认成分不变且分类仍含WATER。再倒空，确认原版切回空容器分类；原版水瓶作对照。若验证交易，选满足原版收购条件的顾客，不以任意顾客拒收推断修复失败。该场景替换液体相关验收项，不扩展成全容器穷举。

结论限于代码层分类丢失路径已修正；尚不能认定其是此前全部售水异常的唯一根因。未部署、未启动游戏、未发布。

## 本轮完整验证结果

- 维护入口36项全部通过（新增ItemTagChecks的编译/执行两步），包含普通/维护真实游戏引用编译、其余替身回归、本地化及两份原版权限扫描。证据：`artifacts/maintenance-checks/20261111-191052-39fdbc1f/`。
- 修改后的便携入口通过：`artifacts/portable-checks/3deec15131f04f22bc49ccb233b4711a/`；与维护入口重复部分不算额外覆盖率。
- 维护入口失败契约检查通过，缺SDK、缺Managed、子命令失败均非零退出且不输出成功；本机记录：`artifacts/liquid-tag-contract.log`。
- 文档与空白检查通过。未执行游戏实测、部署或发布核验。

## 合并前独立审查边界

审查未发现阻断项。原版UpdateContainerGoodType并非绝对无副作用：HOUSEHOLD_GOOD分类变化且对象位于背包根部时，它会刷新相关事件特性；读取缺失成分标签也可能生成默认标签条目。当前Goods.Build创建新对象，DecodeNodes返回时尚未将库存根挂回场景；未据此宣称所有调用情形都不会触发上述原版行为。替身检查中的字典不变证明ItemTags自身不灌装、不清空或重写成分，不是原版方法完全无状态写入的证明。实际背包事件仍以原版分类方法语义为准。

## 远端目录整理后的整合验证

合并前读取GitHub，主分支新增dc04e0c目录整理；保留远端结构，将新增检查放入tools/checks/ItemTagChecks，并调整源码相对引用。生产修复不变。整合后36项维护检查再次全部通过，证据为`artifacts/maintenance-checks/20261111-191803-304c4472/`；便携检查通过，证据为`artifacts/portable-checks/12a99ce1d7624eb3b2085ee3ff08c6db/`；入口失败契约再次通过。独立只读审查确认无合并阻断项。
