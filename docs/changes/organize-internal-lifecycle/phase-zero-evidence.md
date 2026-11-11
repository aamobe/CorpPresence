# F0 · 第零期环境与接口核对

2026-11-11 · 输入源码 6da4a97 · 自动检查记录，游戏验收待完成。

## 环境身份

| 项目 | 本次检测 |
| --- | --- |
| 操作系统 / 架构 | Windows，win-x64 |
| SDK / MSBuild | .NET SDK 8.0.426 / MSBuild 17.11.69 |
| SDK 自带运行时 | .NET 8.0.32 |
| PowerShell / Python | 7.6.9 / 3.12.14 |
| 工程目标 | netstandard2.1，C# 12，官方 IMod |
| Steam 安装 buildid | 26525552 |
| Assembly-CSharp.dll SHA256 | 725CB999D019DFF1FF31BED353C7F9BB8D81A7605A15DE1F80F5A3A6CF7B1D05 |

Steam buildid 来自安装清单。未从当前程序集发现 PlayerStore.GAME_VERSION 或 ModGameVersion，不把 buildid 推断为官方点分版本。当前游戏引用与前轮原生契约记录的哈希一致。

## 实际接口可用性

通过 Mono.Cecil 只读检查类型、字段及方法元数据，不执行游戏代码。

| 接口 | 结果 | 对开发的影响 |
| --- | --- | --- |
| IMod.Init(ModManifest)、OnEnable()、OnDisable() | 存在，返回 void | 一期沿用当前启停入口 |
| ModHook.OnModItemDirectoryInit | Action<ModItemDirectory> | 资源目录接入可继续沿用 |
| ModHook.OnGenerateCustomerVeryLate | Action<StoreClientManager> | 二期顾客事件候选存在 |
| ModHook.OnHandlingNightlyServicesEarly | Action | 二期夜间服务候选存在 |
| ModHelper.GetModData(string,string,string)、SetModData(string,string,string) | 存在 | 既有存档能力可用，读取的副作用仍按原契约处理 |
| ModHelper.AddDebugFunction(string,string,string,Action,bool)、RemoveDebugFunctions(string) | 存在 | 现有官方调试按钮能力存在 |
| GetGameVersion、IsGameVersionAtLeast、GameVersionMatches、ModGameVersion | 不存在 | 当前版本不能直接编译调用 Rev14 版本查询 |
| AddDebugPanel、RemoveDebugPanel、OpenDebugPanel | 不存在 | 自定义 F9 面板暂不接入当前版本 |
| AddEnding、TriggerEnding、RemoveEndings、GetEndingIds、IsGameOver、ModHook.OnGameOver | 不存在 | 当前版本不具备该组官方结局接口 |
| PlayerStore.StartNewGame/SaveGame/LoadGame/BeginDay | 无参 void 方法存在 | 现有生命周期接入签名可核对 |
| PlayerStore.PayMoney(int,bool) | 存在 | 现有真实付款封装的原生依赖存在 |

官方 Rev14 beta 文档与当前安装版本有能力差异。首期状态、日志和租约方案使用当前能力，新增官方 API 等目标版本升级后再重新核对。官方变化来源见[接口跟踪](../../maintenance/official-api.md)，原生调用顺序见[前轮契约](../add-maintenance-interfaces/native-contracts.md)。本次元数据存在性检查不能证明 Harmony 实际触发或新版本运行兼容。

## 检查命令与证据

执行 Check-Maintenance.ps1，显式指定本机 GameManaged、Dotnet 和 Python。DOTNET_CLI_HOME 指向本地忽略的 artifacts/dotnet-cli，避免系统默认 dotnet 缺少 SDK 或写入用户目录受限。测试先 build 后直接执行当次 DLL。

Check-MaintenanceContract.ps1 的 missing-sdk、missing-managed、failed-child 三项通过，均非零退出且没有成功标记。原始输出留在本地 artifacts，含设备路径的日志不提交。

## 游戏验收交接

M1–M6 均待测，见[操作步骤](../add-maintenance-interfaces/verify.md)。优先主菜单报告及在档只读性，再换档和模式切换，最后普通/代金券付款与正式版隔离。记录游戏身份、Mod DLL 哈希、具体操作、实际结果和日期。

第零期自动部分完成后仍需回填这些游戏结果，才按总方案进入一期代码。当前可继续设计评审和只读分析。新设备按上述工具版本和实际游戏路径复现，元数据或哈希变化时重新核对接口。

## 本次自动检查结果

统一检查退出 0，24 步全部通过。本地证据目录为 artifacts/maintenance-checks/20261111-121707-0349fc93。覆盖诊断、事件、真实付款、订单、生命周期、存档、芒果回归、程序集隔离、构建开关冲突、配送恢复及本地化。文档收尾检查覆盖 92 份 Markdown、483 个本地链接。

正式版与维护版均零警告零错误。正式版扫描 2889 方法，维护版另行扫描 2891 方法；两者 Pending、未知与未声明权限均为零，Refused=False，RequireNone 通过。

正式 DLL SHA256 为 C1934A903FDA8F39506DFE59DFBB45A66FA1157E59DD29C9B975BB884B842031。
维护 DLL SHA256 为 BFCAFC07109857416EEBCE8181B2700F730F737D6774178D7F7D1DF2E8E1A85B。

结论为第零期自动检查部分完成。测试执行新编译 DLL；结果仅适用于上述程序集和源码。M1–M6 游戏实测尚未执行。
