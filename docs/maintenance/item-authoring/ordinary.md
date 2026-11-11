# 普通静态商品与固体材料

适用：无使用、容量、生命、生长、成分、设备或动态状态的静态商品。FOOD/MEDICAL等交易分类不自动赋予食用或注射行为。遵循[共用契约](common-contract.md)。

## 完整案例和接入点

[ordinary.json](../../../tools/ItemAuthoring/examples/ordinary.json)是临时样品；复用现有手套图，不是新增美术或发布商品。工具在隔离源码副本完成以下9处登记：

1. [Goods.Definitions](../../../src/CorpPresence/Items/Goods.Definitions.cs)的initial：Def与静态形状/报价。
2. [ItemIdentity](../../../src/CorpPresence/Items/ItemIdentity.cs)：Good(key,base)，ID固定为corp_presence_加key。
3. [GoodsText](../../../src/CorpPresence/Presentation/GoodsText.cs)：名称/描述稳定键。
4. [PastelCo](../../../src/CorpPresence/Companies/PastelCo.cs)：日用目录、会员门槛。
5. Translations.Keys.json和Migration.json：现有本地化元数据。
6. en/fr/ru三个CSV：缺俄法文时显式写英文回退。

现有Goods.Build借助目录基底创建物品，然后设置身份、文案、分类、图像和价值；购买仍经原有报价和配送入口。工具首版只接受已审阅的toothpaste基底及pastel日用目录，其他基底/公司拒绝并需人工扩展，不静默套用。

## 字段与状态

key为小写字母数字下划线；names/description至少中英；value为正整数基础价；width/height为库存格数，cells用#/.和|表示占格。PNG文件须按CorpPresence.art.pastel.<art>.png命名，像素大小不等于库存格数。minTier为1–3（帕斯泰尔实际会员等级）；不自动加入限购或堆叠能力。

| 状态/触发 | 守卫与变化 | 消耗/输出 |
| --- | --- | --- |
| 登记→新建 | 唯一key/ID、存在素材、合法形状 | 原有工厂创建一件；无OnNew隐藏逻辑 |
| 目录→购买 | 原有资格与价格后端复核 | 原有配送/扣款，不由工具生成第二份 |
| 保存→恢复 | 原版保存，ItemIdentity和ItemTags限定恢复 | 不清库存、不替换旧身份 |
| 售出/销毁 | 原版买家条件与归属 | 不额外补发或再次记账 |

## 反例、检查与限制

反例一：为液体加HOUSEHOLD_GOOD便当普通商品生成，仍会留下液体生命周期问题。反例二：声明FOOD就宣称可食用，实际未初始化热量和使用行为。

示例可直接Preview；审阅计划指纹后Apply，再Check。检查覆盖文本转义、九处完整登记、资源/身份和重复运行；实际新商品只需购买一次、放置与重载、语言切换的代表验证。工具演练不发布样品，也不宣称开发时间已量化提升。
