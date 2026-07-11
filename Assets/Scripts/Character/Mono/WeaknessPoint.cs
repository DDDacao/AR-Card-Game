using UnityEngine;

/// <summary>
/// 挂在敌人身上的弱点点（带 Collider，可被攻击射线命中）
/// </summary>
[RequireComponent(typeof(Collider))]
public class WeaknessPoint : MonoBehaviour
{
    [Header("弱点类型")]
    public WeaknessType weaknessType = WeaknessType.RedAttack;

    [Header("显示")]
    public bool showMarker = true;
    public Color markerColor = new Color(1f, 0.2f, 0.2f, 0.75f);
    public float markerScale = 0.35f;

    [Header("所属敌人（可空，自动向上找）")]
    public CharacterStats owner;

    private GameObject markerInstance;

    private void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<CharacterStats>();

        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = false; // 需要被 Raycast 打到
    }

    private void Start()
    {
        ApplyColor();
        if (showMarker)
            EnsureMarker();
        RefreshMarkerColor();
    }

    private void OnEnable()
    {
        ApplyColor();
        if (showMarker && markerInstance == null)
            EnsureMarker();
        if (markerInstance != null)
            markerInstance.SetActive(true);
        RefreshMarkerColor();
    }

    private void RefreshMarkerColor()
    {
        if (markerInstance == null) return;
        var r = markerInstance.GetComponent<Renderer>();
        if (r == null || r.sharedMaterial == null) return;
        var mat = r.material;
        mat.color = markerColor;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", markerColor);
    }

    public CharacterStats GetOwner()
    {
        if (owner == null)
            owner = GetComponentInParent<CharacterStats>();
        return owner;
    }

    public void SetActiveWeakness(bool active)
    {
        gameObject.SetActive(active);
        if (markerInstance != null)
            markerInstance.SetActive(active);
    }

    private void EnsureMarker()
    {
        if (markerInstance != null) return;

        var existing = transform.Find("WeaknessMarker");
        if (existing != null)
        {
            markerInstance = existing.gameObject;
            return;
        }

        // 半透明球体标记弱点位置
        markerInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        markerInstance.name = "WeaknessMarker";
        markerInstance.transform.SetParent(transform, false);
        markerInstance.transform.localPosition = Vector3.zero;
        markerInstance.transform.localScale = Vector3.one * markerScale;

        // 去掉碰撞，只作显示
        var mc = markerInstance.GetComponent<Collider>();
        if (mc != null) Destroy(mc);

        var r = markerInstance.GetComponent<Renderer>();
        if (r != null)
        {
            // URP Lit 简单半透明
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = markerColor;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", markerColor);
                // 尽量半透明
                if (mat.HasProperty("_Surface"))
                    mat.SetFloat("_Surface", 1f);
                r.sharedMaterial = mat;
            }
        }
    }

    private void ApplyColor()
    {
        switch (weaknessType)
        {
            case WeaknessType.RedAttack:
                markerColor = new Color(1f, 0.15f, 0.15f, 0.8f);
                break;
            case WeaknessType.YellowArmor:
                markerColor = new Color(1f, 0.85f, 0.15f, 0.8f);
                break;
            case WeaknessType.PurpleSeal:
                markerColor = new Color(0.7f, 0.25f, 1f, 0.8f);
                break;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Color c = markerColor;
        c.a = 0.5f;
        Gizmos.color = c;
        Gizmos.DrawSphere(transform.position, markerScale * 0.5f);
        Gizmos.DrawWireSphere(transform.position, markerScale * 0.5f);
    }
#endif
}
