using CorpPresence.Malice;
using UnityEngine;

namespace CorpPresence.UI;

internal static partial class CompanyWindow
{
    // Per-open view state; help is constructed only after explicit spoiler consent.
    private static int _guideStage;
    private static int _guidePage;
    private static string[] _guidePages;
    private static bool _guideEnglish;
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
                _guideEnglish = L.En;
                _guidePage = 0;
                MarkCurrentGuidePage();
                _guideStage = 2;
            }
            if (Ui.Button(new Rect(246f, 230f, 180f, 34f), L.T("取消，返回", "Cancel / back"), _theme.BtnStyle)) ResetStoryGuide();
            return;
        }
        if (_guidePages == null || _guideEnglish != L.En)
        {
            _guidePages = StoryGuide.Build(_company);
            _guideEnglish = L.En;
            _guidePage = 0;
            MarkCurrentGuidePage();
        }
        GUI.Label(new Rect(32f, 94f, _rect.width - 64f, _rect.height - 200f), _guidePages[_guidePage],
            _guidePage == _currentGuidePage ? _currentGuideStyle : _theme.Wrap);
        float y = _rect.height - 72f;
        if (Ui.Button(new Rect(32f, y, 140f, 32f), L.T("上一章节", "Previous"), _theme.BtnStyle))
            _guidePage = (_guidePage + _guidePages.Length - 1) % _guidePages.Length;
        GUI.Label(new Rect(180f, y + 4f, 100f, 24f), $"{_guidePage + 1} / {_guidePages.Length}", _theme.Center);
        if (Ui.Button(new Rect(288f, y, 140f, 32f), L.T("下一章节", "Next"), _theme.BtnStyle))
            _guidePage = (_guidePage + 1) % _guidePages.Length;
        if (Ui.Button(new Rect(_rect.width - 190f, y, 158f, 32f), L.T("返回公司服务", "Back to services"), _theme.BtnStyle)) ResetStoryGuide();
    }

    private static void MarkCurrentGuidePage()
    {
        int chapter = CurrentGuideChapter(_company);
        _currentGuidePage = chapter > 0 && chapter <= _guidePages.Length ? chapter - 1 : -1;
        if (_currentGuidePage < 0) return;

        _currentGuideStyle ??= new GUIStyle(_theme.Wrap) { richText = true };
        _guidePages[_currentGuidePage] = L.T("<color=#75CF88>● 当前节点</color>  ", "<color=#75CF88>● Current step</color>  ")
            + _guidePages[_currentGuidePage];
        _guidePage = _currentGuidePage;
    }

    private static int CurrentGuideChapter(Company company)
    {
        if (company.Id == "envoy")
        {
            var state = MaliceState.Get()?.Envoy;
            if (state == null) return 0;
            if (state.Aid != "done" && state.Aid != "declined") return 1;
            if (state.Ch2 != "paid" && state.Ch2 != "bombdone" && state.Ch2 != "maliceover") return 2;
            if (string.IsNullOrEmpty(state.Ch3)) return 3;
            return state.Ch3 == "joined" && !state.Agent ? 4 : 0;
        }
        if (company.Id == "rebel")
        {
            var state = MaliceState.Get()?.Rebel;
            return state == null ? 0 : state.Active > 0 ? state.Active : state.Done < 5 ? state.Done + 1 : 0;
        }

        var character = Engine.Find(company.CharacterId);
        var root = CorpState.Get();
        if (character == null || root == null) return 0;
        if (!root.Characters.TryGetValue(character.Id, out var progress))
            progress = (character.Id == "seline" ? root.Seline : null) ?? new CharacterState();

        string pending = progress.PendingKind;
        if (progress.PendingDay >= 0 && pending != null && pending.StartsWith("ch", System.StringComparison.Ordinal)
            && int.TryParse(pending.Substring(2), out int booked) && booked > 0 && booked <= character.Chapters)
            return booked;

        int main = character.NextMain(progress);
        if (main > 0) return main;
        for (int side = 1; side <= character.Chapters; side++)
            if (character.IsSide(side) && progress.Chapter >= character.SideAfter(side) && !progress.SideDone.Contains(side))
                return side;
        return 0;
    }
}
