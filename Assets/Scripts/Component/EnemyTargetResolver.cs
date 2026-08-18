using UnityEngine;

// 统一把敌人子部件碰撞体解析为所属 EnemyBase。
public static class EnemyTargetResolver
{
    public static bool TryResolve(Collider2D collider, out EnemyBase enemy)
    {
        enemy = null;
        if (collider == null || !collider.CompareTag("Enemy"))
            return false;

        enemy = collider.GetComponentInParent<EnemyBase>();
        return enemy != null;
    }
}
