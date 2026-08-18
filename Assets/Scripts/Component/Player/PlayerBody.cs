using UnityEngine;

public class PlayerBody : MonoBehaviour
{
    public Player Player;

    private void Awake()
    {
        if (Player == null)
            Player = GetComponentInParent<Player>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("BackGround"))
        {
            if (collision.contactCount == 0 || collision.GetContact(0).normal.y > 0.25f)
                Player?.GroundContact();
        }
        else
            HandleEnemyContact(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleEnemyContact(other);
    }

    private void HandleEnemyContact(Collider2D other)
    {
        if (!EnemyTargetResolver.TryResolve(other, out var enemy))
            return;

        if (Player != null && Player.IsFalling)
        {
            enemy.Squash();
            Player.ApplyStompBounce();
            return;
        }

        // 踩中敌人后短暂无敌，避免同一帧或紧接着碰到其它敌人而直接死亡。
        if (Player != null && Player.IsStompProtected)
            return;

        if (GameManager.Instance != null && GameManager.Instance.State == GameState.GameOver)
            return;

        CameraManager.Instance?.Shake();
        AudioManager.PlaySfx(SfxId.PlayerHurt);
        GameManager.Instance?.TriggerGameOver();
    }
}
