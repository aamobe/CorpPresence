using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{
    private static float _overhaulPlanHeight = 850f;

    private static void DrawOverhaulPlan(float w)
    {
        var q = _overhaul;
        var rotation = _rotation;
        var contract = rotation == null ? (OverhaulContractState)q.Contract : rotation.Contract;
        if (contract?.CompletedDay is int closed)
        {
            int paid = contract.Trucks.Sum(truck => truck.Paid);
            string settled = rotation == null
                ? L.F("workshop.overhaul.finished", "第 {0} 天两车已结清，累计实收 {1} 元。", "Both trucks settled on day {0}. Total paid: {1}.", closed, paid)
                : L.F("workshop.rotation.paid", "第 {0} 天结清，累计实收 {1} 元。此窗口只签一次。", "Settled on day {0}. Total paid: {1}. One contract per window.", closed, paid);
            float doneY = 0;
            RotationLine(w, ref doneY, settled);
            _overhaulPlanHeight = doneY + 12;
            return;
        }
        if (_overhaulStock == null || Time.realtimeSinceStartup >= _nextStockRead)
        {
            var repair=_rotation?.Contract;
            _overhaulStock = OverhaulWorkshopStock.Capture(q.Day, repair is { Kind: RotationKind.Repair } ? repair : null);
            _nextStockRead = Time.realtimeSinceStartup + 2f;
        }
        var s = _overhaulStock;
        var trucks = rotation == null ? q.Contract?.Trucks : rotation.Contract?.Trucks;
        var specs = rotation?.Specs;
        int Left(int truck, int kind) => (specs==null ? OverhaulTruck.Quantities[truck][kind] : specs[truck].Quantities[kind])
            - (trucks?[truck].Received[kind] ?? 0);
        int Needed(string key, bool onlyFirst)
        {
            int total=0;
            for (int i=0; i<specs.Length && (!onlyFirst || i==0); i++)
                for (int j=0;j<specs[i].ItemIds.Length;j++)
                    if (specs[i].ItemIds[j]=="corp_presence_"+key) total+=Left(i,j);
            return total;
        }
        OverhaulPlan Plan(bool onlyFirst)
        {
            if (specs==null)
                return new(s.Stock, Left(0,0), Left(0,1), onlyFirst ? 0 : Left(1,0), _commissionRoute, s.Transit, s.Queue, s.Escrow);
            int substitute = rotation.Kind == RotationKind.Repair ? s.CleanShells + Math.Min(s.OldShells,s.WashesAvailable) : 0;
            return new(s.Stock, Needed(IndustrialGoods.Frame,onlyFirst), Needed(IndustrialGoods.Contact,onlyFirst),
                Needed(IndustrialGoods.Unit,onlyFirst), _commissionRoute, s.Transit,s.Queue,s.Escrow,
                new RotationCargo(Needed(IndustrialGoods.Pad,onlyFirst), Needed(ClassifierGoods.Fine,onlyFirst),
                    Needed(IndustrialGoods.Liner,onlyFirst), Needed(CartelReactor.Gel,onlyFirst), substitute));
        }
        var p = Plan(false);
        float y=0;
        void Line(string text,float height=35)
        {
            height = Mathf.Max(height, _theme.Wrap.CalcHeight(new GUIContent(text), w));
            GUI.Label(new Rect(0,y,w,height),text,_theme.Wrap);
            y+=height+3;
        }
        if (Ui.Button(new Rect(0,y,w/2-3,33),L.K("workshop.overhaul.self","自制框架","Forge frames"),!_commissionRoute ? _theme.NavOnStyle : _theme.BtnStyle)) _commissionRoute=false;
        if (Ui.Button(new Rect(w/2,y,w/2-3,33),L.K("workshop.overhaul.outsource","子午线代工","Commission Meridian"),_commissionRoute ? _theme.NavOnStyle : _theme.BtnStyle)) _commissionRoute=true;
        y+=41;
        int held(Part part) => s.Stock.GetValueOrDefault(part);
        void StockHeading(string title)
        {
            GUI.Label(new Rect(0, y, w * .72f, 25), title, _theme.Body);
            GUI.Label(new Rect(w * .74f, y, w * .26f, 25), L.K("workshop.chart.stockRatio", "现货 / 需要", "On hand / needed"), _theme.Center);
            y += 28;
        }
        void Stock(string label, long need, long have) => DrawWorkshopStockRow(w, ref y, label, need, have);
        if (rotation==null)
        {
            StockHeading(L.K("workshop.chart.cargo", "待交货物", "Outstanding cargo"));
            Stock(Goods.NameOf(IndustrialGoods.Frame), Left(0, 0), held(Part.Frame));
            Stock(Goods.NameOf(IndustrialGoods.Contact), Left(0, 1), held(Part.Contact));
            Stock(Goods.NameOf(IndustrialGoods.Unit), Left(1, 0), held(Part.Unit));
        }
        else
        {
            var parts=new Dictionary<string,Part>
            {
                [IndustrialGoods.Frame]=Part.Frame, [IndustrialGoods.Contact]=Part.Contact, [IndustrialGoods.Unit]=Part.Unit,
                [IndustrialGoods.Pad]=Part.Pad, [ClassifierGoods.Fine]=Part.Fine, [IndustrialGoods.Liner]=Part.Liner, [CartelReactor.Gel]=Part.Gel
            };
            for (int i=0;i<specs.Length;i++)
            {
                var spec=specs[i];
                StockHeading(L.F("workshop.chart.truck", "第 {0} 车", "Truck {0}", i + 1));
                for (int j = 0; j < spec.ItemIds.Length; j++)
                {
                    string key = spec.ItemIds[j].Substring("corp_presence_".Length);
                    Stock(Goods.NameOf(key), Left(i, j), held(parts[key]));
                }
            }
            if (rotation.Kind == RotationKind.Repair)
                Line(L.F("workshop.rotation.washStock","本单原壳 {0}、净壳 {1}。按店内各容器实际酒精可洗 {2} 只，每只 15 mL。不足的壳位按新框架备料。",
                    "This contract: {0} used shells, {1} cleaned. Available alcohol covers {2} washes at 15 mL each. Allow new frames for any shortfall.",
                    s.OldShells,s.CleanShells,Math.Min(s.OldShells,s.WashesAvailable)));
            if (p.Pads>0) Line(L.F("workshop.rotation.padWork","还需制作 {0} 块重载筛垫，每块用一包粗砂和一包耐磨合金粉。",
                "Make {0} heavy screen pads, each using one coarse grit and one wear alloy powder.",p.Pads));
        }
        foreach (var batch in s.Queue.Batches)
        {
            string machine = batch.Bay switch {
                OverhaulBay.Mill => Goods.NameOf(PowderClassifier.Key), OverhaulBay.Growth => Goods.NameOf(MineralGrowthStation.Key),
                OverhaulBay.Blue => L.K("workshop.overhaul.blueBay","蓝胆","Blue vessel"),
                _ => L.K("workshop.overhaul.whiteBay","白胆","White vessel")
            };
            if (batch.Day < 0)
            {
                Line(L.F("workshop.overhaul.unknownBatch","{0}：需在设备上核实批次；未计入产出，工位释放日未知。",
                    "{0}: verify the batch at the station. Its output and release day are unconfirmed.", machine));
                continue;
            }
            string product = batch.Products.Length == 0 ? LocHelper.GetLocalizedItem("item_common_ore_name") + " × 1" :
                string.Join("、",batch.Products.Select(product => Goods.NameOf(product.Part switch {
                    Part.Coarse => ClassifierGoods.Coarse, Part.Fine => ClassifierGoods.Fine, Part.Grit => ClassifierGoods.Crystal,
                    Part.Core => CartelReactor.Salt, Part.Gel => CartelReactor.Gel, _ => CartelReactor.Crystal }) + " × " + product.Count));
            Line(L.F("workshop.overhaul.queued","{0} 在制：{1}。预计 {2} 天后可取，另需 {3} 电、{4} mL 水。",
                "{0} in progress: {1}. Collect in {2} days, with {3} more charge and {4} mL water.", machine,product,batch.Day,batch.Power,batch.Water));
        }
        foreach (var arrival in s.Deliveries.OrderBy(entry => entry.Key))
            Line(L.F("workshop.overhaul.arrival","已付框架 {0} 件，预计 {1} 天后配送（0 为今日，需等实物签收）。",
                "{0} paid frames due in {1} days (0 means today). Await physical delivery.",arrival.Value,arrival.Key));
        if (s.UnknownDelivery || s.FrozenCommission)
            Line(L.K("workshop.overhaul.unknownOrder","存在待核的委托或到货记录。该记录未计入可用框架；先到子午线核对。",
                "A commission or delivery record needs reconciliation at Meridian. Its frames are excluded."));
        if (s.Escrow is { } e)
            Line(L.F("workshop.overhaul.escrow","未结委托 {0} 件：已交锭 {1}、粉 {2}、加工费 {3}。续办尚欠锭 {4}、粉 {5}、费用 {6}；结清后起算两日。",
                "Open order for {0}: deposited {1} ingots, {2} alloy and {3} in fees. To resume, supply {4} ingots, {5} alloy and {6} in fees. Delivery takes two days after settlement.",
                e.Quantity,e.Ingots,e.Alloy,e.Paid,e.Quantity-e.Ingots,e.Quantity-e.Alloy,e.Quantity*MeridianCommissionReceipt.Fee-e.Paid));
        Line(L.F("workshop.overhaul.jobs","扣除现货后还要做：框架 {0}，触片 {1}，滤芯坯 {2}，衬层 {3}，胶 {4}，总成 {5}。",
            "After stock: make {0} frames, {1} contacts, {2} filter blanks, {3} liners, {4} gel and {5} assemblies.",
            p.Frames,p.MakeContacts,p.Blanks,p.Liners,p.BlueRuns,p.MakeUnits));
        if (p.Powder.Any(amount => amount > 0) || p.Ingots > 0 || p.Moss > 0 || p.Resin > 0)
        {
            StockHeading(L.K("workshop.chart.powder", "合格粉与备料", "Qualified powder and inputs"));
            for (int i = 0; i < p.Powder.Length; i++)
                if (p.Powder[i] > 0) Stock(PowderLabel(i), p.Powder[i], s.Powder[i]);
            if (p.Ingots > 0) Stock(L.K("workshop.chart.ingot", "高档锭", "High-grade ingot"), p.Ingots, held(Part.Ingot));
            if (p.Moss > 0) Stock(Goods.NameOf("sleep_moss_block"), p.Moss, held(Part.Moss));
            if (p.Resin > 0) Stock(Goods.NameOf(CartelReactor.Resin), p.Resin, held(Part.Resin));
        }
        if (p.FrameOrders > 0)
            Line(L.F("workshop.chart.commission", "代工 {0} 件 · 加工费 {1}₵", "Commission {0} · fee {1}₵", p.FrameOrders, p.FrameFee));
        Line(L.F("workshop.overhaul.cost","现成锭路线预计 {0} 电、{1} mL 工艺水。自烧缺锭另备 {2} 点纯金属、{3} 个烧结夜、{4} 电。",
            "With ingots supplied: {0} charge and {1} mL process water. Smelting missing ingots adds {2} pure metal, {3} sintering nights and {4} charge.",
            p.Power,p.Water,p.ExtraIngotMetal,p.ExtraIngotNights,p.ExtraIngotNights*12),52);
        Line(L.F("workshop.overhaul.pureMetal","自烧缺锭可用纯金属（≥99%）：{0}/{1} 点。纯粉同时计入上方合格金属，先为锭留足。",
            "Pure metal for smelting missing ingots (≥99%): {0}/{1} points. The qualifying metal total above includes this stock.", s.PureMetal,p.ExtraIngotMetal));
        Line(L.F("workshop.overhaul.batches","备料批数　硅砂 {0}，烧合金坯 {1}，回磨 {2}，矿化 {3}，白胆 {4}，晶磨 {5}。",
            "Batches: silica {0}, alloy sintering {1}, regrinding {2}, mineral growth {3}, white vessel {4}, crystal milling {5}.",
            p.SandRuns,p.AlloyRuns,p.Regrinds,p.GrowthRuns,p.WhiteRuns,p.CrystalRuns));
        var missing = string.Join("、",s.MissingEquipment(p, smelt:true));
        if (missing.Length > 0) Line(L.F("workshop.overhaul.stations","待到位的工装或设备：{0}","Equipment still needed: {0}",missing));
        var orderBlock = _commissionRoute && p.FrameOrders > 0 ? Services.OrderBlocked(Companies.Find("meridian")) : null;
        if (!string.IsNullOrEmpty(orderBlock)) Line(orderBlock);
        var firstOnly = Plan(true);
        (int Day, string Reason) Date(OverhaulPlan plan, bool second, bool supply)
        {
            bool smelt = !supply && plan.ExtraIngotNights > 0;
            if (!plan.InputsReady(s.Powder, s.PureMetal, smelt))
            {
                var shortages = new List<string>();
                void Short(string label, long required, long available)
                {
                    if (required > available) shortages.Add(label + " × " + (required - available));
                }
                for (int i = 0; i < plan.Powder.Length; i++)
                    Short(PowderBox.ComponentName((PowderComponent)i), plan.Powder[i] + (i == 0 && smelt ? plan.ExtraIngotMetal : 0), s.Powder[i]);
                if (smelt) Short(PowderBox.ComponentName(PowderComponent.Metal) + " (≥99%)", plan.ExtraIngotMetal,s.PureMetal);
                Short(Goods.NameOf("sleep_moss_block"),plan.Moss,held(Part.Moss));
                Short(Goods.NameOf(CartelReactor.Resin),plan.Resin,held(Part.Resin));
                return (-1,L.F("workshop.overhaul.shortInputs","待排，尚缺 {0}","Pending, short {0}",string.Join("、",shortages)));
            }
            string stations = string.Join("、",s.MissingEquipment(plan,smelt));
            if (stations.Length > 0)
                return (-1,L.F("workshop.overhaul.stations","待到位的工装或设备：{0}","Equipment still needed: {0}",stations));
            if (plan.FrameOrders > 0 && (s.FrozenCommission || !string.IsNullOrEmpty(orderBlock)))
                return (-1,orderBlock ?? MeridianIndustrial.Frozen);
            var dates = plan.Estimate(smelt,s.Deliveries,s.CanOrderToday,supply);
            return (second ? dates.Second : dates.First, L.K("workshop.overhaul.pendingDate","待排（缺料、缺设备、委托未就绪或工位释放日未知）",
                "Pending (short materials, equipment, commission access or unknown station release)"));
        }
        string DayText(int? completed, (int Day,string Reason) forecast) => completed.HasValue ? completed.Value.ToString() :
            forecast.Day >= 0 ? ((long)q.Day + forecast.Day).ToString() : forecast.Reason;
        void Dispatch(bool supply)
        {
            var first = Date(p, false, supply);
            if (first.Day < 0) first = Date(firstOnly, false, supply);
            if (specs?.Length == 1)
            {
                string ready=DayText(trucks?[0].CompletedDay,first);
                Line(supply ? L.F("workshop.rotation.singleSupplied","若今天补齐缺锭，本车交期估算：{0}。","With missing ingots supplied today, estimated dispatch: {0}.",ready)
                    : L.F("workshop.rotation.singleDate","按现货及在制排班，本车交期估算：{0}。","Estimated dispatch from stock and work in progress: {0}.",ready));
                return;
            }
            var second = Date(p,true,supply);
            string firstDay = DayText(trucks?[0].CompletedDay,first), secondDay = DayText(trucks?[1].CompletedDay,second);
            Line(supply ? L.F("workshop.overhaul.suppliedDates","若今天补齐缺锭：首车 {0}，二车 {1}。",
                "If missing ingots are supplied today: first truck {0}, second truck {1}.",firstDay,secondDay)
                : L.F("workshop.overhaul.actualDates","按现货、在制与可用料排班（缺锭自烧）：首车 {0}，二车 {1}。",
                    "Schedule from physical stock, active batches and available feed, smelting missing ingots: first truck {0}, second truck {1}.",firstDay,secondDay));
        }
        Dispatch(false);
        if (p.ExtraIngotNights > 0) Dispatch(true);
        Line(L.K("workshop.overhaul.assume","估算用每类一台基础机、通量筛和基础磨套。预留工位、按配方分盒，白天搬料，备足水电、加工费及柜台空位；代工用当日额度，已付在途按预计到货日计。","Assumes one reserved base station of each kind, throughput mode and a standard sleeve. Split powder by recipe, move goods during the day and supply water, power, fees and counter space. Commissions use the daily order and existing deliveries use their scheduled dates."),75);
        _overhaulPlanHeight = y + 12f;
    }
}
