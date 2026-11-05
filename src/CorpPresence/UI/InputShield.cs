using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CorpPresence.UI;

internal static class InputShield
{
	private static GameObject _root;

	private static RectTransform _hitArea;

	private static bool _failed;

	private static Rect _bounds;

	private static readonly MethodInfo ResetDrag = AccessTools.Method(typeof(ItemMouseDragHandler), "OnEventReset");

	public static bool Blocking
	{
		get
		{
			if (!Pointer.OverWindow)
			{
				return CompanyWindow.Dragging;
			}
			return true;
		}
	}

	public static bool BeforeGraphicRaycast(GraphicRaycaster __instance)
	{
		try
		{
			return !Blocking || (_root != null && __instance != null && __instance.gameObject == _root);
		}
		catch
		{
			return true;
		}
	}

	public static bool BeforeGameInput(InputActionManager __instance, ref Vector3 ___lastMousePosition)
	{
		try
		{
			if (Blocking && (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)))
			{
				ItemMouseDragHandler current = ItemMouseDragHandler.current;
				if (current != null && current.GetCurrentItem() != null)
				{
					ResetDrag?.Invoke(current, null);
				}
			}
			if (!Blocking)
			{
				return true;
			}
			__instance.Reset();
			___lastMousePosition = Input.mousePosition;
			return false;
		}
		catch
		{
			return true;
		}
	}

	public static bool BeforeDoubleClick(CustomEventHandler __instance, ref TreeNodeRender ___lastHandlerClicked, ref bool ___hasRayCast, ref float ___timeDeltaSeconds)
	{
		try
		{
			if (!Blocking)
			{
				return true;
			}
			___lastHandlerClicked = null;
			___hasRayCast = false;
			___timeDeltaSeconds = __instance.doubleClickTimeThreshold;
			return false;
		}
		catch
		{
			return true;
		}
	}

	public static bool BeforeStoreRaycast(StoreUIManager __instance)
	{
		try
		{
			if (!Blocking)
			{
				return true;
			}
			__instance.hoveringStoreUI = true;
			return false;
		}
		catch
		{
			return true;
		}
	}

	public static void AfterStoreUpdate(StoreUIManager __instance)
	{
		try
		{
			if (Blocking)
			{
				__instance.hoveringStoreUI = true;
			}
		}
		catch
		{
		}
	}

	public static bool BeforeEscape(ref bool __result)
	{
		try
		{
			if (!CompanyWindow.Visible)
			{
				return true;
			}
			CompanyWindow.Close();
			__result = true;
			return false;
		}
		catch
		{
			return true;
		}
	}

	public static void Update(bool visible, Rect screenRect)
	{
		if (!visible)
		{
			Hide();
		}
		else
		{
			if (_failed)
			{
				return;
			}
			try
			{
				if (_root == null)
				{
					Create();
				}
				if (_bounds != screenRect)
				{
					_hitArea.anchoredPosition = new Vector2(screenRect.x, screenRect.y);
					_hitArea.sizeDelta = new Vector2(screenRect.width, screenRect.height);
					_bounds = screenRect;
				}
				if (!_root.activeSelf)
				{
					_root.SetActive(value: true);
				}
			}
			catch (Exception ex)
			{
				_failed = true;
				ModLog.Warn("遮挡面板不可用，只靠输入补丁防点穿：" + ex.Message);
				Hide();
			}
		}
	}

	private static void Create()
	{
		_root = Compat.NewObject<RectTransform, Canvas, GraphicRaycaster>("CorpPresence Input Shield");
		_root.SetActive(value: false);
		Canvas component = _root.GetComponent<Canvas>();
		component.renderMode = RenderMode.ScreenSpaceOverlay;
		component.sortingOrder = 32766;
		GameObject gameObject = Compat.NewObject<RectTransform, CanvasRenderer, Image>("CorpPresence Window Hit Area");
		gameObject.transform.SetParent(_root.transform, worldPositionStays: false);
		_hitArea = gameObject.GetComponent<RectTransform>();
		_hitArea.pivot = Vector2.zero;
		_hitArea.anchorMin = Vector2.zero;
		_hitArea.anchorMax = Vector2.zero;
		Image component2 = gameObject.GetComponent<Image>();
		component2.color = new Color(0f, 0f, 0f, 0.001f);
		component2.raycastTarget = true;
		UnityEngine.Object.DontDestroyOnLoad(_root);
		_bounds = default;
	}

	public static void Destroy()
	{
		try
		{
			if (_root != null)
			{
				UnityEngine.Object.Destroy(_root);
			}
		}
		catch
		{
		}
		_root = null;
		_hitArea = null;
	}

	private static void Hide()
	{
		try
		{
			if (_root != null && _root.activeSelf)
			{
				_root.SetActive(value: false);
			}
		}
		catch
		{
		}
	}
}
