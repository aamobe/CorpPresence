# 官方CSV翻译与社区贡献

当前1.0稳定版复用1621个稳定键，通过游戏官方`ModHelper.GetLocalized()`读取游戏加载器提供的CSV译文；模组不自行读取翻译文件，清单保持零权限。

## 修改译文

在实际加载的`CorpPresence.dll`旁，`Localization`目录包含`en.csv`、`ru.csv`、`fr.csv`，每份有1621个键。直接修改对应译文即可，无需重新编译DLL；完全退出并重启游戏后加载。工坊更新可能覆盖修改，请在编辑前自行备份。

每行格式为`键,"译文"`，例如：

```csv
ui.menu.brandCatalog,"Catalogue de marque"
ui.checkout.cash,"Solde {0}₵"
```

保持键名不变。译文内的双引号写成两个双引号`""`；换行写为字面`\n`；逗号可保留在引号内。每个条目占一个物理行，文件保存为UTF-8。保留原占位符编号与格式说明符；可以重排或重复占位符，不能删除参数或增加不存在的参数。

## 语言与回退

CSV文件名必须匹配游戏当前选中的语言代码。例如选中`fr-CA`时，编辑`fr-CA.csv`；官方接口不会自动继承`fr.csv`。缺少地区CSV时，模组仍保留既有俄法地区语言回退。

中文继续以调用处原文为默认，可新建匹配游戏中文代码的局部CSV提供覆盖。中文缺键不会被英文CSV取代。

缺键、空译文、占位符不兼容或格式化失败时，沿用内置稳定表、旧翻译表和调用处原文回退，不直接显示缺失键名。删除全部CSV也能沿用原有中英俄法文本。

可以为游戏已有的其他语言代码添加CSV。添加文件不会自动增加游戏语言菜单选项或字体；当前不提供独立于游戏语言的模组语言选择，不恢复`Translations/*.json`和`language.json`。

## 翻译维护

以`Localization/en.csv`为完整1621键参考。俄法默认CSV来自[内置稳定表](../../src/CorpPresence/Presentation/Translations.Keys.json)及英文原文。生成默认文件使用[Export-OfficialLocalization.ps1](../../tools/Export-OfficialLocalization.ps1)；该工具会覆盖指定输出目录中的默认CSV，译者手工修改后不要直接运行它。

[本地化维护规则](../maintenance/localization-keys.md)记录接口与回退。开发检查同时用已安装游戏的实际CSV值解析器核对默认文件，以及用游戏语言/API边界替代运行回退、格式化、菜单与加载时序检查，不启动游戏。
