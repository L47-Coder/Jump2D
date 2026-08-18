using System.Collections;
using UnityEngine;

// 敌人通用逻辑：待机浮动、受击闪烁、死亡消失并计分
public abstract class EnemyBase : MonoBehaviour
{
    public int MaxHealth = 1;
    public int ScoreValue = 10;
    public float BobAmplitude = 0.06f;
    public float BobSpeed = 2f;
    public float LifeTime = 12f;
    public Color CorpseColor = Color.white;
    public Color SquashedCorpseColor = new(1f, 0f, 0f, 1f);
    public Color HitFlashColor = new(1f, 0.35f, 0.35f, 1f);
    public float HitFlashDuration = 0.08f;
    public EnemyCorpseSettings CorpseSettings = new EnemyCorpseSettings();
    public EnemyBody Body;

    protected SpriteRenderer[] Renderers;
    private Color[] _originalColors;
    private int _health;
    private bool _dead;
    private bool _wasSquashed;
    private Vector2 _deathImpulse;

    // 子类生成尸块时读取这次命中的冲量，让一击死亡的敌人保留击退效果。
    protected Vector2 DeathImpulse => _deathImpulse;
    protected Color DeathCorpseTint => _wasSquashed ? SquashedCorpseColor : CorpseColor;

    protected internal struct CorpseLaunchSpec
    {
        public Vector2 InitialVelocity;
        public float InitialAngularVelocity;
        public float GravityScale;
        public float BounceFactor;
        public float GroundFriction;
        public float Lifetime;

        public CorpseLaunchSpec(
            Vector2 initialVelocity,
            float initialAngularVelocity,
            float gravityScale,
            float bounceFactor,
            float groundFriction,
            float lifetime)
        {
            InitialVelocity = initialVelocity;
            InitialAngularVelocity = initialAngularVelocity;
            GravityScale = gravityScale;
            BounceFactor = bounceFactor;
            GroundFriction = groundFriction;
            Lifetime = lifetime;
        }
    }

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
        if (LifeTime > 0f)
            StartCoroutine(LifeTimeRoutine());
    }

    // 子类可覆盖以对多个部件（如敌人2的头/身体）做异步浮动
    protected abstract void StartIdleBob();

    // 子类可生成独立的物理尸体；原敌人随后立即销毁并计分。
    protected abstract void SpawnDeathCorpse();

    protected void SpawnCorpse(string corpseName, SpriteRenderer source, CorpseLaunchSpec launch)
    {
        if (source == null)
            return;

        EnemyCorpse.Create(
            corpseName,
            source,
            source.transform.position,
            launch,
            DeathImpulse,
            DeathCorpseTint,
            CorpseSettings);
    }

    public void TakeHit(int damage, Vector2 hitImpulse)
    {
        if (_dead || damage <= 0)
            return;

        _deathImpulse += hitImpulse;
        _health -= damage;
        StopCoroutine(nameof(HitFlashRoutine));
        StartCoroutine(HitFlashRoutine());

        if (_health <= 0)
            Die();
    }

    // 下落中的主角踩中敌人时直接压扁，不走普通受击流程。
    public void Squash()
    {
        if (_dead)
            return;

        _wasSquashed = true;
        Die();
    }

    private IEnumerator LifeTimeRoutine()
    {
        yield return new WaitForSeconds(LifeTime);
        if (_dead)
            yield break;

        FinalizeDespawn(false);
    }

    private IEnumerator HitFlashRoutine()
    {
        foreach (var r in Renderers)
            r.color = HitFlashColor;
        yield return new WaitForSeconds(Mathf.Max(0f, HitFlashDuration));
        if (_dead)
            yield break;
        for (int i = 0; i < Renderers.Length; i++)
            Renderers[i].color = _originalColors[i];
    }

    private void Die()
    {
        if (_dead)
            return;

        FinalizeDespawn(true);
    }

    private void FinalizeDespawn(bool spawnDeathEffects)
    {
        _dead = true;
        StopAllCoroutines();

        if (spawnDeathEffects)
        {
            AudioManager.PlaySfx(SfxId.EnemyDeath);
            SpawnDeathCorpse();
            GameManager.Instance?.AddScore(ScoreValue);
        }

        DisableColliders();
        Destroy(gameObject);
    }

    private void DisableColliders()
    {
        foreach (var collider in GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
    }
}
