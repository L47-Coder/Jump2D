using System.Collections.Generic;
using UnityEngine;

// 玉米炮弹：飞行后对范围内所有敌人造成范围伤害
public class CornBullet : ProjectileBase
{
    public float ExplosionRadius = 4f;
    public float ExplosionEffectDuration = 0.24f;
    public int Damage = 99;
    public float ExplosionKnockbackForce = 2.5f;
    public float ExplosionShakeDuration = 0.08f;
    public float ExplosionShakeMagnitude = 0.04f;
    public CornExplosionSettings ExplosionSettings = new CornExplosionSettings();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (EnemyTargetResolver.TryResolve(other, out _))
            Explode();
    }

    private void Explode()
    {
        if (IsFinished)
            return;

        CornExplosionEffect.Play(transform.position, ExplosionRadius, ExplosionEffectDuration, ExplosionSettings);
        CameraManager.Instance?.Shake(ExplosionShakeDuration, ExplosionShakeMagnitude);
        var hits = Physics2D.OverlapCircleAll(transform.position, ExplosionRadius);
        var hitEnemies = new HashSet<EnemyBase>();
        foreach (var hit in hits)
        {
            if (!EnemyTargetResolver.TryResolve(hit, out var enemy) || !hitEnemies.Add(enemy))
                continue;

            Vector2 direction = (Vector2)(enemy.transform.position - transform.position);
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.right;
            else
                direction.Normalize();

            enemy.TakeHit(Damage, direction * Mathf.Max(0f, ExplosionKnockbackForce));
        }
        Finish();
    }
}
