# 当前游戏原生契约核对

2026-11-11，从本机当前 Assembly-CSharp.dll 使用 ilspycmd 按类型重新读取。SHA256：`725CB999D019DFF1FF31BED353C7F9BB8D81A7605A15DE1F80F5A3A6CF7B1D05`。反编译输出仅留本地忽略目录，不提交游戏源码。以下是静态契约证据，不能代替游戏实测。

| 边界 | 当前实现顺序 | 对本轮测试的约束 |
| --- | --- | --- |
| PlayerStore.PayMoney | 先修改 playerCash，再按 playRegisterSound 决定调用音效 | CompanyMoney 保持原生扣款并传 false，再独立吞掉可选音效异常；替身须先扣后抛来验证不重试，不可把异常当回滚 |
| PlayerStore.modData | 公开字典字段；GetModData 会在 null 时新建字典 | 诊断直接读字段；不能调用初始化访问器；纯 MatchesReadonly 不收养替换字典 |
| PlayerStore.StartNewGame | 先 Reset，再初始化新局状态；InitialSave 才生成 runID 并 SaveGame | runID 不能独自作为对象身份；保留 store/modData 绑定。Hooks 前置清理、后置 ResetState，不新增分发点 |
| PlayerStore.LoadGame | 文件缺失会提前返回；正常路径先 LoadedInit，接着 ES3.LoadInto 当前 this，再 LoadedEarly、LoadedNormal、物品解码，之后 LoadedLate | 不能假定换档必然新建 PlayerStore；字典/原文变化也须失配。Hooks 在原位置 ResetState，不把原方法未抛错等同读取成功 |
| PlayerStore.SaveGame | 编码库存与状态后调用写档；部分 IO 异常由原版处理，可能跳过保存 | BeforeSave 是原保存前的准备，不是落盘成功通知；公司保存、事件、卸载清理顺序保持。测试不宣称已验证原版 IO 恢复 |
| PlayerStore.BeginDay | StartDay、租金计数、售货结算、客户/安全/夜间/配给每日处理 | Hooks.AfterBeginDay 仍在原版方法之后；内部 DayStart 原位置 Publish 一次，不能把测试主动 Publish 当作 Harmony 安装验证 |
| DialoguePhoneUIManager.StartDialogue | 赋 currentDialog，清待推进任务，再显示首句；不调用首句 startAction | 保留 MangoGiftChecks 的首句不执行 startAction 驱动，不能用替身补执行掩盖配送预约问题 |
| ContinueConversation / InterruptDialog | 继续时先按条件结束旧句，再切 nextDialogue 并执行新句 startAction；中断清 currentDialog、停止文本与已追踪协程；EndConversation 方法体为空 | 结束/中断不等于预约已完成；首句预约须由实际业务入口建立。OnCurrentTextDisplayed 的 endAction 另有延时调用，不能假定所有中断都会执行它 |

上述原生行为与本轮真实 Pay、SaveSlot、SaveBinding 和订单测试的边界相符。钱包/音效、玩家对象、输入和 Unity 更新循环仍是替身；真实代金券规则、Harmony 调用时序、主菜单资源等待和跨档持久化见 verify.md 的 M1–M6。
