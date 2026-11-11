# 韩语修复的订阅底包

本次工坊更新按用户指定，基于2026-11-09 16:22订阅版“弱兼容精工，缩紧容量代码，字体调整”，不使用较旧公开源码重编译覆盖该版本。

底包DLL SHA256：`855CF8D493E3B8DC74E584449208DCE457ADE86C4159003873D9AD609F35A744`。

该底包完整对应源码目前不在本工作区；GitHub `d0856e8f693ba8837ba3491296aa49e77df810ba`不能代表其中全部兼容、容量和字体改动。因此本次分发DLL由[定向修复工具](../../tools/checks/KoreanStartupPatch/Program.cs)从上述订阅底包生成：仅给`GameResources.ResolveMenuStartupLocale`补上`ko`映射，不新增译文、权限或玩法。

源码中同时保留对应的一行映射修复和启动回归检查，供后续源码合并使用；普通`Build.ps1`产物不能代替本次工坊DLL。在取得与16:22底包一致的完整源码前，后续工坊修复必须先保留并核对订阅底包，避免丢失未合并的功能。

工具会拒绝不匹配的底包哈希及同路径输出，并验证原始故障、12种语言输入、写出后映射、其他2715个方法的IL/局部变量及169份嵌入资源均保持一致。底包与生成DLL均不提交仓库。

```powershell
dotnet build tools/checks/KoreanStartupPatch/Checks.csproj --artifacts-path artifacts/korean-subscription-patch '-p:GameManaged=<游戏Managed目录>'
dotnet artifacts/korean-subscription-patch/bin/Checks/debug/Checks.dll '<16:22订阅底包DLL>' '<独立输出DLL>' '<游戏Managed目录>'
```

分发文件的实际SHA256见[发布文件清单](../releases/1.0.0-artifacts.json)。

## 2026-11-10：底包改动已补回源码

把本机订阅的 1.0.0 文件（SHA256 `7D10C9BDE91F3F59A69A3994A71ED2F638EF3B520A35B4A7BDFA2A64B3846D96`）反编译后，与提交 `111312244b15709774feac5f0da1289b04e83b70` 的编译产物逐文件对比，订阅版多出的改动共五处，已全部补进源码：

- 新增 `src/CorpPresence/Production/RatWheelCapacity.cs`：跑轮发电机笼格的容量恢复规则。
- `Production/RatWheelGenerator.cs`：改用独立笼体和自己的宽高标签（`CORP_RAT_WHEEL_WIDTH`、`CORP_RAT_WHEEL_HEIGHT`），读档时换算并移除旧的 `CAGE_SIZE_TAG`，接管扩容、扩容判断和说明文字四个入口。
- `Integration/Hooks.cs`：核心阶段安装上述四个补丁。
- `UI/Theme.cs`：帕斯泰尔窗口的字号。
- `UI/CompanyWindow.Pastel.cs`：帕斯泰尔窗口九处布局尺寸。

核对方式和范围：补入后重新编译（0 警告、0 错误），再反编译，与订阅版反编译结果比较，125 个源文件中 124 个完全一致；剩下的 `GameResources.ResolveMenuStartupLocale` 只是写法不同（订阅版是修补工具插入的判断，源码是 switch 里的一项），行为相同。167 张嵌入图片与订阅版逐字节一致。

没有做的：没有启动游戏验证；重新编译的文件与订阅文件不是逐字节相同（大小相差约 2 KB）。跑轮发电机的代码是从反编译结果整理的，变量名是反编译器起的，原来的注释无法恢复。

从这次起，普通 `Build.ps1` 的产物在逻辑上对应订阅版，下次发布可以从源码构建，不必再从订阅底包修补。

