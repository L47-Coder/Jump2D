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
        var player = other.GetComponentInParent<Player>();
        if (player == null)
            return;

        player.ApplyWeaponBuff(_weaponType, BuffDuration);
        AudioManager.PlaySfx(SfxId.WeaponPickup);
        Destroy(gameObject);
    }
}
