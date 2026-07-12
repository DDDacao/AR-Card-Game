using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗胜利美术叠层（底层墨圈 + 上层「战斗胜利」字体）。
/// 与 HUD 一样：在 Hierarchy 里拖各个 *_可调 节点即可微调位置/尺寸；
/// Inspector 上的 scale / offset 也可在编辑器里快速改（OnValidate 同步）。
/// </summary>
[ExecuteAlways]
public sealed class BattleResultVictorySkin : MonoBehaviour
{
    [Header("根（整体位移/缩放请调这个）")]
    public RectTransform skinRoot;

    [Header("底层（墨圈特效）")]
    public RectTransform bottomLayer;
    public Image bottomImage;
    [Tooltip("底层显示尺寸（宽×高）")]
    public Vector2 bottomSize = new Vector2(720f, 1020f);
    public Vector2 bottomAnchoredPos = new Vector2(0f, 40f);

    [Header("上层字体（须在底层之上）")]
    public RectTransform titleLayer;
    public Image titleImage;
    [Tooltip("标题字显示尺寸（宽×高）")]
    public Vector2 titleSize = new Vector2(640f, 900f);
    public Vector2 titleAnchoredPos = new Vector2(0f, 50f);

    [Header("副标题（请扫描下一关…）")]
    public RectTransform detailRoot;
    public TextMeshProUGUI detailText;
    public Vector2 detailSize = new Vector2(720f, 100f);
    public Vector2 detailAnchoredPos = new Vector2(0f, -280f);
    public float detailFontSize = 28f;

    [Header("整体缩放（乘在 size 上，方便等比缩放）")]
    [Range(0.3f, 2.5f)]
    public float overallScale = 1f;

    [Header("美术贴图（可空，运行时从 Resources 补）")]
    public Sprite bottomSprite;
    public Sprite titleSprite;

    private void OnEnable()
    {
        EnsureSprites();
        ApplyLayout();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyLayout();
    }
#endif

    public void EnsureSprites()
    {
        if (bottomSprite == null)
            bottomSprite = Resources.Load<Sprite>("BattleVictorySkin/victory_bottom");
        if (titleSprite == null)
            titleSprite = Resources.Load<Sprite>("BattleVictorySkin/victory_title");

        if (bottomImage != null && bottomSprite != null)
        {
            bottomImage.sprite = bottomSprite;
            bottomImage.preserveAspect = true;
            bottomImage.raycastTarget = false;
        }

        if (titleImage != null && titleSprite != null)
        {
            titleImage.sprite = titleSprite;
            titleImage.preserveAspect = true;
            titleImage.raycastTarget = false;
        }
    }

    /// <summary>把 Inspector 参数写回各可调 Rect（场景里直接拖节点也可，之后再改参数会覆盖）。</summary>
    public void ApplyLayout()
    {
        EnsureSprites();

        float s = Mathf.Max(0.05f, overallScale);

        if (bottomLayer != null)
        {
            bottomLayer.anchorMin = bottomLayer.anchorMax = new Vector2(0.5f, 0.5f);
            bottomLayer.pivot = new Vector2(0.5f, 0.5f);
            bottomLayer.anchoredPosition = bottomAnchoredPos * s;
            bottomLayer.sizeDelta = bottomSize * s;
            bottomLayer.localScale = Vector3.one;
            // 底层在最下
            bottomLayer.SetAsFirstSibling();
        }

        if (titleLayer != null)
        {
            titleLayer.anchorMin = titleLayer.anchorMax = new Vector2(0.5f, 0.5f);
            titleLayer.pivot = new Vector2(0.5f, 0.5f);
            titleLayer.anchoredPosition = titleAnchoredPos * s;
            titleLayer.sizeDelta = titleSize * s;
            titleLayer.localScale = Vector3.one;
            // 字体永远在底层之上
            if (bottomLayer != null)
                titleLayer.SetSiblingIndex(bottomLayer.GetSiblingIndex() + 1);
            else
                titleLayer.SetAsLastSibling();
        }

        if (detailRoot != null)
        {
            detailRoot.anchorMin = detailRoot.anchorMax = new Vector2(0.5f, 0.5f);
            detailRoot.pivot = new Vector2(0.5f, 0.5f);
            detailRoot.anchoredPosition = detailAnchoredPos * s;
            detailRoot.sizeDelta = detailSize * s;
            // 副标题在最前（字上）
            detailRoot.SetAsLastSibling();
        }

        if (detailText != null)
        {
            detailText.fontSize = detailFontSize * s;
            detailText.alignment = TextAlignmentOptions.Center;
            detailText.enableWordWrapping = true;
            TmpChineseFontUtil.BindChineseFont(detailText);
        }
    }

    public void SetDetail(string message)
    {
        if (detailText == null) return;
        string text = message ?? "";
        detailText.text = text;
        TmpChineseFontUtil.Apply(detailText, text);
        bool show = !string.IsNullOrWhiteSpace(text);
        if (detailRoot != null)
            detailRoot.gameObject.SetActive(show);
        else
            detailText.gameObject.SetActive(show);
    }

    public void ShowSkin(bool show)
    {
        if (skinRoot != null)
            skinRoot.gameObject.SetActive(show);
        else
            gameObject.SetActive(show);

        if (show)
            ApplyLayout();
    }
}
