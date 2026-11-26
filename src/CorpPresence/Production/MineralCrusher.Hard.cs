using System.Collections.Generic;
using System.Linq;

namespace CorpPresence;

internal static partial class MineralCrusher
{
    internal const string HardLiningReceipt = "hard_lining";

    private sealed class HardDuty
    {
        private const string ModeTag = "CORP_CRUSHER_HARD_DUTY", NightTag = "CORP_CRUSHER_HARD_NIGHT";
        internal const int ExtraCharge = 2;
        internal readonly GameSlotInventory Slot = new(2, 2)
        {
            mayInventoryAddItemFunc = (item, _) => CrusherToothRoller.Learned
                && MachineService.IsStockPart(item, RodMaterial.ToothRoller) && CrusherToothRoller.SturdyHub(RodCondition.Read(item))
        };
        internal readonly MachineryButton Toggle = new();

        private GameItem machine;
        internal bool Selected => machine.GetTagReadonly(ModeTag).GetBool();
        internal bool Settled => machine.GetTagReadonly(NightTag).GetLong() is > 0 and var night
            && night == (long)StoreStation.GetDayCounter() + 1;

        internal sealed record Roller(GameItem Item, RodCondition Stock)
        {
            internal int Capacity => (int)(Stock.Jacket.Material.PowderPoints * 2);
        }

        internal Roller Fitted => CrusherToothRoller.Learned && MachineService.IsStockPart(Slot.childItem, RodMaterial.ToothRoller)
            && RodCondition.Read(Slot.childItem) is { } stock && CrusherToothRoller.SturdyHub(stock)
            && stock.Jacket != null && CrusherToothRoller.HardFace(stock) ? new(Slot.childItem, stock) : null;

        internal int Capacity => Fitted?.Capacity ?? 0;

        internal void Register(GameItem crusher, System.Action<bool> open, System.Action refresh)
        {
            machine = crusher;
            MachineService.Register(machine, () => machine.identifier == "corp_presence_" + Key && CrusherToothRoller.Learned, open);
            Slot.onSlotAddItemEndFunc += (_, _, _) => refresh();
            Slot.onSlotRemoveItemEndFunc += (_, _, _) => refresh();
            Toggle.SetCallback(() =>
            {
                if (!GeneralHelper.IsItemOwned(machine) || !SaveSlot.CanWrite("CorpPresence") || (!Selected && Capacity == 0)) return;
                machine.ModifyTag(ModeTag, tag => tag.Enable().SetBool(!Selected), skipValidate: true);
                refresh();
            });
        }

        internal string Status(int standbyCharge)
        {
            Toggle.SetText(Selected ? L.K("workshop.crusher.hardOn", "硬料档 · 开", "Hard duty · On") :
                L.K("workshop.crusher.hardOff", "硬料档 · 关", "Hard duty · Off"));
            int capacity = Capacity;
            if (capacity == 0) return Selected || CrusherToothRoller.Learned ? MineralNightReport.Reason(MineralStop.CrusherTeeth) : "";
            return L.F("workshop.crusher.hardCapacity", "硬料：{0} 电／夜 · 齿面余 {1} 件旧炉衬",
                "Hard duty: {0} charge/night · teeth last {1} linings", standbyCharge + ExtraCharge, capacity);
        }

        internal void Finish(Roller tool, int items)
        {
            RodJacket.Wear(tool.Item, tool.Stock, items);
            machine.ModifyTag(NightTag, tag => tag.Enable().SetLong((long)StoreStation.GetDayCounter() + 1), skipValidate: true);
        }
    }

    private static IEnumerable<GameItem> NightFeed(GameGridInventory input, bool hard) => hard
        ? FeedInOrder(input).Where(item => CrusherBatch.Classify(item.identifier) == CrusherFeed.SpentLining)
        : FeedInOrder(input);
}

internal static class CrusherToothRoller
{
    internal const string Reface = "reface_crusher_tooth_roller";
    internal static bool Learned => RodCladding.Learned && WaterStationRepair.EngineeringCleared(CorpState.Current);

    internal static bool IsRecipe(string key) => key is RodMaterial.ToothRoller or Reface;

    internal static bool SturdyHub(RodCondition stock) => stock != null && (stock.Traits & RodTrait.Impact) != 0;

    internal static bool HardFace(RodCondition stock) => stock != null && (stock.Traits & RodTrait.HardTooth) != 0
        && stock.Surface.Hardness >= 80;
}
