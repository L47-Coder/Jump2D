using UnityEngine;

// 投射物共用的移动、逐帧动画、寿命和结束逻辑。
public abstract class ProjectileBase : MonoBehaviour
{
    public SpriteRenderer SpriteRenderer;
    public Sprite[] FlipbookFrames;
    public float Speed = 12f;
    public float LifeTime = 3f;
    public float FrameInterval = 0.06f;

    private float _age;
    private float _frameTimer;
    private int _frameIndex;
    private bool _finished;

    protected bool IsFinished => _finished;

    protected virtual void OnEnable()
    {
        _age = 0f;
        _frameTimer = 0f;
        _frameIndex = 0;
        _finished = false;
        if (SpriteRenderer != null && FlipbookFrames != null && FlipbookFrames.Length > 0)
            SpriteRenderer.sprite = FlipbookFrames[0];
    }

    protected virtual void Update()
    {
        transform.Translate(Vector3.right * (Speed * Time.deltaTime), Space.World);
        AnimateFlipbook();

        _age += Time.deltaTime;
        if (_age >= LifeTime)
            Finish();
    }

    private void AnimateFlipbook()
    {
        if (FlipbookFrames == null || FlipbookFrames.Length == 0 || SpriteRenderer == null)
            return;

        _frameTimer += Time.deltaTime;
        if (_frameTimer < Mathf.Max(0.001f, FrameInterval))
            return;

        _frameTimer = 0f;
        _frameIndex = (_frameIndex + 1) % FlipbookFrames.Length;
        SpriteRenderer.sprite = FlipbookFrames[_frameIndex];
    }

    protected void Finish()
    {
        if (_finished)
            return;

        _finished = true;
        Destroy(gameObject);
    }
}
