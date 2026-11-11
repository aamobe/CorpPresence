# 新增物品接入清单

公司增强以物品扩展为主。新增内容复用现有定义、身份、官方目录和配送流程，不新增一套加载器或注册协议。

物品模块属于[总框架](framework.md)：Main 安装及激活，RuntimeLifetime 负责释放，Maintenance 显示安装/清理状态，统一检查验证与其他模块的连接。接入物品还须核对获取、付款配送、保存恢复及表现路径。

2026-11-11：已在原有流程上增加带来源的定义汇总与真实编译内容检查。入口为 [Goods.Catalog](../../src/CorpPresence/Items/Goods.Definitions.cs) 和 [DefinitionSet](../../src/CorpPresence/Items/DefinitionSet.cs)。实施范围及证据见[物品框架实施](../changes/archive/organize-internal-lifecycle/item-framework-evidence.md)。

## 一件物品经过哪些入口

`Goods.Def / 生产线定义 → Goods 汇总 → ItemIdentity 身份 → ItemRegistry 官方工厂 → Goods.Make / ItemRegistry.Create → 订货或剧情发放`

| 要做的事 | 入口与约束 |
| --- | --- |
| 定义名称、用途、价值、占格、图像键 | [Goods.Def](../../src/CorpPresence/Items/Goods.cs) 与 [Catalog 汇总](../../src/CorpPresence/Items/Goods.Definitions.cs)；生产线已有 Defs 时把定义留在所属生产线。DefinitionSet 的初始化与 AddRange 均拒绝重复，报错含键和来源。 |
| 建立持久身份 | [ItemIdentity](../../src/CorpPresence/Items/ItemIdentity.cs)；独立 ID、旧 ID/贴图对、GoodsKey 或 Company。旧映射不能随重命名改变。 |
| 注册与生成 | [ItemRegistry](../../src/CorpPresence/Items/ItemRegistry.cs)；Install 监听目录，资源就绪后 Activate 注册，生成用已有入口。ID 冲突时保留别人，不直接覆盖官方目录；本机官方 Add 会先移除已有同 ID 条目，不能跳过前置检查。 |
| 配置文案 | [GoodsText](../../src/CorpPresence/Presentation/GoodsText.cs)、[本地化维护](localization-keys.md)；稳定键、内置回退与 Localization CSV 一起维护。 |
| 准备图像 | [素材规范](../../art/AGENTS.md)；先按类型阅读，再改素材。资源文件经项目 EmbeddedResource 嵌入，运行时图像键与资源名是不同概念，不能直接拿 LegacySprite 当文件名。 |
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

该入口先编译正式与维护 DLL，再执行包含 ItemChecks 的检查，最后完成本地化及正式/维护两份官方零权限扫描。实际步数以当次入口输出为准，实际范围见[总框架验收](framework.md)。

ItemChecks 的注册器测试链接真实 ItemRegistry，但目录、工厂产物及少量身份输入是替身；身份兼容检查则读取刚编译 DLL 的真实 ItemIdentity 表及 ResolveId，并与 `tools/checks/ItemChecks/IdentityBaseline.json` 的 90 条基线逐条比较。允许追加，拒绝删除或修改旧映射；重复 ID、无工厂类别或两种类别并存、歧义旧映射会报错。

该检查同时链接真实 RuntimeLifetime，覆盖等待、激活、回滚、重复停用、失败清理重试和重启。创建与旧档迁移必须确认本地及官方最终工厂仍归本模块所有，ID 存在本身不代表可用。清理失败不遗忘所有权，也不删除其他模块替换后的工厂。

上述所有权约束只针对 ItemIdentity 声明的独立身份。ItemRegistry.Create 同时用于原版原料、标签模板及奖励；这些 ID 保留既有官方目录创建路径，不要求由本模组注册。

公司注册工厂在官方入口后处理前检查结果：null 或错误身份明确失败，避免官方空值回退生成同 ID 占位物品、或重写错误身份后被误认成功。ItemRegistry.Create 对本模块这类失败返回 null；游戏直接调用注册工厂时会得到明确异常。成功仍经 DirectoryMaster.Item 完成官方后处理；原版工厂的回退语义不改。此保护不验证任意非空物品的全部功能，也不代替 Shell/Setup 的实际游戏验收。

**不要为通过检查直接重生成身份基线。** 新增条目在首个正式发布确认后单独审查并追加；改变旧映射需要明确迁移设计和旧档验证。该基线保护的是 99719c4 已有身份，不证明所有新增物品已经可生成。

ItemDefinitionChecks 读取本次正式 DLL 的 Catalog、ItemIdentity、GoodsText 与实际嵌入资源，检查名称、稳定文本键、定义到身份的创建命名、身份贴图键解析和自有图像。公司卡片身份单独核对公司及 card 图像。基础图像、19 组显式状态图像与电池半电/空电图像使用现有规则；共享定义及 candle_lit 是合法关系，不要求 GoodsKey 唯一。

检查不会执行 Shell/Setup/OnNew 或调用 Unity 工厂，不证明原版外观名称在实际游戏里可用，也不覆盖生产线所有动态资源访问、公司目录/剧情获取路径、视觉、占格、原版音效和加工结算。现有本地化检查继续覆盖语言回退和实际 CSV 解析，游戏行为仍须手测。

实际描述中的 Crate 或 Shell 表示独立壳体；两者都没有时，每个对应身份必须提供非空 LegacyId 作为创建基底。检查拒绝缺失/空白基底，但不会据此宣称原版基底存在或其工厂行为通过。

## 接入步骤与边界

1. 普通物品：在所属 DefinitionSet 添加 Def，填写非空 Name 与其他需要的字段；GoodsText 增加稳定名称键及实际非空描述对应的键，同时维护翻译资源。定义、名称、身份和图片由检查关联，获取入口仍需明确配置。
2. 身份：在 ItemIdentity 追加稳定记录；Goods.Make(key) 使用 `corp_presence_` 加 key 定位身份。旧记录和基线不改。LegacySprite 必须能通过 Catalog.TryGetArt 对应的 `corp_med_` 路径解析；公司卡片使用原有 `corp_card_` 路径。
3. 图像：默认 ArtSource=Embedded，Company/Art 映射到 `CorpPresence.art.<Company>.<Art>.png`，生产与工具共用 ArtResource.Name。Fallback 不能让缺自有图像通过检查。确实仅用原版外观才明确标 NativeSprite 并填写 Fallback；TintedNativeSprite 目前仅描述已有 fate_coin_ 染色路径，不能拿它给任意新物品添加染色行为。
4. 新生产线：将该线的 DefinitionSet 以固定来源名接入 Catalog 的 AddRange；不得在运行时改目录。批次存在冲突或非法定义时，在写入该批次任何条目前失败。最终目录提供只读字典视图；Def 的业务字段仍沿用原来的数据结构，不是深度不可变对象。
5. 设备：在所属线提供 Shell/Setup 等实际行为，Catalog 只保存描述与委托。Shell 创建壳体，Setup 在 Goods.Build 设置物品时运行；OnNew 仅由 Goods.Make 的新建流程调用，不用于重放读档奖励或清空原料。新增定义不能在静态初始化时调用游戏对象、读取存档或加载纹理，否则离线检查无法安全读取。
6. 容器：沿用专属创建与恢复入口，槽位只追加；库存、扩容、筛选和改造归所属模块。框架不会自动实现任意新容器的恢复。
7. 状态差分：按实际情况复用基础定义，加入明确 ExtraArt 或既有电池状态规则；不要为每张状态图伪造持久身份。扩展新的解析规则时，同时补真实解析入口的正负例。
8. 运行统一检查，再按上表选择专项测试和下方手测。依次验证取得物品、首次创建、使用/加工、保存重载；“可注册”不代表“可购买或可加工”。

开发时可单独编译 `tools/checks/ItemDefinitionChecks/Checks.csproj`，直接执行当次 Checks.dll 并传两个参数：正式 CorpPresence.dll 绝对路径、游戏 Managed 绝对路径。无参数模式只运行检查器自身的负例，不算真实内容验收。正式验收使用 Check-Maintenance 的完整入口。

## 游戏验收

用测试档分别生成普通物品、生产设备、容器，保存退出再读入，确认身份、数量、原料/库存和改造保留；以普通水和原版物品作对照，不能被识别为公司物品。检查新物品可获得、贴图/占格正确、失败不扣款，并按所属生产线测试加工。记录游戏构建、源码提交、DLL 哈希和实际结果。
