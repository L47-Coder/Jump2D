using System.Collections;
using UnityEngine;

// 敌人通用逻辑：待机浮动、受击闪烁、死亡消失并计分
public abstract class EnemyBase : MonoBehaviour
{
    public int MaxHealth = 1;
    public int ScoreValue = 10;
    public float BobAmplitude = 0.06f;
    public float BobSpeed = 2f;
    public float DeathDuration = 0.25f;
    public EnemyBody Body;

    protected SpriteRenderer[] Renderers;
    private Color[] _originalColors;
    private int _health;
    private bool _dead;

    protected virtual void Awake()
    {
        if (Body == null)
            Body = GetComponentInChildren<EnemyBody>();
        Renderers = GetComponentsInChildren<SpriteRenderer>();
        _originalColors = new Color[Renderers.Length];
        for (int i = 0; i < Renderers.Length; i++)
            _originalColors[i] = Renderers[i].color;
        _health = Mathf.Max(1, MaxHealth);
    }

    protected virtual void Start()
    {
        StartIdleBob();
    }

    // 子类可覆盖以对多个部件（如敌人2的头/身体）做异步浮动
    protected virtual void StartIdleBob()
    {
        Tween.PingPongLocalY(this, transform, BobAmplitude, BobSpeed);
    }

    // 子类可生成独立的物理尸体；原敌人仍负责淡出和计分。
    protected virtual void SpawnDeathCorpse()
    {
    }

    public void TakeHit(int damage)
    {
        if (_dead || damage <= 0)
            return;

        _health -= damage;
        StopCoroutine(nameof(HitFlashRoutine));
        StartCoroutine(HitFlashRoutine());

        if (_health <= 0)
            Die();
    }

    private IEnumerator HitFlashRoutine()
    {
        Color flash = new(1f, 0.35f, 0.35f);
        foreach (var r in Renderers)
            r.color = flash;
        yield return new WaitForSeconds(0.08f);
        if (_dead)
            yield break;
        for (int i = 0; i < Renderers.Length; i++)
            Renderers[i].color = _originalColors[i];
    }

    private void Die()
    {
        _dead = true;
        StopAllCoroutines();
        SpawnDeathCorpse();
        foreach (var collider in GetComponentsInChildren<Collider2D>())
            collider.enabled = false;

        GameManager.Instance?.AddScore(ScoreValue);
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        Vector3 startScale = transform.localScale;
        float duration = Mathf.Max(0f, DeathDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            transform.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, t);
            for (int i = 0; i < Renderers.Length; i++)
            {
                var color = _originalColors[i];
                color.a = Mathf.Lerp(_originalColors[i].a, 0f, t);
                Renderers[i].color = color;
            }
            yield return null;
        }

        transform.localScale = Vector3.zero;
        Destroy(gameObject);
    }
}
