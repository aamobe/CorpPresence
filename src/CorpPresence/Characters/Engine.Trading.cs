using System;
using System.Collections.Generic;
using System.Linq;
namespace CorpPresence;
internal static partial class Engine
{
    private static StoreClient CurrentBuyer(GameItem item)
    {
        if (!BuyActive || !GameResources.Ready || !Owned(item)) return null;
        StoreClient client = SaveSlot.CurrentStore?.currentClientInstance?.GetClientBlueprint();
        return client?.identifier?.StartsWith("corp_", StringComparison.Ordinal) == true
            && (client.clientIntent == StoreClient.ClientIntent.BUY || client.clientIntent == StoreClient.ClientIntent.SELLNBUY) ? client : null;
    }

    private static Character FindBuyer(StoreClient client)
    {
        foreach (Character character in All)
            if (client.identifier.StartsWith(character.Prefix, StringComparison.Ordinal)) return character;
        return null;
    }

    private static bool AcceptsVisit(Character character, GameItem item, string kind) =>
        kind.StartsWith("daily", StringComparison.Ordinal) ? character.DailyAccepts(item) : character.Accepts(item);

    public static long? BuyFixedPrice(GameItem item)
    {
        StoreClient client = CurrentBuyer(item);
        if (client == null) return null;
        Character character = FindBuyer(client);
        if (character == null) return null;
        string kind = client.identifier.Substring(character.Prefix.Length);
        return AcceptsVisit(character, item, kind) ? character.FixedPrice(item, kind) : null;
    }

    public static double BuyMultiplier(GameItem item)
    {
        StoreClient client = CurrentBuyer(item);
        if (client == null) return 1.0;
        if (client.identifier.StartsWith("corp_solana_lab_", StringComparison.Ordinal)) return CompanyVisitors.LabMultiplier(client, item);
        Character character = FindBuyer(client);
        if (character == null) return 1.0;
        string kind = client.identifier.Substring(character.Prefix.Length);
        return AcceptsVisit(character, item, kind) ? character.PriceMultiplier(item, kind) : 1.0;
    }

    private static bool Owned(GameItem item)
    {
        try { return item?.IsTag("IS_OWNED_TAG") == true; }
        catch { return false; }
    }

    private static long Place(Character c, string id, bool owned)
    {
        TryPlace(c, id, owned, out long value);
        return value;
    }

    private static bool TryPlace(Character c, string id, bool owned, out long value)
    {
        value = 0;
        StoreSession session = StoreSession.Capture();
        PlayerStore store = SaveSlot.CurrentStore;
        if (!session.IsCurrent || !SaveSlot.CanWrite("CorpPresence") || CompanyVisitors.CounterFull()) return false;
        GameItem item = null;
        try
        {
            item = c.MakeItem(id);
            if (item == null || !session.IsCurrent) return false;
            bool attached;
            try { attached = store.TryAddDirectSellingItemToTable(item, owned) || item.parentInventory != null; }
            catch (Exception error)
            {
                WarnVisit(c.Name + "摆货异常：" + error.Message);
                attached = item.parentInventory != null;
            }
            if (!attached) return false;
            try { value = item.GetCurrentValue(useRetailMarkup: false, withChild: true, includeEvents: false, forceMarkup: false, 0L); }
            catch { }
            return true;
        }
        catch (Exception error) { WarnVisit(c.Name + "摆货失败：" + error.Message); return false; }
        finally
        {
            if (item != null && item.parentInventory == null)
                try { item.Destroy(); } catch (Exception error) { WarnVisit("未放置物品回收失败：" + error.Message); }
        }
    }

    private static bool PlaceVisitGifts(Character c, string[] gifts, CharacterVisitSession visit)
    {
        if (gifts.Length == 0) return true;
        if (!visit.IsCurrent) return false;
        Company company = Companies.ByCharacter(c.Id);
        CompanyState state = company == null ? null : Companies.StateOf(company);
        if (state?.Orders == null) return false;
        // Persist owed gifts through the existing delivery schema before creating objects.
        Order pending = new Order { Day = StoreStation.GetDayCounter() + 1, Items = new List<string>(gifts) };
        state.Orders.Add(pending);
        foreach (string id in gifts)
        {
            if (!visit.IsCurrent) return false;
            if (TryPlace(c, id, owned: true, out _)) pending.Items.Remove(id);
        }
        if (pending.Items.Count == 0) state.Orders.Remove(pending);
        else LogVisit(c.Name + "的未领取赠品保留为配送订单");
        return true;
    }

    private static void TopUp(Character c, string[] pool, long total, int count)
    {
        StoreSession session = StoreSession.Capture();
        PlayerStore store = SaveSlot.CurrentStore;
        if (!session.IsCurrent || store == null || pool == null || pool.Length == 0) return;
        int cash = store.GetCash();
        long target = Math.Min(cash / 4, 4000);
        int remaining = Math.Clamp(3 + cash / 1000, 3, 10) - count;
        if (remaining <= 0 || total >= target || CompanyVisitors.CounterFull()) return;
        var candidates = new List<(string Id, long Value)>();
        foreach (string id in pool.Distinct()) { if (!session.IsCurrent) return; candidates.Add((id, ValueOf(c, id))); }
        System.Random random = new System.Random();
        while (session.IsCurrent && remaining > 0 && total < target && !CompanyVisitors.CounterFull())
        {
            long each = (target - total) / remaining;
            int first = -1, second = -1;
            long firstDistance = long.MaxValue, secondDistance = long.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                long price = candidates[i].Value;
                long distance = price >= each ? price - each : each - price;
                if (first < 0 || distance < firstDistance)
                { second = first; secondDistance = firstDistance; first = i; firstDistance = distance; }
                else if (second < 0 || distance < secondDistance)
                { second = i; secondDistance = distance; }
            }
            int chosen = second < 0 || random.Next(2) == 0 ? first : second;
            total += Place(c, candidates[chosen].Id, owned: false);
            remaining--;
        }
    }

    private static long ValueOf(Character c, string id)
    {
        GameItem item = null;
        try
        {
            item = c.MakeItem(id);
            return Math.Max(0L, item?.GetCurrentValue(useRetailMarkup: false, withChild: true, includeEvents: false, forceMarkup: false, 0L) ?? 0);
        }
        catch { return 0; }
        finally { if (item != null && item.parentInventory == null) try { item.Destroy(); } catch { } }
    }
}

