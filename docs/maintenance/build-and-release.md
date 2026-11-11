# 构建与发布

1.0.0 订阅版曾从 16:22 订阅底包定向修补得到；底包里的改动已在 2026-11-10 补回源码，经反编译对比与订阅版逻辑一致（未做游戏实测），详见[订阅底包记录](korean-startup-release-baseline.md)。此后从源码构建发布。

## 构建条件

当前源码为开发版本：Main.Version 和 AssemblyInformationalVersion 在编译时追加 `-dev+<源码提交>`，有未提交变更时带 `-dirty`，源码归档无法核实 Git 时为 `-dev+unknown`；维护构建仍以 `-maintenance` 结尾。清单和数值文件版本保留发布基线，它们不能单独证明二进制与工坊相同。真实发布需另行确定版本和发布身份，不能把开发构建直接称作已发布 1.1.0。

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

物品身份发布门禁：对准备发布的 DLL 执行 `./tools/Check-ReleaseIdentities.ps1 -Assembly '<待发布 DLL>' -Dotnet '<SDK dotnet>'`。开发检查允许新增身份；发布检查要求每条身份已经进入人工审阅的 IdentityBaseline.json，缺失记录会非零退出。不得为了通过自动覆盖基线；新增条目和基线来源需一并审阅。本门禁只验证身份，不代替游戏验收、权限扫描或发布授权。

1. 从[版本目录](../releases/README.md)确认基线，区分已发布源码与本地草稿，禁止从旧版本文件夹名推断真实版本。
2. 检查`Main.ReleaseVersion`、`AssemblyFileVersion`和`manifest.xml`一致性。同一0.9.9修复用提交和发布说明区分，不伪称不同二进制是同一个文件。
3. 编译并检查受影响的入口、后端和存档路径。运行测试需有明确授权和适当环境。
4. 准备实际改动说明，列出已验证和未验证项。公共记录不放本机绝对路径、账号或日志中的隐私。
5. GitHub提交只包含审阅过的源码、素材和文档，不上传`bin`、`obj`、SDK、游戏程序集、凭证或存档。
6. 工坊更新使用正确的appid和publishedfileid；不需要改变简介时，不在上传配置中填`description`或`title`。
7. 保存发布结果。只有明确成功回执才能说“上传成功”；获得下载权限后可回下载并比较SHA256，未执行则如实记录。

发布目录准备好后运行`tools/Check-ModPermissions.ps1 -GameManaged <游戏Managed目录> -ModFolder <待发布目录>`。此工具直接调用游戏安装目录中的`ModScanner.Scan`，不启动游戏或执行模组代码；扫描错误、拒绝项、未知权限或未声明权限均使检查失败。当前内置翻译版清单必须保持`<Capabilities />`，发布前加`-RequireNone`确认实际检测权限、声明和 `Pending` 过渡期警告均为零。只检查 `Capabilities` 或 `Refused` 会漏掉新版扫描器的待生效规则。

## 新功能的权限边界

按官方 2026-11-08 的 API 更新记录，增加公司、人物、剧情、商品、合作度、订单或生产线本身不需要权限；是否触发检查取决于实现使用的接口。

| 功能或实现 | 可能触发的规则 | 推荐方式 |
| --- | --- | --- |
| 导出订单、交易流水到桌面，或读取外部配置 | 通用文件 API 触发 `files`，目前不细分只读与写入 | 优先使用限定目录的 `ReadModFile*`、`Read/WriteModDataFile*`；配置用 `GetConfig`，本局数据用 `SetModData` |
| 在线更新目录、翻译或立绘，联网检查更新 | `network`；直接保存文件还可能触发 `files` | 按实际使用声明权限，并向玩家解释用途 |
| 自建线程、Task、定时器；在其他游戏组件上启动协程 | `threads` 过渡期警告 | 使用 `ModHelper.StartCoroutine`、`Delay`、`RunInBackground` 等随模组停止而取消的接口 |
| 动态加载插件 DLL 或生成代码 | `dynamic-code` | 优先静态引用与官方依赖接口 |
| 调用原生库、指针，或无法静态确定目标的外部反射 | `native`、`reflection-external` | 使用受支持游戏 API，按需使用受限的 `ModReflect` |

官方受限文件接口可只读模组目录、读写自身模组数据目录，并拒绝绝对或越界路径。通用 `files` 仍是较宽的类别，不代表扫描器已经确认实际读写了任意文件。翻译 CSV 由加载器读取，当前公司增强无需文件权限。

2026-11-11 核对官方 100-Rev14 beta 文档，`files` / `network` 的强制开发者验证要求已撤回，玩家授权提示继续保留。官方仍公告 2026-12-10 起 `threads`、`dynamic-code`、`native`、`reflection-external` 将被拒绝。以实际安装版本和官方后续公告为准；静态扫描通过不代替游戏内功能测试。来源与新版接口见[官方接口跟踪](official-api.md)。

参考：[官方文件接口](https://github.com/dragonpan2/probably-stolen-modding/blob/main/ModdingGuide.md#reading-and-writing-files-without-the-capability)、[权限分类](https://github.com/dragonpan2/probably-stolen-modding/blob/main/ModdingGuide.md#declaring-capabilities)、[2026-11-08 更新记录](https://github.com/dragonpan2/probably-stolen-modding/blob/main/APIChanges.md#november-8-2026)。

工坊目标：游戏appid `4348910`，项目ID `3828913695`。上传凭证只保存在本地工具环境，不进仓库。

## 版本记录维护

每次发布更新[版本目录](../releases/README.md)、对应版本说明及玩家受影响的操作文档。确认源码未偏离当前已记录发布快照时，运行：

```powershell
./tools/Check-Repository.ps1 -CheckReleaseBaseline
```

这个参数是发布快照比对，不是运行测试。文本使用去UTF-8 BOM、统一LF后的内容计算SHA256，避免Git的CRLF转换造成误报；PNG使用原始字节。开发中新修改尚未发布时可能故意不匹配，不要为让检查通过而覆盖未审阅的发布记录。后续正式更新时，按实际发布源码重新生成[对应版本的源码快照](../releases/README.md)。
## 显式发布构建（框架开发中）

普通 `Build.ps1` 继续输出带源码提交身份的开发构建。正式构建使用：

```powershell
./tools/Build.ps1 -GameManaged '<游戏 Managed>' -PublishRelease
./tools/Check-ReleaseBuild.ps1 -Assembly 'artifacts/publish-build/bin/CorpPresence/release/CorpPresence.dll'
```

发布模式要求 Release 配置、自身 Git 工作区干净、当前提交包含与源码 ReleaseVersion 一致的 `v主.次.补丁` 标签、清单版本及文件版本一致，并拒绝维护和测试开关。它不创建标签、不提交文件，也不上传工坊。版本选择、提交和正式标签应在实际发布准备中完成，不能通过临时改版本绕过检查。

发布 DLL 的运行版本和信息版本不含 dev 后缀，SourceRevision 程序集元数据保留实际提交；核验脚本检查三类版本、源码提交和开发专用类型。此检查不替代物品身份基线、权限扫描和游戏验收。

SDK 实际已默认开启 Deterministic；本轮显式保留并增加源码/中间输出路径映射。同一工作树两个输出目录的 DLL 哈希已验证一致，不等于已验证不同源码目录或不同机器。完整可复现性与干净标签发布成功路径留待最终统一验收。
