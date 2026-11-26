using System;
using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal sealed class OverhaulWorkshopStock
{
    private static readonly Dictionary<string, Part> Parts = new()
    {
        [IndustrialGoods.Pad]=Part.Pad, [IndustrialGoods.Frame]=Part.Frame, [IndustrialGoods.Contact]=Part.Contact, [IndustrialGoods.Unit]=Part.Unit,
        [IndustrialGoods.Blend]=Part.Blend, [IndustrialGoods.Alloy]=Part.Alloy, [IndustrialGoods.Billet]=Part.AlloyBlank,
        [IndustrialGoods.Blank]=Part.Blank, [IndustrialGoods.Liner]=Part.Liner, [CartelReactor.Gel]=Part.Gel,
        [ClassifierGoods.Crystal]=Part.Grit, [CartelReactor.Crystal]=Part.Precipitate, [CartelReactor.Salt]=Part.Core,
        [ClassifierGoods.Coarse]=Part.Coarse, [ClassifierGoods.Fine]=Part.Fine, [CartelReactor.Resin]=Part.Resin
    };
    internal readonly Dictionary<Part,int> Stock = new();
    internal readonly long[] Powder = new long[5];
    internal readonly Dictionary<int,int> Deliveries = new();
    private readonly HashSet<string> machines = new();
    internal readonly OverhaulQueue Queue = new();
    internal long PureMetal;
    internal int OldShells, CleanShells, WashesAvailable;
    internal bool CanOrderToday = true, UnknownDelivery, FrozenCommission;
    internal OverhaulEscrow Escrow;
    internal int Transit => Deliveries.Values.Sum();

    internal static OverhaulWorkshopStock Capture(int day, RotationContract repair = null)
    {
        var result = new OverhaulWorkshopStock();
        var owned = EmporiumEntry.Instance?.GetAllOwnedItems(includeAfterhour: false);
        if (owned == null) return result;
        var seen = new HashSet<GameItem>();
        var seenShells = new HashSet<int>();
        foreach (var item in owned)
        {
            if (item == null || !seen.Add(item) || !ShopInventory.Contains(item) || !GeneralHelper.IsItemOwned(item)
                || item is GameItemElement element && element.IsDestroyed()) continue;
            var key = Goods.IdOf(item);
            if (key != null) result.machines.Add(key);
            if (key == PowderSieve.Key)
                for (int bin=0; bin<6; bin++) result.AddPowder(PowderSieve.ReadBin(item,bin));
            result.ReadMachine(item, key);
            bool box = PowderBox.IsOwnedBox(item);
            if (box || key == CartelReactor.Crystal)
            {
                // A charge-only receipt has not established whether this particular feed was debited.
                var parent = item.parentInventory?.GetParentItem();
                if (Goods.IdOf(parent) == PowderClassifier.Key && PowderClassifier.ReadQueue(parent) is { Active: true, Recipe: null })
                    continue;
            }
            if (box)
            {
                if (item.MayRemove() && item.MaxNumRemove() > 0)
                {
                    var parts = new int[5];
                    if (PowderBox.Read(item,parts) <= PowderColor.Capacity) result.AddPowder(parts);
                }
                continue;
            }
            if (repair?.Kind == RotationKind.Repair && item.MayRemove() && item.MaxNumRemove() > 0)
            {
                int alcohol = AlcoholPreparationStation.AlcoholUlAvailable(item);
                result.WashesAvailable += alcohol / IndustrialWorkbench.WashAlcoholUl;
                if (IndustrialGoods.TryReadOrigin(item,out string run,out int serial,out int slot)
                    && run == CorpState.Current.RunId && serial == repair.Serial && RepairReturns.Outstanding(item)
                    && seenShells.Add(slot))
                {
                    if (key == IndustrialGoods.OldShell) result.OldShells++;
                    else if (key == IndustrialGoods.CleanShell) result.CleanShells++;
                    else if (key == IndustrialGoods.Unit) result.Stock[Part.Unit]=result.Stock.GetValueOrDefault(Part.Unit)+1;
                }
            }
            Part part;
            if (item.identifier == Forge.BilletId) part=Part.Steel;
            else if (item.identifier == SleepMoss.BlockId) part=Part.Moss;
            else if (Forge.IsGoodIngot(item)) part=Part.Ingot;
            else if (key == null || !Parts.TryGetValue(key,out part)) continue;
            if (part == Part.Unit && !IndustrialGoods.IsFreshUnit(item)) continue;
            if (!item.MayRemove()) continue;
            int available = Math.Min(item.unitCount, item.MaxNumRemove());
            if (available > 0) result.Stock[part] = (int)Math.Min(int.MaxValue,(long)result.Stock.GetValueOrDefault(part)+available);
        }
        if (CorpState.Current?.Companies?.TryGetValue("meridian",out var meridian) == true)
        {
            result.CanOrderToday = meridian.LastOrderDay < day;
            foreach (var order in meridian.Orders.Where(o => o?.Kind == MeridianIndustrial.OrderKind))
            {
                if (order.Day < 0 || order.Items == null || order.Items.Count is < 1 or > MeridianCommissionReceipt.Maximum
                    || order.Items.Any(id => id != MeridianCommissionReceipt.FrameId))
                { result.UnknownDelivery = true; continue; }
                int arrival = Math.Max(0, order.Day - day);
                result.Deliveries[arrival] = result.Deliveries.GetValueOrDefault(arrival) + order.Items.Count;
            }
        }
        var receipt = CorpState.Current?.Workshop?.Commission;
        if (receipt != null)
        {
            result.FrozenCommission = !receipt.Valid() || receipt.InFlight;
            if (!result.FrozenCommission) result.Escrow = new(receipt.Quantity, receipt.Ingots, receipt.Alloy, receipt.Paid);
        }
        return result;
    }

    private void ReadMachine(GameItem item, string key)
    {
        if (key == PowderClassifier.Key)
        {
            var batch = PowderClassifier.ReadQueue(item);
            if (!batch.Active) Queue.Observe(OverhaulBay.Mill, 0);
            else if (batch.Recipe == null) Queue.Observe(OverhaulBay.Mill, -1);
            else
            {
                var r = batch.Recipe;
                // A blocked mature classifier or mineralizer retries on the next closing cycle.
                var products = new[] { (Part.Coarse,r.Coarse), (Part.Fine,r.Fine), (Part.Grit,r.Crystal) };
                Queue.Observe(OverhaulBay.Mill, Math.Max(1,batch.Nights), batch.Nights * r.NightPower, 0,
                    products.Where(p => p.Item2 > 0).ToArray());
            }
        }
        else if (key == MineralGrowthStation.Key)
        {
            var batch = MineralGrowthStation.ReadQueue(item);
            if (!batch.Active) Queue.Observe(OverhaulBay.Growth, 0);
            else if (batch.Recipe == null) Queue.Observe(OverhaulBay.Growth, -1);
            else Queue.Observe(OverhaulBay.Growth, Math.Max(1,batch.Nights), batch.Charge, batch.Water,
                batch.Recipe == MineralGrowthBatch.LightSalt ? new[]{(Part.Core,batch.Recipe.Count)} : Array.Empty<(Part,int)>());
        }
        else if (key == CartelReactor.Key)
        {
            ReadVessel(OverhaulBay.Blue, CartelReactor.ReadVessel(item,true), Part.Gel);
            ReadVessel(OverhaulBay.White, CartelReactor.ReadVessel(item,false), Part.Precipitate);
        }
        void ReadVessel(OverhaulBay bay, int state, Part part)
        {
            if (state == 0) Queue.Observe(bay, 0);
            else if (state is 1 or 2) Queue.Observe(bay, state == 1 ? 1 : 0,
                state == 1 ? MachineryHelper.GetMachinePowerUsage(item) : 0, 0, (part,1));
            else Queue.Observe(bay, -1);
        }
    }

    private void AddPowder(int[] parts)
    {
        var grade = PowderClassification.FromParts(parts);
        if (grade.Branch is not PowderComponent c) return;
        Powder[(int)c] += parts[(int)c];
        if (c == PowderComponent.Metal && grade.Grade == PowderGrade.Pure) PureMetal += parts[(int)c];
    }

    internal IEnumerable<string> MissingEquipment(OverhaulPlan p, bool smelt)
    {
        var required = new (bool Needed,string Id)[] {
            (p.SandRuns+p.CrystalRuns>0,PowderClassifier.Key), (p.CrystalRuns>0,ClassifierGoods.Sleeve),
            (p.AlloyRuns+p.Blanks+(smelt ? p.ExtraIngotNights : 0)>0,SinteringStation.Key), (p.Regrinds>0,MineralCrusher.Key),
            (p.GrowthRuns>0,MineralGrowthStation.Key), (p.GrowthRuns>0,MineralGrowthBatch.LightSaltBlueprint),
            (p.Moss>0,CartelReactor.Key), (p.MakeUnits+p.MakeContacts+p.Liners+p.Blends+p.Pads>0,CraftingStation.Key),
            (p.Steel>0,"forge_furnace"), (p.SelfFrames>0,"forge_anvil") };
        return required.Where(row=>row.Needed && !machines.Contains(row.Id)).Select(row=>Goods.NameOf(row.Id));
    }
}
