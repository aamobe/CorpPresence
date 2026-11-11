# 新增物品接入清单

公司增强以物品扩展为主。新增内容复用现有定义、身份、官方目录和配送流程，不新增一套加载器或注册协议。

## 一件物品经过哪些入口

`Goods.Def / 生产线定义 → Goods 汇总 → ItemIdentity 身份 → ItemRegistry 官方工厂 → Goods.Make / ItemRegistry.Create → 订货或剧情发放`

| 要做的事 | 入口与约束 |
| --- | --- |
| 定义名称、用途、价值、占格、图像键 | [Goods.Def 与汇总](../../src/CorpPresence/Items/Goods.cs)；生产线已有 Defs 时把定义留在所属生产线。检查合并键不会覆盖已有定义。 |
| 建立持久身份 | [ItemIdentity](../../src/CorpPresence/Items/ItemIdentity.cs)；独立 ID、旧 ID/贴图对、GoodsKey 或 Company。旧映射不能随重命名改变。 |
| 注册与生成 | [ItemRegistry](../../src/CorpPresence/Items/ItemRegistry.cs)；Install 监听目录，资源就绪后 Activate 注册，生成用已有入口。ID 冲突时保留别人，不直接覆盖官方目录；本机官方 Add 会先移除已有同 ID 条目，不能跳过前置检查。 |
| 配置文案 | [GoodsText](../../src/CorpPresence/Presentation/GoodsText.cs)、[本地化维护](localization-keys.md)；稳定键、内置回退与 Localization CSV 一起维护。 |
| 准备图像 | [素材规范](../probably-stolen-art/AGENTS.md)；先按类型阅读，再改素材。资源文件经项目 EmbeddedResource 嵌入，运行时图像键与资源名是不同概念，不能直接拿 LegacySprite 当文件名。 |
| 放入目录或剧情 | [公司目录](../../src/CorpPresence/Companies/CatalogEntry.cs)、所属公司/剧情入口；可购买门槛与提交校验一致，沿用付款和发货流程。 |
| 机器、容器或可用物品 | 所属 Production 文件、Setup/OnNew/交互入口；先明确新建与读档恢复的区别。新增槽位只能追加，不能重排旧槽。 |
| 旧档与卸载 | [兼容约束](compatibility-and-regressions.md)、[注册器迁移](../../src/CorpPresence/Items/ItemRegistry.cs)；只按已知旧身份配对迁移，不清档、不重发整单、不收养原版物品。 |

## 按物品类型选现有例子

- 普通用品：Goods 中 `stamina`。看定义、身份、使用入口及文本；已有 ID 和旧映射只作为参考，不能复制给新物品。
- 生产设备：[ProductionWorkBay](../../src/CorpPresence/Production/ProductionWorkBay.cs)。看独立壳体、初始化和旧物品恢复；机器额外规则仍归该生产线。
- 容器：[CargoWarehouse](../../src/CorpPresence/Production/CargoWarehouse.cs)。看库存槽、扩容、排序与持久化，不能把普通物品的最小清单当作容器验收。
- 状态差分：Goods 的 `candle` / `candle_lit`。区分显示状态和持久身份；同一 GoodsKey 可以对应多个明确的身份，不能一律按 GoodsKey 唯一来检查。

## 自动检查

推荐运行统一入口，使用本机真实依赖路径：

```powershell
./tools/Check-Maintenance.ps1 -GameManaged '<游戏 Managed>' -Dotnet '<SDK dotnet>' -Python '<Python>'
```

该入口先编译正式与维护 DLL，再执行包含 ItemChecks 的检查，最后完成本地化及正式版官方扫描。维护版单独运行零权限扫描，命令见[框架验证](../changes/organize-internal-lifecycle/verify.md)。

ItemChecks 的注册器测试链接真实 ItemRegistry，但目录、工厂产物及少量身份输入是替身；身份兼容检查则读取刚编译 DLL 的真实 ItemIdentity 表及 ResolveId，并与 `tools/ItemChecks/IdentityBaseline.json` 的 90 条基线逐条比较。允许追加，拒绝删除或修改旧映射；重复 ID、无工厂类别或两种类别并存、歧义旧映射会报错。

**不要为通过检查直接重生成身份基线。** 新增条目在首个正式发布确认后单独审查并追加；改变旧映射需要明确迁移设计和旧档验证。该基线保护的是 99719c4 已有身份，不证明所有新增物品已经可生成。

现有本地化检查覆盖键、语言回退和实际 CSV 解析；本期没有完成所有 Goods 定义、所有贴图资源与所有公司目录的自动关联校验。视觉、防杂色、占格、原版材质/音效、加工结算及真实旧档恢复仍需各物品的检查和游戏手测。

## 游戏验收

用测试档分别生成普通物品、生产设备、容器，保存退出再读入，确认身份、数量、原料/库存和改造保留；以普通水和原版物品作对照，不能被识别为公司物品。检查新物品可获得、贴图/占格正确、失败不扣款，并按所属生产线测试加工。记录游戏构建、源码提交、DLL 哈希和实际结果。
