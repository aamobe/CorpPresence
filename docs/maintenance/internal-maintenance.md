# 内部维护接口

仅供本项目开发维护；不提供第三方兼容承诺。完整范围见[总方案](../changes/add-maintenance-interfaces/overview.md)。

## 构建和请求报告

```powershell
dotnet build src/CorpPresence.csproj -c Release --artifacts-path artifacts/regular -p:GameManaged='<游戏 Managed>'
dotnet build src/CorpPresence.csproj -c Release --artifacts-path artifacts/maintenance -p:GameManaged='<游戏 Managed>' -p:Maintenance=true
```

第二份版本带 `-maintenance`。仅维护版中，按 Ctrl+Shift+F10 输出一次 `[维护]` 报告到已有日志；按住不重复，最短间隔 1 秒。主菜单和存档内均可请求；不创建 UI，不写诊断文件或存档。部署和启动游戏需另行授权。

维护模式不得与 LocalTestFunds 或 WaterTrace 同时构建；正式 DLL 不含 MaintenanceConsole。两份产物必须分目录，不能用维护 DLL 发布工坊。

## 解读报告

- NoStore：尚无玩家实例；NotLoaded：存在玩家，但公司数据尚未载入。
- Bound：当前已有绑定匹配；不等同全面验证了存档。写保护字段独立显示。
- Stale：公司对象与当前字典/原始记录不匹配；Unstable：采集中对象或资源变化，或采集失败。
- 未加载、失配、不稳定和保护中的订单计数显示“未知”，不可理解为零订单。
- 订单数是订单张数；剩余物品条目数是各订单 Items.Count 的和，不推测堆叠数量或现金奖励。
- 缓存和物品注册计数只读现有集合，不代表场景实际物品扫描。
- 故障最多 64 个聚合键，重复计数饱和；不保存异常原文、个人路径或存档 JSON。切换会话和停用清理。

## 当前阶段

只读诊断和维护输入已实现；事件登记迁移、真实付款检查和统一检查入口待后续 T4–T6 完成。诊断当前不声称提供全部扩展字段的归属查询。

本地检查 `dotnet run --project tools/MaintenanceChecks -p:GameManaged='<游戏 Managed>'` 使用真实存档类型和诊断实现、替身游戏边界。DLL 类型隔离检查使用 `tools/MaintenanceBuildChecks`，参数为正式 DLL、维护 DLL。不能将这些检查当作游戏内验收。

游戏场景及结果记录见[验证清单](../changes/add-maintenance-interfaces/verify.md)。
