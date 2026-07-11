using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 世界空间伤害跳字（面向主相机）。
/// </summary>
public class DamagePopup : MonoBehaviour
{
    [Header("外观")]
    public float normalFontSize = 5.5f;
    public float empoweredFontSize = 8.5f;
    public Color normalColor = new Color(1f, 0.38f, 0.32f, 1f);
    public Color empoweredColor = new Color(1f, 0.88f, 0.28f, 1f);
    public float heightOffset = 1.55f;
    public float normalRise = 0.65f;
    public float empoweredRise = 0.95f;
    public float duration = 0.85f;

    private TextMeshPro label;
    private Camera cam;

    /// <summary>在目标附近弹出伤害数字。empowered=QTE 成功等强化表现。</summary>
    public static void Show(Transform target, int amount, bool empowered = false)
    {
        if (target == null || amount <= 0) return;
        Vector3 pos = target.position + Vector3.up * 1.55f;
        // 略微随机，避免连击重叠
        pos += new Vector3(Random.Range(-0.12f, 0.12f), Random.Range(0f, 0.08f), Random.Range(-0.08f, 0.08f));
        ShowAt(pos, amount, empowered);
    }

    public static void ShowAt(Vector3 worldPosition, int amount, bool empowered = false)
    {
        if (amount <= 0) return;

        var go = new GameObject(empowered ? "DamagePopup_QTE" : "DamagePopup");
        go.transform.position = worldPosition;

        var popup = go.AddComponent<DamagePopup>();
        popup.Play(amount, empowered);
    }

    private void Play(int amount, bool empowered)
    {
        cam = Camera.main;
        label = gameObject.AddComponent<TextMeshPro>();
        label.text = amount.ToString();
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.fontSize = empowered ? empoweredFontSize : normalFontSize;
        label.color = empowered ? empoweredColor : normalColor;
        label.outlineWidth = 0.22f;
        label.outlineColor = new Color(0.05f, 0.03f, 0.02f, 0.9f);

        var rt = label.rectTransform;
        rt.sizeDelta = new Vector2(3.5f, 1.2f);

        // 起始略小，弹出
        transform.localScale = Vector3.one * (empowered ? 0.35f : 0.45f);
        float peakScale = empowered ? 1.25f : 1f;
        float rise = empowered ? empoweredRise : normalRise;
        float life = empowered ? duration + 0.12f : duration;

        FaceCamera();

        var seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        seq.Append(transform.DOScale(peakScale, 0.14f).SetEase(Ease.OutBack));
        if (empowered)
            seq.Append(transform.DOScale(1.05f, 0.08f).SetEase(Ease.OutQuad));

        seq.Join(transform.DOMoveY(transform.position.y + rise, life).SetEase(Ease.OutCubic));

        Color start = label.color;
        Color end = start;
        end.a = 0f;
        seq.Insert(life * 0.35f, DOTween.To(() => label.color, c => label.color = c, end, life * 0.65f));

        seq.OnComplete(() =>
        {
            if (gameObject != null)
                Destroy(gameObject);
        });
    }

    private void LateUpdate()
    {
        FaceCamera();
    }

    private void FaceCamera()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
