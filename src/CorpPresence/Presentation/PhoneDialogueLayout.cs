using UnityEngine;
using UnityEngine.UI;

namespace CorpPresence;

internal static class PhoneDialogueLayout
{
	private const float RightInset = 8f;
	private static BubbleLayout _client;
	private static BubbleLayout _player;

	internal static void AfterClientText(DialoguePhoneUIManager __instance) => FitOwned(__instance.clientBubble, ref _client);

	internal static void AfterPlayerText(DialoguePhoneUIManager __instance) => FitOwned(__instance.playerBubble, ref _player);

	internal static void AfterBubbleScale(DialoguePhoneUIManager __instance)
	{
		if (__instance.currentDialog == null) return;
		if (__instance.clientBubble != null && __instance.clientBubble.gameObject.activeSelf)
			AfterClientText(__instance);
		if (__instance.playerBubble != null && __instance.playerBubble.gameObject.activeSelf)
			AfterPlayerText(__instance);
	}

	private static void FitOwned(TextBubble bubble, ref BubbleLayout state)
	{
		var client = PhoneUIManager.Instance?.currentPhoneClient;
		string locId = client?.locID;
		if (locId != MangoGift.PhoneId && locId != WasteBatteryGift.PhoneId && Companies.ByLocId(locId) == null)
		{
			Reset();
			return;
		}
		Fit(bubble, ref state);
	}

	private static void Fit(TextBubble bubble, ref BubbleLayout state)
	{
		if (bubble == null || bubble.textfield == null || bubble.backgroundImage == null) return;
		var rect = bubble.backgroundImage.rectTransform;
		if (rect.parent is not RectTransform phone || rect.localScale.x <= 0f) return;
		var layout = rect.GetComponent<HorizontalLayoutGroup>();
		var fitter = rect.GetComponent<ContentSizeFitter>();
		if (layout == null || fitter == null) return;

		float scale = rect.localScale.x;
		float left = rect.localPosition.x - rect.rect.width * rect.pivot.x * scale;
		float maxWidth = Mathf.Floor((phone.rect.xMax - left - RightInset) / scale);
		if (maxWidth <= layout.padding.horizontal) return;
		float preferredWidth = Mathf.Ceil(bubble.textfield.preferredWidth + layout.padding.horizontal);
		if (state?.Rect != rect)
		{
			state?.Restore();
			state = new BubbleLayout(rect, fitter);
		}
		rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(preferredWidth, maxWidth));
		LayoutRebuilder.MarkLayoutForRebuild(rect);
	}

	internal static void Reset()
	{
		_client?.Restore();
		_player?.Restore();
		_client = null;
		_player = null;
	}

	private sealed class BubbleLayout
	{
		internal readonly RectTransform Rect;
		private readonly ContentSizeFitter _fitter;
		private readonly ContentSizeFitter.FitMode _horizontalFit;
		private readonly ContentSizeFitter.FitMode _verticalFit;
		private readonly Vector2 _pivot;
		private readonly Vector2 _position;
		private readonly Vector2 _size;

		internal BubbleLayout(RectTransform rect, ContentSizeFitter fitter)
		{
			Rect = rect;
			_fitter = fitter;
			_horizontalFit = fitter.horizontalFit;
			_verticalFit = fitter.verticalFit;
			_pivot = rect.pivot;
			_position = rect.anchoredPosition;
			_size = rect.sizeDelta;

			// Keep the original top edge, so a taller client bubble grows away from its nameplate.
			float top = rect.rect.height * (1f - _pivot.y) * rect.localScale.y;
			rect.pivot = new Vector2(_pivot.x, 1f);
			rect.anchoredPosition = _position + Vector2.up * top;
			fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		}

		internal void Restore()
		{
			if (Rect == null || _fitter == null) return;
			_fitter.horizontalFit = _horizontalFit;
			_fitter.verticalFit = _verticalFit;
			Rect.pivot = _pivot;
			Rect.anchoredPosition = _position;
			Rect.sizeDelta = _size;
			LayoutRebuilder.MarkLayoutForRebuild(Rect);
		}
	}
}
