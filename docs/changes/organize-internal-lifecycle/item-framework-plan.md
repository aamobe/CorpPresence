# 既有物品框架补齐实施计划

使用 executing-plans 在当前分支执行，test-driven-development 验证新增失败契约。

目标：依托 Goods、ItemIdentity、ItemRegistry、GoodsText 和 Tex，提供可追踪的定义汇总与真实内容检查。设计见 [item-framework-design.md](item-framework-design.md)；用户已确认依托原有框架搭建。

## 全局约束

保留官方 IMod / netstandard2.1、已有身份、存档、顺序和业务逻辑；不增加注册系统，不运行游戏或部署。错误检查不调用 Unity 工厂，测试读取真实描述数据而非复制字典。

## 任务与文件

- [x] T1 定义汇总：新增 Items/DefinitionSet.cs，提供 Add(source,key,value)、AddRange(source,entries)、只读来源与字典视图。先在 tools/ItemDefinitionChecks 测试重复键、空键/空来源、批次冲突不部分写入及顺序，再实现。把 Goods 的定义初始化移到 Goods.Definitions.cs 的嵌套 Catalog；Goods 仍消费同一批 Def。保留运行态缓存初始化在 Goods，禁止检查触发 Goods 静态构造。
- [x] T2 真实描述检查：新增工具读取 Catalog、ItemIdentity、GoodsText 的真实数据及 DLL 资源，复用 Tex 的资源名规则。检查定义身份双向关联、公司身份单独分类、必需文本键、基础与额外图像；具名处理 candle_lit 等状态映射和动态电池资源。每条新增拒绝规则先用坏输入证明失败，再运行真实目录。无游戏对象生成。
- [x] T3 接入统一检查与回归：更新 Check-Maintenance 传入本次正式 DLL 和 Managed 给新增工具；运行全套、双版本零权限与入口拒绝检查。保留旧身份基线，独立审查后修正重要意见，更新接入说明、计划状态和证据。

检查命令：dotnet build tools/ItemDefinitionChecks/Checks.csproj --artifacts-path artifacts/item-definitions；直接执行该目录本次生成的 Checks.dll。正式 DLL 使用 Build/Check-Maintenance 显式指定 GameManaged。预期错误用例全部被拒绝，真实定义检查无未解释失败。

## 审查重点

批次失败不得留下半份定义；状态变体不能误判独立定义；原版回退不能掩盖缺自有资源；静态构造不得进入引擎；旧创建顺序和实例委托不得被检查执行。

## 进度与裁决

初始：b1e1dd2 的 26 步检查已通过，本次需按改动重跑。T1/T2 共享真实 Catalog 描述，T3 消费工具退出码；不创建第二套手工元数据表。

- T1 完成：重复键、表内重复与批次原子性先失败后通过；所有既有定义表采用 DefinitionSet，Catalog 与运行态分离。
- T2 完成：84 定义/90 身份/19 显式状态图像通过。手册原版外观与硬币染色采用明确 ArtSource；Catalog.TryGetArt 共用于渲染及身份贴图检查。审查发现的必需名称和贴图关联缺口按失败→通过修正。
- T3 完成：28 步、31 条新断言、41 条原身份/注册断言、37 条配送恢复断言及双版本零权限扫描通过；入口拒绝契约通过。证据见 [item-framework-evidence.md](item-framework-evidence.md)。游戏与发布未执行。
- 裁决：保留原版外观和共享状态的真实例外，不将它们改成新图像；旧配送替身脚本仍使用文本提取，只更新被移动的输入位置及空清单错误，新增真实关联检查不沿用该方法。
