using System;
using System.Collections.Generic;

namespace CorpPresence;

internal static class OrderContents
{
    // Keep remaining rolled items in the existing save schema, including duplicates.
    public static void ExpandModBoxes(Order order, Func<List<string>> roll)
    {
        if (!order.Items.Contains("@modbox")) return;
        var expanded = new List<string>();
        foreach (string id in order.Items)
        {
            if (id != "@modbox") { expanded.Add(id); continue; }
            List<string> contents = roll();
            if (contents == null || contents.Count == 0 || contents.Exists(string.IsNullOrEmpty))
                expanded.Add(id);
            else expanded.AddRange(contents);
        }
        // A failed roll must leave the whole original order intact.
        order.Items = expanded;
    }
}
