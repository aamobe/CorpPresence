# 构建与发布

## 构建条件

- .NET 8 SDK或更高版本；目标仍为`netstandard2.1`，C#版本为12。
- 本地安装游戏；`GameManaged`必须包含`Assembly-CSharp.dll`、Unity库、`0Harmony.dll`和`Newtonsoft.Json.dll`。
- 仓库不分发游戏DLL、反编译代码、存档、登录缓存或令牌。
- `NuGet.Config`清空远端包源，当前模组依靠SDK与本地游戏引用构建。

从仓库根目录执行：

```powershell
./tools/Check-Repository.ps1
./tools/Build.ps1 -GameManaged 'F:/SteamLibrary/steamapps/common/Probably Stolen/Probably Stolen_Data/Managed'
```

构建脚本只编译。DLL位于`artifacts/build/bin/CorpPresence/release/CorpPresence.dll`。直接调用SDK也可以：

```powershell
dotnet build ./src/CorpPresence.csproj -c Release --artifacts-path ./artifacts/build '-p:GameManaged=F:/SteamLibrary/steamapps/common/Probably Stolen/Probably Stolen_Data/Managed'
```

## 本地部署

用户要求部署时，先记录目标路径并备份原文件，再将DLL和根目录的`manifest.xml`一起复制到本地模组目录。确保本地版和工坊版没有同时加载相同的`CorpPresence` ID。启动游戏与游戏内测试需要单独安排；编译成功不代表功能已经实测。

日常修复、整理和“继续工作”不自动生成分发包。仅在用户明确要求打包、压缩或导出安装包时生成。上传授权只用于指定发布目标，不顺带修改页面介绍、标题或其他服务。

## 发布前

1. 从[版本目录](../releases/README.md)确认基线，区分已发布源码与本地草稿，禁止从旧版本文件夹名推断真实版本。
2. 检查`Main.ReleaseVersion`、`AssemblyFileVersion`和`manifest.xml`一致性。同一0.9.9修复用提交和发布说明区分，不伪称不同二进制是同一个文件。
3. 编译并检查受影响的入口、后端和存档路径。运行测试需有明确授权和适当环境。
4. 准备实际改动说明，列出已验证和未验证项。公共记录不放本机绝对路径、账号或日志中的隐私。
5. GitHub提交只包含审阅过的源码、素材和文档，不上传`bin`、`obj`、SDK、游戏程序集、凭证或存档。
6. 工坊更新使用正确的appid和publishedfileid；不需要改变简介时，不在上传配置中填`description`或`title`。
7. 保存发布结果。只有明确成功回执才能说“上传成功”；获得下载权限后可回下载并比较SHA256，未执行则如实记录。

发布目录准备好后运行`tools/Check-ModPermissions.ps1 -GameManaged <游戏Managed目录> -ModFolder <待发布目录>`。此工具直接调用游戏安装目录中的`ModScanner.Scan`，不启动游戏或执行模组代码；扫描错误、拒绝项、未知权限或未声明权限均使检查失败。外部翻译包读取需要清单包含`<Capabilities><Capability>files</Capability></Capabilities>`；游戏将该权限统一显示为可读取或写入文件。

工坊目标：游戏appid `4348910`，项目ID `3828913695`。上传凭证只保存在本地工具环境，不进仓库。

## 版本记录维护

每次发布更新[版本目录](../releases/README.md)、对应版本说明及玩家受影响的操作文档。确认源码未偏离当前已记录发布快照时，运行：

```powershell
./tools/Check-Repository.ps1 -CheckReleaseBaseline
```

这个参数是发布快照比对，不是运行测试。文本使用去UTF-8 BOM、统一LF后的内容计算SHA256，避免Git的CRLF转换造成误报；PNG使用原始字节。开发中新修改尚未发布时可能故意不匹配，不要为让检查通过而覆盖未审阅的发布记录。后续正式更新时，按实际发布源码重新生成[源码快照](../releases/0.9.9-source-manifest.json)。
