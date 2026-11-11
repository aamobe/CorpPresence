# 工具目录

本目录外层是入口脚本；回归检查工程在 [checks/](checks/README.md) 下，每个目录一个。

物品框架新增 `ItemDefinitionChecks`：读取正式 DLL 的真实定义/身份/文本/资源，并验证重复定义、缺名称/键/图像和身份贴图解析。`Check-Maintenance.ps1` 已包含构建和执行该工具两步（实际步数以该次入口输出为准，含正式/维护双扫描）。单独执行编译后的 Checks.dll 时传正式 DLL 和 Managed 两个路径；无参数只测检查器负例，不算真实内容验收。详见[物品接入](../docs/maintenance/item-extension.md)。

静态检查脚本需要PowerShell 7或更高版本，以按大小写区分翻译JSON的键。正式构建仍由.NET SDK完成。

| 工具 | 用途 | 是否启动游戏 |
| --- | --- | --- |
| [AquacultureFlowChecks](checks/AquacultureFlowChecks) | 银血水检列表隔离与延后死亡检查、逐件配送提交；直接编译流程辅助类 | 否 |
| [SealerChecks](checks/SealerChecks) | 水产封装配方、模式标签恢复、满输出与普通电池失败回滚；库存、水和供电为替身 | 否 |
| [GameResourcesChecks](checks/GameResourcesChecks) | 检查包括韩语在内的启动语言映射及切换等待；使用真实GameResources与游戏边界替代 | 否 |
| [Build.ps1](Build.ps1) | 编译正式模组，要求本地游戏引用 | 否 |
| [Check-ModPermissions.ps1](Check-ModPermissions.ps1) | 用已安装游戏的真实扫描器检查待发布目录、清单权限和拒绝项 | 否 |
| [Check-Repository.ps1](Check-Repository.ps1) | 检查本地文档链接、版本字段；可选核对发布源码快照 | 否 |
| [Check-Localization.ps1](Check-Localization.ps1) | 检查稳定键引用、四语言菜单、旧表一致性、格式/缺键回退；可核对编译后嵌入资源 | 否 |
| [Export-OfficialLocalization.ps1](Export-OfficialLocalization.ps1) | 将稳定键及译文导出为游戏官方CSV；会覆盖默认CSV | 否 |
| [Export-TranslationTemplate.ps1](Export-TranslationTemplate.ps1) | 导出稳定键和英文参考，供翻译协作及源码集成；运行时不加载 | 否 |
| [ShopInventoryChecks](checks/ShopInventoryChecks) | 已有的库存/血液回收逻辑诊断程序，独立于正式模组构建 | 否 |
| [RestorationChecks/State](checks/RestorationChecks/State) | 已有的配送、状态和恢复诊断；Python入口会生成临时代码并调用dotnet | 否 |

后两项是仓库已有的局部诊断，不能代替游戏内验证；本次目录整理未运行它们，也未据此宣称当前版本全面通过测试。不要把本地诊断入口或生成文件打进正式模组。

所有运行与发布操作见[构建与发布](../docs/maintenance/build-and-release.md)。

养殖列表检查：`dotnet run --project tools/checks/AquacultureFlowChecks/Checks.csproj`。七项局部检查不覆盖原版养殖引擎、Harmony 安装和真实缸内占位。

水产封装检查使用 .NET 8 或更高 SDK：`dotnet run --project tools/checks/SealerChecks/Checks.csproj`。直接编译当前封装与出料代码，15 项断言覆盖正常流程与扣电前后失败；故障注入时出现预期异常日志。它不验证真实游戏堆叠、外观、模块槽定位、转换器或超科技电池。

本地化检查用法见[本地化键维护](../docs/maintenance/localization-keys.md)，译者说明见[内置翻译](../docs/localization/README.md)。检查程序运行真实官方API适配层、内置回退、菜单与品牌缓存代码，只替代游戏本地化API/语言/帧、物品创建及图像等游戏边界，并使用安装游戏的实际CSV值解析器核对默认文件；不能代替游戏内字体、排版与语言切换验收。

## 芒果彩蛋替身检查

`dotnet run --project tools/checks/MangoGiftChecks` 检查一次领取、投递事务、电话回调和背包工厂边界，不启动游戏。

## 内部维护检查

`Check-Portable.ps1 -Dotnet '<SDK dotnet>'` 在没有游戏程序集的机器上运行文档静态检查及 RuntimeLifetime、Extension、Host、SpacerModValues 替身检查；GitHub 的 Portable checks 工作流使用同一入口。它不能替代下列完整入口、原版权限扫描或游戏验收。

`Check-ReleaseIdentities.ps1 -Assembly '<待发布 DLL>' -Dotnet '<SDK dotnet>'` 是发布前的严格身份门禁：新增身份必须已写入人工审阅基线；开发期 ItemChecks 仍允许新增。不执行打包或上传。

`HostChecks` 检查真实 Host 的事件路由与模块任务；`SpacerModValuesChecks` 检查真实盲盒估值采样和临时物品释放。两者已纳入完整入口。

`Check-BuildIdentity.ps1 -GameManaged '<游戏 Managed>' -Dotnet '<SDK dotnet>'` 验证正常/维护构建的 dev 与实际提交标识、错误覆盖输入和源码归档 unknown；已纳入完整入口。可选 `-OutputRoot` 隔离证据目录。生成身份发生在编译期，运行时不访问 Git。

`./tools/Check-Maintenance.ps1 -GameManaged '<游戏 Managed>' -Dotnet '<SDK dotnet>' -Python '<Python>'` 固定串联维护相关检查并记录分层证据；失败非零退出，不启动游戏、部署或打包。`Check-MaintenanceContract.ps1` 使用相同参数验证总入口失败契约。详细使用及证据边界见[内部维护手册](../docs/maintenance/internal-maintenance.md)。

## 物品接入检查

`ItemChecks` 链接真实 ItemRegistry 并使用目录/物品替身；同时读取本次正式 DLL 的真实 ItemIdentity 与 ResolveId，对照固定 90 条身份基线。已接入 Check-Maintenance，统一入口步数以当次输出为准，含真实 RuntimeLifetime 与注册器组合回归及正式/维护双扫描。单独运行需先 build 到独立 artifacts 目录，再执行新 Checks.dll 并传入正式 CorpPresence.dll 路径。不要自动覆盖 IdentityBaseline.json；接入与覆盖边界见[物品接入清单](../docs/maintenance/item-extension.md)。

## 液体动态分类回归

`ItemTagChecks` 链接真实ItemTags，覆盖静态标签修复后的原版液体分类委托、新建/读档/重复修复、空瓶、无water成分混合物及原版/外部物品隔离，已接入维护和便携检查。游戏对象和WaterHelper为边界替身，验证调用时序及状态不被改写，不重新实现或验证原版纯度公式，也不证明实际售水通过。完整证据见[液体分类修复](../docs/maintenance/liquid-classification-fix-2026-11-12.md)。
