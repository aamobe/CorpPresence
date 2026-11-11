using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CorpPresence.UI;

internal sealed class CargoTabRename : MonoBehaviour
{
    internal static void Open(GameItem bay, int page)
    {
        StoreSession session = StoreSession.Capture();
        if (!session.IsCurrent) return;
        var labeler = LabelerBlueUIManager.Instance;
        if (labeler == null || labeler.panel == null || labeler.inputField == null
            || StorePlacement.Destroyed(bay) || !GeneralHelper.IsItemOwned(bay)) return;

        var previous = labeler.panel.GetComponentInChildren<CargoTabRename>(includeInactive: true);
        if (previous != null) Destroy(previous.gameObject);
        labeler.OpenUI(StorageExpansionHelper.GetTabName(bay, page) ?? "", SetName, () => SetName(null),
            StorageExpansionHelper.TAB_NAME_MAX_LENGTH);
        AddSaveButton(labeler);

        void SetName(string name)
        {
            if (session.IsCurrent && !StorePlacement.Destroyed(bay) && GeneralHelper.IsItemOwned(bay))
                StorageExpansionHelper.SetTabName(bay, page, name);
        }
    }

    private static void AddSaveButton(LabelerBlueUIManager labeler)
    {
        var root = new GameObject("CorpPresenceCargoRenameSave", typeof(RectTransform), typeof(Image),
            typeof(Button), typeof(CargoTabRename));
        var rect = (RectTransform)root.transform;
        rect.SetParent(labeler.panel.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 20f);
        rect.sizeDelta = new Vector2(160f, 36f);

        var background = root.GetComponent<Image>();
        if (labeler.inputField.targetGraphic is Image template)
        {
            background.sprite = template.sprite;
            background.type = template.type;
            background.color = template.color;
        }

        var caption = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI));
        var captionRect = (RectTransform)caption.transform;
        captionRect.SetParent(rect, false);
        captionRect.anchorMin = Vector2.zero;
        captionRect.anchorMax = Vector2.one;
        captionRect.offsetMin = captionRect.offsetMax = Vector2.zero;
        var text = caption.GetComponent<TextMeshProUGUI>();
        text.font = labeler.inputField.textComponent.font;
        text.fontSize = 22f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color32(245, 245, 236, 255);
        text.raycastTarget = false;
        text.text = L.K("production.cargoWarehouse.controller.saveExit", "保存退出", "Save and close");

        var button = root.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() =>
        {
            labeler.OnEditEnd(labeler.inputField.text);
            labeler.CloseUI();
        });
    }

    private void OnDisable() => Destroy(gameObject);
}
