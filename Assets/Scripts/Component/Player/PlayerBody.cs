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
        var enemy = other.GetComponentInParent<EnemyBase>();
        if (enemy == null)
            return;

        if (Player != null && Player.IsFalling)
        {
            enemy.Squash();
            Player.ApplyStompBounce();
            return;
        }

        CameraManager.Instance?.Shake();
        GameManager.Instance?.TriggerGameOver();
    }
}
