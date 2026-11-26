using System;

namespace CorpPresence;

internal static partial class PowderSieve
{
    private const string TargetTag = "CORP_SIEVE_TARGET";

    private static SinteringRecipes.PowderTarget GoalFor(string saved, bool next = false) =>
        SinteringRecipes.SieveTarget(saved, SinteringStation.Unlocked, next);

    private static string GoalName(SinteringRecipes.PowderTarget target) => target.Recipe.Ingredients.Length == 1
        ? SinteringStation.RecipeName(target.Recipe)
        : SinteringStation.RecipeName(target.Recipe) + " · " + PowderBox.ComponentName(target.Ingredient.Component);

    private static MachineryStack MixingPanel(Func<GameItem> owner, GameSlotInventory input, GameSlotInventory[] bins,
        GameSlotInventory receiver, Action refreshMachine, out Action refreshPanel)
    {
        int sourceIndex = 0;
        long slotRevision = 0;
        var sourceButton = new MachineryButton();
        var goalButton = new MachineryButton();
        var amount = new MachinerySlider(1).SetRange(1, PowderColor.Capacity, 1);
        var preview = new MachineryText(uppercase: false);
        var less = new MachineryButton("−1");
        var more = new MachineryButton("+1");
        var all = new MachineryButton(L.K("workshop.sieve.all", "全部", "All"));
        var topUp = new MachineryButton(L.K("workshop.sieve.topUp", "补至目标", "Meet target"));
        var move = new MachineryButton(L.K("workshop.sieve.move", "确认分装", "Transfer"));
        GameSlotInventory SourceSlot() => sourceIndex == 0 ? input : bins[sourceIndex - 1];
        SinteringRecipes.PowderTarget Goal() => GoalFor(owner()?.GetTagReadonly(TargetTag).GetString());
        PowderBox.TransferStock Stock() => SourceSlot() == receiver ? null : PowderBox.InspectTransfer(SourceSlot().childItem, receiver.childItem);
        bool Daytime() => SaveSlot.CurrentStore?.storeState is PlayerStore.StoreState.OPEN or PlayerStore.StoreState.MORNING;
        bool Available() => !StoreSession.CompanyProgressPaused && owner() != null && Ready(owner()) && ShopInventory.Contains(owner())
            && Daytime() && LowerMachinist.StarterUnlocked(CorpState.Current);

        string Detail(PowderTransfer plan, SinteringIngredient goal)
        {
            int total = 0;
            foreach (int points in plan.TargetAfter) total += points;
            int percent = total > 0 ? (int)(plan.TargetAfter[(int)goal.Component] * 10000L / total) : 0;
            return MineralNightReport.Order + "\n" + L.F("workshop.sieve.transferDetail",
                "移出 {0}\n来源余 {1}\n目标盒 {2}\n{3} {4}%（需 81%）· 差 {5} 点",
                "Move {0}\nSource left {1}\nTarget box {2}\n{3} {4}% (needs 81%) · short {5} points",
                MineralNightReport.Vector(plan.Moved), MineralNightReport.Vector(plan.SourceAfter),
                MineralNightReport.Vector(plan.TargetAfter), PowderBox.ComponentName(goal.Component), percent / 100m, plan.Missing(goal));
        }

        void Refresh()
        {
            if (owner() == null) return;
            string sourceName = sourceIndex == 0 ? L.T("进料盒", "Feed box") : L.T(Names[sourceIndex - 1].Zh, Names[sourceIndex - 1].En);
            sourceButton.SetText(L.F("workshop.sieve.source", "来源 · {0}", "Source · {0}", sourceName));
            var recipe = Goal();
            goalButton.SetText(L.F("workshop.sieve.goal", "目标 · {0}", "Goal · {0}", GoalName(recipe)));
            if (!LowerMachinist.StarterUnlocked(CorpState.Current))
            {
                preview.SetText(MineralNightReport.Reason(MineralStop.Locked));
                return;
            }
            var stock = Stock();
            int request = amount.GetInt();
            int limit = stock?.Limit ?? 0;
            var plan = stock?.Plan(Math.Min(request, limit));
            string header = L.F("workshop.sieve.amount", "选择 {0} 点 · 可移 {1} 点", "Selected {0} · available {1} points", request, limit);
            preview.SetText(header + "\n" + (plan == null
                ? L.K("workshop.sieve.needBoxes", "选有粉的来源，放入有余量的取料盒", "Select a source with powder and insert a receiving box with space")
                : Detail(plan, recipe.Ingredient)));
        }

        void ChangeAmount(int next)
        {
            amount.SetInt(next);
            Refresh();
        }

        void ChangedSlot()
        {
            slotRevision++;
            Refresh();
        }

        void Confirm()
        {
            var stock = Stock();
            var session = StoreSession.Capture();
            if (!Available() || !session.IsCurrent || stock == null || stock.Limit == 0) { Refresh(); return; }
            int day = StoreStation.GetDayCounter();
            var sourceSlot = SourceSlot();
            long revision = slotRevision;
            int wanted = amount.GetInt();
            int actual = Math.Min(wanted, stock.Limit);
            var plan = stock.Plan(actual);
            if (plan == null) return;
            var ui = GenericConfirmationUIManager.Instance;
            string summary = L.F("workshop.sieve.confirm", "分装 {0} 点？", "Transfer {0} points?", actual);
            if (actual < wanted) summary = L.F("workshop.sieve.limit", "选择了 {0} 点，本次最多可移 {1} 点。按上限分装？",
                "Requested {0} points. Transfer the available {1} points?", wanted, actual);
            var recipe = Goal();
            ui?.OpenPanel(summary + "\n" + Detail(plan, recipe.Ingredient), L.K("production.canning.buildCanner.oK", "确定", "OK"),
                L.K("workshop.sieve.cancel", "取消", "Cancel"), () => HookGuard.Run("筛分机分装", () =>
                {
                    ui.ClosePanel();
                    if (!session.IsCurrent || !Available() || StoreStation.GetDayCounter() != day || revision != slotRevision
                        || sourceSlot != SourceSlot() || sourceSlot.childItem != stock.Source || receiver.childItem != stock.Target
                        || Goal().Id != recipe.Id || amount.GetInt() != wanted || !PowderBox.Transfer(stock, actual))
                        StoreUIManager.Instance?.Notify(L.K("workshop.sieve.changed", "库存或目标已变化，请重新预览", "Stock or target changed. Review the transfer again."));
                    Refresh();
                }), () => ui.ClosePanel());
        }

        sourceButton.SetCallback(() => { sourceIndex = (sourceIndex + 1) % (bins.Length + 1); Refresh(); });
        goalButton.SetCallback(() =>
        {
            if (!Available()) return;
            var next = GoalFor(Goal().Id, next: true);
            owner().ModifyTag(TargetTag, tag => tag.Enable().SetString(next.Id));
            refreshMachine();
        });
        amount.onChanged += _ => Refresh();
        less.SetCallback(() => ChangeAmount(amount.GetInt() - 1));
        more.SetCallback(() => ChangeAmount(amount.GetInt() + 1));
        all.SetCallback(() => ChangeAmount(Stock()?.Limit ?? 0));
        topUp.SetCallback(() =>
        {
            var stock = Stock();
            if (stock == null) { Refresh(); return; }
            int needed = PowderTransfer.ToGoal(stock.From, stock.To, stock.Capacity, Goal().Ingredient, WaterHelper.SplitProportionally);
            if (needed > 0) ChangeAmount(needed);
            else StoreUIManager.Instance?.Notify(needed < 0
                ? L.K("workshop.sieve.unreachable", "这些粉凑不齐目标的点数和纯度", "These powders cannot reach the required amount and purity")
                : L.K("workshop.sieve.noTopUp", "无需补料或没有可移余量", "No top-up needed or no transferable stock"));
        });
        move.SetCallback(Confirm);
        foreach (var slot in new[] { input, receiver })
        {
            slot.onSlotAddItemEndFunc += (_, _, _) => ChangedSlot();
            slot.onSlotRemoveItemEndFunc += (_, _, _) => ChangedSlot();
        }
        refreshPanel = Refresh;
        return MachineryStack.Vertical(MachineryStack.Horizontal(sourceButton, goalButton),
            MachineryStack.Horizontal(receiver, preview), amount, MachineryStack.Horizontal(less, more, all, topUp), move);
    }
}
