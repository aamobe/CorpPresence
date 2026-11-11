using System;
using System.Collections.Generic;
using System.Linq;
using CorpPresence.Malice;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{
	private static string _brandSel;

	private static float _swapScan = -1f;

	private static readonly List<(string Name, string Reason, string Target)> SwapRows = new List<(string, string, string)>();

	private static int _swapCount;

	private static Company _company;

	private static Theme _theme;

	private static Rect _rect;

	private static int _module;

	private static string _desk;

	private static string _toast;

	private static float _toastUntil;

	private static bool _openedThisFrame;

	private static readonly Dictionary<CatalogEntry, int> Cart = new Dictionary<CatalogEntry, int>();

	private static string _page = "日用";

	private static Vector2 _size;

	private const float DragBar = 80f;

	private static bool _dragging;

	private static bool _moved;

	private static Vector2 _grab;

	private static Vector2 _center;

	private static CatalogEntry _leviathan;

	private static int _partsPage;

	private const int PartsPerPage = 8;

	private static float _refurbScan = -1f;

	private static readonly List<(string Name, string From, string To, int Fee)> RefurbRows = new List<(string, string, string, int)>();

	private static int _refurbCount;

	private static readonly int[] SolanaModuleTier = new int[5] { 1, 2, 3, 0, 2 };

	private static readonly Dictionary<string, string> NameCache = new Dictionary<string, string>();

	private static readonly string[] MerchantPages = new string[3] { "码头", null, "目录" };

	private static readonly string[] AlliancePages = new string[6] { "军械", "药房", "码头", null, "阵线", "目录" };

	private static int _orderPage;

	private static string _orderPageKey;

	private static string[] CarmModules => WithExtra(CarmBase);

	private static string[] CarmBase => MenuLabels.Get(0, L.En);

	public static bool Visible { get; private set; }

	private static float Scale
	{
		get
		{
			float num = Mathf.Clamp((float)Screen.height / 1080f, 0.6f, 2f) * Settings.PanelScale;
			if (_size.x > 0f)
			{
				num = Mathf.Min(num, (float)Screen.width * 0.96f / _size.x, (float)Screen.height * 0.96f / _size.y);
			}
			return num;
		}
	}

	private static int Day => StoreStation.GetDayCounter();

	private static CompanyState State => Companies.StateOf(_company);

	public static bool Dragging
	{
		get
		{
			if (Visible)
			{
				return _dragging;
			}
			return false;
		}
	}

	private static int Cash => SaveSlot.CurrentStore?.GetCash() ?? 0;

	private static int DrawVoucherDetails(Rect r, int amount)
	{
		int cashDue = Services.CashDue(_company, amount);
		if (cashDue < amount)
		{
			int voucherDeduction = amount - cashDue;
			GUI.Label(r, L.K("ui.checkout.voucherDetails", (FormattableString)$"代金券抵扣 {voucherDeduction}₵ · 应付现金 {cashDue}₵",
				(FormattableString)$"Vouchers −{voucherDeduction}₵ · Cash due {cashDue}₵"), _theme.Small);
		}
		return cashDue;
	}

	private static int DrawCheckoutBalance(Rect r, int amount)
	{
		GUI.Label(new Rect(r.x, r.y, 120f, r.height), L.K("ui.checkout.cash", (FormattableString)$"现金 {Cash}₵", (FormattableString)$"Cash {Cash}₵"), _theme.Small);
		return DrawVoucherDetails(new Rect(r.x + 126f, r.y, r.width - 126f, r.height), amount);
	}

	private static string[] DavreModules => WithExtra(DavreBase);

	private static string[] DavreBase => MenuLabels.Get(1, L.En);

	private static string[] MeridianModules => WithExtra(MeridianBase);

	private static string[] MeridianBase => MenuLabels.Get(2, L.En);

	private static string[] PastelModules => WithExtra(PastelBase);

	private static string[] PastelBase => MenuLabels.Get(3, L.En);

	private static string[] MiloLines => MenuLabels.Get(4, L.En);

	private static string[] RebelModules
	{
		get
		{
			if (!Rebels.Arrested)
			{
				if (!Rebels.Cut)
				{
					if (!Rebels.FenceClosed)
					{
						return MenuLabels.Get(5, L.En);
					}
					return MenuLabels.Get(6, L.En);
				}
				return MenuLabels.Get(7, L.En);
			}
			return MenuLabels.Get(8, L.En);
		}
	}

	private static string[] SolanaModules => WithExtra(SolanaBase);

	private static string[] SolanaBase => MenuLabels.Get(9, L.En);

	private static string[] SpacerServices => WithExtra(SpacerBase);

	private static string[] SpacerBase
	{
		get
		{
			if (!Settings.MaliceMode)
			{
				return SpacerBaseFull;
			}
			return MenuLabels.Get(10, L.En);
		}
	}

	private static string[] SpacerPages
	{
		get
		{
			if (!Settings.MaliceMode)
			{
				return AlliancePages;
			}
			return MerchantPages;
		}
	}

	private static string[] SpacerBaseFull => MenuLabels.Get(11, L.En);

	private static int SpacerModuleCount => SpacerServices.Length;

	public static void Open(Company c)
	{
		if (c != null)
		{
			Ui.ClearHoverCache();
			ResetStoryGuide();
			_company = c;
			Theme theme;
			if (c is SolanaCo || c is EnvoyCo)
			{
				theme = Theme.Solana();
			}
			else if (c is CarmichaelCo)
			{
				theme = Theme.Carmichael();
			}
			else if (c is RebelCo)
			{
				theme = Theme.Rebel();
			}
			else if (c is SpacerCo || c is JacksonContact)
			{
				theme = Theme.Spacer();
			}
			else if (c is MeridianCo)
			{
				theme = Theme.Meridian();
			}
			else
			{
				theme = ((c is DavreCo) ? Theme.Davre() : Theme.Pastel());
			}
			_theme = theme;
			_module = 0;
			_page = "日用";
			_brandSel = null;
			Brand.ScanOwned();
			Cart.Clear();
			_desk = c.FrontDesk[new System.Random().Next(c.FrontDesk.Length)];
			_toast = null;
			Vector2 size;
			if (c is SolanaCo || c is EnvoyCo)
			{
				size = new Vector2(880f, 600f);
			}
			else if (c is CarmichaelCo)
			{
				size = new Vector2(860f, 640f);
			}
			else if (c is JacksonContact)
			{
				size = new Vector2(700f, 473f);
			}
			else if (c is SpacerCo || c is RebelCo)
			{
				size = new Vector2(820f, 640f);
			}
			else if (c is PastelCo)
			{
				size = new Vector2(1040f, 900f);
			}
			else
			{
				size = ((c is MeridianCo || c is DavreCo) ? new Vector2(860f, 640f) : new Vector2(660f, 796f));
			}
			_refurbScan = -1f;
			_swapScan = -1f;
			_size = size;
			_rect = new Rect(((float)Screen.width / Scale - size.x) / 2f, ((float)Screen.height / Scale - size.y) / 2f, size.x, size.y);
			if (_moved)
			{
				_rect.center = _center;
			}
			ClampToScreen();
			_dragging = false;
			Visible = true;
			_openedThisFrame = true;
			Pointer.PendingClick = null;
		}
	}

	public static void Close()
	{
		ResetStoryGuide();
		Visible = false;
		Pointer.PendingClick = null;
		InputShield.Update(visible: false, default);
		Portraits.ReleasePanelTextures();
	}

	public static void ReleaseUi()
	{
		try { Close(); }
		finally
		{
			_theme = null; _company = null; _toast = null; _brandSel = null;
			Cart.Clear(); NameCache.Clear(); SwapRows.Clear(); RefurbRows.Clear(); BrandFit.Clear(); _brandFitTheme = null;
			_dragging = false; _openedThisFrame = false;
			Ui.ResetCaches();
		}
	}
	private static void Toast(string text)
	{
		_toast = text;
		_toastUntil = Time.unscaledTime + 4.5f;
	}

	public static void Tick()
	{
		float scale = Scale;
		Pointer.Update(_rect, scale, Visible);
		InputShield.Update(Visible, new Rect(_rect.x * scale, (float)Screen.height - _rect.yMax * scale, _rect.width * scale, _rect.height * scale));
		if (Visible)
		{
			if (_openedThisFrame)
			{
				_openedThisFrame = false;
				Pointer.PendingClick = null;
			}
			else if (!Drag() && Input.GetMouseButtonDown(0) && !Pointer.OverWindow)
			{
				Close();
			}
		}
	}

	private static bool Drag()
	{
		if (_dragging)
		{
			if (!Input.GetMouseButton(0))
			{
				_dragging = false;
				return true;
			}
			_rect.position = Pointer.Logical - _grab;
			ClampToScreen();
			_moved = true;
			_center = _rect.center;
			Pointer.PendingClick = null;
			return true;
		}
		if (!Input.GetMouseButtonDown(0) || !Pointer.OverWindow)
		{
			return false;
		}
		Vector2 local = Pointer.Local;
		bool flag = local.x > _rect.width - (_company is JacksonContact ? 40f : 220f) && local.y < 38f;
		if ((local.y >= 80f) | flag)
		{
			return false;
		}
		_dragging = true;
		_grab = local;
		Pointer.PendingClick = null;
		return true;
	}

	private static void ClampToScreen()
	{
		float scale = Scale;
		float num = (float)Screen.width / scale;
		float num2 = (float)Screen.height / scale;
		_rect.x = Mathf.Clamp(_rect.x, 120f - _rect.width, num - 120f);
		_rect.y = Mathf.Clamp(_rect.y, 0f, num2 - 60f);
	}

	public static void Draw()
	{
		if (!Visible || _company == null)
		{
			return;
		}
		Matrix4x4 matrix = GUI.matrix;
		bool enabled = GUI.enabled;
		Color color = GUI.color;
		Color contentColor = GUI.contentColor;
		Color backgroundColor = GUI.backgroundColor;
		GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Scale, Scale, 1f));
		GUI.Box(_rect, GUIContent.none, _theme.Box);
		GUI.BeginGroup(_rect);
		Ui.Origin = Vector2.zero;
		Ui.Clip = null;
		try
		{
			if (_guideStage != 0)
			{
				DrawStoryGuide();
			}
			else if (_company is JacksonContact)
			{
				DrawJackson();
			}
			else if (_company is EnvoyCo)
			{
				DrawEnvoy();
			}
			else if (_company is RebelCo)
			{
				DrawRebel();
			}
			else if (_company is SolanaCo)
			{
				DrawSolana();
			}
			else if (_company is CarmichaelCo)
			{
				DrawCarmichael();
			}
			else if (_company is SpacerCo)
			{
				DrawSpacer();
			}
			else if (_company is MeridianCo)
			{
				DrawMeridian();
			}
			else if (_company is DavreCo)
			{
				DrawDavre();
			}
			else
			{
				DrawPastel();
			}
			if (_company is not JacksonContact && _guideStage == 0 && Ui.Button(new Rect(_rect.width - 214f, 8f, 170f, 24f), L.K("ui.storyGuide.open", "剧情流程与条件", "Story requirements"), _theme.BtnStyle))
			{
				_guideStage = 1;
			}
			if (Ui.Button(new Rect(_rect.width - 34f, 8f, 26f, 22f), "×", _theme.BtnStyle))
			{
				Close();
			}
			if (!string.IsNullOrEmpty(_toast) && Time.unscaledTime < _toastUntil)
			{
				Rect position = new Rect(_rect.width / 2f - 170f, 100f, 340f, 30f);
				GUI.Box(position, GUIContent.none, _theme.CellOnBox);
				GUI.Label(position, _toast, _theme.Center);
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("面板绘制出错：" + ex.Message);
		}
		finally
		{
			Ui.Origin = Vector2.zero;
			Ui.Clip = null;
			GUI.EndGroup();
			GUI.matrix = matrix;
			GUI.enabled = enabled;
			GUI.color = color;
			GUI.contentColor = contentColor;
			GUI.backgroundColor = backgroundColor;
		}
		Event current = Event.current;
		if (current != null && current.type == EventType.Repaint)
		{
			Pointer.PendingClick = null;
		}
	}

	private static string[] WithExtra(string[] modules)
	{
		Func<Company, string> f = Ext.ExtraModule;
		Company c = _company;
		string text = ((f == null || c == null) ? null : Ext.Ask("ExtraModule", () => f(c), null));
		string[] array = MenuLabels.Append(modules, text);
		if (_module >= array.Length)
		{
			_module = 0;
		}
		return array;
	}

	private static bool DrawExtraIfSelected(int baseCount, Rect body)
	{
		if (_module < baseCount)
		{
			return false;
		}
		Action<Company, Rect, Theme> f = Ext.DrawExtraModule;
		Company c = _company;
		Theme t = _theme;
		if (f != null)
		{
			Ext.Ask("DrawExtraModule", () =>
			{
				f(c, body, t);
				return true;
			}, fallback: false);
		}
		return true;
	}

	private static void Banner(Rect r)
	{
		if (_theme.BannerTile > 0)
		{
			int num = 24;
			for (float num2 = r.x; num2 < r.xMax; num2 += (float)num)
			{
				for (float num3 = r.y; num3 < r.yMax; num3 += (float)num)
				{
					Ui.Image(new Rect(num2, num3, Mathf.Min(num, r.xMax - num2), Mathf.Min(num, r.yMax - num3)), _theme.Banner);
				}
			}
		}
		else if (_theme.Banner != null)
		{
			Ui.Image(r, _theme.Banner);
		}
	}

	private static void CoopBar(Rect r)
	{
		CompanyState state = State;
		Ui.Fill(r, _theme.Accent2);
		Ui.Fill(new Rect(r.x + 1f, r.y + 1f, r.width - 2f, r.height - 2f), _theme.Paper);
		Ui.Fill(new Rect(r.x + 1f, r.y + 1f, (r.width - 2f) * (float)state.Coop / 100f, r.height - 2f), _theme.Accent);
		int[] tierAt = Company.TierAt;
		foreach (int num in tierAt)
		{
			Ui.Fill(new Rect(r.x + r.width * (float)num / 100f, r.y, 1f, r.height), _theme.Ink);
		}
	}

	private static string NextTierHint()
	{
		CompanyState state = State;
		int num = _company.NextTierAt(state);
		if (num != 0)
		{
			return L.K("ui.company.nextTier", (FormattableString)$"距“{_company.TierNames[Array.IndexOf(Company.TierAt, num)]}”还差 {num - state.Coop} 点",
				(FormattableString)$"{num - state.Coop} more to “{_company.TierNames[Array.IndexOf(Company.TierAt, num)]}”");
		}
		return L.K("ui.company.topTier", "已是最高档", "Top tier reached");
	}

	private static string TierLabel(CompanyState s)
	{
		return _company.TierName(s);
	}
}



