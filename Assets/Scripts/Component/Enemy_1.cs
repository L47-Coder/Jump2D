using UnityEngine;

public class Enemy_1 : EnemyBase
{
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

        EnemyCorpse.Create(
            "Enemy_1_Corpse",
            bodyRenderer,
            bodyRenderer.transform.position,
            new Vector2(-0.8f, 0.25f),
            Random.Range(-35f, 35f),
            3f,
            0.22f,
            0.72f,
            4.5f);
    }
}
