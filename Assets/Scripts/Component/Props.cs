using UnityEngine;

// 场景高处的武器道具：漂浮展示图标，被玩家碰到后赋予限时武器
public class Props : MonoBehaviour
{
    public SpriteRenderer IconRenderer;
    public Transform BubbleRoot;
    public Sprite MachineGunIcon;
    public Sprite CornIcon;
    public float BobAmplitude = 0.15f;
    public float BobSpeed = 1.5f;
    public float BuffDuration = 10f;
    private WeaponType _weaponType = WeaponType.MachineGun;
    private bool _collected;

    private static bool IsPlayingState()
    {
        var manager = GameManager.Instance;
        return manager == null || manager.State == GameState.Playing;
    }

    private void OnEnable()
    {
        _collected = false;
    }

    void Start()
    {
        if (BubbleRoot != null)
            Tween.PingPongLocalY(this, BubbleRoot, BobAmplitude, BobSpeed);
    }

    public void SetWeaponType(WeaponType type)
    {
        _weaponType = type;
        if (IconRenderer != null)
            IconRenderer.sprite = type == WeaponType.Corn ? CornIcon : MachineGunIcon;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_collected || !IsPlayingState())
            return;

        var player = other.GetComponentInParent<Player>();
        if (player == null)
            return;

        _collected = true;
        player.ApplyWeaponBuff(_weaponType, BuffDuration);
        AudioManager.PlaySfx(SfxId.WeaponPickup);
        Destroy(gameObject);
    }
}
