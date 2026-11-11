# R2 · 二期验证进度

本期仍在进行，按 AGENTS.md 和总方案涉及钱货业务的要求，区分计算、支付边界及完整订单入口。

- SaveStateChecks 的四个来访场景使用真实 CharacterVisitSession 和 StoreSession，验证完成标记一次性及顾客、存档字典、运行时代次替换后的失效。未执行实际 OnArrival 事件。
- RuntimeLifetimeChecks 的三组金额额度场景使用真实 OrderAmounts 和 QuotaCounts，覆盖金额和件数溢出、预检不写历史、异常计数、1000 组稀疏与密集滚动窗口对照。
- CompanyMoney 故障注入覆盖收银音效抛异常后扣款正常返回、现金配送清零后重复调用只入账一次。原版余额方法采用替身，此结果只覆盖支付适配层。
- RestorationChecks/State 已执行的 36 项包含延期、失败放置、部分库存及堆叠配送场景，详见一期证据。

源码追踪确认 Pastel、Davre、Meridian、Carmichael、Spacer 窗口调用 Services.PlaceOrder。索拉纳工艺目录和博士目录复用 CompanyWindow.Meridian.cs 中的 DrawEquipmentOrder，也提交到同一入口。索拉纳 UI 使用 CatalogTier 筛门槛，提交再检查 Perfume.Catalog 成员及 MinTier；博士仓库 UI 检查 Unlocked，提交再次检查公司类型、解锁、固定价格和控制器库存。上述属于源码追踪。

完整订单入口的付款回调重入、换档边界仍需执行检查，不能由计算层通过推定交易已验收。另外需核对 MeridianCo 的设备订单、CarmichaelCo 的标本订单及 Services.Special 的预约和赠礼等独立登记路径。

OrderChecks 现已链接完整 Services.Orders、OrderAmounts 和 QuotaCounts，付款和目录依赖采用替身。付款回调重入用例在旧代码得到两次扣款及两张订单；入口加保护后同一用例得到一次扣款和一张订单。付款失败、成功后当天再提交、报价或付款抛异常后恢复正常提交、零数/负数/超额度/极大数量均通过。跨档付款边界尚待验证。

MakeUpForge / PayForgeFee 已完成日志故障注入。旧实现出现一次扣款、ForgeOwned=true、零订单；因此再次操作会被已购买条件挡住。隔离 PayForgeFee 提交后的日志异常后，同样输入登记一张订单，重复调用不再扣费。测试直接提取当前两个方法，钱包和公司状态依赖采用替身，入口为本地 artifacts/forge-payment-check/Run.ps1。Release 零警告、零错误。已经发生该异常的旧档是否需要补偿尚无实档证据，本次只修复触发路径。

OrderForgeMachine 和 OrderSpecimen 的日志故障注入均已复现一次扣款、一张订单后抛异常。隔离各自提交后的日志异常后，两条路径都正常返回成功；Release 零警告、零错误。检查直接提取当前方法，入口为本地 artifacts/special-order-check/Run.ps1。合作度回调异常及付款换档仍未覆盖。

已复现并单点处理：Malice.Charge 在现金和代金券变更之后调用 ModLog.Info，再返回代付成功标记。注入日志异常时，旧代码在现金从 1000 变成 920、代金券从 20 结算为 8 后抛异常；Services.Pay 会将这种异常转成付款状态异常。修复仅隔离提交后的日志异常。相同真实 Charge 方法提取检查由失败转为通过，余额及返券结果一致，成功标记正常返回。该检查替代公司、钱包与订阅等边界，完整 PlaceOrder 链仍待验证。Release 编译为零警告、零错误。复现入口保存在本地 artifacts/charge-check/Run.ps1。

手测待办包括日常到店立即请离后计数、剧情未完成请离后保留预约、余额或额度不足时余额与订单不变、部分配送重试只补余量。

付款回调成功返回前替换公司状态的故障注入，复现了合作度误写入新状态（新档增加 2 点）。现在结算后的合作度更新再次核对原会话和公司状态。相同用例新状态合作度为 0、订单为 0，旧状态内存对象登记一张已付订单。完整订货回归和 Release 编译通过。此结果仅证明新状态隔离及旧对象内存记账；已经切走存档的持久化到账和恢复仍未验证，不能据此宣称跨档付款事务完整。

报价期间替换公司状态的 OrderChecks 故障注入已复现旧实现仍扣款一次、向旧状态登记一张订单。PlaceOrder 现在捕获 StoreSession，并在付款前检查会话、公司状态引用及日期；同场景结果变为零扣款、零订单。现有失败付款、正常成功、当天重复、回调重入、异常退出及数量检查继续通过。此诊断的会话边界使用替身，实际 StoreSession 的替换失效由 SaveStateChecks 独立覆盖。付款回调内部已经扣款后才切换存档的情况仍待验证。Release 零警告、零错误。
