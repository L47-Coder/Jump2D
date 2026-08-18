using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 轻量协程缓动工具，避免引入第三方缓动插件
public static class Tween
{
    private static readonly Dictionary<Transform, Coroutine> ActivePunches = new();

    // 使用目标当前缩放作为 restScale 的便捷重载，适合 UI 文本等没有单独基准缩放的对象。
    public static Coroutine Punch(MonoBehaviour host, Transform target, float scaleMultiplier = 1.2f, float duration = 0.2f)
    {
        if (target == null)
            return null;

        return Punch(host, target, target.localScale, scaleMultiplier, duration);
    }

    // 缩放的“弹一下”反馈：先放大/缩小再回落到 restScale。重复调用会先取消上一次，避免缩放不断叠加
    public static Coroutine Punch(MonoBehaviour host, Transform target, Vector3 restScale, float scaleMultiplier = 1.2f, float duration = 0.2f)
    {
        if (host == null || target == null)
            return null;

        scaleMultiplier = Mathf.Max(0f, scaleMultiplier);
        duration = Mathf.Max(0f, duration);

        if (ActivePunches.TryGetValue(target, out var running) && running != null)
            host.StopCoroutine(running);

        var routine = host.StartCoroutine(PunchRoutine(target, restScale, scaleMultiplier, duration));
        ActivePunches[target] = routine;
        return routine;
    }

    private static IEnumerator PunchRoutine(Transform target, Vector3 restScale, float scaleMultiplier, float duration)
    {
        Vector3 peak = restScale * scaleMultiplier;
        float half = duration * 0.5f;
        if (half <= 0f)
        {
            target.localScale = restScale;
            ActivePunches.Remove(target);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            target.localScale = Vector3.LerpUnclamped(restScale, peak, elapsed / half);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            target.localScale = Vector3.LerpUnclamped(peak, restScale, elapsed / half);
            yield return null;
        }
        target.localScale = restScale;
        ActivePunches.Remove(target);
    }

    // 简单的往复浮动，返回可用于 StopCoroutine 的句柄
    public static Coroutine PingPongLocalY(MonoBehaviour host, Transform target, float amplitude, float speed)
    {
        if (host == null || target == null)
            return null;

        return host.StartCoroutine(PingPongRoutine(target, amplitude, speed));
    }

    private static IEnumerator PingPongRoutine(Transform target, float amplitude, float speed)
    {
        float baseY = target.localPosition.y;
        float seed = UnityEngine.Random.Range(0f, Mathf.PI * 2);
        while (true)
        {
            float offset = Mathf.Sin(Time.time * speed + seed) * amplitude;
            Vector3 pos = target.localPosition;
            pos.y = baseY + offset;
            target.localPosition = pos;
            yield return null;
        }
    }
}
