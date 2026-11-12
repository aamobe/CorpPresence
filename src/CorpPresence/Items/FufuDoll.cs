using System;

namespace CorpPresence;

internal static class FufuDoll
{
    internal const string Key = "fufu_doll";
    internal const string ItemId = "corp_presence_fufu_doll";

    internal static Goods.Def Definition() => new()
    {
        Name = "Fufu玩偶", NameEn = "Fufu Doll",
        Flavor = "感谢芙老师的支持。", FlavorEn = "Thank you, Teacher Fu, for your support.",
        Desc = "拥有时夜间偷盗率×50%。放在背包或店内容器中也生效；多个不叠加。",
        DescEn = "While owned, nighttime theft chance is multiplied by 50%. Works inside bags and store containers; multiple dolls do not stack.",
        Company = "jackson_private", Art = Key, Fallback = "ammo_box",
        Value = 0, W = 3, H = 3, Shape = "###|###|###",
        Types = Array.Empty<string>(), Shell = () => ItemDirectory.CreateEmptyItem()
    };
}
