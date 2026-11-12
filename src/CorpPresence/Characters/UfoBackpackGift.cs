using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static class UfoBackpackGift
{
    internal const long Number = 9985;
    internal const string PhoneId = "corp_phone_ufo_backpack_gift";
    internal const string VisitorId = "corp_ufo_backpack_gift_aamobe";
    private static readonly GiftDeliveryGate Gate = new();

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
            return;
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
        string text = !Available()
            ? L.K("ufoBackpack.phone.notReady", "暂时无法安排礼物，请稍后再拨。", "The gift cannot be arranged right now. Please call again later.")
            : state.UfoBackpackGiftStatus == 2
                ? L.K("ufoBackpack.phone.claimed", "飞碟背包已经送到啦，感谢支持！", "Your UFO Backpack has already arrived. Thank you for your support!")
                : L.K("ufoBackpack.phone.pending", "飞碟背包已为你留好，我会亲自送来。请在柜台留出3×3空位。", "Your UFO Backpack is reserved. I will bring it personally. Leave a 3 x 3 space on the counter.");
        return new Dialogue().SetText("Aamobe", text).SetEndAction(() =>
        {
            if (!session.IsCurrent || PhoneUIManager.Instance?.currentPhoneClient?.locID != PhoneId) return;
            try
            {
                if (!Available() || state.UfoBackpackGiftStatus == 2) return;
                state.UfoBackpackGiftStatus = 1;
                Schedule(SaveSlot.CurrentStore?.storeClientManager);
            }
            finally { PhoneUIManager.Instance.OnPhoneDialogEnd(); }
        });
    }

    internal static void Schedule(StoreClientManager manager)
    {
        if (Settings.CleanupForUninstall || !GameResources.Ready || manager?.clientStack == null) return;
        CorpState state = CorpState.Get();
        StoreSession session = StoreSession.Capture();
        if (state?.UfoBackpackGiftStatus != 1 || !session.IsCurrent || !SaveSlot.CanWrite("CorpPresence")) return;
        if (manager.clientStack.Any(c => c?.identifier == VisitorId)
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
            L.K("ufoBackpack.visitor.thanks", "飞碟赠礼", "A gift from UFO"))
            .SetStartAction(() =>
            {
                if (!Current()) return;
                bool delivered = Gate.TryDeliver(() => Current() && state.UfoBackpackGiftStatus == 1,
                    () => PlaceGift(Current), () => state.UfoBackpackGiftStatus = 2);
                if (!delivered && Current() && state.UfoBackpackGiftStatus == 1)
                    StoreUIManager.Instance?.Notify(L.K("ufoBackpack.delivery.full", "请在柜台留出3×3空位，飞碟背包会再送来。", "Leave a 3 x 3 space on the counter. The UFO Backpack will be delivered again."));
            }).SetEndAction(() =>
            {
                if (Current()) UnityEngine.Object.FindObjectOfType<DialogUIManager>()?.CurrentClientLeave();
            });
        if (session.IsCurrent) manager.clientStack.Insert(0, client);
    }

    private static bool PlaceGift(Func<bool> current)
    {
        GameItem item = null;
        bool placed = false;
        try
        {
            item = Goods.Make(UfoBackpack.Key);
            if (item == null || !current()) return false;
            GeneralHelper.SetItemOwned(item);
            if (!UfoBackpack.InitializeGift(item)) return false;
            if (!current()) return false;
            try { SaveSlot.CurrentStore.TryAddDirectSellingItemToTable(item, isOwend: true); }
            catch (Exception error) { try { ModLog.Warn("飞碟背包放置：" + error.Message); } catch { } }
            placed = item.parentInventory != null;
            return placed;
        }
        catch (Exception error)
        {
            ModLog.Warn("飞碟背包准备失败：" + error.Message);
            return false;
        }
        finally { if (!placed && !StorePlacement.Destroyed(item)) item.Destroy(); }
    }
}
