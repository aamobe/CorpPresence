# 物品开发工具

开发端工具，不编入Mod，也不随工坊分发。需要PowerShell 7、.NET 8 SDK和本仓库；Check还需要本机游戏Managed目录。使用SDK已有Roslyn，无额外NuGet依赖。

工具同时承担规范化作用：统一身份命名、文案键与转义、素材定位、公司目录接入和检查流程，让每件新物品有一致的审查依据。提速来自减少重复登记；规范化则减少漏项和写法分歧，方便后续排错、维护与交接。复杂类别的需求表统一状态、所有权和失败语义的记录方式，液体禁区明确独立开发边界；这不等于工具能保证任意物品实现正确。

首版自动接入**普通静态商品**，支持已核验的toothpaste基底、pastel公司日用目录。其他非液体类别输出六栏人工待办和参考链接，Apply/Check非成功退出；不会登记半成品。任何液体/成分/配方/液体容器请求都整单拒绝，三个模式都返回`LIQUID_REQUIRES_SEPARATE_DEVELOPMENT`，只能转[独立流程](../../docs/maintenance/item-authoring/liquid-systems.md)。不能用未知字段、组合类别或强制选项绕过。

## 普通商品流程

复制[输入示例](examples/ordinary.json)，按[schema](schema.json)填写。默认即Preview：

```powershell
./tools/ItemAuthoring/item-authoring.ps1 -Spec ./my-item.json -Preview
./tools/ItemAuthoring/item-authoring.ps1 -Spec ./my-item.json -Apply -ExpectedPlan '<Preview输出的PLAN哈希>'
./tools/ItemAuthoring/item-authoring.ps1 -Spec ./my-item.json -Check -GameManaged '<游戏Managed目录>'
```

`-Repository`指定目标源码副本，默认当前工具所属仓库。演练必须使用隔离副本，不在发布源码加入示例商品。`-Dotnet`可指定SDK路径；`-ToolCache`可指定目标仓库之外的工具构建缓存，默认位于工具仓库的父目录。缓存不随GitHub同步。脚本不启动游戏、不部署、不提交、不上传。

Preview只输出变更、原因、翻译回退与计划指纹。Apply必须携带审阅过的指纹；目标/依赖变化后重新预览。所有目标先验冲突，原始字节备份存入目标的`artifacts/item-authoring/backup-*`；写入异常回滚本次修改。若回滚时发现外部新修改，保留外部修改并报告备份位置，不能无条件覆盖用户工作。进程被强行终止时不保证自动回滚，可按备份manifest人工恢复；不要直接恢复到已经有后续编辑的文件。

重复应用相同输入不重复登记；若想改已接入商品，先人工审查已有代码，不用生成器静默覆盖。不同基底/公司需要独立核验并扩展工具，不用任意C#或Shell字段注入。

## 接入与检查

生成器编辑四处已识别C#初始化节点、两个翻译JSON和三个CSV，总计9处。文本分别按C#/JSON/CSV规则转义。名称/描述至少中英，缺俄法文时明确使用英文。素材必须先位于`src/Assets`，命名与现有嵌入资源规则一致；工具不生成/缩放美术。

Check要求已完整Apply，编译真实Mod后运行ItemDefinitionChecks、ItemChecks（允许追加身份的开发模式）和Check-Localization。只有它们全部成功才报告“代码接入检查通过”，不是“游戏可用”。发布身份基线只读，新物品上线前须另行人工审阅；Check不绕过发布门禁。

工具自身检查：

```powershell
./tools/Check-ItemAuthoring.ps1
```

功能参考、状态表、失败反例和必要实机项见[15类参考索引](../../docs/maintenance/item-authoring/README.md)。复杂类别的输入只是待办摘要，不能代替独立设计；所有液体示例仅用于拒绝验证，不提供配方参数。

## 限制

液体检测对输入声明采取保守拒绝，可能拒绝包含相关词的无害文案；请转人工审阅，不改写文案绕过真实能力限制。未知字段/类别不会被忽略。此校验不能证明用户另行手写的任意源码没有液体行为。源码布局变化、资源缺失和冲突均停止；最终仍应审阅diff。
