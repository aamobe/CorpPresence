# 原版接入作用域核查

当前是首批源码核查，不是全补丁清单或运行验收。安装以 Hooks.InstallCore、InstallGameplay、InstallResources 及单独安装器为依据；仍需继续核对下列未覆盖组。

| 原版入口 | Mod 处理 | 已核对的限制/退出 |
| --- | --- | --- |
| ResolveSpriteByName、ResolveItemSprite、SpriteDict.GetSprite、LoadFromAtlasRaw | Hooks 的精灵前缀 | 自有名片或 Goods.FindSprite 命中才替换；Goods 使用 corp_med_ 及两种公司植物专用键；机器覆盖先要求 corp_med_ |
| GameItem.BuildShapeFromSprite | Goods.BeforeBuildShape | 非 corp_med_ 立即交回原版；已登记定义、状态变体或专用素材才建立形状 |
| Image.IsRaycastLocationValid | Goods.AfterRaycastValid | 无透明度命中阈值立即退出；自有物品实际库存图像按格位检查，后备路径要求 corp_med_ 精灵 |
| PowerHelper.ChargeBattery 两个重载 | BeforeChargeBattery | 仅 anchor_cell 阻止充电，其他交回原版 |
| 更新电量、设置电量、抽电及充电后 | Goods.RefreshBattery | 仅自有定义且有 Power 配置时刷新公司电池精灵 |
| OnPortableWaterPurifierUsed | BeforePurifierUsed | 仅利维坦处理沟渠水时拦截；其他净水器或水质交回原版 |
| GameItem.GetNegociatedValue | AfterNegociatedValue | 尊重 __runOriginal；公司交易固定价/倍率及 Ext.BuyFactor 可影响原版物品，属于业务范围扩展，需要普通模式对照 |
| StorageExpansionHelper.RestoreTabs | ProductionWorkBay.BeforeRestoreNativeTabs | 仅两种仓库 ID 且已注册库存时接管；普通仓继续原版 |
| 养殖成长/游动/Fits、水检和 TickTank | SilverBlood | 物种及上下文限定，详见三期证据；原版仍推进成长 |

Main 使用 mod.<manifest.ID> 作为 Harmony 所有者，RuntimeLifetime 登记 UnpatchAll(_harmony.Id) 释放。这里确认的是卸载调用的所有者参数，未模拟游戏中其他 Harmony 所有者及启停时序。

尚待逐项核对：顾客/电话/日切事件、交易统计与售水计数、人物效果/健康/搜刮、输入拦截、枪械和切割、鼠笼单独安装器、注册与解码补丁、所有可选方法的目标版本匹配。WATER_TRACE 与 TESTKIT 条件代码须与正式构建区分，不能把源码存在等同于发布安装。

## 输入与电话补充

InputShield 的全局输入、射线与双击前缀均以 Blocking 为门槛；该值只在分类窗活动、指针位于公司窗或公司窗拖动时为真。Escape 优先关闭分类窗，其次公司窗，否则放行。CompanyWindow.Dragging 在不可见时为 false；Tick 更新 Pointer 的可见性，Close 隐藏遮挡层并释放面板纹理，ReleaseUi 另清空拖动状态和缓存。关闭到下次 Tick 之间的指针缓存尚未实测，不据此宣称零帧延迟。

电话对话与名称后处理通过 Companies.ByLocId 识别公司；接听判定通过 ByNumber，未识别号码不改结果。普通电话不会被这些分支直接替换。拒接提示与电话簿扩展仍需继续追踪其内部过滤。
