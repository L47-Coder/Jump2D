using System.Collections;
using UnityEngine;

// 用扩张环直接把玉米炮弹的真实伤害范围表现出来。
public class CornExplosionEffect : MonoBehaviour
{
    private const int RingSegments = 48;
    private const int BurstRays = 12;
    private static Material _lineMaterial;

    private LineRenderer _ring;
    private LineRenderer _burst;
    private float _radius;
    private float _duration;

    public static void Play(Vector3 position, float radius, float duration)
    {
        var effectObject = new GameObject("CornExplosionEffect");
        effectObject.transform.position = position;
        var effect = effectObject.AddComponent<CornExplosionEffect>();
        effect.Initialize(radius, duration);
    }

    private void Initialize(float radius, float duration)
    {
        _radius = Mathf.Max(0.1f, radius);
        _duration = Mathf.Max(0.05f, duration);
        _ring = CreateLine("RadiusRing", RingSegments, true, 0.14f);
        _burst = CreateLine("BurstLines", BurstRays * 2, false, 0.1f);
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
        line.endWidth = width * 0.7f;
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
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            float visibleRadius = Mathf.Lerp(0.15f, _radius, eased);
            float alpha = 1f - t;
            Color ringColor = new Color(1f, 0.78f, 0.18f, alpha);
            Color burstColor = new Color(1f, 0.95f, 0.5f, alpha);

            UpdateRing(visibleRadius);
            UpdateBurst(visibleRadius, t);
            _ring.startColor = ringColor;
            _ring.endColor = ringColor;
            _burst.startColor = burstColor;
            _burst.endColor = new Color(burstColor.r, burstColor.g, burstColor.b, 0f);
            _ring.startWidth = Mathf.Lerp(0.16f, 0.03f, t);
            _ring.endWidth = _ring.startWidth * 0.7f;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void UpdateRing(float radius)
    {
        for (int i = 0; i < RingSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / RingSegments;
            _ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
    }

    private void UpdateBurst(float radius, float progress)
    {
        float innerRadius = radius * 0.35f;
        float outerRadius = Mathf.Lerp(radius * 0.55f, radius * 1.08f, progress);
        for (int i = 0; i < BurstRays; i++)
        {
            float angle = i * Mathf.PI * 2f / BurstRays;
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
