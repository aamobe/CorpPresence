# 素材任务记录模板

先选路线，实际验收按 [AGENTS.md](AGENTS.md) 与 [NATIVE-WORKFLOW.md](NATIVE-WORKFLOW.md)。

## 规格

```text
名称／资源键：
类别：物品 / 机器 / 人物 / UI
路线：模型直出测试 / 原生直接绘制 / 生成稿转换后原生整理 / 游戏接入
交付范围与造型确认：
结构主参考／材质参考（资源名、版本、继承内容）：
最终真实W×H／玩法占格／主体包围盒与留白：
锁定区域／允许修改区域：
光源约定：
色板RGB及每组来源（原版与MOD分开）：
Alpha原始分布／目标规则：
PPU / pivot / 图集 / 过滤（未接入则写未验证）：
机械连接、支撑与遮挡：
高光二维范例：主亮带、邻行暗带、端部、坐标、段长：
Aseprite与集成可用性（原生路线）：
```

## 生图提示词：严格像素周期（约束目标）

这些是要求，不是能力保证。已测入口不能稳定按提示输出准确小尺寸或精确RGB；不得据模板宣布验收通过。默认透明；单色不透明背景只在实验或用户指定时替换。

```text
Faithfully reproduce [SUBJECT] from the supplied reference as a standalone [WIDTH]×[HEIGHT] pixel-art sprite.
Preserve its silhouette, proportions, component positions, viewing angle and specified margins.
Use one coherent logical pixel grid. Every pixel block has one constant RGB value.
Use only this measured palette: [HEX COLORS AND MATERIAL ROLES].
Use flat continuous color regions and hard-edged patches to describe planes, seams and short highlights.
Preserve functional connections and thin details that fit the target grid. No invented decorative noise.
Transparent background and internal gaps; opaque object pixels. No gradients, texture, dithering, antialiasing, glow or shadow outside the object.
Only the sprite; no captions, grid lines or surrounding scene.
```

重新设计时将忠实复现两句替换为“Design a new [SUBJECT] using [REFERENCE] for [STRUCTURE/MATERIAL]. Follow this component layout and margins: [LAYOUT].” 不同时要求新造型和完全保留旧造型。填写实际参数，删除不适用句。

## 转换与原生整理记录

```text
生成原文件路径／SHA256／实际宽高／Alpha／RGB：
是否有可信统一网格，检测方法与局限：
转换参数：目标尺寸、取样、色距、色板、Alpha阈值、裁切、原点：
未修整草稿及哈希：
原生编辑图层／逐像素修改清单：
每轮修改数／相对自动稿净修改数（去除重叠）：
连接、管路、支承、格栅、连续平面检查：
最终PNG／Aseprite／哈希：
实际宽高、RGB数、Alpha分布：
1×检查及必要的最近邻检查预览（另存）：
源稿重新导出RGBA差异：
已完成：生成 / 原生像素验收 / 编译部署 / 游戏实测
未验证项与接入时待测场景：
```

只交模型原文件时，转换与修整字段写“未执行”，不补做转换后再称直出。
