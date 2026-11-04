using System;

namespace CorpPresence.UI;

internal static class MenuLabels
{
	private static readonly string[][] Chinese = new string[12][]
	{
		new string[5] { "正品订货", "体力增强剂申领", "赝品换正品", "标本", "品牌目录" },
		new string[4] { "订货", "利维坦净水器", "罐装设备", "品牌目录" },
		new string[5] { "制式订货", "高端配件", "返厂翻新", "锻造", "品牌目录" },
		new string[4] { "邮购订货", "下层合作商", "下层客流", "品牌目录" },
		new string[3] { "今天进货吗？", "又是我！", "货多，随便挑。" },
		new string[3] { "军械订货", "地下销赃网络", "任务" },
		new string[2] { "军械订货", "任务" },
		new string[1] { "和解" },
		new string[1] { "营救奥丁" },
		new string[4] { "贵宾引荐", "实验室采购单", "会员礼遇", "品牌目录" },
		new string[3] { "码头补给", "街坊互助", "品牌目录" },
		new string[6] { "开源军械", "地下药房", "码头补给", "街坊互助", "合作阵线", "品牌目录" }
	};

	private static readonly string[][] English = new string[12][]
	{
		new string[5] { "Genuine Orders", "Stamina Booster Request", "Fake-for-Genuine Swap", "Specimens", "Brand Catalog" },
		new string[4] { "Orders", "Leviathan Purifier", "Canning Equipment", "Brand Catalog" },
		new string[5] { "Standard Issue Orders", "High-End Attachments", "Factory Refurbishment", "Forging", "Brand Catalog" },
		new string[4] { "Mail Order", "Lower Dealer", "Lower Foot Traffic", "Brand Catalog" },
		new string[3] { "Stocking up today?", "Me again!", "Plenty of goods. Take your pick." },
		new string[3] { "Arms Orders", "Fence Network", "Tasks" },
		new string[2] { "Arms Orders", "Tasks" },
		new string[1] { "Make Peace" },
		new string[1] { "Rescue Odin" },
		new string[4] { "VIP Referral", "Lab Purchase Order", "Member Perks", "Brand Catalog" },
		new string[3] { "Dock Supplies", "Mutual Aid", "Brand Catalog" },
		new string[6] { "Open-Source Arms", "Back-Alley Pharmacy", "Dock Supplies", "Mutual Aid", "Joint Front", "Brand Catalog" }
	};

	private static string[] _lastBase;

	private static string _lastExtra;

	private static string[] _lastCombined;

	public static string[] Get(int index, bool english)
	{
		return (english ? English : Chinese)[index];
	}

	public static string[] Append(string[] basis, string extra)
	{
		if (extra == null)
		{
			return basis;
		}
		if (basis == _lastBase && extra == _lastExtra)
		{
			return _lastCombined;
		}
		string[] array = new string[basis.Length + 1];
		Array.Copy(basis, array, basis.Length);
		array[basis.Length] = extra;
		_lastBase = basis;
		_lastExtra = extra;
		return _lastCombined = array;
	}
}
