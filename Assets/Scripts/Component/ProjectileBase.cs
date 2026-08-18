using UnityEngine;

// 投射物共用的移动、逐帧动画和结束逻辑。
public abstract class ProjectileBase : MonoBehaviour
{
    public SpriteRenderer SpriteRenderer;
    public Sprite[] FlipbookFrames;
    public float Speed = 12f;
    public float FrameInterval = 0.06f;

    private float _frameTimer;
    private int _frameIndex;
    private bool _finished;
    private Camera _worldCamera;

    protected bool IsFinished => _finished;

    protected virtual void OnEnable()
    {
        _frameTimer = 0f;
        _frameIndex = 0;
        _finished = false;
        _worldCamera = null;
        if (SpriteRenderer != null && FlipbookFrames != null && FlipbookFrames.Length > 0)
            SpriteRenderer.sprite = FlipbookFrames[0];
    }

    protected virtual void Update()
    {
        transform.Translate(Vector3.right * (Speed * Time.deltaTime), Space.World);
        AnimateFlipbook();

        if (HasReachedScreenRightBoundary())
            Finish();
    }

    private bool HasReachedScreenRightBoundary()
    {
        if (_worldCamera == null)
            _worldCamera = Camera.main;

        if (_worldCamera == null)
            return false;

        Vector3 viewportPosition = _worldCamera.WorldToViewportPoint(transform.position);
        return viewportPosition.z >= 0f && viewportPosition.x >= 1f;
    }

    protected void OnTriggerEnter2D(Collider2D other)
    {
        if (!EnemyTargetResolver.TryResolve(other, out var enemy))
            return;

        HandleEnemyHit(enemy);
        Finish();
    }

    protected abstract void HandleEnemyHit(EnemyBase enemy);

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
