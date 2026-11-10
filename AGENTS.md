# AGENTS.md

```yaml
# 公司增强 CorpPresence（Probably Stolen 的 Mod）。每次会话读本文。细节在同目录 总方案.md（下称 总方案 或 方案）。
# 读法：按 index 找到小节标题 → 在方案里搜该标题（带上标题前的 #，如 ### 章程）→ 只读那一节。不要整份读方案。
# 优先级：用户当前要求 > 方案 > 本文。
# 项目里的这一份（本文和总方案）是该项目开发时的依据，可以按项目改；GitHub 上的通用版只是参考，两边不自动同步。模板版本 2026-11-11。
# <…> 是每台机器不同的值，执行时换成本机的，不要把本机路径写回本文。index.project 是本仓库自己的文档。
# 记号：2章"章程" = 方案第二章里标题含“章程”的小节；附G = 方案附录 G。

project:
  game: Probably Stolen / Questing Goose Studio / Steam AppID 4348910
  engine: unity-mono
  loader: official（游戏官方 IMod 接口 + Harmony）；目标 netstandard2.1，需要 .NET 8 SDK 或更高
  dir_game: <本机游戏目录，不写进仓库>
  dir_work: <游戏目录和本仓库之外的本机目录，放反编译输出、参考图、草稿>
  dir_src: src/CorpPresence/（工程文件 src/CorpPresence.csproj，嵌入图片 src/Assets/）
  dir_saves: <本机存档目录，不写进仓库>
  log_game: '%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen\Player.log'
  log_loader: 无单独的加载器日志，Mod 的输出在 log_game 里
  l10n: 稳定键 + 游戏官方 ModHelper.GetLocalized 读取 Localization/*.csv（en、ru、fr）；中文是调用处原文。见 docs/maintenance/localization-keys.md
  release: Steam 创意工坊，appid 4348910，项目 ID 3828913695
  stage: released
  ingame_verify: user-manual     # 本仓库不自行启动游戏；游戏内验证由用户做，按 待用户验证清单 交接
  baseline: 1.0.0；工坊文件 SHA256 7D10C9BDE91F3F59A69A3994A71ED2F638EF3B520A35B4A7BDFA2A64B3846D96；源码自提交 34590e3 起与它逻辑一致（见 docs/decisions）
  repo: https://github.com/aamobe/CorpPresence  主分支 main
  machines: 两台机器轮流开发。游戏路径各不相同，用 -p:GameManaged 传入；不要把本机路径写进任何提交的文件

cmd:            # 每项：run 可直接复制执行；ok 为通过标准。<…> 换成本机的值
  build:        {run: "./tools/Build.ps1 -GameManaged '<游戏>/Probably Stolen_Data/Managed'", ok: 0 错误、0 警告（警告基线为 0）；产物在 artifacts/build/bin/CorpPresence/release/CorpPresence.dll}
  build_direct: {run: "dotnet build ./src/CorpPresence.csproj -c Release --artifacts-path ./artifacts/build '-p:GameManaged=<游戏>/Probably Stolen_Data/Managed'", ok: 同 build}
  check:        {run: ./tools/Check-Repository.ps1, ok: 文档链接和版本字段全部通过（需要 PowerShell 7）}
  check_l10n:   {run: ./tools/Check-Localization.ps1, ok: 稳定键、四语言菜单、格式和缺键回退全部通过（需要 PowerShell 7）}
  check_baseline: {run: ./tools/Check-Repository.ps1 -CheckReleaseBaseline, ok: 与已记录的发布快照一致；开发中的改动会故意不一致，不要为了通过去改快照}
  scan:         {run: "./tools/Check-ModPermissions.ps1 -GameManaged '<游戏 Managed 目录>' -ModFolder '<待发布目录>' -RequireNone", ok: 权限、拒绝项、待生效警告均为零}
  deploy:       {run: 手动把 CorpPresence.dll、manifest.xml、Localization/ 复制到 <游戏>/Mods/CorpPresence/, ok: 游戏已关闭；已备份原文件；没有同时启用相同 ID 的工坊版}
  decomp_mono:  {run: 'ilspycmd -p -o "<dir_work>/decomp" "<游戏>/Probably Stolen_Data/Managed/Assembly-CSharp.dll"', ok: 输出只在 dir_work}
  decomp_type:  {run: 'ilspycmd -t <完整类型名> -r "<游戏 Managed 目录>" "<某个 dll>"', ok: 带上 -r 才能解析出游戏类型和枚举名}
  test_start:   {run: 无。游戏内验证由用户手动进行, ok: 已把要看的场景按待用户验证清单交给用户}
  log_errors:   {run: 在 log_game 里搜 Exception 和 公司增强, ok: 先核对日志开头的 Mod 版本和时间}
  status:       {run: git status --short --ignored, ok: 无夹带的无关文件}
  verify_release: {run: '把发布文件和源码的 Release 编译产物分别反编译后逐文件比较，做法见 附H"发布物与源码的反编译对比"', ok: 只在大型更新时做；除已记录的等价写法外没有差异}
  sync_check:   {run: 'git fetch origin && git status -sb && git log --oneline HEAD..@{u} && git log --oneline @{u}..HEAD', ok: 已知本地相对远端是 一致、落后、领先 还是 分叉，以及有无未提交改动}
  sync_pull:    {run: git pull --ff-only, ok: 快进成功；不能快进就停下问用户}
  upload:       {run: 上传脚本不在仓库里，由用户在本机执行, ok: 有明确的成功回执；不需要改简介时不填 description 和 title}

session:        # 跨机器开发，GitHub 是唯一交接点。详见 3章"跨机器开发"
  start:        # 只在一项任务开始时做一次（接到任务后、第一次读代码或改文件之前）；同一任务的后续对话不重复
    - 跑 cmd.sync_check
    - 一致且本地干净 → 开始
    - 落后且本地干净 → 跑 cmd.sync_pull，重新编译，再开始
    - 领先 → 告诉用户哪些提交没推送，问要不要先推
    - 有未提交改动、或与远端分叉 → 不拉取、不改文件，列出情况等用户决定
    - 取不到远端（没网、没权限、本地没配远端或上游分支）→ 说明没能核对，问用户是否按本地继续；没配远端的要提醒这台机器的改动传不到另一台
    - 拉取后：游戏目录里已部署的文件可能是旧的，进游戏验证前比对哈希；看最近的提交和进行中的变更，知道上次做到哪
  end:          # 只在用户确认这项任务结束时做一次；任务中途不做
    - 跑 cmd.status 和 cmd.sync_check，报告未提交的改动和未推送的提交
    - 做到一半的工作提交到分支，并在任务清单里写清做到哪、下一步是什么
    - 提醒用户不随 GitHub 同步的东西：本机配置、dir_work、已部署的文件、测试存档、游戏版本、发布平台上的版本、助手的本机记忆

ask_first:      # 未经用户在本次对话里明确同意，不执行
  - 启动游戏；关闭用户正在运行的游戏
  - 部署到游戏目录；打包；上传；改发布页的标题和简介
  - 删除或覆盖存档；初始化 Git 仓库
  - 合并到主分支、推送到 GitHub（用户说过本项目收工自动推送的除外）。在工作分支上的本地提交不用问
  - 强制推送、改写已推送的历史、丢弃本地未提交的改动（任何时候都先问）

stage_rules:    # 按 project.stage 取用。详见 1章"按 Mod 所处的阶段调整"
  prototype: 只守 rules.work 和 rules.verify；用测试存档；不写提案、规格、任务清单；代码不直接进正式版；留一份笔记记试了什么和结论
  pre-release: 全部 rules；存档结构可直接改（只有自己的测试存档），改了要更新存档数据模型；不需要迁移、弃用期、回滚预案、紧急通道
  released: 全部 rules 和 steps；存档里的 ID、键、槽位顺序是承诺

rules:          # 硬规则。不读方案也不能违反。
  work:         # 详见 2章"基本约定"
    - 中文回答；说明、日志、文档默认中文；提交信息的语言见 rules.project
    - 只做用户要求的；想法先提，同意再做；不顺手改别的，无关问题另记
    - 一项任务开始时按 session.start 对一遍 GitHub（只做一次）；用户确认任务结束时按 session.end 检查（只做一次）
    - 先看工作区状态和历史；保留用户已有的修改
    - 原版程序集和反编译输出只读；与 Mod 无关的生成物放 dir_work，不放 dir_game
    - 不提交 游戏本体、加载器、反编译输出、日志、缓存、构建产物、凭证、存档
    - 删除优先进回收站；删前列出内容并提醒未备份项
    - 目录分工 代码 src/<ModName>/、脚本 scripts/、说明 docs/、对外说明 share/；最终美术和源文件放 src/<ModName>/assets/ 并跟踪
    - 按职责组织代码；文件超过约 500 行评估拆分
    - Mod 只放在游戏目录时，在 dir_work 留一份源码备份
    - 本文和方案放同一目录一起用；必要的证据文档放 docs/
    - 查得到的不问；要问就一次一个，只问答案会改变做法的
    - 不清楚的标 待澄清，不猜
  design:       # 详见 附G
    - 物品、状态、资源、回调归谁要说得清；谁创建谁释放；借来的只清引用
    - 改公共入口必须带自己的 ID 过滤；原版已负责的流程不重复接管
    - 存档里的 ID、键、槽位顺序不改，槽位只追加；改存档前先更新存档数据模型文档
    - 旧档迁移保留已有内容和玩家改造，跑两遍结果不变；不为修复而清档、重置进度、整单补发
    - 扣钱、销毁、发货之前完成全部校验和准备；按可观察的事实记账；重试只补余量
    - 界面门槛与提交校验是同一套；不加玩家解不开的锁
    - 临时游戏对象用完即销毁；缓存有失效条件，失败值不入缓存；周期性代码不做全场景搜索
    - 就绪用可检查的条件判断，不用固定延时
  verify:       # 详见 3章"证据先于结论"、5章"第 7 步"
    - 没有当场跑出的证据，不说 完成、修好了、通过了
    - 汇报分层 读源码、静态检查、编译、替身测试、游戏实测、发布核验；没做的层明说
    - 没查到根因不提修复；一次一个假设，一次只变一个量；修三次不行就停下和用户讨论
    - bug 结论只有 已验证、部分、失败、已发布待确认；只有 已验证 能对玩家说修好了
    - 交付时必须列出只能在游戏里看的场景（怎么看、预期看到什么）交给用户，修复和小改也一样；只写一句 未做游戏实测 不算交付
  release:      # 详见 5章"第 8 步"、7章
    - 发布物对应到确定的提交、文件哈希、资源；版本号在入口代码、清单、更新日志三处一致
    - 发布包不带测试代码；不同时加载同一 Mod ID 的两份
    - 保留上一版发布包，能退回
  project:      # 本仓库特有的边界
    - 提交信息用英文（本仓库惯例），正文写明实际做过的验证和没做的验证
    - 普通水的灌装、纯度、分类、估值和原版顾客收货保持原版；不安装覆盖所有液体的补丁。改液体、物品身份、读档、订货、会员之前先读 docs/maintenance/compatibility-and-regressions.md
    - 以发布记录确认基线（docs/releases/README.md），不根据本机目录名判断版本；未发布的素材不混进已发布源码
    - 做素材、改素材、缩放或导入之前先读 docs/probably-stolen-art/AGENTS.md；缩小后的物品图必须完成防杂色检查；只是讨论素材时不生成或替换贴图
    - 日常修复和“继续工作”只改源码、编译、验证；用户明确要求时才生成分发包
    - 排查重复问题或想复用旧方案时，先查 docs/retrospective/README.md，确认那个方案有没有被撤回
    - 公开仓库里不放本机绝对路径、账号、完整个人日志、存档

route:          # 先判断走哪条；拿不准按高一档
  不确定值不值得做: 评估，只查不写代码 → 4章
  全新玩法，不确定好不好玩: 先做灰盒原型给用户试玩，用户说做再补规格 → 5章"第 1 步" 里的 原型先行
  小改（文案、数值、贴图、不改行为的整理）: 直接做并说明；steps 7、8 照走。试玩后调数值、改台词也走这条，例外见 5章"内容类工作"
  功能（新物品、新界面、新剧情）: steps 1-8；规格用轻量写法
  高风险（物品 ID、存档结构、扣钱发货、删除清理、全局补丁、共享资源、对外接口、换加载器）: steps 1-8 全做；单独写设计和决策记录；九项全过
  修 bug: step 9
  紧急（玩家在丢存档、丢物品、进不了游戏）: 6章"紧急修复通道"

steps:          # done = 做完的标志；read = 方案里的小节
  1_立项:       {done: 空 Mod 在游戏里加载成功且日志有版本行；cmd 的编译、部署、看日志可用；警告基线和 ingame_verify 已记, read: [5章"第 0 步"]}
  2_摸底与需求: {done: 用户确认了方向和路线, read: [5章"第 1 步"]}
  3_提案与规格: {done: 无待澄清；用户确认, read: [5章"第 2 步", 附A, 附B]}
  4_设计:       {done: 九项都答得上（所有权、边界、存档、不可逆、门槛、简单、性能、权限、授权）, read: [5章"第 3 步", 附C, 附G]}
  5_任务与实现: {done: 每个勾选项单独验证过并补了结果（默认用轻量清单）, read: [5章"第 4 步", 5章"第 5 步", 附D]}
  6_审查:       {done: 无阻塞级意见, read: [5章"第 6 步"]}
  7_验证:       {done: 自己能跑的层都跑过并附证据；只能在游戏里看的场景已列入待用户验证清单并交给用户, read: [3章"证据先于结论", 5章"第 7 步"]}
  8_发布:       {done: check 和 scan 全过；有回执；回滚预案在；大型更新另做 cmd.verify_release, read: [5章"第 8 步", 7章]}
  9_修bug:      {done: 结论为 已验证、部分、失败 或 已发布待确认, read: [6章, 附E]}
  10_复盘:      {done: 条目进了复盘目录, read: [8章, 附F]}

index:          # 遇到什么 → 读方案的哪一节
  files:
    新项目立项、换游戏或加载器版本: 5章"第 0 步"
    建项目文档骨架: 2章 开头的目录树
    写项目原则: 2章"章程"
    记录 Mod 现在的行为: 2章"行为规格库"
    改存档、加物品 ID、加槽位: 2章"存档数据模型"
    把规则变成脚本检查: 2章"把原则写成能自动跑的检查"
    定日志怎么打: 2章"日志约定"
    用新素材、字体、第三方代码: 2章"许可与素材授权"
  actions:
    判断想法做不做: 4章
    换了机器、开工前同步、收工前检查: 3章"跨机器开发"
    向用户提问、摆路线: 3章"先看再问"；5章"第 1 步"
    做灰盒原型: 5章"第 1 步" 里的 原型先行
    写台词、交接美术、试玩后调数值: 5章"内容类工作"
    把游戏内验证交给用户、回填结果: 5章"第 7 步" 里的 待用户验证清单
    不确定某个入口或接口的行为: 5章"第 1 步" 里的 技术试探
    做界面: 5章"第 1 步" 里的 界面先给示意
    写提案、写规格: 5章"第 2 步"；附A；附B
    做重要取舍、撤回方案: 5章"第 3 步" 里的 决策记录；附C
    拆任务: 5章"第 4 步"；附D
    审查改动: 5章"第 6 步"
    准备说完成: 3章"证据先于结论"
    写提交信息、定版本号、写更新日志: 7章
    移除物品、配置、接口: 7章"版本号" 里的弃用规则
    准备发布、准备回滚: 5章"第 8 步"
    确认发布物和源码一致、找回没进仓库的改动: 附H"发布物与源码的反编译对比"
    查 bug、下结论: 6章；附E
    玩家发来报告或日志: 6章"处理玩家反馈"
    写复盘: 8章；附F
    把子任务交给另一个 AI: 8章"项目管理与协作"
    各档位需要哪些产物: 9章
  domains:      # 都在 附G；值为小节标题
    启动、注册、停用、资源释放: 所有权与生命周期
    打补丁、扩展原版系统: 边界：原版、游戏更新和其他 Mod；扩展原版系统
    游戏更新后出问题、和别的 Mod 冲突: 兼容：游戏更新和其他 Mod
    读档、存档、迁移、卸载清理: 存档
    订单、付款、发货、配送: 涉及钱和货的业务
    会员、档位、解锁条件: 界面门槛和后端规则
    变卡、内存增长、缓存: 性能与缓存
    文件读写、联网、线程、反射: 加载器的权限与审核
    翻译、多语言: 本地化
    定价、利润、平衡: 数值与经济
    贴图、立绘、占格、缩放: 美术（出图步骤在 附H"美术资源"）
  tech:         # 都在 附H；值为小节标题
    判断引擎和后端: 引擎识别
    选加载器、MelonLoader 要点: 加载器
    搭 csproj 和部署目标: 工程模板
    反编译 IL2CPP、读 ISIL: 逆向工作流
    做 Godot 游戏的 Mod: Godot 游戏
    让用户进游戏看之前准备状态: 测试驱动 Mod
    两个 Mod 联动: 跨 Mod 联动
    画 IMGUI 面板: 自绘界面的经验
    闪退、溢出、Steam 副本、PowerShell 的坑: 常见的坑
  project:      # 本仓库自己的文档（路径相对仓库根目录）
    文档总目录: docs/README.md
    项目原则（章程）: docs/章程.md
    代码在哪、各部分负责什么: docs/源码地图.md
    各功能现在的行为和数值: docs/specs/README.md
    兼容边界和回归检查清单: docs/maintenance/compatibility-and-regressions.md
    构建、部署、发布步骤和权限边界: docs/maintenance/build-and-release.md
    本地化键和回退规则: docs/maintenance/localization-keys.md
    版本记录和发布文件清单: docs/releases/README.md；未发布的改动记在 CHANGELOG.md
    决策记录: docs/decisions/README.md
    历史问题复盘（40 个案例）: docs/retrospective/README.md
    进行中和已归档的变更: docs/changes/README.md
    美术规范: docs/probably-stolen-art/AGENTS.md
    工具脚本: tools/README.md
  templates:
    提案、规格、决策记录、任务清单、缺陷记录、复盘条目: 附A 至 附F
    任务卡、问题记录: 附I
    案例编号（A01、B07 等）的含义: 附I"原仓库案例索引"
    某做法出自哪个参考仓库: 10章
    某个参考仓库的原始做法和总结: 附J
```
