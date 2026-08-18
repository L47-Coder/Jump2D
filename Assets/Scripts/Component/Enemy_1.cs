using UnityEngine;

public class Enemy_1 : EnemyBase
{
    // 根节点固定在生成基准线上，只让身体浮动，避免地面影子跟着飞到空中。
    protected override void StartIdleBob()
    {
        if (Body != null)
            Tween.PingPongLocalY(this, Body.transform, BobAmplitude, BobSpeed);
    }
}
