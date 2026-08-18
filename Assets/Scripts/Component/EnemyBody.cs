using UnityEngine;

// 挂在 BodyRoot 上：碰撞体在这一层，向根节点的 EnemyBase 转发引用（与 PlayerBody 对主角的处理方式一致）
public class EnemyBody : MonoBehaviour
{
    public EnemyBase Enemy;
    public Collider2D Collider;

    private void Awake()
    {
        if (Enemy == null)
            Enemy = GetComponentInParent<EnemyBase>();
        if (Collider == null)
            Collider = GetComponent<Collider2D>();
    }
}
