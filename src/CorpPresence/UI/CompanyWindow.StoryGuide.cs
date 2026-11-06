using System;
using CorpPresence.Malice;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{
	private static int _guideStage;
	private static int _guidePage;
	private static string[] _guidePages;
	private static string _guideLocale;
	private static int _currentGuidePage;
	private static GUIStyle _currentGuideStyle;

	private static void ResetStoryGuide()
	{
		_guideStage = 0;
		_guidePage = 0;
		_guidePages = null;
		_currentGuidePage = -1;
		_currentGuideStyle = null;
	}

	private static void DrawStoryGuide()
	{
		GUI.Label(new Rect(24f, 28f, _rect.width - 48f, 32f), _company.Name + L.T(" · 剧情流程与条件", " · Story requirements"), _theme.Title);
		if (_guideStage == 1)
		{
			GUI.Label(new Rect(32f, 110f, _rect.width - 64f, 100f), L.T("以下内容会公开本公司的全部章节、支线及隐藏触发条件，可能包含剧透。\n\n确定查看吗？查看不会推进剧情或修改存档。", "This guide reveals this company's chapters, side stories and hidden requirements. It contains spoilers.\n\nView it? Reading does not advance the story or change your save."), _theme.Wrap);
			if (Ui.Button(new Rect(32f, 230f, 200f, 34f), L.T("确定，查看流程", "Confirm: view guide"), _theme.BtnHotStyle))
			{
				_guidePages = StoryGuide.Build(_company);
				_guideLocale = L.Locale;
				_guidePage = 0;
				MarkCurrentGuidePage();
				_guideStage = 2;
			}
			if (Ui.Button(new Rect(246f, 230f, 180f, 34f), L.T("取消，返回", "Cancel / back"), _theme.BtnStyle))
			{
				ResetStoryGuide();
			}
			return;
		}
		if (_guidePages == null || _guideLocale != L.Locale)
		{
			_guidePages = StoryGuide.Build(_company);
			_guideLocale = L.Locale;
			_guidePage = 0;
			MarkCurrentGuidePage();
		}
		GUI.Label(new Rect(32f, 94f, _rect.width - 64f, _rect.height - 200f), _guidePages[_guidePage], (_guidePage == _currentGuidePage) ? _currentGuideStyle : _theme.Wrap);
		float num = _rect.height - 72f;
		if (Ui.Button(new Rect(32f, num, 140f, 32f), L.T("上一章节", "Previous"), _theme.BtnStyle))
		{
			_guidePage = (_guidePage + _guidePages.Length - 1) % _guidePages.Length;
		}
		GUI.Label(new Rect(180f, num + 4f, 100f, 24f), $"{_guidePage + 1} / {_guidePages.Length}", _theme.Center);
		if (Ui.Button(new Rect(288f, num, 140f, 32f), L.T("下一章节", "Next"), _theme.BtnStyle))
		{
			_guidePage = (_guidePage + 1) % _guidePages.Length;
		}
		if (Ui.Button(new Rect(_rect.width - 190f, num, 158f, 32f), L.T("返回公司服务", "Back to services"), _theme.BtnStyle))
		{
			ResetStoryGuide();
		}
	}

	private static void MarkCurrentGuidePage()
	{
		int num = StoryGuide.CurrentChapter(_company);
		_currentGuidePage = ((num > 0 && num <= _guidePages.Length) ? (num - 1) : (-1));
		if (_currentGuidePage >= 0)
		{
			if (_currentGuideStyle == null)
			{
				_currentGuideStyle = new GUIStyle(_theme.Wrap)
				{
					richText = true
				};
			}
			_guidePages[_currentGuidePage] = L.T("<color=#75CF88>● 当前节点</color>  ", "<color=#75CF88>● Current step</color>  ") + _guidePages[_currentGuidePage];
			_guidePage = _currentGuidePage;
		}
	}

}
