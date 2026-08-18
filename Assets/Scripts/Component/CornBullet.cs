using System;
using UnityEngine;

// 玉米炮弹：飞行后对范围内所有敌人造成范围伤害
public class CornBullet : MonoBehaviour
{
    public SpriteRenderer SpriteRenderer;
    public Sprite[] FlipbookFrames;
    public float Speed = 8f;
    public float LifeTime = 2.5f;
    public float ExplosionRadius = 2.5f;
    public int Damage = 99;
    public Action<CornBullet> OnRecycle;

    private const float FrameInterval = 0.06f;
    private float _age;
    private float _frameTimer;
    private int _frameIndex;
    private bool _recycled;

    void OnEnable()
    {
        _age = 0f;
        _frameTimer = 0f;
        _frameIndex = 0;
        _recycled = false;
        if (SpriteRenderer != null && FlipbookFrames != null && FlipbookFrames.Length > 0)
            SpriteRenderer.sprite = FlipbookFrames[0];
    }

    void Update()
    {
        transform.Translate(Vector3.right * (Speed * Time.deltaTime), Space.World);
        AnimateFlipbook();

        _age += Time.deltaTime;
        if (_age >= LifeTime)
            Recycle();
    }

    private void AnimateFlipbook()
    {
        if (FlipbookFrames == null || FlipbookFrames.Length == 0 || SpriteRenderer == null)
            return;

        _frameTimer += Time.deltaTime;
        if (_frameTimer < FrameInterval)
            return;

        _frameTimer = 0f;
        _frameIndex = (_frameIndex + 1) % FlipbookFrames.Length;
        SpriteRenderer.sprite = FlipbookFrames[_frameIndex];
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
            Explode();
    }

    private void Explode()
    {
        CameraManager.Instance?.Shake(0.2f, 0.2f);
        var hits = Physics2D.OverlapCircleAll(transform.position, ExplosionRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
                continue;
            var enemy = hit.GetComponentInParent<EnemyBase>();
            enemy?.TakeHit(Damage);
        }
        Recycle();
    }

    private void Recycle()
    {
        if (_recycled)
            return;

        _recycled = true;
        if (OnRecycle != null)
            OnRecycle(this);
        else
            Destroy(gameObject);
    }
}
