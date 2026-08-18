using System.Collections;
using UnityEngine;

// 尸块共用的物理与滚动参数，挂在敌人预制体上即可统一调整。
[System.Serializable]
public class EnemyCorpseSettings
{
    public float Mass = 0.65f;
    public float LinearDrag = 0.08f;
    public float AngularDrag = 0.25f;
    public float RandomImpulseMin = 0.35f;
    public float RandomImpulseMax = 1.05f;
    public float CircleRadiusScale = 0.34f;
    public float MinimumCircleRadius = 0.12f;
    public float BounceImpactThreshold = 0.25f;
    public float GroundAngularDamping = 0.62f;
}

// 独立的物理尸体：不带 Enemy 标签，不会继续触发伤害或被子弹锁定。
public class EnemyCorpse : MonoBehaviour
{
    private const string CorpseLayerName = "Corpse";
    private const string GroundLayerName = "BackGround";
    private static bool _collisionLayerConfigured;
    private Rigidbody2D _rigidbody;
    private float _bounceFactor;
    private float _groundFriction;
    private float _bounceImpactThreshold;
    private float _groundAngularDamping;
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
        float lifetime,
        Vector2 hitImpulse,
        Color corpseTint,
        EnemyCorpseSettings settings)
    {
        if (source == null || source.sprite == null)
            return null;

        if (settings == null)
            settings = new EnemyCorpseSettings();

        var root = new GameObject(corpseName);
        ConfigureCollisionLayer(root);
        root.transform.position = position;
        root.transform.localScale = source.transform.lossyScale;

        var rigidbody = root.AddComponent<Rigidbody2D>();
        rigidbody.gravityScale = Mathf.Max(0f, gravityScale);
        rigidbody.mass = Mathf.Max(0.01f, settings.Mass);
        rigidbody.drag = Mathf.Max(0f, settings.LinearDrag);
        rigidbody.angularDrag = Mathf.Max(0f, settings.AngularDrag);
        rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        rigidbody.velocity = initialVelocity;
        rigidbody.angularVelocity = initialAngularVelocity;
        rigidbody.AddForce(hitImpulse, ForceMode2D.Impulse);

        Vector2 randomDirection = Random.insideUnitCircle;
        if (randomDirection.sqrMagnitude < 0.0001f)
            randomDirection = Vector2.up;
        else
            randomDirection.Normalize();
        float randomImpulseMin = Mathf.Max(0f, settings.RandomImpulseMin);
        float randomImpulseMax = Mathf.Max(randomImpulseMin, settings.RandomImpulseMax);
        rigidbody.AddForce(
            randomDirection * Random.Range(randomImpulseMin, randomImpulseMax),
            ForceMode2D.Impulse);

        var collider = root.AddComponent<CircleCollider2D>();
        Vector2 spriteSize = source.sprite.bounds.size;
        collider.radius = Mathf.Max(
            Mathf.Max(0.01f, settings.MinimumCircleRadius),
            Mathf.Max(0f, Mathf.Max(spriteSize.x, spriteSize.y) * settings.CircleRadiusScale));

        var spriteObject = new GameObject("Sprite");
        spriteObject.transform.SetParent(root.transform, false);
        var renderer = spriteObject.AddComponent<SpriteRenderer>();
        renderer.sprite = source.sprite;
        renderer.color = source.color * corpseTint;
        renderer.flipX = source.flipX;
        renderer.flipY = source.flipY;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder;
        renderer.sharedMaterial = source.sharedMaterial;

        var corpse = root.AddComponent<EnemyCorpse>();
        corpse._rigidbody = rigidbody;
        corpse._bounceFactor = Mathf.Clamp01(bounceFactor);
        corpse._groundFriction = Mathf.Clamp01(groundFriction);
        corpse._bounceImpactThreshold = Mathf.Max(0f, settings.BounceImpactThreshold);
        corpse._groundAngularDamping = Mathf.Clamp01(settings.GroundAngularDamping);
        corpse._lifetime = Mathf.Max(0.1f, lifetime);
        corpse.StartCoroutine(corpse.DestroyAfterLifetime());
        return corpse;
    }

    private static void ConfigureCollisionLayer(GameObject root)
    {
        int corpseLayer = LayerMask.NameToLayer(CorpseLayerName);
        int groundLayer = LayerMask.NameToLayer(GroundLayerName);
        if (corpseLayer < 0 || groundLayer < 0)
        {
            Debug.LogWarning("EnemyCorpse requires Corpse and BackGround layers.");
            return;
        }

        root.layer = corpseLayer;
        if (_collisionLayerConfigured)
            return;

        for (int layer = 0; layer < 32; layer++)
        {
            if (layer != groundLayer)
                Physics2D.IgnoreLayerCollision(corpseLayer, layer, true);
        }

        Physics2D.IgnoreLayerCollision(corpseLayer, groundLayer, false);
        _collisionLayerConfigured = true;
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
        velocity.y = impactSpeed > _bounceImpactThreshold ? impactSpeed * _bounceFactor : 0f;
        _rigidbody.velocity = velocity;
        _rigidbody.angularVelocity *= _groundAngularDamping;
    }
}
