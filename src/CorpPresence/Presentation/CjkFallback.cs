using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CorpPresence;

internal static class CjkFallback
{
	private static bool _done;

	private static float _next;

	public static void Tick()
	{
		if (_done || Time.unscaledTime < _next)
		{
			return;
		}
		_next = Time.unscaledTime + 1f;
		try
		{
			FontLoader instance = FontLoader.Instance;
			TMP_FontAsset tMP_FontAsset = instance?.simplifiedChineseFont;
			if (tMP_FontAsset == null)
			{
				return;
			}
			List<TMP_FontAsset> fallbackFontAssets = TMP_Settings.fallbackFontAssets;
			if (fallbackFontAssets != null && !fallbackFontAssets.Contains(tMP_FontAsset))
			{
				fallbackFontAssets.Add(tMP_FontAsset);
			}
			TMP_FontAsset[] array = new TMP_FontAsset[15]
			{
				instance.westernFont, instance.notoSans, instance.notoSans_Normal, instance.notoSans_Bold, instance.notoSerif, instance.notoSerif_Medium, instance.notoSerif_Bold, instance.rubik, instance.caveat, instance.lubrifont,
				instance.tektur, instance.tektur_Bold, instance.polishDisplayFont, instance.japaneseFont, instance.koreanFont
			};
			foreach (TMP_FontAsset tMP_FontAsset2 in array)
			{
				if (!(tMP_FontAsset2 == null) && !(tMP_FontAsset2 == tMP_FontAsset))
				{
					TMP_FontAsset tMP_FontAsset3 = tMP_FontAsset2;
					if (tMP_FontAsset3.fallbackFontAssetTable == null)
					{
						List<TMP_FontAsset> list = (tMP_FontAsset3.fallbackFontAssetTable = new List<TMP_FontAsset>());
					}
					if (!tMP_FontAsset2.fallbackFontAssetTable.Contains(tMP_FontAsset))
					{
						tMP_FontAsset2.fallbackFontAssetTable.Add(tMP_FontAsset);
					}
				}
			}
			_done = true;
			ModLog.Info("已把中文字体 " + tMP_FontAsset.name + " 加为全局后备字体：西文界面下的中文不再显示成空白");
		}
		catch (Exception ex)
		{
			_done = true;
			ModLog.Warn("加中文后备字体失败：" + ex.Message);
		}
	}
}
