using System.Collections;
using UnityEngine;

// 玉米爆炸视觉参数，放在玉米子弹预制体上即可直接调节。
[System.Serializable]
public class CornExplosionSettings
{
    public int RingSegments = 48;
    public int BurstRays = 12;
    public float RingStartWidth = 0.14f;
    public float BurstStartWidth = 0.1f;
    public float RingAnimationStartWidth = 0.16f;
    public float RingEndWidth = 0.03f;
    public float StartRadius = 0.15f;
    public float BurstInnerRadius = 0.35f;
    public float BurstOuterRadiusMin = 0.55f;
    public float BurstOuterRadiusMax = 1.08f;
    public float EaseExponent = 2f;
    public float LineEndWidthMultiplier = 0.7f;
    public float BurstEndAlpha = 0f;
    public Color RingColor = new(1f, 0.78f, 0.18f, 1f);
    public Color BurstColor = new(1f, 0.95f, 0.5f, 1f);
}

// 用扩张环直接把玉米炮弹的真实伤害范围表现出来。
public class CornExplosionEffect : MonoBehaviour
{
    private static Material _lineMaterial;

    private LineRenderer _ring;
    private LineRenderer _burst;
    private float _radius;
    private float _duration;
    private CornExplosionSettings _settings;

    public static void Play(Vector3 position, float radius, float duration, CornExplosionSettings settings)
    {
        var effectObject = new GameObject("CornExplosionEffect");
        effectObject.transform.position = position;
        var effect = effectObject.AddComponent<CornExplosionEffect>();
        effect.Initialize(radius, duration, settings);
    }

    private void Initialize(float radius, float duration, CornExplosionSettings settings)
    {
        _radius = Mathf.Max(0.1f, radius);
        _duration = Mathf.Max(0.05f, duration);
        _settings = settings ?? new CornExplosionSettings();
        _ring = CreateLine(
            "RadiusRing",
            Mathf.Max(3, _settings.RingSegments),
            true,
            Mathf.Max(0f, _settings.RingStartWidth));
        _burst = CreateLine(
            "BurstLines",
            Mathf.Max(1, _settings.BurstRays) * 2,
            false,
            Mathf.Max(0f, _settings.BurstStartWidth));
        StartCoroutine(PlayRoutine());
    }

    private LineRenderer CreateLine(string objectName, int positionCount, bool loop, float width)
    {
        var lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(transform, false);
        var line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = loop;
        line.positionCount = positionCount;
        line.material = GetLineMaterial();
        line.textureMode = LineTextureMode.Stretch;
        line.numCapVertices = 2;
        line.startWidth = width;
        line.endWidth = width * Mathf.Max(0f, _settings.LineEndWidthMultiplier);
        line.sortingLayerName = "Player";
        line.sortingOrder = 10;
        return line;
    }

    private IEnumerator PlayRoutine()
    {
        float elapsed = 0f;
        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _duration);
            float eased = 1f - Mathf.Pow(1f - t, Mathf.Max(0.01f, _settings.EaseExponent));
            float visibleRadius = Mathf.Lerp(Mathf.Max(0f, _settings.StartRadius), _radius, eased);
            float alpha = 1f - t;
            Color ringColor = _settings.RingColor;
            ringColor.a *= alpha;
            Color burstColor = _settings.BurstColor;
            burstColor.a *= alpha;
            Color burstEndColor = burstColor;
            burstEndColor.a *= Mathf.Clamp01(_settings.BurstEndAlpha);

            UpdateRing(visibleRadius);
            UpdateBurst(visibleRadius, t);
            _ring.startColor = ringColor;
            _ring.endColor = ringColor;
            _burst.startColor = burstColor;
            _burst.endColor = burstEndColor;
            _ring.startWidth = Mathf.Lerp(
                Mathf.Max(0f, _settings.RingAnimationStartWidth),
                Mathf.Max(0f, _settings.RingEndWidth),
                t);
            _ring.endWidth = _ring.startWidth * Mathf.Max(0f, _settings.LineEndWidthMultiplier);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void UpdateRing(float radius)
    {
        int ringSegments = Mathf.Max(3, _settings.RingSegments);
        for (int i = 0; i < ringSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / ringSegments;
            _ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
    }

    private void UpdateBurst(float radius, float progress)
    {
        float innerRadius = radius * Mathf.Max(0f, _settings.BurstInnerRadius);
        float outerRadius = radius * Mathf.Lerp(
            _settings.BurstOuterRadiusMin,
            _settings.BurstOuterRadiusMax,
            progress);
        int burstRays = Mathf.Max(1, _settings.BurstRays);
        for (int i = 0; i < burstRays; i++)
        {
            float angle = i * Mathf.PI * 2f / burstRays;
            int index = i * 2;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            _burst.SetPosition(index, direction * innerRadius);
            _burst.SetPosition(index + 1, direction * outerRadius);
        }
    }

    private static Material GetLineMaterial()
    {
        if (_lineMaterial != null)
            return _lineMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        _lineMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        return _lineMaterial;
    }
}
