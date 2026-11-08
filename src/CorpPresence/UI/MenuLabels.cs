namespace CorpPresence.UI;

// Fixed-size labels; no growing cache and no PNG data retention.
internal static class MenuLabels
{
 private static readonly string[][] Chinese = {
new string[5] { "正品订货", "体力增强剂申领", "赝品换正品", "标本", "品牌目录" },
new string[4] { "订货", "利维坦净水器", "罐装设备", "品牌目录" },
new string[5] { "制式订货", "高端配件", "返厂翻新", "锻造", "品牌目录" },
new string[4] { "邮购订货", "下层合作商", "下层客流", "品牌目录" },
new string[3] { "今天进货吗？", "又是我！", "货多，随便挑。" },
new string[3] { "军械订货", "地下销赃网络", "任务" },
new string[2] { "军械订货", "任务" },
new string[1] { "和解" },
new string[1] { "营救奥丁" },
new string[5] { "贵宾引荐", "实验室采购单", "会员礼遇", "品牌目录", "香水工艺" },
new string[3] { "码头补给", "街坊互助", "品牌目录" },
new string[6] { "开源军械", "地下药房", "码头补给", "街坊互助", "合作阵线", "品牌目录" }
 };
 private static readonly string[][] English = {
new string[5] { "Genuine Orders", "Stamina Booster Request", "Fake-for-Genuine Swap", "Specimens", "Brand Catalog" },
new string[4] { "Orders", "Leviathan Purifier", "Canning Equipment", "Brand Catalog" },
new string[5] { "Standard Issue Orders", "High-End Attachments", "Factory Refurbishment", "Forging", "Brand Catalog" },
new string[4] { "Mail Order", "Lower Dealer", "Lower Foot Traffic", "Brand Catalog" },
new string[3] { "Stocking up today?", "Me again!", "Plenty of goods. Take your pick." },
new string[3] { "Arms Orders", "Fence Network", "Tasks" },
new string[2] { "Arms Orders", "Tasks" },
new string[1] { "Make Peace" },
new string[1] { "Rescue Odin" },
new string[5] { "VIP Referral", "Lab Purchase Order", "Member Perks", "Brand Catalog", "Perfumery" },
new string[3] { "Dock Supplies", "Mutual Aid", "Brand Catalog" },
new string[6] { "Open-Source Arms", "Back-Alley Pharmacy", "Dock Supplies", "Mutual Aid", "Joint Front", "Brand Catalog" }
 };
 private static string[] _lastBase;

 // Same shape and order as the existing menu arrays; these keys never select services.
 private static readonly string[][] Keys = {
new[] { "ui.menu.carmichael.genuineOrders", "ui.menu.carmichael.staminaRequest", "ui.menu.carmichael.swap", "ui.menu.carmichael.specimens", "ui.menu.brandCatalog" },
new[] { "ui.menu.davre.orders", "ui.menu.davre.purifier", "ui.menu.davre.canning", "ui.menu.brandCatalog" },
new[] { "ui.menu.meridian.orders", "ui.menu.meridian.attachments", "ui.menu.meridian.refurbishment", "ui.menu.meridian.forging", "ui.menu.brandCatalog" },
new[] { "ui.menu.pastel.mailOrder", "ui.menu.pastel.dealer", "ui.menu.pastel.footTraffic", "ui.menu.brandCatalog" },
new[] { "ui.menu.milo.stocking", "ui.menu.milo.again", "ui.menu.milo.goods" },
new[] { "ui.menu.rebels.arms", "ui.menu.rebels.fence", "ui.menu.rebels.tasks" },
new[] { "ui.menu.rebels.arms", "ui.menu.rebels.tasks" },
new[] { "ui.menu.rebels.peace" },
new[] { "ui.menu.rebels.rescueOdin" },
new[] { "ui.menu.solana.vip", "ui.menu.solana.lab", "ui.menu.solana.perks", "ui.menu.brandCatalog", "ui.menu.solana.perfumery" },
new[] { "ui.menu.spacer.supplies", "ui.menu.spacer.aid", "ui.menu.brandCatalog" },
new[] { "ui.menu.spacer.arms", "ui.menu.spacer.pharmacy", "ui.menu.spacer.supplies", "ui.menu.spacer.aid", "ui.menu.spacer.front", "ui.menu.brandCatalog" }
 };
 private static string _lastExtra;
 private static string[] _lastCombined;

 private static readonly System.Lazy<string[][]> Russian = new(() => TranslateEnglish("ru"));
 private static readonly System.Lazy<string[][]> French = new(() => TranslateEnglish("fr"));

 private static string[][] TranslateEnglish(string locale)
 {
     var result = new string[English.Length][];
     for (int group = 0; group < English.Length; group++)
     {
         result[group] = new string[English[group].Length];
         for (int line = 0; line < English[group].Length; line++)
             result[group][line] = TextTranslations.SelectKey(Keys[group][line], English[group][line], locale);
     }
     return result;
 }

 public static string[] Get(int index, bool english)
 {
     string locale = L.Locale;
     if (!english) return Chinese[index];
     if (TextTranslations.IsBuiltInLocale(locale, "ru")) return Russian.Value[index];
     if (TextTranslations.IsBuiltInLocale(locale, "fr")) return French.Value[index];
     return English[index];
 }

 // Exactly one cached extension. Dynamic labels cannot grow a dictionary.
 public static string[] Append(string[] basis, string extra)
 {
     if (extra == null) return basis;
     if (object.ReferenceEquals(basis, _lastBase) && extra == _lastExtra) return _lastCombined;
     var combined = new string[basis.Length + 1];
     System.Array.Copy(basis, combined, basis.Length);
     combined[basis.Length] = extra;
     _lastBase = basis;
     _lastExtra = extra;
     return _lastCombined = combined;
 }
}

