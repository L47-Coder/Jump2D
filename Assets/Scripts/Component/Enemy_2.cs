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
}
