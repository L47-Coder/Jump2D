using UnityEngine;

// 沿世界坐标水平飞行的普通子弹，命中敌人造成伤害后销毁
public class Bullet : ProjectileBase
{
    public int Damage = 1;
    public float KnockbackForce = 1.4f;

    protected override void HandleEnemyHit(EnemyBase enemy)
    {
        Vector2 direction = Speed < 0f ? Vector2.left : Vector2.right;
        AudioManager.PlaySfx(SfxId.ProjectileHit);
        enemy.TakeHit(Damage, direction * Mathf.Max(0f, KnockbackForce));
    }
}
