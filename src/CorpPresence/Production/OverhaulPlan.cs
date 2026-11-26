using System;
using System.Collections.Generic;

namespace CorpPresence;

internal enum Part { Frame, Contact, Unit, Blend, Alloy, AlloyBlank, Blank, Liner, Gel, Grit, Precipitate, Core, Coarse, Fine, Steel, Ingot, Moss, Resin, Pad }

internal enum OverhaulBay { Mill, Growth, Blue, White }

internal sealed class OverhaulQueue
{
    internal sealed record Batch(OverhaulBay Bay, int Day, int Power, int Water, (Part Part, int Count)[] Products);
    internal readonly List<Batch> Batches = new();
    private readonly Dictionary<OverhaulBay,int> free = new();

    // One reserved machine of each kind. Other machines already working can still deliver their own paid batches.
    internal void Observe(OverhaulBay bay, int available, int charge = 0, int water = 0, params (Part, int)[] products)
    {
        int release = available < 0 ? int.MaxValue : available;
        free[bay] = Math.Min(release, free.TryGetValue(bay, out int prior) ? prior : int.MaxValue);
        if (available != 0 || products.Length > 0) Batches.Add(new(bay, available, charge, water, products));
    }

    internal bool Free(OverhaulBay bay, int day) => !free.TryGetValue(bay,out int when) || day >= when;
    internal void AddProducts(Dictionary<Part,int> bins, int? day = null)
    {
        foreach (var batch in Batches)
            if (batch.Day >= 0 && (!day.HasValue || day.Value == batch.Day))
                foreach (var (part,count) in batch.Products) bins[part] = bins.GetValueOrDefault(part) + count;
    }
}

internal sealed record OverhaulEscrow(int Quantity, int Ingots, int Alloy, int Paid);

// Extra first-load goods and usable, source-checked shells for repair assemblies.
internal sealed record RotationCargo(int Pads = 0, int Fine = 0, int Liners = 0, int Gel = 0, int ReusedShells = 0);

// Quantities still required by the two trucks, after crediting stock at each production step.
internal sealed class OverhaulPlan
{
    private readonly Dictionary<Part, int> stock;
    private readonly int frames, contacts, units;
    private readonly OverhaulQueue queue;
    private readonly RotationCargo cargo;
    internal readonly int Pads;
    private readonly OverhaulEscrow escrow;
    internal readonly int SelfFrames;
    internal readonly int MakeUnits, MakeContacts, Blends, Frames, AlloyRuns, Regrinds, Blanks, Liners;
    internal readonly int SandRuns, CrystalRuns, GrowthRuns, WhiteRuns, BlueRuns, Steel;
    internal readonly int[] Powder;
    internal readonly int Ingots, ExtraIngotNights, Moss, Resin, FrameOrders, Power, Water;
    internal int ExtraIngotMetal => ExtraIngotNights * 120;
    internal int FrameFee => FrameOrders * 35 - (escrow?.Paid ?? 0);

    internal OverhaulPlan(Dictionary<Part, int> stock, int frames, int contacts, int units, bool outsourced, int incoming,
        OverhaulQueue queue = null, OverhaulEscrow openCommission = null, RotationCargo cargo = null)
    {
        this.stock = stock;
        this.cargo = cargo ?? new RotationCargo();
        this.frames = frames;
        this.contacts = contacts;
        this.units = units;
        this.queue = queue ?? new OverhaulQueue();
        var credited = new Dictionary<Part,int>(stock);
        this.queue.AddProducts(credited);
        int Need(Part key, int count) => Math.Max(0, count - credited.GetValueOrDefault(key));
        Pads = Need(Part.Pad, this.cargo.Pads);
        MakeUnits = Need(Part.Unit, units);
        MakeContacts = Need(Part.Contact, contacts);
        Blends = Need(Part.Blend, Batches(MakeContacts, IndustrialRecipes.ContactYield));
        Frames = Math.Max(0, Need(Part.Frame, frames + Math.Max(0,MakeUnits - this.cargo.ReusedShells)) - Math.Max(0, incoming));
        escrow = outsourced && Frames > 0 ? openCommission : null;
        FrameOrders = outsourced ? Math.Max(Frames, escrow?.Quantity ?? 0) : 0;
        SelfFrames = outsourced ? 0 : Frames;
        Steel = Need(Part.Steel, SelfFrames);
        Ingots = Steel + FrameOrders - (escrow?.Ingots ?? 0);
        ExtraIngotNights = Need(Part.Ingot, Ingots);
        Regrinds = Batches(Need(Part.Alloy, SelfFrames + FrameOrders + Pads - (escrow?.Alloy ?? 0)), IndustrialRecipes.AlloyYield);
        AlloyRuns = Need(Part.AlloyBlank, Regrinds);
        Blanks = Need(Part.Blank, MakeUnits);
        Liners = Need(Part.Liner, MakeUnits + this.cargo.Liners);
        SandRuns = Math.Max(Batches(Need(Part.Coarse, Blanks + Pads), 2), Need(Part.Fine, Blends + this.cargo.Fine));
        CrystalRuns = Batches(Need(Part.Grit, Liners), 2);
        WhiteRuns = Need(Part.Precipitate, CrystalRuns);
        GrowthRuns = Batches(Need(Part.Core, WhiteRuns), 2);
        BlueRuns = Need(Part.Gel, MakeUnits + this.cargo.Gel);
        Moss = BlueRuns + WhiteRuns;
        Resin = BlueRuns;
        Powder = new[] { AlloyRuns * IndustrialRecipes.AlloyMetal, SandRuns * 60,
            AlloyRuns * IndustrialRecipes.AlloyRare + Blends * IndustrialRecipes.BlendRare + GrowthRuns * 10,
            Blends * IndustrialRecipes.BlendConductive, GrowthRuns * 40 };
        Power = (AlloyRuns + Blanks + Regrinds) * 12 + (SandRuns + CrystalRuns * 2 + Moss) * 8
            + GrowthRuns * 12 + Batches(Steel, 2) * 15 + SelfFrames * 6;
        Water = GrowthRuns * 40 + SelfFrames * 100;
    }

    internal bool InputsReady(long[] qualified, long pureMetal, bool smelt)
    {
        for (int i = 0; i < Powder.Length; i++)
            if (qualified[i] < Powder[i]) return false;
        return (!smelt || pureMetal >= ExtraIngotMetal && qualified[0] >= (long)Powder[0] + ExtraIngotMetal)
            && stock.GetValueOrDefault(Part.Moss) >= Moss && stock.GetValueOrDefault(Part.Resin) >= Resin;
    }

    private static int Batches(int units, int yield) => (units + yield - 1) / yield;

    // One available machine of each required kind, basic throughput, transfers during business hours.
    // Raw powder, power and water are checked separately. Night outputs become available together.
    internal (int First, int Second) Estimate(bool smelt, IReadOnlyDictionary<int,int> deliveries = null, bool canOrderToday = true, bool supplyMissingIngots = false)
    {
        var bins = new Dictionary<Part,int>(stock);
        if (supplyMissingIngots) bins[Part.Ingot] = bins.GetValueOrDefault(Part.Ingot) + ExtraIngotNights;
        int Qty(Part p) => bins.GetValueOrDefault(p);
        void Add(Part p, int n) => bins[p] = Qty(p) + n;
        var dates = new Dictionary<int,int>();
        if (deliveries != null) foreach (var pair in deliveries) dates[pair.Key] = pair.Value;
        bool resume = escrow != null;

        int pads = Pads, shells = Math.Min(MakeUnits,cargo.ReusedShells);
        int first = -1, alloy = AlloyRuns, blanks = Blanks, ingots = smelt ? ExtraIngotNights : 0;
        int sand = SandRuns, grind = Regrinds, crystal = CrystalRuns, millProgress = 0, growth = GrowthRuns;
        int steel = Steel, build = SelfFrames, order = FrameOrders, blend = Blends;
        int contact = Batches(MakeContacts, IndustrialRecipes.ContactYield), liner = Liners, unit = MakeUnits;
        int white = WhiteRuns, blue = BlueRuns, growthProgress = 0;
        for (int day = 0; day <= 256; day++)
        {
            Add(Part.Frame, dates.GetValueOrDefault(day));
            queue.AddProducts(bins, day);
            int Make(Part input, Part output, ref int remaining, int yield = 1)
            {
                int n = Math.Min(Qty(input), remaining);
                Add(input, -n); Add(output, n * yield); remaining -= n; return n;
            }
            Make(Part.Fine, Part.Blend, ref blend);
            Make(Part.Blend, Part.Contact, ref contact, IndustrialRecipes.ContactYield);
            Make(Part.Grit, Part.Liner, ref liner);
            int forged = Math.Min(build, Math.Min(Qty(Part.Steel), Qty(Part.Alloy)));
            Add(Part.Steel, -forged); Add(Part.Alloy, -forged); Add(Part.Frame, forged); build -= forged;
            if (canOrderToday || day > 0)
            {
                int placed = Math.Min(order, Math.Min(4, Math.Min(Qty(Part.Ingot), Qty(Part.Alloy))));
                int creditedIngots = 0, creditedAlloy = 0;
                if (resume)
                {
                    creditedIngots = escrow.Ingots;
                    creditedAlloy = escrow.Alloy;
                    placed = Qty(Part.Ingot) >= escrow.Quantity - creditedIngots && Qty(Part.Alloy) >= escrow.Quantity - creditedAlloy
                        ? escrow.Quantity : 0;
                }
                if (placed > 0)
                {
                    Add(Part.Ingot, creditedIngots - placed); Add(Part.Alloy, creditedAlloy - placed); resume = false;
                    dates[day + 2] = dates.GetValueOrDefault(day + 2) + placed; order -= placed;
                }
            }
            int madePads = Math.Min(pads,Math.Min(Qty(Part.Coarse),Qty(Part.Alloy)));
            Add(Part.Coarse,-madePads); Add(Part.Alloy,-madePads); Add(Part.Pad,madePads); pads-=madePads;
            if (first < 0 && Qty(Part.Frame) >= frames && Qty(Part.Contact) >= contacts
                && Qty(Part.Pad) >= cargo.Pads && Qty(Part.Fine) >= cargo.Fine
                && Qty(Part.Liner) >= cargo.Liners && Qty(Part.Gel) >= cargo.Gel)
            {
                first = day; Add(Part.Frame, -frames); Add(Part.Contact, -contacts);
                Add(Part.Pad,-cargo.Pads); Add(Part.Fine,-cargo.Fine); Add(Part.Liner,-cargo.Liners); Add(Part.Gel,-cargo.Gel);
            }
            int freeFrames = Math.Max(0, Qty(Part.Frame) - (first < 0 ? frames : 0));
            int assembled = Math.Min(unit, Math.Min(freeFrames + shells, Math.Min(Qty(Part.Blank), Math.Min(Qty(Part.Gel), Qty(Part.Liner)))));
            int usedShells = Math.Min(shells,assembled);
            shells -= usedShells;
            Add(Part.Frame,usedShells-assembled);
            foreach (var input in new[]{Part.Blank,Part.Gel,Part.Liner}) Add(input,-assembled);
            Add(Part.Unit, assembled); unit -= assembled;
            if (first >= 0 && Qty(Part.Unit) >= units) return (first, day);
            var next = new List<(Part Part,int Count)>();
            if (grind > 0 && Qty(Part.AlloyBlank) > 0)
            { grind--; Add(Part.AlloyBlank,-1); next.Add((Part.Alloy,IndustrialRecipes.AlloyYield)); }
            if (alloy > 0) { alloy--; next.Add((Part.AlloyBlank,1)); }
            else if (blanks > 0 && Qty(Part.Coarse) > 0)
            { blanks--; Add(Part.Coarse,-1); next.Add((Part.Blank,1)); }
            else if (ingots > 0) { ingots--; next.Add((Part.Ingot,1)); }
            if (queue.Free(OverhaulBay.Mill, day) && sand > 0) { sand--; next.Add((Part.Coarse,2)); next.Add((Part.Fine,1)); }
            else if (queue.Free(OverhaulBay.Mill, day) && crystal > 0 && (millProgress == 1 || Qty(Part.Precipitate) > 0))
            {
                if (millProgress == 0) { Add(Part.Precipitate,-1); millProgress=1; }
                else { crystal--; millProgress=0; next.Add((Part.Grit,2)); }
            }
            if (queue.Free(OverhaulBay.Growth, day) && growth > 0 && ++growthProgress == 2)
            { growth--; growthProgress=0; next.Add((Part.Core,2)); }
            if (queue.Free(OverhaulBay.Blue, day) && blue > 0 && Qty(Part.Moss) > 0 && Qty(Part.Resin) > 0)
            { blue--; Add(Part.Moss,-1); Add(Part.Resin,-1); next.Add((Part.Gel,1)); }
            if (queue.Free(OverhaulBay.White, day) && white > 0 && Qty(Part.Moss) > 0 && Qty(Part.Core) > 0)
            { white--; Add(Part.Moss,-1); Add(Part.Core,-1); next.Add((Part.Precipitate,1)); }
            // Wait for a pair when smelting our own ingots to keep furnace power within the quote.
            int nSteel = Math.Min(2, steel);
            if (nSteel > 0 && Qty(Part.Ingot) >= nSteel)
            { Add(Part.Ingot,-nSteel); steel-=nSteel; next.Add((Part.Steel,nSteel)); }
            foreach (var earned in next) Add(earned.Part, earned.Count);
        }
        return (first, -1);
    }
}
