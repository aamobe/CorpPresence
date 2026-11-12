using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static class FufuDollGift
{
    internal const long Number = 1215;
    internal const string PhoneId = "corp_phone_fufu_doll_gift";
    internal const string VisitorId = "corp_fufu_doll_gift_aamobe";
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
            ? L.K("fufuDoll.phone.notReady", "暂时无法安排礼物，请稍后再拨。", "The gift cannot be arranged right now. Please call again later.")
            : state.FufuDollGiftStatus == 2
                ? L.K("fufuDoll.phone.claimed", "Fufu玩偶已经送到啦，感谢支持！", "Your Fufu Doll has already arrived. Thank you for your support!")
                : L.K("fufuDoll.phone.pending", "Fufu玩偶已为你留好，我会亲自送来。请在柜台留出3×3空位。", "Your Fufu Doll is reserved. I will bring it personally. Leave a 3 x 3 space on the counter.");
        return new Dialogue().SetText("Aamobe", text).SetEndAction(() =>
        {
            if (!session.IsCurrent || PhoneUIManager.Instance?.currentPhoneClient?.locID != PhoneId) return;
            try
            {
                if (!Available() || state.FufuDollGiftStatus == 2) return;
                state.FufuDollGiftStatus = 1;
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
        if (state?.FufuDollGiftStatus != 1 || !session.IsCurrent || !SaveSlot.CanWrite("CorpPresence")) return;
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
            L.K("fufuDoll.visitor.thanks", "感谢芙老师的支持。", "Thank you, Teacher Fu, for your support."))
            .SetStartAction(() =>
            {
                if (!Current()) return;
                bool delivered = Gate.TryDeliver(() => Current() && state.FufuDollGiftStatus == 1,
                    () => PlaceGift(Current), () => state.FufuDollGiftStatus = 2);
                if (!delivered && Current() && state.FufuDollGiftStatus == 1)
                    StoreUIManager.Instance?.Notify(L.K("fufuDoll.delivery.full", "请在柜台留出3×3空位，Fufu玩偶会再送来。", "Leave a 3 x 3 space on the counter. The Fufu Doll will be delivered again."));
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
            item = Goods.Make(FufuDoll.Key);
            if (item == null || !current()) return false;
            GeneralHelper.SetItemOwned(item);
            if (!current()) return false;
            try { SaveSlot.CurrentStore.TryAddDirectSellingItemToTable(item, isOwend: true); }
            catch (Exception error) { try { ModLog.Warn("Fufu玩偶放置：" + error.Message); } catch { } }
            placed = item.parentInventory != null;
            return placed;
        }
        finally { if (!placed && !StorePlacement.Destroyed(item)) item.Destroy(); }
    }
}
