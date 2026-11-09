# 韩语修复的订阅底包

本次工坊更新按用户指定，基于2026-11-09 16:22订阅版“弱兼容精工，缩紧容量代码，字体调整”，不使用较旧公开源码重编译覆盖该版本。

底包DLL SHA256：`855CF8D493E3B8DC74E584449208DCE457ADE86C4159003873D9AD609F35A744`。

该底包完整对应源码目前不在本工作区；GitHub `d0856e8f693ba8837ba3491296aa49e77df810ba`不能代表其中全部兼容、容量和字体改动。因此本次分发DLL由[定向修复工具](../../tools/KoreanStartupPatch/Program.cs)从上述订阅底包生成：仅给`GameResources.ResolveMenuStartupLocale`补上`ko`映射，不新增译文、权限或玩法。

源码中同时保留对应的一行映射修复和启动回归检查，供后续源码合并使用；普通`Build.ps1`产物不能代替本次工坊DLL。在取得与16:22底包一致的完整源码前，后续工坊修复必须先保留并核对订阅底包，避免丢失未合并的功能。

工具会拒绝不匹配的底包哈希及同路径输出，并验证原始故障、12种语言输入、写出后映射、其他2715个方法的IL/局部变量及169份嵌入资源均保持一致。底包与生成DLL均不提交仓库。

```powershell
dotnet build tools/KoreanStartupPatch/Checks.csproj --artifacts-path artifacts/korean-subscription-patch '-p:GameManaged=<游戏Managed目录>'
dotnet artifacts/korean-subscription-patch/bin/Checks/debug/Checks.dll '<16:22订阅底包DLL>' '<独立输出DLL>' '<游戏Managed目录>'
```

分发文件的实际SHA256见[发布文件清单](../releases/1.0.0-artifacts.json)。
