using UnityEngine;

// 沿世界坐标水平飞行的普通子弹，命中敌人造成伤害后销毁
public class Bullet : MonoBehaviour
{
    public SpriteRenderer SpriteRenderer;
    public Sprite[] FlipbookFrames;
    public float Speed = 12f;
    public float LifeTime = 3f;
    public int Damage = 1;
    public float KnockbackForce = 1.4f;
    public float FrameInterval = 0.06f;
    private float _age;
    private float _frameTimer;
    private int _frameIndex;
    private bool _finished;

    void OnEnable()
    {
        _age = 0f;
        _frameTimer = 0f;
        _frameIndex = 0;
        _finished = false;
        if (SpriteRenderer != null && FlipbookFrames != null && FlipbookFrames.Length > 0)
            SpriteRenderer.sprite = FlipbookFrames[0];
    }

    void Update()
    {
        transform.Translate(Vector3.right * (Speed * Time.deltaTime), Space.World);
        AnimateFlipbook();

        _age += Time.deltaTime;
        if (_age >= LifeTime)
            Finish();
    }

    private void AnimateFlipbook()
    {
        if (FlipbookFrames == null || FlipbookFrames.Length == 0 || SpriteRenderer == null)
            return;

        _frameTimer += Time.deltaTime;
        if (_frameTimer < Mathf.Max(0.001f, FrameInterval))
            return;

        _frameTimer = 0f;
        _frameIndex = (_frameIndex + 1) % FlipbookFrames.Length;
        SpriteRenderer.sprite = FlipbookFrames[_frameIndex];
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        var enemy = other.GetComponentInParent<EnemyBase>();
        Vector2 direction = Speed < 0f ? Vector2.left : Vector2.right;
        enemy?.TakeHit(Damage, direction * Mathf.Max(0f, KnockbackForce));
        Finish();
    }

    private void Finish()
    {
        if (_finished)
            return;

        _finished = true;
        Destroy(gameObject);
    }
}
