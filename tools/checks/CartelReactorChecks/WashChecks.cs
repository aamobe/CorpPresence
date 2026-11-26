using CorpPresence;

internal static class WashChecks
{
    internal static void Refusals(GameItem bench, GameItem shell, GameItem vessel)
    {
        var source = shell.parentInventory;
        var output = bench.Slot("output");
        var liquid = bench.Slot("liquid");
        var material = ((GameSlotInventory[])bench.contentWindow.childElement.GetPanel("input").Content)[0];
        var assemble = ((object[])bench.contentWindow.childElement.GetPanel("control").Content).OfType<MachineryButton>().Last();
        var parts = WaterHelper.GetLiquidParts(vessel);
        source.Expel(shell);
        material.Put(shell);
        var wine = new GameItem("wine");
        wine.EnableTag("LIQUID_CONTAINER_TAG");
        wine.EnableTag("LIQUID_CONTAINER_BASE_PRICE_TAG");
        wine.EnableTag("WINE_CONTAINER_TAG");
        wine.ModifyTag("alcohol", t => t.SetInt(30000));
        if (liquid.mayInventoryAddItemFunc(wine, liquid) || AlcoholPreparationStation.AlcoholUlAvailable(wine) != 0)
            throw new Exception("Wine bottles cannot feed the repair wash slot");

        void Unchanged(int expectedAlcohol)
        {
            if (material.childItem != shell || shell.unitCount != 1 || output.childItems.Count != 0
                || !WaterHelper.GetLiquidParts(vessel).SequenceEqual(new[]{parts[0], expectedAlcohol, parts[2]}))
                throw new Exception("Failed wash changed stock, alcohol, or other liquid components");
        }
        vessel.ModifyTag("alcohol", t => t.SetInt(14999));
        assemble.Click();
        Unchanged(14999);
        vessel.ModifyTag("alcohol", t => t.SetInt(parts[1]));
        int capacity = output.Capacity;
        output.Capacity = 0;
        assemble.Click();
        Unchanged(parts[1]);
        output.Capacity = capacity;
        GraphUtils.AfterAccept = (_, _) => liquid.Expel(vessel);
        try { assemble.Click(); }
        finally { GraphUtils.AfterAccept = null; }
        Unchanged(parts[1]);
        liquid.Put(vessel);
        material.Expel(shell);
        source.Put(shell);
        shell.onSlotRemoveItemFunc = (_, inv, _) =>
        {
            if (inv != material) return;
            CompanyTransaction.DeferSave(SaveSlot.CurrentStore, () =>
            {
                if (material.childItems.Contains(shell) || shell.parentInventory != null || output.childItems.Count != 1
                    || AlcoholPreparationStation.AlcoholUlAvailable(vessel) != parts[1] - IndustrialWorkbench.WashAlcoholUl)
                    throw new Exception("A native save saw the spent housing still attached to the paid wash");
            });
            throw new Exception("injected pre-removal callback on spent housing");
        };
    }

    internal static void DismantleRollback(GameItem bench, GameItem unit)
    {
        var source = unit.parentInventory;
        var output = bench.Slot("output");
        var input = ((GameSlotInventory[])bench.contentWindow.childElement.GetPanel("input").Content)[0];
        var assemble = ((object[])bench.contentWindow.childElement.GetPanel("control").Content).OfType<MachineryButton>().Last();
        source.Expel(unit);
        input.Put(unit);
        int capacity = output.Capacity;
        bool observedSave = false;
        var prior = output.onSlotRemoveItemFunc;
        try
        {
            output.onSlotRemoveItemFunc += (_, _, _) => throw new Exception("injected native grid callback before removing staged housing");
            GraphUtils.AfterAccept = (inv, _) =>
            {
                inv.Capacity = 1; // The next component fails to land after the tagged housing is staged.
                if (!CompanyTransaction.DeferSave(SaveSlot.CurrentStore, () =>
                {
                    // Native snapshots enumerate the inventory graph, including a destroyed child left attached.
                    observedSave = inv.childItems.Count == 0 && input.childItem == unit && unit.unitCount == 1;
                })) throw new Exception("Assembly did not defer a save requested by the output callback");
            };
            assemble.Click();
        }
        finally
        {
            GraphUtils.AfterAccept = null;
            output.onSlotRemoveItemFunc = prior;
            output.Capacity = capacity;
        }
        if (!observedSave || input.childItem != unit || output.childItems.Count != 0)
            throw new Exception("Failed dismantling left a serializable refund beside the original assembly");
        input.Expel(unit);
        source.Put(unit);
    }
}
