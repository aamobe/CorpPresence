#if LOCAL_TEST_SPECIMEN
using UnityEngine;

namespace CorpPresence.Runtime;

internal static class LocalTestSpecimen
{
    private static readonly RuntimeTask Grant = new("F2 三叶蜥测试发放", Give,
        (name, error, _) => ModLog.Warn(name + "失败：" + error.Message));

    internal static void Tick()
    {
        if (Input.GetKeyDown(KeyCode.F2)) Grant.Run(Time.unscaledTime);
    }

    private static void Give()
    {
        var ui = StoreUIManager.Instance;
        var shop = EmporiumEntry.Instance;
        if (ui == null || shop == null || !GameResources.Ready || Settings.CleanupForUninstall
            || !StoreSession.Capture().IsCurrent || !SaveSlot.CanWrite("CorpPresence")) return;

        GameItem item = Goods.Make("silverblood");
        if (item == null)
        {
            ui.Notify("测试：三叶蜥工厂尚未就绪。");
            return;
        }

        try
        {
            var species = SilverBlood.Species;
            int age = species.stageStartDays[(int)FishStage.Adult];
            item.ModifyTag("FISH_AGE_TAG", state => state.Enable().SetInt(age));
            AquacultureHelper.SeedKcalEatenForAge(item, species);
            AquacultureHelper.UpdateNameAndSprite(item);
            GeneralHelper.SetItemOwned(item);
            bool placed = EscapeePlacement.TryPlace(shop, item);
            ui.Notify(placed ? "测试：已发放一只成年银血三叶蜥。" : "测试：主库存和后柜台放不下三叶蜥。");
        }
        finally
        {
            if (item.parentInventory == null && !(item is GameItemElement element && element.IsDestroyed()))
                item.Destroy();
        }
    }
}
#endif
