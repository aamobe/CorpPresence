using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.Malice;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{
	private static (Rect Item, Rect Unit, Rect Quantity, Rect Subtotal, Rect ListPrice) OrderColumns(Rect row)
	{
		const float listWidth = 78f;
		const float subtotalWidth = 64f;
		const float quantityWidth = 120f;
		const float unitWidth = 78f;
		float listX = row.xMax - listWidth;
		float subtotalX = listX - subtotalWidth;
		float quantityX = subtotalX - quantityWidth;
		float unitX = quantityX - unitWidth;
		return (new Rect(row.x + 8f, row.y, unitX - row.x - 10f, row.height),
			new Rect(unitX, row.y, unitWidth, row.height),
			new Rect(quantityX, row.y, quantityWidth, row.height),
			new Rect(subtotalX, row.y, subtotalWidth, row.height),
			new Rect(listX, row.y, listWidth, row.height));
	}

	private static void DrawOrderHeader(Rect row, GUIStyle nameStyle, GUIStyle priceStyle)
	{
		var columns = OrderColumns(row);
		GUI.Label(columns.Item, L.K("ui.catalog.header.item", "品名", "Item"), nameStyle);
		GUI.Label(columns.Unit, L.K("ui.catalog.header.orderPrice", "订货价", "Order price"), priceStyle);
		GUI.Label(columns.Quantity, L.K("ui.catalog.header.quantity", "数量", "Qty"), priceStyle);
		GUI.Label(columns.Subtotal, L.K("ui.catalog.header.subtotal", "小计", "Subtotal"), priceStyle);
		GUI.Label(columns.ListPrice, L.K("ui.catalog.header.itemValue", "物品标价", "Item value"), priceStyle);
	}

	private static void DrawOrderItem(Rect cell, CatalogEntry entry, CompanyState state, Theme theme)
	{
		string name = entry.Name();
		string quota = Services.LeftText(state, entry);
		if (quota.Length == 0)
		{
			GUI.Label(cell, name, theme.Body);
			return;
		}
		float half = cell.height / 2f;
		GUI.Label(new Rect(cell.x, cell.y, cell.width, half + 2f), name, theme.Body);
		GUI.Label(new Rect(cell.x, cell.y + half - 1f, cell.width, half + 1f), quota.TrimStart(), theme.Small);
	}

	private static int ItemListValue(CatalogEntry entry)
	{
		int value = entry.ListValue();
		return value < 0 ? value : value / Math.Max(1, entry.Bundle);
	}

	private static string ItemListPrice(int value, bool labeled = false)
	{
		string amount = value < 0 ? "—" : $"{value}₵";
		return labeled ? L.K("ui.catalog.item.listPrice", (FormattableString)$"标价 {amount}", (FormattableString)$"List {amount}") : amount;
	}

	private static void DrawBrand(Rect r)
	{
		Theme theme = _theme;
		CompanyState state = State;
		string[] array = Brand.Of(_company);
		int num = 0;
		string[] array2 = array;
		foreach (string item in array2)
		{
			if (state.Brand.Contains(item))
			{
				num++;
			}
		}
		GUI.Label(new Rect(r.x, r.y, r.width, 22f), L.K("ui.catalog.brand.progress", (FormattableString)$"已收录 {num}/{array.Length}\u3000买进、卖出或拿到过的本公司产品会收录进来",
			(FormattableString)$"Recorded {num}/{array.Length}   Products you buy, sell or receive from this company get recorded here"), theme.Small);
		int num2 = Mathf.Max(3, (int)((r.width + 6f) / 124f));
		float num3 = (r.width - 6f * (float)(num2 - 1)) / (float)num2;
		int num4 = (array.Length + num2 - 1) / num2;
		float num5 = r.y + 28f;
		float num6 = r.height - 28f - 120f - 10f;
		float num7 = Mathf.Min(56f, (num6 - 6f * (float)(num4 - 1)) / (float)Mathf.Max(1, num4));
		for (int j = 0; j < array.Length; j++)
		{
			string text = array[j];
			bool flag = state.Brand.Contains(text);
			Rect rect = new Rect(r.x + (float)(j % num2) * (num3 + 6f), num5 + (float)(j / num2) * (num7 + 6f), num3, num7);
			Rect r2 = rect;
			GUIStyle style;
			if (flag && text == Brands.Selected)
			{
				style = theme.NavOnStyle;
			}
			else
			{
				style = (flag ? theme.CellOnBox : theme.CellBox);
			}
			if (Ui.Button(r2, "", style) & flag)
			{
				Brands.Selected = text;
			}
			if (!flag)
			{
				GUI.Label(rect, "？", theme.Center);
				continue;
			}
			Brand.Entry entry = Brand.Info(text);
			float num8 = num7 - 8f;
			DrawIcon(new Rect(rect.x + 4f, rect.y + 4f, num8 * 1.4f, num8), entry.Icon);
			Rect position = new Rect(rect.x + 8f + num8 * 1.4f, rect.y + 2f, rect.width - 12f - num8 * 1.4f, num7 - 4f);
			GUI.Label(position, entry.Name, NameFits(theme, entry.Name, position.width, position.height) ? theme.Wrap : theme.Tight);
		}
		Rect position2 = new Rect(r.x, r.yMax - 120f, r.width, 120f);
		GUI.Box(position2, GUIContent.none, theme.CellBox);
		if (Brands.Selected == null || !state.Brand.Contains(Brands.Selected) || Array.IndexOf(array, Brands.Selected) < 0)
		{
			GUI.Label(new Rect(position2.x + 10f, position2.y + 10f, position2.width - 20f, 24f), (num == 0) ? L.K("ui.catalog.brand.empty", "还没有经手过这家公司的产品。", "You haven't handled any of this company's products yet.") : L.K("ui.catalog.brand.selectItem", "点一件已收录的物品，看它的介绍。", "Click a recorded item to read about it."), theme.Small);
			return;
		}
		Brand.Entry entry2 = Brand.Info(Brands.Selected);
		DrawIcon(new Rect(position2.x + 10f, position2.y + 10f, 128f, 96f), entry2.Icon);
		GUI.Label(new Rect(position2.x + 150f, position2.y + 8f, position2.width - 160f, 26f), entry2.Name, theme.Title);
		GUI.Label(new Rect(position2.x + 150f, position2.y + 38f, position2.width - 160f, position2.height - 44f), entry2.Desc, theme.Wrap);
	}

	// 品牌名在格子里放不放得下：按名字、格子尺寸缓存，不再每次绘制都量一遍文字
	private static readonly GUIContent FitContent = new GUIContent();
	private static bool NameFits(Theme theme, string name, float width, float height)
	{
		if (!ReferenceEquals(Brands.Theme, theme))
		{
			Brands.Fits.Clear();
			Brands.Theme = theme;
		}
		string key = name ?? "";
		if (Brands.Fits.TryGetValue(key, out var fit) && fit.w == width && fit.h == height)
		{
			return fit.fits;
		}
		FitContent.text = key;
		bool fits = theme.Wrap.CalcHeight(FitContent, width) <= height;
		Brands.Fits[key] = (width, height, fits);
		return fits;
	}

	private static void DrawIcon(Rect box, Texture2D tex)
	{
		if (!(tex == null))
		{
			float num = Mathf.Min(box.width / (float)tex.width, box.height / (float)tex.height);
			if (num >= 1f)
			{
				num = Mathf.Floor(num);
			}
			float num2 = (float)tex.width * num;
			float num3 = (float)tex.height * num;
			Ui.Image(new Rect(box.x + (box.width - num2) / 2f, box.y + (box.height - num3) / 2f, num2, num3), tex);
		}
	}

	private static string ItemName(string id) => Catalog.Name(id);

}

