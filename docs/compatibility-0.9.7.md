# 0.9.7 兼容修复边界

> 历史版本记录：其中额外权限和跨模组钩子说明不代表当前版本。当前清单的Capabilities为空，维护时先读[当前兼容约束](maintenance/compatibility-and-regressions.md)。

发行为 `0.9.7compat`，文件版本 `0.9.7.2`。包含 `0.9.7fix` 的已有修复。

## 配送

装箱仍调用原版 `GraphUtils.TryAcceptAll`，由游戏及已安装的堆叠 Mod 决定落点和堆叠上限。公司只比较装箱前后同标识物品的总件数，以实际增加量确认成功；源物品合并后数量为零但尚未销毁，不再单凭对象归属误报失败。箱子上柜台成功后才核销订单。不设置每格一件，不修复历史丢货。

## Neon Undertow

以下是 0.9.7compat 的历史行为。当前源码已撤除该钩子。

针对本地检查的 0.15.9，在 `WaterSellerPack.NeonFish.Fish(GameItem)` 上增加可选前置钩子，只有 `FISH_CORP_SILVERBLOOD` 被排除。其余物种保留原判断。可选挂载延后至运行时首帧，避免公司先加载时查找不到稍后加载的 Neon。兼容钩子与公司 Harmony 实例一起卸载；类型缺失时跳过，接口变化或挂载异常时记录警告，不阻止公司启动。不修改第三方磁盘文件、既有基因或全局水缓存；对方其他独立规则不由此钩子接管。

## 回收范围

查询结果可以由第三方扩展，不能直接作为取货授权。电话银血回收在报价和取货时核对物品的容器祖先，只接受原版店内库存根节点下的物品。店内普通箱子仍可使用；原有机器内样品排除规则保留；异常循环祖先链拒绝。品牌及任务检测仍使用扩展拥有物品范围。

## 验证

- `tools/RestorationChecks/State/run.py`：30 项状态及配送检查，游戏接口替身。
- `dotnet run --project tools/ShopInventoryChecks/Checks.csproj -c Release`：7 项回收边界检查，游戏接口替身。
- 0.9.7compat 曾使用游戏 Harmony 执行 6 项可选兼容检查；旧检查已随钩子移除。

尚未完成实际游戏联合验证。新增 `reflection-external` 权限仅为可选跨 Mod 兼容钩子，更新应替换 DLL 和清单两个文件。
