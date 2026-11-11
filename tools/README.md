# 工具目录

静态检查脚本需要PowerShell 7或更高版本，以按大小写区分翻译JSON的键。正式构建仍由.NET SDK完成。

| 工具 | 用途 | 是否启动游戏 |
| --- | --- | --- |
| [AquacultureFlowChecks](AquacultureFlowChecks) | 银血水检列表隔离与延后死亡检查、逐件配送提交；直接编译流程辅助类 | 否 |
| [SealerChecks](SealerChecks) | 水产封装配方、模式标签恢复、满输出与普通电池失败回滚；库存、水和供电为替身 | 否 |
| [GameResourcesChecks](GameResourcesChecks) | 检查包括韩语在内的启动语言映射及切换等待；使用真实GameResources与游戏边界替代 | 否 |
| [Build.ps1](Build.ps1) | 编译正式模组，要求本地游戏引用 | 否 |
| [Check-ModPermissions.ps1](Check-ModPermissions.ps1) | 用已安装游戏的真实扫描器检查待发布目录、清单权限和拒绝项 | 否 |
| [Check-Repository.ps1](Check-Repository.ps1) | 检查本地文档链接、版本字段；可选核对发布源码快照 | 否 |
| [Check-Localization.ps1](Check-Localization.ps1) | 检查稳定键引用、四语言菜单、旧表一致性、格式/缺键回退；可核对编译后嵌入资源 | 否 |
| [Export-OfficialLocalization.ps1](Export-OfficialLocalization.ps1) | 将稳定键及译文导出为游戏官方CSV；会覆盖默认CSV | 否 |
| [Export-TranslationTemplate.ps1](Export-TranslationTemplate.ps1) | 导出稳定键和英文参考，供翻译协作及源码集成；运行时不加载 | 否 |
| [ShopInventoryChecks](ShopInventoryChecks) | 已有的库存/血液回收逻辑诊断程序，独立于正式模组构建 | 否 |
| [RestorationChecks/State](RestorationChecks/State) | 已有的配送、状态和恢复诊断；Python入口会生成临时代码并调用dotnet | 否 |

后两项是仓库已有的局部诊断，不能代替游戏内验证；本次目录整理未运行它们，也未据此宣称当前版本全面通过测试。不要把本地诊断入口或生成文件打进正式模组。

所有运行与发布操作见[构建与发布](../docs/maintenance/build-and-release.md)。

养殖列表检查：`dotnet run --project tools/AquacultureFlowChecks/Checks.csproj`。七项局部检查不覆盖原版养殖引擎、Harmony 安装和真实缸内占位。

水产封装检查使用 .NET 8 或更高 SDK：`dotnet run --project tools/SealerChecks/Checks.csproj`。直接编译当前封装与出料代码，15 项断言覆盖正常流程与扣电前后失败；故障注入时出现预期异常日志。它不验证真实游戏堆叠、外观、模块槽定位、转换器或超科技电池。

本地化检查用法见[本地化键维护](../docs/maintenance/localization-keys.md)，译者说明见[内置翻译](../docs/localization/README.md)。检查程序运行真实官方API适配层、内置回退、菜单与品牌缓存代码，只替代游戏本地化API/语言/帧、物品创建及图像等游戏边界，并使用安装游戏的实际CSV值解析器核对默认文件；不能代替游戏内字体、排版与语言切换验收。

## 芒果彩蛋替身检查

`dotnet run --project tools/MangoGiftChecks` 检查一次领取、投递事务、电话回调和背包工厂边界，不启动游戏。

## 内部维护检查

`./tools/Check-Maintenance.ps1 -GameManaged '<游戏 Managed>' -Dotnet '<SDK dotnet>' -Python '<Python>'` 固定串联维护相关检查并记录分层证据；失败非零退出，不启动游戏、部署或打包。`Check-MaintenanceContract.ps1` 使用相同参数验证总入口失败契约。详细使用及证据边界见[内部维护手册](../docs/maintenance/internal-maintenance.md)。
