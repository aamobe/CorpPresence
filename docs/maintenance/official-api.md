# 官方 Mod 接口跟踪

核对日期为 2026-11-11，官方文档提交为 3541332ad755a07e423077659863268256ccd233。最新条目标注 100-Rev14（1.0.0.14）beta branch。实际接入前须核对目标游戏程序集及对应签名。[官方更新记录](https://github.com/dragonpan2/probably-stolen-modding/blob/3541332ad755a07e423077659863268256ccd233/APIChanges.md)

| 官方变化 | 公司增强的处理 |
| --- | --- |
| 撤回 files/network 的强制开发者验证要求，玩家授权提示仍保留 | 修正发布指南；继续以实际扫描结果核对能力 |
| GameVersions 使用点分版本及前缀匹配，新增 GetGameVersion 等查询 | 当前清单为空；下一次兼容性发布时根据实测版本填写范围 |
| 新增 AddDebugPanel 等自定义 F9 面板接口 | 后续维护入口的候选，需要开发工具开启及对应游戏版本支持 |
| 新增 AddEnding、TriggerEnding 和 OnGameOver | 涉及结局时按需求评估，现有剧情需独立回归 |
| 扩展第三方库扫描范围，明确部分控制台和工具库调用的权限归类 | 游戏升级后用新版官方扫描器重新检查正式 DLL |
| 补充文本动画库的免权限目标、音频解码及窗口控制接口 | 对应功能有需求时查阅具体契约 |

以上为官方文档变化及本项目的评估安排。[固定版本接口指南](https://github.com/dragonpan2/probably-stolen-modding/blob/3541332ad755a07e423077659863268256ccd233/ModdingGuide.md)

## 当前验证基线

公司接口版源码为 2500a6e，最近本地扫描基于 Assembly-CSharp.dll 的 SHA256 725CB999D019DFF1FF31BED353C7F9BB8D81A7605A15DE1F80F5A3A6CF7B1D05。该哈希下的扫描通过只覆盖当时的安装程序集。新增 API 在该程序集中的可用性、beta 分支运行时兼容性均待核实。

保存、开店、换档和付款的已有本地契约见[原生契约记录](../changes/add-maintenance-interfaces/native-contracts.md)。正式接口版的游戏验收见[M1–M6](../changes/add-maintenance-interfaces/verify.md)。

## 后续核对入口

从官方更新记录定位变化，核对本地版本与程序集哈希，再核对涉及的方法签名及调用时机。更新目标游戏后重新执行本地检查和官方权限扫描，并完成受影响场景的游戏验收。将结果附到当期变更证据中。

## 本机核对（2026-11-11）

Steam buildid 26525552 的本地程序集缺少 Rev14 的版本查询、自定义 F9 面板和结局接口。既有 IMod、物品目录及顾客/夜间服务事件存在。详细签名、哈希与工具环境见[F0 · 第零期现场证据](../changes/organize-internal-lifecycle/phase-zero-evidence.md)。
