using UnityEngine;

public class Enemy_2 : EnemyBase
{
    public Transform HeadRoot;
    public Transform BodySprite;
    public Vector2 HeadCorpseInitialVelocity = new(-1.35f, 1.45f);
    public float HeadCorpseInitialAngularVelocity = -220f;
    public float HeadCorpseGravityScale = 3f;
    public float HeadCorpseBounceFactor = 0.2f;
    public float HeadCorpseGroundFriction = 0.86f;
    public float HeadCorpseLifetime = 5f;
    public Vector2 BodyCorpseInitialVelocity = new(1.05f, 1.05f);
    public float BodyCorpseInitialAngularVelocity = 170f;
    public float BodyCorpseGravityScale = 3f;
    public float BodyCorpseBounceFactor = 0.2f;
    public float BodyCorpseGroundFriction = 0.86f;
    public float BodyCorpseLifetime = 5f;

    // 头部与身体略微异步浮动，看起来更生动
    protected override void StartIdleBob()
    {
        if (HeadRoot != null)
            Tween.PingPongLocalY(this, HeadRoot, BobAmplitude * 1.4f, BobSpeed * 1.15f);
        if (BodySprite != null)
            Tween.PingPongLocalY(this, BodySprite, BobAmplitude, BobSpeed);
    }

    protected override void SpawnDeathCorpse()
    {
        var headRenderer = HeadRoot != null ? HeadRoot.GetComponentInChildren<SpriteRenderer>() : null;
        if (headRenderer != null)
        {
            SpawnCorpse(
                "Enemy_2_Head_Corpse",
                headRenderer,
                new CorpseLaunchSpec(
                    HeadCorpseInitialVelocity,
                    HeadCorpseInitialAngularVelocity,
                    HeadCorpseGravityScale,
                    HeadCorpseBounceFactor,
                    HeadCorpseGroundFriction,
                    HeadCorpseLifetime));
        }

        var bodyRenderer = BodySprite != null ? BodySprite.GetComponent<SpriteRenderer>() : null;
        if (bodyRenderer != null)
        {
            SpawnCorpse(
                "Enemy_2_Body_Corpse",
                bodyRenderer,
                new CorpseLaunchSpec(
                    BodyCorpseInitialVelocity,
                    BodyCorpseInitialAngularVelocity,
                    BodyCorpseGravityScale,
                    BodyCorpseBounceFactor,
                    BodyCorpseGroundFriction,
                    BodyCorpseLifetime));
        }
    }
}
