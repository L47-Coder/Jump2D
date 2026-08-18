using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class Player : MonoBehaviour
{
    public GameObject BodyRoot;
    public Rigidbody2D Rigidbody2D;
    [FormerlySerializedAs("ShootSpeed")]
    public float PeaFireRate = 2f;
    public GameObject BulletPrefab;
    public GameObject CornBulletPrefab;
    [FormerlySerializedAs("MachineGunFireRateMultiplier")]
    public float MachineGunFireRate = 6f;
    [FormerlySerializedAs("CornFireInterval")]
    public float CornFireRate = 2f;
    public float FollowSpeed = 5f;
    public float JumpImpulse = 7f;
    public int MaxJumpCount = 2;
    public float RisingVelocityThreshold = 3f;
    public float RisingGravityScale = 1f;
    public float FallingGravityScale = 5f;
    public float FallingVelocityThreshold = -0.1f;
    public float StompBounceImpulse = 4f;
    public float StompInvincibilityDuration = 0.1f;
    public Transform Shadow;
    public SpriteRenderer MouthSprite;
    public Sprite DefaultMouthSprite;
    public Sprite MachineGunMouthSprite;
    public Sprite CornMouthSprite;
    public float IdleHopHeight = 0.1f;
    public float IdleHopDuration = 0.38f;
    public float MouthKickDuration = 0.1f;
    public float MouthKickScale = 1.2f;
    public float MouthKickVerticalScale = 0.86f;
    public float MouthKickForward = 0.06f;
    public float MinimumIdleHopDuration = 0.05f;
    public float MinimumMouthKickDuration = 0.01f;
    private bool _isvalid = false;
    private GameObject _targetPosObj;
    private int _hasJumpCount;
    private bool _isGrounded = true;
    private float _stompInvincibilityUntil;
    private bool _wasDescending;
    private float _shootTimer;
    private float _idleHopTime;
    private float _idleVisualOffset;
    private float _mouthKickOffsetX;
    private WeaponType _weaponType = WeaponType.Pea;
    private Coroutine _weaponRoutine;
    private Coroutine _mouthRoutine;
    private Transform _bodySprite;
    private Vector3 _bodyRestPosition;
    private Vector3 _shadowRestPosition;
    private Vector3 _mouthRestScale;
    private Vector3 _mouthRestPosition;
    private int NormalizedMaxJumpCount => Mathf.Max(0, MaxJumpCount);
    public bool IsFalling => _isvalid && !_isGrounded && Rigidbody2D != null &&
        (_wasDescending || Rigidbody2D.velocity.y < FallingVelocityThreshold);
    public bool IsStompProtected => Time.time < _stompInvincibilityUntil;

    public void ApplyStompBounce()
    {
        if (!_isvalid || Rigidbody2D == null)
            return;

        Rigidbody2D.velocity = Vector2.zero;
        _isGrounded = false;
        _stompInvincibilityUntil = Time.time + Mathf.Max(0f, StompInvincibilityDuration);
        _wasDescending = false;
        _idleHopTime = 0f;
        SetIdleVisualOffset(0f);
        AlignBodyHorizontally();
        if (StompBounceImpulse > 0f)
            Rigidbody2D.AddForce(Vector2.up * StompBounceImpulse, ForceMode2D.Impulse);
    }

    void Awake()
    {
        if (BodyRoot == null)
            BodyRoot = transform.Find("BodyRoot")?.gameObject;

        if (Shadow == null)
            Shadow = transform.Find("Shadow");

        if (Rigidbody2D == null)
            Rigidbody2D = BodyRoot != null
                ? BodyRoot.GetComponent<Rigidbody2D>()
                : GetComponentInChildren<Rigidbody2D>();

        if (MouthSprite == null)
        {
            var mouth = transform.Find("MouseSprite");
            if (mouth != null)
                MouthSprite = mouth.GetComponent<SpriteRenderer>();
        }

        if (BodyRoot == null || Rigidbody2D == null)
        {
            Debug.LogError("Player requires BodyRoot and Rigidbody2D references.", this);
            return;
        }

        _hasJumpCount = NormalizedMaxJumpCount;
        _bodySprite = BodyRoot.transform.Find("BodySprite");
        if (_bodySprite != null)
            _bodyRestPosition = _bodySprite.localPosition;
        if (Shadow != null)
            _shadowRestPosition = Shadow.localPosition;
        if (MouthSprite != null)
        {
            _mouthRestScale = MouthSprite.transform.localScale;
            _mouthRestPosition = MouthSprite.transform.localPosition;
        }
        UpdateMouthSprite();
        _isvalid = true;
    }

    void Start()
    {
        if (!_isvalid)
        {
            enabled = false;
            return;
        }

        // PlayerManager 在 Start 中注入目标点；缺失时退回自身，避免每帧空引用。
        if (_targetPosObj == null)
        {
            Debug.LogWarning("Player target position is not assigned; using the current position.", this);
            _targetPosObj = gameObject;
        }

        // 待机跳跃只移动身体贴图，不改变 Rigidbody2D，避免卡地或影响真实跳跃。
        _idleHopTime = 0f;
        SetIdleVisualOffset(0f);
    }

    void Update()
    {
        if (!_isvalid || _targetPosObj == null || Rigidbody2D == null)
            return;

        var targetPos = new Vector3(_targetPosObj.transform.position.x, transform.position.y, 0);
        transform.position = Vector3.Lerp(transform.position, targetPos, FollowSpeed * Time.deltaTime);

        TryJump();
        UpdateIdleHopAnimation();
        TryShoot();

        //下落时自己的重力增加
        if (Rigidbody2D.velocity.y < RisingVelocityThreshold)
            Rigidbody2D.gravityScale = FallingGravityScale;
        else
            Rigidbody2D.gravityScale = RisingGravityScale;
    }

    void FixedUpdate()
    {
        if (!_isvalid || Rigidbody2D == null)
        {
            _wasDescending = false;
            return;
        }

        // 记录物理步开始前的下降状态，避免碰撞求解把当前速度清零后误判为非下落。
        _wasDescending = !_isGrounded && Rigidbody2D.velocity.y < FallingVelocityThreshold;
    }

    private void LateUpdate()
    {
        if (!_isvalid || Rigidbody2D == null)
            return;

        // BodyRoot 是动态刚体，可能被敌人的实体碰撞横向挤开；玩家没有横向物理输入，
        // 因此每帧将它拉回 Player 根节点的横向锚点，避免偏移永久残留。
        AlignBodyHorizontally();
        SyncShadowHorizontalPosition();
    }

    private void AlignBodyHorizontally()
    {
        if (Rigidbody2D == null)
            return;

        Vector2 position = Rigidbody2D.position;
        position.x = transform.position.x;
        Rigidbody2D.position = position;

        Vector2 velocity = Rigidbody2D.velocity;
        velocity.x = 0f;
        Rigidbody2D.velocity = velocity;
    }

    private void SyncShadowHorizontalPosition()
    {
        if (Shadow == null || Rigidbody2D == null)
            return;

        Vector3 bodyLocalPosition = transform.InverseTransformPoint(Rigidbody2D.position);
        Vector3 shadowPosition = _shadowRestPosition;
        shadowPosition.x = bodyLocalPosition.x;
        Shadow.localPosition = shadowPosition;
    }

    public void SetTargetPosObj(GameObject target) => _targetPosObj = target;

    public void ApplyWeaponBuff(WeaponType type, float duration)
    {
        _weaponType = type;
        UpdateMouthSprite();

        if (_weaponRoutine != null)
            StopCoroutine(_weaponRoutine);

        if (duration <= 0f)
        {
            _weaponType = WeaponType.Pea;
            UpdateMouthSprite();
            _weaponRoutine = null;
            return;
        }

        _weaponRoutine = StartCoroutine(RevertWeaponAfter(duration));
    }

    private IEnumerator RevertWeaponAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        _weaponType = WeaponType.Pea;
        UpdateMouthSprite();
        _weaponRoutine = null;
    }

    private void UpdateMouthSprite()
    {
        if (MouthSprite == null)
            return;

        MouthSprite.sprite = _weaponType switch
        {
            WeaponType.MachineGun => MachineGunMouthSprite,
            WeaponType.Corn => CornMouthSprite,
            _ => DefaultMouthSprite,
        };
    }

    //接触地面
    public void GroundContact()
    {
        _hasJumpCount = NormalizedMaxJumpCount;
        _isGrounded = true;
        _wasDescending = false;
        // 落地后从 0 相位重新开始，下一帧立即进入上升段。
        _idleHopTime = 0f;
        SetIdleVisualOffset(0f);
    }

    private void TryJump()
    {
        if (!TryReadJumpInput())
            return;

        if (_hasJumpCount > 0)
        {
            Rigidbody2D.velocity = new Vector2(Rigidbody2D.velocity.x, 0);
            Rigidbody2D.AddForce(Vector2.up * JumpImpulse, ForceMode2D.Impulse);
            _hasJumpCount--;
            _isGrounded = false;
            _wasDescending = false;
            _idleHopTime = 0f;
            SetIdleVisualOffset(0f);
        }
    }

    private void UpdateIdleHopAnimation()
    {
        if (!_isGrounded)
        {
            _idleHopTime = 0f;
            SetIdleVisualOffset(0f);
            return;
        }

        float duration = Mathf.Max(0.001f, MinimumIdleHopDuration, IdleHopDuration);
        _idleHopTime += Time.deltaTime;
        float phase = Mathf.Clamp01(_idleHopTime / duration);
        SetIdleVisualOffset(Mathf.Sin(phase * Mathf.PI) * Mathf.Max(0f, IdleHopHeight));
        if (_idleHopTime >= duration)
            _idleHopTime = 0f;
    }

    private void SetIdleVisualOffset(float offset)
    {
        _idleVisualOffset = offset;
        ApplyVisualOffsets();
    }

    private void ApplyVisualOffsets()
    {
        if (_bodySprite != null)
        {
            Vector3 position = _bodyRestPosition;
            position.y += _idleVisualOffset;
            _bodySprite.localPosition = position;
        }

        if (MouthSprite != null)
        {
            Vector3 position = _mouthRestPosition;
            position.x += _mouthKickOffsetX;
            position.y += _idleVisualOffset;
            MouthSprite.transform.localPosition = position;
        }
    }

    private bool TryReadJumpInput()
    {
        bool mousePressed = Input.GetMouseButtonDown(0);
        bool keyboardPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow);
        bool touchPressed = false;
        bool touchOverUI = false;
        EventSystem eventSystem = EventSystem.current;
        bool mouseOverUI = mousePressed && eventSystem != null && eventSystem.IsPointerOverGameObject();

        int touchCount = Input.touchCount;
        for (int i = 0; i < touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase != TouchPhase.Began)
                continue;

            touchPressed = true;
            if (eventSystem != null && eventSystem.IsPointerOverGameObject(touch.fingerId))
                touchOverUI = true;
        }

        if (mouseOverUI || touchOverUI)
            return false;

        return mousePressed || keyboardPressed || touchPressed;
    }

    private void TryShoot()
    {
        GameObject prefab;
        float rate;
        switch (_weaponType)
        {
            case WeaponType.MachineGun:
                prefab = BulletPrefab;
                rate = MachineGunFireRate;
                break;
            case WeaponType.Corn:
                prefab = CornBulletPrefab;
                rate = CornFireRate;
                break;
            default:
                prefab = BulletPrefab;
                rate = PeaFireRate;
                break;
        }

        if (prefab == null || rate <= 0f)
            return;

        _shootTimer += Time.deltaTime;
        float interval = 1f / rate;
        if (_shootTimer < interval)
            return;

        _shootTimer = 0f;
        Vector3 spawnPosition = MouthSprite != null ? MouthSprite.transform.position : transform.position;
        Instantiate(prefab, spawnPosition, Quaternion.identity);
        TriggerMouthKick();
    }

    private void TriggerMouthKick()
    {
        if (MouthSprite == null)
            return;

        if (_mouthRoutine != null)
            StopCoroutine(_mouthRoutine);
        _mouthKickOffsetX = 0f;
        _mouthRoutine = StartCoroutine(MouthKickRoutine());
    }

    private IEnumerator MouthKickRoutine()
    {
        float duration = Mathf.Max(0.001f, MinimumMouthKickDuration, MouthKickDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float kick = Mathf.Sin(t * Mathf.PI);

            Vector3 scale = _mouthRestScale;
            scale.x *= Mathf.Lerp(1f, Mathf.Max(1f, MouthKickScale), kick);
            scale.y *= Mathf.Lerp(1f, Mathf.Clamp(MouthKickVerticalScale, 0.5f, 1f), kick);
            MouthSprite.transform.localScale = scale;

            _mouthKickOffsetX = MouthKickForward * kick;
            ApplyVisualOffsets();
            yield return null;
        }

        MouthSprite.transform.localScale = _mouthRestScale;
        _mouthKickOffsetX = 0f;
        ApplyVisualOffsets();
        _mouthRoutine = null;
    }

    void OnDisable()
    {
        _stompInvincibilityUntil = 0f;

        if (_weaponRoutine != null)
        {
            StopCoroutine(_weaponRoutine);
            _weaponRoutine = null;
        }

        if (_mouthRoutine != null)
        {
            StopCoroutine(_mouthRoutine);
            _mouthRoutine = null;
        }

        _mouthKickOffsetX = 0f;
        SetIdleVisualOffset(0f);

        if (MouthSprite != null)
        {
            MouthSprite.transform.localScale = _mouthRestScale;
        }
    }
}

