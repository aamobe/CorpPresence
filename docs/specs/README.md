# 功能目录

操作步骤见[游玩流程](../../公司增强游玩流程.txt)。下表用于定位维护代码；目录里的门槛和数值应与实际实现一起修改。

| 功能 | 规则与实现入口 |
| --- | --- |
| 人物章节、日常来访 | [Characters](../../src/CorpPresence/Characters)，`Engine.Scheduling`、`Engine.Dialogues`、`Engine.Trading` |
| 公司目录、会员、电话订货 | [Companies](../../src/CorpPresence/Companies)，`Company`、`CatalogEntry`、`Services.Orders`、`CompanyWindow.*` |
| 帕斯泰尔卷烟 | [Tobacco.cs](../../src/CorpPresence/Production/Tobacco.cs)、`Milo`、`PastelCo`、`CompanyWindow.Pastel` |
| 达夫雷食品与水产 | [Canning.cs](../../src/CorpPresence/Production/Canning.cs)、[Canning.Seafood.cs](../../src/CorpPresence/Production/Canning.Seafood.cs)、`Doreen`、`DavreCo` |
| 子午线定制锻造 | [Forge.cs](../../src/CorpPresence/Production/Forge.cs)、`Rosen`、`MeridianCo` |
| 卡迈克尔银血 | [SilverBlood.cs](../../src/CorpPresence/Production/SilverBlood.cs)及同名前缀文件，`Isa`、`CarmichaelCo` |
| 索拉纳香水 | [详细规则](perfume.md)，[Perfume.cs](../../src/CorpPresence/Production/Perfume.cs)及同名前缀文件，`Seline`、`SolanaCo` |
| 专用工作仓 | [五条产线的收纳与私人订购](production-work-bay.md) |
| 通用机器出料、扩容与外观 | [ProductionOutput.cs](../../src/CorpPresence/Production/ProductionOutput.cs)、[MachineUpgrade.cs](../../src/CorpPresence/Production/MachineUpgrade.cs)、[MachineAppearance.cs](../../src/CorpPresence/Production/MachineAppearance.cs) |
| 恶意巨企、订阅和阵营 | [Malice](../../src/CorpPresence/Malice)、`CompanyWindow.Malice` |
| 存档与卸载清理 | [Persistence](../../src/CorpPresence/Persistence)、`LoadedItemRepair` |

共用模块往往被多家公司调用。改UI文字时确认对应后端；改后端时检查其所有调用者。配置默认值另见[0.9.9默认难度](default-difficulty-0.9.9.md)。
