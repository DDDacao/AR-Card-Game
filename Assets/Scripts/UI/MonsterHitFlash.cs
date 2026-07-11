using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 受击时在怪物网格上闪一下弱点色（淡淡叠色后恢复）。
/// </summary>
public class MonsterHitFlash : MonoBehaviour
{
    [Range(0.1f, 0.8f)]
    public float intensity = 0.38f;
    public float duration = 0.38f;

    private struct Slot
    {
        public Renderer renderer;
        public int materialIndex;
        public Color originalColor;
        public int colorId;
        public bool useBaseColor;
    }

    private readonly List<Slot> slots = new List<Slot>();
    private Coroutine routine;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public static void Play(CharacterStats target, WeaknessType weakness, float intensity = 0.38f, float duration = 0.38f)
    {
        if (target == null || weakness == WeaknessType.None) return;
        var flash = target.GetComponent<MonsterHitFlash>();
        if (flash == null)
            flash = target.gameObject.AddComponent<MonsterHitFlash>();
        flash.intensity = intensity;
        flash.duration = duration;
        flash.Play(ResolveColor(weakness));
    }

    public static Color ResolveColor(WeaknessType type)
    {
        switch (type)
        {
            case WeaknessType.RedAttack:
                return new Color(1f, 0.22f, 0.18f, 1f);
            case WeaknessType.YellowArmor:
                return new Color(1f, 0.86f, 0.22f, 1f);
            case WeaknessType.PurpleSeal:
                return new Color(0.72f, 0.32f, 1f, 1f);
            default:
                return new Color(1f, 1f, 1f, 1f);
        }
    }

    public void Play(Color flashColor)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            RestoreImmediate();
        }
        CacheSlots();
        if (slots.Count == 0) return;
        routine = StartCoroutine(FlashRoutine(flashColor));
    }

    private void CacheSlots()
    {
        slots.Clear();
        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            if (r is ParticleSystemRenderer) continue;
            if (r.gameObject.name.Contains("WeaknessMarker")) continue;
            // 跳过 UI
            if (r.GetComponentInParent<Canvas>() != null) continue;

            var mats = r.materials; // instance materials for safe tint
            if (mats == null) continue;
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m];
                if (mat == null) continue;

                bool useBase = mat.HasProperty(BaseColorId);
                int id = useBase ? BaseColorId : (mat.HasProperty(ColorId) ? ColorId : -1);
                if (id < 0) continue;

                slots.Add(new Slot
                {
                    renderer = r,
                    materialIndex = m,
                    originalColor = mat.GetColor(id),
                    colorId = id,
                    useBaseColor = useBase
                });
            }
        }
    }

    private IEnumerator FlashRoutine(Color flashColor)
    {
        // 淡入叠色
        float half = duration * 0.35f;
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / half);
            ApplyBlend(flashColor, a * intensity);
            yield return null;
        }

        ApplyBlend(flashColor, intensity);

        // 淡出恢复
        float outDur = Mathf.Max(0.05f, duration - half);
        t = 0f;
        while (t < outDur)
        {
            t += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(t / outDur);
            ApplyBlend(flashColor, a * intensity);
            yield return null;
        }

        RestoreImmediate();
        routine = null;
    }

    private void ApplyBlend(Color flashColor, float amount)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s.renderer == null) continue;
            var mats = s.renderer.materials;
            if (s.materialIndex < 0 || s.materialIndex >= mats.Length) continue;
            var mat = mats[s.materialIndex];
            if (mat == null) continue;
            Color c = Color.Lerp(s.originalColor, flashColor, amount);
            c.a = s.originalColor.a;
            mat.SetColor(s.colorId, c);
        }
    }

    private void RestoreImmediate()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s.renderer == null) continue;
            var mats = s.renderer.materials;
            if (s.materialIndex < 0 || s.materialIndex >= mats.Length) continue;
            var mat = mats[s.materialIndex];
            if (mat == null) continue;
            mat.SetColor(s.colorId, s.originalColor);
        }
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        RestoreImmediate();
    }
}
