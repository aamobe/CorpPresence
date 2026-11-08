# 工具目录

静态检查脚本需要PowerShell 7或更高版本，以按大小写区分翻译JSON的键。正式构建仍由.NET SDK完成。

| 工具 | 用途 | 是否启动游戏 |
| --- | --- | --- |
| [Build.ps1](Build.ps1) | 编译正式模组，要求本地游戏引用 | 否 |
| [Check-Repository.ps1](Check-Repository.ps1) | 检查本地文档链接、版本字段；可选核对发布源码快照 | 否 |
| [ShopInventoryChecks](ShopInventoryChecks) | 已有的库存/血液回收逻辑诊断程序，独立于正式模组构建 | 否 |
| [RestorationChecks/State](RestorationChecks/State) | 已有的配送、状态和恢复诊断；Python入口会生成临时代码并调用dotnet | 否 |

后两项是仓库已有的局部诊断，不能代替游戏内验证；本次目录整理未运行它们，也未据此宣称当前版本全面通过测试。不要把本地诊断入口或生成文件打进正式模组。

所有运行与发布操作见[构建与发布](../docs/maintenance/build-and-release.md)。
