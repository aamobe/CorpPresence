using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static class MangoGift
{
    internal const long Number = 1231;
    internal const string PhoneId = "corp_phone_mango_gift";
    internal const string VisitorId = "corp_mango_gift_aamobe";
    internal const string BackpackKey = "mango_backpack";
    private static readonly GiftDeliveryGate Gate = new();

    internal static Goods.Def BackpackDefinition() => new()
    {
        Name = "芒果背包", NameEn = "Mango Backpack",
        Flavor = "美味的大芒果", FlavorEn = "A delicious giant mango",
        Desc = "尝试打开", DescEn = "Try opening it",
        Company = "jackson_private", Art = BackpackKey, Value = 0,
        W = 2, H = 2, Shape = "##|##", Types = Array.Empty<string>(),
        Fallback = "ammo_box", Shell = CreateBackpack
    };

    private static GameItem CreateBackpack()
    {
        var (window, inventory) = DirectoryUtils.CreateInventoryWindow(12, 10);
        var bag = ItemDirectory.CreateEmptyItem().SetContentWindow(window);
        ContainerHelper.InitBackpackItem(inventory, bag);
        var nativeAccepts = inventory.mayInventoryAddItemFunc;
        inventory.mayInventoryAddItemFunc = (item, target) =>
            item?.identifier == CargoWarehouse.MangoId
                ? GeneralHelper.IsItemOwned(item, bag)
                : nativeAccepts == null || nativeAccepts(item, target);
        return bag;
    }

    internal static void Register(Dictionary<long, StorePhoneClient> phones)
    {
        if (phones == null || Settings.CleanupForUninstall) return;
        if (phones.TryGetValue(Number, out var existing))
        {
            if (existing.locID == PhoneId)
            {
                existing.phoneState = StorePhoneClient.PhoneState.Regular;
                existing.phoneClientType = StorePhoneClient.PhoneClientType.None;
            }
            return; // Never steal a number belonging to another mod.
        }
        phones.Add(Number, new StorePhoneClient
        {
            phoneClientType = StorePhoneClient.PhoneClientType.None,
            displayName = "Aamobe", locID = PhoneId, dialogFuncId = "",
            phoneState = StorePhoneClient.PhoneState.Regular,
            cooldownDuration = 0, currentCooldown = 0, dialedBefore = false
        });
    }

    internal static Dialogue Call()
    {
        StoreSession session = StoreSession.Capture();
        CorpState state = CorpState.Get();
        bool Available() => session.IsCurrent && ReferenceEquals(CorpState.Current, state)
            && state != null && SaveSlot.CanWrite("CorpPresence") && !Settings.CleanupForUninstall;
        string text;
        if (!Available()) text = L.K("mango.phone.notReady", "暂时无法安排礼物，请稍后再拨。", "The gift cannot be arranged right now. Please call again later.");
        else if (state.MangoGiftStatus == 2) text = L.K("mango.phone.claimed", "这份芒果心意已经送到啦，感谢支持！", "Your mango gift has already arrived. Thank you for your support!");
        else text = L.K("mango.phone.pending", "礼物已为你留好，我会亲自送来。请在柜台留出 2×2 的空位。", "Your gift is reserved. I will bring it personally. Please leave a 2 x 2 space on the counter.");
        var dialogue = new Dialogue().SetText("Aamobe", text);
        // Native phone UI does not invoke the first line's startAction.
        dialogue.SetEndAction(() =>
        {
            if (!session.IsCurrent || PhoneUIManager.Instance?.currentPhoneClient?.locID != PhoneId) return;
            try
            {
                if (!Available() || state.MangoGiftStatus == 2) return;
                if (state.MangoGiftStatus == 0) state.MangoGiftStatus = 1;
                Schedule(SaveSlot.CurrentStore?.storeClientManager);
            }
            finally { PhoneUIManager.Instance.OnPhoneDialogEnd(); }
        });
        return dialogue;
    }

    internal static void Schedule(StoreClientManager manager)
    {
        if (Settings.CleanupForUninstall || !GameResources.Ready || manager?.clientStack == null) return;
        CorpState state = CorpState.Get();
        StoreSession session = StoreSession.Capture();
        if (state?.MangoGiftStatus != 1 || !session.IsCurrent || !SaveSlot.CanWrite("CorpPresence")) return;
        if (manager.clientStack.Any(client => client?.identifier == VisitorId)
            || SaveSlot.CurrentStore?.currentClientInstance?.GetClientBlueprint()?.identifier == VisitorId) return;
        var client = new StoreClient
        {
            identifier = VisitorId, displayName = "Aamobe",
            spriteName = Portraits.Ensure("malice_envoy", "maleUpper_1"),
            clientFaction = "FACTION_UPPER_LEVEL", clientIntent = StoreClient.ClientIntent.DIALOGUE,
            dismissable = true
        };
        client.AddBasicDialog();
        client.CompleteClientCreation();
        ModHelper.SetClientSource(client, Main.DisplayName);
        bool Current() => session.IsCurrent && ReferenceEquals(CorpState.Current, state)
            && ReferenceEquals(SaveSlot.CurrentStore?.currentClientInstance?.GetClientBlueprint(), client)
            && SaveSlot.CanWrite("CorpPresence") && !Settings.CleanupForUninstall;
        client.mainDialogue = new Dialogue().SetText("Aamobe",
            L.K("mango.visitor.thanks", "感谢芒果老师的支持", "Thank you, Teacher Mango, for your support."))
            .SetStartAction(() =>
            {
                if (!Current()) return;
                bool delivered = Gate.TryDeliver(() => Current() && state.MangoGiftStatus == 1,
                    () => PlaceGift(Current), () => state.MangoGiftStatus = 2);
                if (!delivered && Current() && state.MangoGiftStatus == 1)
                    StoreUIManager.Instance?.Notify(L.K("mango.delivery.full", "请在柜台留出 2×2 空位，礼物会再送来。", "Leave a 2 x 2 space on the counter. Your gift will be delivered again."));
            }).SetEndAction(() =>
            {
                if (Current()) UnityEngine.Object.FindObjectOfType<DialogUIManager>()?.CurrentClientLeave();
            });
        if (session.IsCurrent) manager.clientStack.Insert(0, client);
    }

    private static bool PlaceGift(Func<bool> current)
    {
        GameItem box = null;
        var made = new List<GameItem>();
        bool placed = false;
        try
        {
            box = Goods.Make(BackpackKey);
            if (box == null) return false;
            string[] contents = { CargoWarehouse.MangoKey, CargoWarehouse.ModuleKey, CargoWarehouse.ModuleKey,
                CargoWarehouse.ModuleKey, CargoWarehouse.ModuleKey, CargoWarehouse.ControllerKey };
            foreach (string key in contents)
            {
                if (!current()) return false;
                GameItem item = Goods.Make(key);
                if (item == null) return false;
                made.Add(item);
                GeneralHelper.SetItemOwned(item);
                if (!Crates.Put(box, item)) return false;
                if (item.parentInventory == null && !StorePlacement.Destroyed(item)) item.Destroy();
            }
            if (!current()) return false;
            GeneralHelper.SetItemOwned(box);
            // Observe actual ownership even if native placement throws after accepting.
            try { SaveSlot.CurrentStore.TryAddDirectSellingItemToTable(box, isOwend: true); }
            catch (Exception error) { try { ModLog.Warn("芒果礼盒放置：" + error.Message); } catch { } }
            placed = box.parentInventory != null;
            return placed;
        }
        finally
        {
            if (!placed)
            {
                foreach (var item in made) if (!StorePlacement.Destroyed(item)) item.Destroy();
                if (box != null && !StorePlacement.Destroyed(box)) box.Destroy();
            }
        }
    }
}
