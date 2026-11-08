# 内置翻译与社区贡献

当前1.0稳定版采用内置翻译，保留1621个稳定本地化键、中英俄法文本及缺键回退。语言跟随游戏设置。

为取消文件访问权限要求，已移除外部JSON翻译包加载和`language.json`语言覆盖。将文件放入`Translations`目录不会生效；旧文件无需删除，也不会被模组读取或修改。

翻译者仍可通过稳定键维护译文。中英原文保留在源码调用处，俄法译文位于[Translations.Keys.json](../../src/CorpPresence/Presentation/Translations.Keys.json)，每个键对应`[ru, fr]`数组。保留键、占位符、格式说明符和换行；缺译保持空值，沿用既有回退。

[导出工具](../../tools/Export-TranslationTemplate.ps1)仍可生成键和英文参考，供协作、审阅及源码集成使用；导出文件不能直接作为当前模组的运行时翻译包。修改内置译文或新增语言需要调整源码、编译并发布。新增语言还需接入语言选择和字体支持，当前不承诺任意语言可直接加载。

维护规则见[本地化键维护](../maintenance/localization-keys.md)。
