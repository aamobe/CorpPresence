# 本地化键维护

本轮基线是2026-11-08重新确认的main提交`adf24fdfb47054f22f860428b3f357a0d267302c`，版本仍为0.9.9香水线及索拉纳会员目录修复。此文记录尚未发布的开发改动，不更新已发布源码快照。

**最终状态：既有本地化调用及自有动态文案已完成稳定键迁移，累计1621键。源码中的旧`L.T`/`L.Translate`消费调用为0；旧接口和旧表保留兼容。**下方前三批数据是实施历史，当前范围以“最终收尾”和“迁移完成状态”为准。

**外部翻译包已实现（1.0.0）：**玩家可在DLL旁`Translations`放UTF-8 JSON，按合法语言代码提供译文，并用`language.json`选择游戏菜单没有的Mod语言。无需重新编译译文；使用方式、完整模板导出和回退规则见[玩家说明](../localization/README.md)。

外部包功能验证：35组检查通过（含完整模板真实加载和实际Release资源），Release编译0警告、0错误，最终独立审阅通过。未启动游戏；不将托管边界检查视为游戏内实测。

## 查询与回退

```csharp
L.K("ui.storyGuide.previous", "上一章节", "Previous");
L.K("ui.checkout.cash", (FormattableString)$"现金 {Cash}₵",
    (FormattableString)$"Cash {Cash}₵");
```

键采用`区域.模块.用途`命名，如`ui.menu.solana.perfumery`，不以英文原文、数组位置或句子哈希命名。改文案时保留原用途的键；含义不同则使用不同键。

- 无外部包时，中文继续返回调用处中文原文；英文和不支持的语言返回调用处英文原文。
- 外部完整语言代码、父语言优先；之后俄法查[Translations.Keys.json](../../src/CorpPresence/Presentation/Translations.Keys.json)，数组仍为`[ru, fr]`。未命中、null、空翻译或短数组时，查原[Translations.json](../../src/CorpPresence/Presentation/Translations.json)，最后回退英文；中文缺键保留中文。资源读取异常也沿用该回退。
- 两张表分别懒加载，使用固定嵌入资源名`CorpPresence.Translations.json`和`CorpPresence.Translations.Keys.json`。
- 插值文本需要**显式转换两个参数为`FormattableString`**，否则C#会选择string重载，模板会提前格式化。俄法先翻译英文模板，再用原参数和当前文化格式化；格式错误回退原英文格式化结果。中文模板中的参数顺序可与英文不同，不把中文占位索引套到英文上。
- `L.T`、`L.Translate`及其数组调用继续兼容。旧翻译表保持原样，不删除已迁移条目，因为其他旧调用可能共享同一原文。

需要按已有模板及参数格式化时，使用`L.F(key, zhFormat, enFormat, args...)`；指定语言的内部适配使用`TextTranslations.FormatKey`。不要在取得外部模板后自行`string.Format(L.K(...), args)`，否则实际类型不兼容的格式可能绕过回退。外部格式化会检查原编号、支持重排/重复，并在实际`FormatException`时尝试父语言及内置文本。已有先插值行为由`KRendered`保留无包输出，同时允许外部模板获取原参数。

第二批增加兼容既有调用形态的重载，原语言分支不需要重排：

```csharp
// 原来 L.En ? L.Translate(template) : chinese 的分支保留。
L.En ? L.K("ui.davre.orders.total", (FormattableString)$"Total {num6}₵") : $"合计 {num6}₵";
// 原来 L.T(已格式化中文, L.Translate(英文模板)) 保留中文先行格式化。
L.K("ui.carmichael.bloodRecovery.availableAgainDay", $"第 {nextDay} 天可再次回收",
    (FormattableString)$"Available again on day {nextDay}");
```

两参数`L.K(key, en)`对应原`L.Translate(en)`，即使在中文环境调用，也像旧入口一样返回英文回退；仅在原代码已有中文分支时使用。混合重载`L.K(key, string zh, FormattableString en)`保留中文字符串已经格式化的调用方式。插值英文仍须显式转换为`FormattableString`；已有三参数双模板重载的调用无需改写。

中英回退文本仍保留在源码中，内置稳定表承载俄法译文；外部包按语言代码使用独立的键到字符串字典，可覆盖中英及新增语言。修改英文原文不会使稳定键失效；若含义或占位符改变，须同步审查对应译文。

## 第一批实际范围

| 文件 | 迁移内容 |
| --- | --- |
| [MenuLabels.cs](../../src/CorpPresence/UI/MenuLabels.cs) | 12组、42个菜单位置；原中英数组、顺序、长度和按语言懒缓存保留，仅加平行展示键 |
| [CompanyWindow.StoryGuide.cs](../../src/CorpPresence/UI/CompanyWindow.StoryGuide.cs) | 8处指引标题、剧透确认、导航及当前节点提示 |
| [CompanyWindow.cs](../../src/CorpPresence/UI/CompanyWindow.cs) | 5处现金、代金券、下一档提示、最高档和指引入口 |
| [CompanyWindow.Catalog.cs](../../src/CorpPresence/UI/CompanyWindow.Catalog.cs) | 9处表头、标价、收录进度、空状态和选中提示 |

合计64个展示位置、54个不同键。53键原有俄法翻译原样复制；`ui.menu.solana.perfumery`原本没有俄法译文，保留空数组及英文`Perfumery`回退，没有借本次迁移补译或改文案。

## 第二批实际范围

| 文件 | 本批迁移调用数 |
| --- | ---: |
| [CompanyWindow.Carmichael.cs](../../src/CorpPresence/UI/CompanyWindow.Carmichael.cs) | 46 |
| [CompanyWindow.Davre.cs](../../src/CorpPresence/UI/CompanyWindow.Davre.cs) | 27 |
| [CompanyWindow.Jackson.cs](../../src/CorpPresence/UI/CompanyWindow.Jackson.cs) | 2 |
| [CompanyWindow.Malice.cs](../../src/CorpPresence/UI/CompanyWindow.Malice.cs) | 67 |
| [CompanyWindow.Meridian.cs](../../src/CorpPresence/UI/CompanyWindow.Meridian.cs) | 32 |
| [CompanyWindow.Pastel.cs](../../src/CorpPresence/UI/CompanyWindow.Pastel.cs) | 45 |
| [CompanyWindow.Solana.cs](../../src/CorpPresence/UI/CompanyWindow.Solana.cs) | 15 |
| [CompanyWindow.Spacer.cs](../../src/CorpPresence/UI/CompanyWindow.Spacer.cs) | 23 |
| [Notebook.cs](../../src/CorpPresence/UI/Notebook.cs) | 1 |

本批迁移258处调用，新增250个用途键，累计304键、322个展示位置。嵌套`L.T(zh, L.Translate(en))`合为一个键调用，因此本批实际消除271处旧API出现。UI目录已没有`L.T`/`L.Translate`直接调用。

248个新键原样复制既有俄法翻译；索拉纳香水供应说明和大小写为`Place Order`的提交按钮原本缺译，继续回退原英文。第一批54键的翻译值和旧`Translations.json`保持不变。索拉纳菜单括号与价格倍率的两处纯标点/数字`L.En`分支原样保留，没有借迁移改译文或数字格式。

仅修改展示查询：原`L.En`分支、中文拼接、格式参数表达式、`0.0`/`0.#`/`P0`等格式符、空格/换行、按钮条件、会员门槛、付款回调、任务状态和内部选页标识保留。Notebook仅迁移“记事簿”标题，联系人和翻页逻辑不变。

没有把原版游戏键、物品ID、资源名、存档字段、配方成分、任务状态、配置键或UI内部选页标识改成本地化键。普通水、原版顾客规则、会员门槛、订单付款与生产逻辑未修改。

## 第三批实际范围

公司服务与提示迁移16个文件中的158处调用，覆盖171个展示文本，新增168键，累计472键、493个展示位置。包括服务/付款/限购/预约/换货/夜间提示、公司资料、接线文案和来访固定对白。9个档位/接线文案数组改为逐项键查询，数组长度、顺序和四语言文本保留。

167个新键从旧表原样复制俄法译文；香水供应需金卡的提示原本缺译，继续回退英文。前两批304键的翻译值不变，原`Translations.json`字节哈希不变。

本批没有改`L`或`TextTranslations`。16个修改文件将键调用反向恢复后与批前快照全文一致；订单付款、金额/数量/额度校验、会员门槛、状态写入、配送回调和物品身份不变。

第三批结束时，公司目录暂留9处动态入口，以免把不同内容误共用同一翻译；这些入口现已在最终收尾中从定义端完成迁移：

| 位置 | 剩余内容 |
| --- | --- |
| `CatalogEntry.Name` | 商品`Label/LabelEn`，需要从文案定义端逐项分配键，保留商品身份 |
| `CompanyVisitors.Courier` | `item`、`text`、`text2`三处按公司/场景动态选出的文本 |
| `JacksonContact.FrontDesk` | `Greeting/GreetingEn`数组 |
| `MeridianCo`成色显示 | 按级别选择的`LadderName/LadderNameEn` |
| `PastelCo` | `LimitedFlavor.Zh/En`动态物品说明 |
| `SpacerCo` | `TechCreditsEn`中的Company/Tech两处动态文本 |

当时上述9处沿用原翻译逻辑；最终收尾已迁移其生产端及静态表，保留商品ID、级别和运行时变量的原用途。

## 最终收尾

用户授权持续到完成后，继续迁移了生产线、人物对话、恶意模式、剧情指引、设置及其余固定展示文本：39文件、785处调用、793个展示文本，新增774键。该阶段逐调用逆向还原后，与阶段前39文件全文一致。

随后完成全部动态入口，新增375键：

- [GoodsText.cs](../../src/CorpPresence/Presentation/GoodsText.cs)为78个既有物品定义显式绑定231个名称/风味/描述/手册/模组说明键。物品ID只用来关联独立展示元数据，不修改ID、存档、注册、成分或配方。定义完成初始化后统一绑定，未登记的外部定义保持旧回退。前后78份定义数据快照完全一致，包括原字符串、数值、类型、形状等捕获字段。
- 目录Label增加独立`LabelKey`；杰克逊接线数组、子午线成色数组、星际客技术署名、海明威说明和设置周期标题/提示均从定义端绑定键，共38个展示字段。数组顺序、返回引用语义、配置存储键和选项值保留。
- [ConfigDisplay.cs](../../src/CorpPresence/Presentation/ConfigDisplay.cs)将77个已有配置显示身份映射到独立稳定键。旧section名称、`entry.Key`、选项保存值不变。外部或未知值沿用旧表回退，不为翻译而改配置身份。
- 配送文案的22个英文分支在生产处查键，中文分支原样保留；原末端英文查表不再需要。7个拼接句改成完整带参数模板，俄法模板从旧片段的已有译文按原顺序组合，保留每个动态参数表达式及中文拼接。不是借迁移重新翻译句子。
- 机器说明及旧手册标题识别使用对应展示元数据；原匹配文本与兼容范围保留。`L.K(string[] keys, string[] zh, string[] en)`保持原数组的中文/英文引用、俄法逐项翻译及短键数组回退。

累计1621个用途键，原有472键值和旧`Translations.json`字节均不变。有67键按原来行为回退，不表示本项目已补齐所有俄法翻译。油花浆产量有一处原先先插值再查表的调用，仍保留先插值行为，未趁迁移启用以前未显示的模板译文。

只剩语言格式装饰分支：人物动作的斜体括号、索拉纳菜单括号及数值倍率单位；其内容参数已经来自稳定键或原数值，原标点和格式保留。内部日志、资源名、物品/任务/存档/配方身份及原版游戏文案不在此次替换范围。

## 验证

```powershell
./tools/Check-Repository.ps1
./tools/Build.ps1 -GameManaged '<游戏目录>/Probably Stolen_Data/Managed'
./tools/Check-Localization.ps1 -GameManaged '<游戏目录>/Probably Stolen_Data/Managed' -CompiledAssembly './artifacts/build/bin/CorpPresence/release/CorpPresence.dll'
```

定向检查链接真实`L`、`TextTranslations`、`MenuLabels`、配置显示适配器、物品键元数据和真实嵌入JSON，只替代游戏语言/帧边界及元数据绑定所需的最小定义壳。覆盖稳定键脱离英文原文、缺键/空翻译/短数组回退、错误格式及参数顺序、语言切换、1621键与原译文行为一致、42菜单位置的四语言文本和缓存，以及编译后实际嵌入资源内容。

[Migration.json](../../tools/LocalizationChecks/Migration.json)记录已迁移键到迁移时英文模板的对应关系，仅供检查，不嵌入正式模组。[MenuBaseline.json](../../tools/LocalizationChecks/MenuBaseline.json)固定迁移前四语言菜单输出。后续有意改文案时应同步审查这些检查数据，不能为了让检查通过而自动覆盖基线。

[Batch2Cases.json](../../tools/LocalizationChecks/Batch2Cases.json)记录第二批258处调用的中英模板和原调用形态，逐项比较旧接口和新接口在中英俄法、`en-US`/`fr-FR`数字文化下的输出（2064项组合）。本地迁移过程另按逐调用记录反向恢复9个目标文件，与批前快照全文比较一致；该过程检查展示调用以外的代码没有变化，不把它当作游戏实测。

[Batch3Cases.json](../../tools/LocalizationChecks/Batch3Cases.json)记录第三批171个展示文本，以同样四语言和两种文化比较1368项组合。三处中文拼接表达式保持原源码，检查程序用示例字符串验证混合重载边界，实际参数和拼接顺序另由源码反向比较核对。

已完成静态文本/差异检查、定向程序和Release编译。未启动游戏，未验证实际字体、布局、点击交互或真实游戏内切语言；未部署、打包或发布。

第二批结果：13组定向检查全部通过（含编译后嵌入资源），Release编译0警告、0错误；独立审阅无可行动问题，并独立核对了91个英文及13个中文插值模板。静态检查38份Markdown及274个本地链接通过。

第三批结果：15组检查全部通过（含编译后资源），Release编译0警告、0错误；独立审阅确认16文件的原逻辑和参数、9个数组22元素及顺序、171个模板均保持一致。静态检查39份Markdown及277个本地链接通过。

最终结果：23组检查全部通过（含编译后资源），Release编译0警告、0错误。固定批[Batch4Cases.json](../../tools/LocalizationChecks/Batch4Cases.json)、物品字段[DefinitionCases.json](../../tools/LocalizationChecks/DefinitionCases.json)、动态字段[DynamicCases.json](../../tools/LocalizationChecks/DynamicCases.json)、配置[ConfigCases.json](../../tools/LocalizationChecks/ConfigCases.json)、配送[CourierCases.json](../../tools/LocalizationChecks/CourierCases.json)逐项比较四语言和两种数字文化；[Composed.json](../../tools/LocalizationChecks/Composed.json)独立记录旧句子片段，用于核对组合后的俄法模板。检查数据均不嵌入正式模组。78份定义数据的核对只初始化纯托管定义字典，没有调用模组生命周期或游戏运行入口。

最终独立审阅未发现可行动问题，另用Roslyn核对固定展示模板、动态参数、元数据绑定和数组顺序。静态检查40份Markdown、286个本地链接通过。

## 迁移完成状态

第一批后1333处、第二批后1062处、第三批后886处旧入口，最终已全部完成。上述计数是API调用出现次数，不是独立文案数。

| 目录/文件 | L.T | L.Translate |
| --- | ---: | ---: |
| 全部源码消费点 | 0 | 0 |

后续工作是按需要补齐原有缺译，以及游戏内字体、排版、点击交互和语言切换实测；它们不属于本轮保持文案不变的键迁移。配置外部值和未登记外部物品保持旧回退，旧接口不删除。本轮没有启动游戏、部署、打包或发布。
