using System.Collections;
using UnityEngine;

// 独立的物理尸体：不带 Enemy 标签，不会继续触发伤害或被子弹锁定。
public class EnemyCorpse : MonoBehaviour
{
    private Rigidbody2D _rigidbody;
    private float _bounceFactor;
    private float _groundFriction;
    private float _lifetime;

    public static EnemyCorpse Create(
        string corpseName,
        SpriteRenderer source,
        Vector3 position,
        Vector2 initialVelocity,
        float initialAngularVelocity,
        float gravityScale,
        float bounceFactor,
        float groundFriction,
        float lifetime)
    {
        if (source == null || source.sprite == null)
            return null;

        var root = new GameObject(corpseName);
        root.transform.position = position;
        root.transform.localScale = source.transform.lossyScale;

        var rigidbody = root.AddComponent<Rigidbody2D>();
        rigidbody.gravityScale = Mathf.Max(0f, gravityScale);
        rigidbody.mass = 0.65f;
        rigidbody.drag = 0.08f;
        rigidbody.angularDrag = 0.25f;
        rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        rigidbody.velocity = initialVelocity;
        rigidbody.angularVelocity = initialAngularVelocity;

        var collider = root.AddComponent<BoxCollider2D>();
        Vector2 spriteSize = source.sprite.bounds.size;
        collider.size = new Vector2(
            Mathf.Max(0.24f, spriteSize.x * 0.68f),
            Mathf.Max(0.24f, spriteSize.y * 0.68f));

        var spriteObject = new GameObject("Sprite");
        spriteObject.transform.SetParent(root.transform, false);
        var renderer = spriteObject.AddComponent<SpriteRenderer>();
        renderer.sprite = source.sprite;
        renderer.color = source.color;
        renderer.flipX = source.flipX;
        renderer.flipY = source.flipY;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder;
        renderer.sharedMaterial = source.sharedMaterial;

        var corpse = root.AddComponent<EnemyCorpse>();
        corpse._rigidbody = rigidbody;
        corpse._bounceFactor = Mathf.Clamp01(bounceFactor);
        corpse._groundFriction = Mathf.Clamp01(groundFriction);
        corpse._lifetime = Mathf.Max(0.1f, lifetime);
        corpse.StartCoroutine(corpse.DestroyAfterLifetime());
        return corpse;
    }

    private IEnumerator DestroyAfterLifetime()
    {
        yield return new WaitForSeconds(_lifetime);
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("BackGround") || collision.contactCount == 0)
            return;

        ContactPoint2D contact = collision.GetContact(0);
        if (contact.normal.y <= 0.25f)
            return;

        float impactSpeed = Mathf.Abs(collision.relativeVelocity.y);
        Vector2 velocity = _rigidbody.velocity;
        velocity.x *= _groundFriction;
        velocity.y = impactSpeed > 0.25f ? impactSpeed * _bounceFactor : 0f;
        _rigidbody.velocity = velocity;
        _rigidbody.angularVelocity *= 0.62f;
    }
}
