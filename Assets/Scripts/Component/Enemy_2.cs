using UnityEngine;

public class Enemy_2 : EnemyBase
{
    public Transform HeadRoot;
    public Transform BodySprite;

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
            EnemyCorpse.Create(
                "Enemy_2_Head_Corpse",
                headRenderer,
                headRenderer.transform.position,
                new Vector2(-1.35f, 1.45f),
                -220f,
                3f,
                0.2f,
                0.86f,
                5f,
                DeathImpulse);
        }

        var bodyRenderer = BodySprite != null ? BodySprite.GetComponent<SpriteRenderer>() : null;
        if (bodyRenderer != null)
        {
            EnemyCorpse.Create(
                "Enemy_2_Body_Corpse",
                bodyRenderer,
                bodyRenderer.transform.position,
                new Vector2(1.05f, 1.05f),
                170f,
                3f,
                0.2f,
                0.86f,
                5f,
                DeathImpulse);
        }
    }
}
