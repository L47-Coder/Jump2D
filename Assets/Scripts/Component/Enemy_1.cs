using UnityEngine;

public class Enemy_1 : EnemyBase
{
    public Vector2 CorpseInitialVelocity = new(-0.8f, 0.25f);
    public float CorpseInitialAngularVelocityMin = -35f;
    public float CorpseInitialAngularVelocityMax = 35f;
    public float CorpseGravityScale = 3f;
    public float CorpseBounceFactor = 0.22f;
    public float CorpseGroundFriction = 0.72f;
    public float CorpseLifetime = 4.5f;

    // 根节点固定在生成基准线上，只让身体浮动，避免地面影子跟着飞到空中。
    protected override void StartIdleBob()
    {
        if (Body != null)
            Tween.PingPongLocalY(this, Body.transform, BobAmplitude, BobSpeed);
    }

    protected override void SpawnDeathCorpse()
    {
        var bodyRenderer = Body != null ? Body.GetComponentInChildren<SpriteRenderer>() : null;
        if (bodyRenderer == null)
            return;

        SpawnCorpse(
            "Enemy_1_Corpse",
            bodyRenderer,
            new CorpseLaunchSpec(
                CorpseInitialVelocity,
                Random.Range(
                    Mathf.Min(CorpseInitialAngularVelocityMin, CorpseInitialAngularVelocityMax),
                    Mathf.Max(CorpseInitialAngularVelocityMin, CorpseInitialAngularVelocityMax)),
                CorpseGravityScale,
                CorpseBounceFactor,
                CorpseGroundFriction,
                CorpseLifetime));
    }
}
