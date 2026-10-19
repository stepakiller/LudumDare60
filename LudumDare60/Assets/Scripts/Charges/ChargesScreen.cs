using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChargesScreen : MonoBehaviour
{
    [SerializeField] CanvasGroup panel;
    [SerializeField] float fadeDuration = 0.2f;

    [Header("Описание")]
    [SerializeField] TMP_Text nameText;
    [SerializeField] string nameSuffix = ":";
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] Image diagramImage;

    [Header("Список")]
    [SerializeField] RectTransform listRoot;
    [SerializeField] Image itemTemplate;
    [SerializeField] float firstGap = 110f;
    [SerializeField] float spacing = 70f;
    [SerializeField] float selectedScale = 1f;
    [SerializeField] float otherScale = 0.5f;
    [SerializeField] float neighbourAlpha = 0.6f;
    [SerializeField] int visibleNeighbours = 2;
    [SerializeField] float moveDuration = 0.2f;
    [SerializeField] Ease moveEase = Ease.OutQuad;

    readonly List<Image> items = new List<Image>();
    IReadOnlyList<ChargeData> charges;
    float direction = -1f;
    Tween fadeTween;

    void Awake()
    {
        panel.alpha = 0f;
        panel.blocksRaycasts = false;
        if (itemTemplate != null) itemTemplate.gameObject.SetActive(false);
    }

    public void Build(IReadOnlyList<ChargeData> list, bool firstAtBottom)
    {
        charges = list;
        direction = firstAtBottom ? 1f : -1f;

        foreach (Image item in items) Destroy(item.gameObject);
        items.Clear();

        for (int i = 0; i < charges.Count; i++)
        {
            Image item = Instantiate(itemTemplate, listRoot);
            item.name = charges[i].chargeName;
            item.sprite = charges[i].icon;
            item.preserveAspect = true;
            item.gameObject.SetActive(true);
            items.Add(item);
        }
    }

    public void Show(int highlightIndex)
    {
        Layout(highlightIndex, instant: true);
        ShowInfo(highlightIndex);

        fadeTween?.Kill();
        fadeTween = panel.DOFade(1f, fadeDuration).SetLink(gameObject);
    }

    public void Hide()
    {
        fadeTween?.Kill();
        fadeTween = panel.DOFade(0f, fadeDuration).SetLink(gameObject);
    }

    public void SetHighlight(int highlightIndex)
    {
        Layout(highlightIndex, instant: false);
        ShowInfo(highlightIndex);
    }

    void ShowInfo(int index)
    {
        ChargeData charge = charges[index];
        if (nameText != null) nameText.text = charge.chargeName + nameSuffix;
        if (descriptionText != null) descriptionText.text = charge.description;
        if (diagramImage != null)
        {
            diagramImage.sprite = charge.diagram;
            diagramImage.enabled = charge.diagram != null;
        }
    }

    void Layout(int highlightIndex, bool instant)
    {
        for (int i = 0; i < items.Count; i++)
        {
            int distance = i - highlightIndex;
            int abs = Mathf.Abs(distance);

            float y = abs == 0 ? 0f : direction * Mathf.Sign(distance) * (firstGap + (abs - 1) * spacing);
            float scale = abs == 0 ? selectedScale : otherScale;
            float alpha = abs == 0 ? 1f : abs <= visibleNeighbours ? neighbourAlpha : 0f;

            RectTransform rect = items[i].rectTransform;
            Image image = items[i];

            rect.DOKill();
            image.DOKill();

            if (instant)
            {
                rect.anchoredPosition = new Vector2(0f, y);
                rect.localScale = Vector3.one * scale;
                Color c = image.color;
                c.a = alpha;
                image.color = c;
                continue;
            }

            rect.DOAnchorPos(new Vector2(0f, y), moveDuration).SetEase(moveEase).SetLink(rect.gameObject);
            rect.DOScale(scale, moveDuration).SetEase(moveEase).SetLink(rect.gameObject);
            image.DOFade(alpha, moveDuration).SetLink(rect.gameObject);
        }
    }
}
