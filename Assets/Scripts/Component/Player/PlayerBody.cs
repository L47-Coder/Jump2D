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
        else if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.GetComponentInParent<EnemyBase>() != null)
            HandleEnemyContact();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
            HandleEnemyContact();
    }

    private void HandleEnemyContact()
    {
        CameraManager.Instance?.Shake();
        GameManager.Instance?.TriggerGameOver();
    }
}
