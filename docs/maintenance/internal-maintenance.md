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

只读诊断、维护输入和三个生命周期事件已实现；真实付款检查已接入；统一检查入口待 T6 完成。报告仅列出 BeforeSave、DayStart、StateReset 的登记者，其他委托尚未迁移。

登记使用固定 owner 和 Subscribe，返回租约由 OwnedCallbacks.Own 持有。重复 owner 拒绝；分发按登记顺序，新增者下次生效，已撤销者跳过；异常分别记录，重入不递归。安装全部成功才提交标志，失败释放本次租约；模式开关不是卸载，不能用关闭恶意模式推断登记必须消失。

本地检查 `dotnet run --project tools/MaintenanceChecks -p:GameManaged='<游戏 Managed>'` 使用真实存档类型和诊断实现、替身游戏边界。DLL 类型隔离检查使用 `tools/MaintenanceBuildChecks`，参数为正式 DLL、维护 DLL。不能将这些检查当作游戏内验收。

游戏场景及结果记录见[验证清单](../changes/add-maintenance-interfaces/verify.md)。

## 付款测试边界

PaymentChecks 链接正式 Services.Payment、CompanyMoney、SaveSlot、CompanyState、Order 与 Visitor；钱包、音效和公司/恶意模式的加载入口是游戏边界替身。OrderChecks 也链接相同正式付款实现，不再复制 Pay。

扩展返回 null 才走现金，空串代表扩展已成功，非空表示拒绝；抛异常返回状态不确定，禁止补扣。支付后异常没有被证明已回滚。现金原生扣款抛错仍按原契约向上抛出；音效失败不影响成功结算。诊断失败不能影响这些结果。

`dotnet run --project tools/PaymentChecks -p:GameManaged='<游戏 Managed>'` 检查付款契约；`dotnet run --project tools/OrderChecks` 检查真实付款与订单提交的连接。它们不证明跨档付款持久化、原版存档异常恢复或真实代金券/期限规则，仍需 M5 验收。
