# 矿化机：生成稿到48×64原生整理

2026-11-13。此目录为美术研究样例，不属于运行时src/Assets，未注册物品、未编译部署、未做游戏验证。用户确认保留refined.png的造型后要求继续细化材质，得到mineral.png；没有将尺寸转换结果称为模型直出。

![最终原尺寸精灵](mineral.png)

## 文件与来源

| 文件 | 含义 |
|---|---|
| [generated.png](generated.png) | 透明生成原文件1086×1448；为此前认可横筒造型的透明重生成版，不是原图无损抠底 |
| [automatic.png](automatic.png) | 全画布中心采样到48×64，固定色板、Alpha128二值化；自动草稿 |
| [refined.png](refined.png) | 原生整理351像素，料斗、管路、束带、窗框与底座分面 |
| [mineral.png](mineral.png) | 在整理稿上细化108像素，最终相对自动稿净改422像素 |
| [mineral.aseprite](mineral.aseprite) | 分层RGB源稿：automatic-base、native-repairs、material-polish，另有空白初始层 |
| [palette.json](palette.json) | 28色并集，来自下述原版与MOD参考，不是统一原版色板 |
| [refinement-edits.json](refinement-edits.json)、[material-edits.json](material-edits.json) | 人工设计的坐标、before/after RGBA与部位分组，可重放，不是自动修复算法 |
| [manifest.json](manifest.json) | 相对路径、源文件哈希、参数、验收与能力边界 |

参考色板：原版 [炉子](../../evidence/native/Sprite/furnace.png)、[便携净水器](../../evidence/native/Sprite/portable_water_purifier.png) 与MOD [卷烟机](../../../src/Assets/CorpPresence.art.pastel.rolling_machine.png)。便携净水器不等于机器purifier。结构遵循已认可的料斗—横向培养筒—侧置水路/泵—下托盘和承重底座。

manifest对PNG/Aseprite按原字节校验，对JSON按UTF-8、去BOM、统一LF校验，避免跨机器Git换行转换被误判为素材变化。

## 已验证与限制

最终48×64、23个可见RGB、1252个不透明像素、1820个透明像素、0半透明；100%属于参考并集。包围盒(2,14)..(45,55)，保留源图留白。自动稿到最终稿Alpha逐像素变化0，净改422像素，即主体33.71%；两轮编辑有重叠，不能用351+108代替净变化。

在原尺寸及最近邻检查下，主料斗、横筒、水箱、泵和托盘可辨；材质采用连续底色、短高光、邻接暗面及局部反光。Aseprite源稿在本次交付时重新导出，RGBA差异0，manifest锁定源稿/PNG哈希。脚本重放不调用Aseprite，因此不能代替未来编辑后的源稿重新导出校验。

单四连通组件只说明整机像素相连，不证明每条管道功能正确。23色与精确尺寸不证明审美质量；用户确认的是保留造型继续细化材质，没有据此宣称其已通过游戏验收。主体占44×42与画布48×64、玩法3×4占格分开记录，不能自动从透明掩膜推定点击区域。

## 复现

Windows PowerShell 7，从仓库根目录运行，输出目录选择一个不存在的仓库外路径：

```powershell
./art/tools/native/Test-NativeWorkflow.ps1 -OutputDirectory '<新的工作目录>'
```

会验证原版放大恢复、网格/连通计数、生成源到自动稿的转换、两轮像素编辑重放、351/108/422改动数、Alpha与色板及文件哈希，并拒绝把修改清单应用到错误版本。不会重新请求图像模型、启动Aseprite或游戏，也不会覆盖本目录。工具详见[README](../../tools/native/README.md)。
