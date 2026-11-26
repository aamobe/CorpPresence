namespace CorpPresence;

internal static class SieveGoalChecks
{
    private static void Check(bool ok,string issue){ if(!ok) throw new Exception(issue); }
    private static void RestoreSinterRecipe()
    {
        var sinter = SinteringStation.Definition.Shell();
        sinter.ModifyTag("CORP_SINTERING_RECIPE",t=>t.SetString(IndustrialRecipes.Billet));
        var slots = (GameSlotInventory[])sinter.contentWindow.childElement.GetPanel("input").Content;
        SaveManager.IsDecoding = true;
        try
        {
            void Restore(int index,int[] parts)
            {
                var box = Goods.Make(PowderBox.Key);
                box.Parts = parts;
                slots[index].UncheckedAccept(box);
                slots[index].onSlotAddItemEndFunc?.Invoke(box,slots[index],null);
            }
            Restore(0,new[]{120,0,0,0,0});
            Restore(1,new[]{0,0,5,0,0});
        }
        finally { SaveManager.IsDecoding = false; }
        Check(sinter.GetTagReadonly("CORP_SINTERING_RECIPE").GetString() == IndustrialRecipes.Billet,
            "Restoring the metal box first must not switch the saved alloy recipe");
        sinter.Slot("power").Put(new GameItem("energy_credit"){Charge=12});
        sinter.Night();
        Check(sinter.Slot("output").childItems.Single().identifier=="corp_presence_"+IndustrialRecipes.Billet
            && slots[0].childItem.Parts[0]==75 && slots[1].childItem.Parts[2]==0,
            "The first post-load night makes a billet with exact inputs");
    }

    internal static void Run()
    {
        RestoreSinterRecipe();
        var sieve = PowderSieve.Definition.Shell();
        sieve.identifier = "corp_presence_" + PowderSieve.Key;
        EmporiumEntry.Instance.frontInvinvElement.Put(sieve);
        const string tag = "CORP_SIEVE_TARGET";
        sieve.ModifyTag(tag,t=>t.SetString(IndustrialRecipes.Blank));
        sieve.onLoaded(sieve);
        var control = (object[])sieve.contentWindow.childElement.GetPanel("control").Content;
        var forecast = control.OfType<MachineryText>().Single();
        var tabs = (MachineryTabs)sieve.contentWindow.childElement.GetPanel("bins").Content;
        var panel = tabs.Panels[1];
        var goal = (MachineryButton)((MachineryStack)panel.Items[0]).Items[1];
        var topUp = (MachineryButton)((MachineryStack)panel.Items[3]).Items[3];
        var amount = (MachinerySlider)panel.Items[2];
        var receiver = (GameSlotInventory)((MachineryStack)panel.Items[1]).Items[0];
        var from = sieve.Slot("input").Put(Goods.Make(PowderBox.Key));
        var to = receiver.Put(Goods.Make(PowderBox.Key));
        from.Parts = new[]{120,0,0,0,0};
        topUp.Click();
        Check(amount.GetInt()==120 && forecast.Text.Contains("item_metal_ingot_name"),
            "A saved solid-only recipe safely falls back in both the mixing panel and bin forecast");
        LowerMachinist.SecondVisit=false;
        for(int i=0;i<10;i++)
        {
            goal.Click();
            Check(!sieve.GetTagReadonly(tag).GetString().StartsWith(IndustrialRecipes.Billet)
                && sieve.GetTagReadonly(tag).GetString()!=IndustrialRecipes.Blank, "Locked and solid-only recipes must be skipped");
        }
        LowerMachinist.SecondVisit=true;
        for(int i=0;i<10 && sieve.GetTagReadonly(tag).GetString()!=IndustrialRecipes.Billet;i++) goal.Click();
        topUp.Click();
        Check(amount.GetInt()==45 && forecast.Text.Contains("Metal"), "Billet metal target and top-up");
        goal.Click(); from.Parts=new[]{0,0,12,0,0}; topUp.Click();
        Check(sieve.GetTagReadonly(tag).GetString()==IndustrialRecipes.Billet+"/2" && amount.GetInt()==5
            && forecast.Text.Contains("RareMetal"), "Cycle to the real second component and preview five rare points");
        sieve.onLoaded(sieve);
        Check(goal.Text.Contains("RareMetal"), "Reload the selected secondary target");
        goal.Click();
        Check(sieve.GetTagReadonly(tag).GetString()==SinteringRecipes.StandardRod,
            "Skip the solid blank and reach the standard rod metal target");
        goal.Click();
        Check(sieve.GetTagReadonly(tag).GetString()==SinteringRecipes.StandardRod+"/1",
            "Standard rod silica has an independent saveable target");
        var power = sieve.Slot("power").Put(new GameItem("energy_credit"){Charge=12});
        sieve.Night();
        Check(power.Charge==0 && PowderSieve.ReadBin(sieve,2)[2]==6, "Normal overnight sieve still settles the actual chosen feed");
        Console.WriteLine("PASS sieve save/load, target cycling, metal/rare top-up, locked recipes and normal night");
    }
}

internal sealed class MachinerySlider
{
    private int number;
    internal Action<float> onChanged;
    internal MachinerySlider(int initial){number=initial;}
    internal MachinerySlider SetRange(int min,int max,int step)=>this;
    internal int GetInt()=>number;
    internal void SetInt(int value){number=value;onChanged?.Invoke(value);}
}
internal sealed class MachineryTabs
{
    internal readonly List<MachineryStack> Panels = new();
    internal event Action<int> onChanged { add {} remove {} }
    internal void AddTab(string label,MachineryStack content)=>Panels.Add(content);
}
internal static class SieveNativeBoundary
{
    internal static void ValidateShapeState(this GameItem item){}
    internal static bool UncheckedAccept(this GameSlotInventory slot,GameItem item)
    { slot.childItems.Add(item); item.parentInventory=slot; return true; }
}
internal static partial class PowderBox
{
    internal const string Key="powder_box";
    private static readonly HashSet<GameItem> Bound=new();
    internal static bool IsBoundBin(GameItem box)=>Bound.Contains(box);
    internal static void BindBin(GameItem box,GameItem machine,Func<int[]> read,Action<int[]> write,Action refresh)=>Bound.Add(box);
    internal static void Changed(GameItem item){}
    internal sealed record TransferStock(GameItem Source, GameItem Target, int[] From,int[] To,int Capacity)
    {
        internal int Limit => PowderTransfer.Limit(From,To,Capacity);
        internal PowderTransfer Plan(int amount)=>PowderTransfer.Plan(From,To,Capacity,amount,WaterHelper.SplitProportionally);
    }
    internal static TransferStock InspectTransfer(GameItem source,GameItem target) =>
        source==null || target==null ? null : new(source,target,source.Parts,target.Parts,PowderColor.Capacity);
    internal static bool Transfer(TransferStock stock,int amount)
    {
        var plan = stock.Plan(amount);
        if (plan == null) return false;
        CommitComposition(stock.Source, plan.SourceAfter);
        CommitComposition(stock.Target, plan.TargetAfter);
        RefreshComposition(stock.Source, plan.SourceAfter);
        RefreshComposition(stock.Target, plan.TargetAfter);
        return true;
    }
}
internal sealed class StoreUIManager
{
    internal static StoreUIManager Instance=>null;
    internal void Notify(string text){}
}
