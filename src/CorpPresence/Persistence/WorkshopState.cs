using System.Collections.Generic;

namespace CorpPresence;

internal sealed class WorkshopState
{
    public int Version { get; set; } = 1;
    public string CenKit { get; set; }
    public int CenKitRemaining { get; set; }
    public WorkshopSaleState Sales { get; set; } = new();

    public List<RotationContract> Rotations { get; set; } = new();
    public FirstOverhaulState FirstOverhaul { get; set; }
    public int ShizukaKitRemaining { get; set; } = 5;
    public WorkshopSaleReceipt GelOrder { get; set; }
    public WorkshopSaleReceipt CartelDaily { get; set; }

    public MeridianCommissionReceipt Commission { get; set; }
    public FeederTrialState FeederTrial { get; set; }
    public WaterPumpRepairState PumpRepair { get; set; }
    public CoreIntakeState CoreIntake { get; set; }
    public PhaseSiteState PhaseSite { get; set; }
    public JointWaterWorkState JointWaterWork { get; set; }

    internal bool Valid() => Version == 1 && Sales?.Valid() == true && (Commission == null || Commission.Valid())
        && (FeederTrial == null || FeederTrial.Valid())
        && (PumpRepair == null || PumpRepair.Valid() && FirstOverhaul?.CompletedDay <= PumpRepair.IssuedDay && FeederTrial?.Handbook == true)
        && (CoreIntake == null || CoreIntake.Valid() && PumpRepair?.CompletedDay.HasValue == true)
        && (PhaseSite == null || PhaseSite.Valid() && PumpRepair?.CompletedDay <= PhaseSite.SurveyDay)
        && (JointWaterWork == null || JointWaterWork.Valid()
            && PhaseSite is { Route: PhaseRoute.Whole, CargoId: > 0 } && PhaseSite.DueDay <= JointWaterWork.SignedDay
            && JointWaterWork.KitId != PhaseSite.CargoId && JointWaterWork.KitId != PhaseSite.StationId)
        && (FirstOverhaul == null || FirstOverhaul.Valid())
        && RotationContract.ValidHistory(Rotations, FirstOverhaul?.CompletedDay)
        && ShizukaKitRemaining is >= 0 and <= 5
        && (GelOrder == null || GelOrder.ValidForFirstGel()) && (CartelDaily == null || CartelDaily.ValidForCartelDaily()) &&
        (CenKit == null ? CenKitRemaining == 0 :
            CenKit is "glass" or "metal" or "salvage" && CenKitRemaining is >= 0 and <= 4);
}

internal sealed class JointWaterWorkState
{
    internal const int ArrivalAmount = 90, LoadAmount = 135, ServiceAmount = 235, ShiftsRequired = 2;
    internal const int Total = ArrivalAmount + LoadAmount + ServiceAmount;

    public int SignedDay { get; set; } = -1;
    public int KitId { get; set; }
    public int? ArrivalDay { get; set; }
    public int? LoadDay { get; set; }
    public int ServiceNights { get; set; }
    public int? LastServiceDay { get; set; }
    public int Paid { get; set; }
    public bool InFlight { get; set; }

    internal int Earned => ServiceNights == ShiftsRequired ? Total
        : LoadDay.HasValue ? ArrivalAmount + LoadAmount : ArrivalDay.HasValue ? ArrivalAmount : 0;

    internal int Due
    {
        get
        {
            int next = Paid < ArrivalAmount ? ArrivalAmount : Paid < ArrivalAmount + LoadAmount ? ArrivalAmount + LoadAmount : Total;
            return System.Math.Max(0, System.Math.Min(Earned, next) - Paid);
        }
    }

    internal bool Valid() => SignedDay >= 0 && KitId > 0 && (!ArrivalDay.HasValue || ArrivalDay > SignedDay)
        && (!LoadDay.HasValue || ArrivalDay.HasValue && LoadDay > ArrivalDay) && ServiceNights is >= 0 and <= ShiftsRequired
        && (ServiceNights == 0 ? LastServiceDay == null : LoadDay.HasValue && LastServiceDay >= (long)LoadDay.Value + ServiceNights)
        && Paid >= 0 && Paid <= Earned && (!InFlight || Due > 0);
}

internal enum PhaseRoute { None, Crew, Parts, Whole }

internal sealed class PhaseSiteState
{
    internal const int CrewFee = 45, SplitPower = 16, WholePower = 24;

    public int SurveyDay { get; set; } = -1;
    public PhaseRoute Route { get; set; }
    public int CrewPaid { get; set; }
    public bool InFlight { get; set; }
    public int? DispatchDay { get; set; }
    public int? StationId { get; set; }
    public int PowerSpent { get; set; }
    public int? CargoId { get; set; }
    internal int? DueDay => DispatchDay + (Route == PhaseRoute.Whole ? 2 : 1);

    internal bool Valid()
    {
        if (SurveyDay < 0 || SurveyDay > int.MaxValue - 3 || CargoId is <= 0 || CargoId.HasValue && !DispatchDay.HasValue)
            return false;
        if (DispatchDay is int depart && (depart <= SurveyDay || depart > int.MaxValue - 2)) return false;
        return Route switch
        {
            PhaseRoute.None => CrewPaid == 0 && !InFlight && !DispatchDay.HasValue && !StationId.HasValue && PowerSpent == 0,
            PhaseRoute.Crew => !StationId.HasValue && PowerSpent == 0 && CrewPaid is >= 0 and <= CrewFee
                && (DispatchDay.HasValue ? CrewPaid == CrewFee && !InFlight : CrewPaid < CrewFee && (CrewPaid > 0 || InFlight)),
            PhaseRoute.Parts or PhaseRoute.Whole => !InFlight && CrewPaid == 0 && DispatchDay.HasValue && StationId is > 0
                && (Route == PhaseRoute.Whole ? PowerSpent is WholePower or 32 : PowerSpent is SplitPower or WholePower or 32)
                && CargoId != StationId,
            _ => false
        };
    }
}

internal enum IndustrialCoreKind { Timing, Phase }

internal sealed class CoreIntakeState
{
    public int? TimingId { get; set; }
    public int? PhaseId { get; set; }

    internal int? Id(IndustrialCoreKind kind) => kind == IndustrialCoreKind.Timing ? TimingId : PhaseId;

    internal void Set(IndustrialCoreKind kind, int id)
    {
        if (kind == IndustrialCoreKind.Timing) TimingId = id;
        else PhaseId = id;
    }

    internal bool Valid() => (TimingId is > 0 || PhaseId is > 0) && TimingId is null or > 0
        && PhaseId is null or > 0 && TimingId != PhaseId;
}

internal sealed class WaterPumpRepairState
{
    internal const int BasePay = 165, BaseBuildPay = 330, LowLossBonus = 30, PressureBonus = 45;

    public int IssuedDay { get; set; } = -1;
    public int PumpId { get; set; }
    public int? FabricatedPumpId { get; set; }
    public int? ReceivedDay { get; set; }
    public int Amount { get; set; }
    public int Paid { get; set; }
    public bool InFlight { get; set; }
    public int? CompletedDay { get; set; }

    internal bool Valid()
    {
        if (PumpId < 0 || IssuedDay < 0 || FabricatedPumpId is <= 0 || FabricatedPumpId == PumpId
            || PumpId == 0 && !FabricatedPumpId.HasValue) return false;
        if (ReceivedDay is not int received)
            return Amount == 0 && Paid == 0 && CompletedDay == null && (!FabricatedPumpId.HasValue || InFlight);
        bool validAmount = FabricatedPumpId.HasValue ? Amount is BaseBuildPay or BaseBuildPay + LowLossBonus
            : Amount is BasePay or BasePay + LowLossBonus or BasePay + PressureBonus or BasePay + LowLossBonus + PressureBonus;
        if (received < IssuedDay || !validAmount || Paid < 0 || Paid > Amount) return false;
        return CompletedDay is int completed ? completed >= received && Paid == Amount && !InFlight : Paid < Amount;
    }
}

internal sealed class FeederTrialState
{
    internal const int ToolCount = 2, RequiredBatches = 2;
    public int? LoanId { get; set; }
    public int ToolsRemaining { get; set; } = ToolCount;
    public bool Returned { get; set; }
    public bool ReturnInFlight { get; set; }
    public int Batches { get; set; }
    public int? LastNight { get; set; }
    public WorkshopSaleReceipt Spec { get; set; }

    internal bool Passed => Batches == RequiredBatches;
    internal bool Handbook => Passed && Spec is { Complete: true } && Spec.ValidFor(WorkshopSaleBook.MeridianFeeder);

    internal bool Valid() => (LoanId == null || LoanId > 0) && ToolsRemaining is >= 0 and <= ToolCount
        && (LoanId != null || (ToolsRemaining == ToolCount && !Returned && !ReturnInFlight && Batches == 0))
        && (!(Returned || ReturnInFlight) || Passed) && !(Returned && ReturnInFlight)
        && Batches is >= 0 and <= RequiredBatches
        && (Batches == 0 ? LastNight == null : LastNight >= 0)
        && (Spec == null || (Passed && Spec.Day >= LastNight && Spec.ValidFor(WorkshopSaleBook.MeridianFeeder)));
}

internal sealed class MeridianCommissionReceipt
{
    internal const int Fee = 35;
    internal const int Maximum = 4;
    internal const string OrderKind = "frame_commission";
    internal const string FrameId = "corp_presence_pump_frame";
    public int Quantity { get; set; }
    public int Ingots { get; set; }
    public int Alloy { get; set; }
    public int Paid { get; set; }
    public bool InFlight { get; set; }

    internal bool Valid() => Quantity is >= 1 and <= Maximum
        && Ingots >= 0 && Ingots <= Quantity && Alloy >= 0 && Alloy <= Quantity
        && Paid >= 0 && Paid <= Quantity * Fee && (Paid == 0 || (Ingots == Quantity && Alloy == Quantity));
}
