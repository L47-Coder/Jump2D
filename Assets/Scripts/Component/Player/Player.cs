using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class Player : MonoBehaviour
{
    public GameObject BodyRoot;
    public Rigidbody2D Rigidbody2D;
    public float ShootSpeed;
    public GameObject BulletPrefab;
    public GameObject CornBulletPrefab;
    public float MachineGunFireRateMultiplier = 4f;
    public float CornFireInterval = 0.35f;
    public float FollowSpeed = 5f;
    public float JumpImpulse = 7f;
    public int MaxJumpCount = 2;
    public float RisingVelocityThreshold = 3f;
    public float RisingGravityScale = 1f;
    public float FallingGravityScale = 5f;
    public float FallingVelocityThreshold = -0.1f;
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
    private int _hasJumpCount = 2;
    private bool _isGrounded = true;
    private float _shootTimer;
    private float _idleHopTime;
    private float _idleVisualOffset;
    private float _mouthKickOffsetX;
    private WeaponType _weaponType = WeaponType.Pea;
    private Coroutine _weaponRoutine;
    private Coroutine _mouthRoutine;
    private Transform _bodySprite;
    private Vector3 _bodyRestPosition;
    private Vector3 _mouthRestScale;
    private Vector3 _mouthRestPosition;
    public bool IsFalling => _isvalid && !_isGrounded && Rigidbody2D != null && Rigidbody2D.velocity.y < FallingVelocityThreshold;

    void Awake()
    {
        if (BodyRoot == null)
            BodyRoot = transform.Find("BodyRoot")?.gameObject;

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

        _hasJumpCount = Mathf.Max(0, MaxJumpCount);
        _bodySprite = BodyRoot.transform.Find("BodySprite");
        if (_bodySprite != null)
            _bodyRestPosition = _bodySprite.localPosition;
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
        _hasJumpCount = 2;
        _isGrounded = true;
        // 落地后从 0 相位重新开始，下一帧立即进入上升段。
        _idleHopTime = 0f;
        SetIdleVisualOffset(0f);
    }

    private void TryJump()
    {
        if (!IsJumpPressedThisFrame() || IsJumpPointerOverUI())
            return;

        if (_hasJumpCount > 0)
        {
            Rigidbody2D.velocity = new Vector2(Rigidbody2D.velocity.x, 0);
            Rigidbody2D.AddForce(Vector2.up * JumpImpulse, ForceMode2D.Impulse);
            _hasJumpCount--;
            _isGrounded = false;
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

    private bool IsJumpPressedThisFrame()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
            return true;

        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
                return true;
        }

        return false;
    }

    private bool IsJumpPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        if (Input.GetMouseButtonDown(0) && EventSystem.current.IsPointerOverGameObject())
            return true;

        for (int i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return true;
        }

        return false;
    }

    private void TryShoot()
    {
        GameObject prefab;
        float rate;
        switch (_weaponType)
        {
            case WeaponType.MachineGun:
                prefab = BulletPrefab;
                rate = ShootSpeed * MachineGunFireRateMultiplier;
                break;
            case WeaponType.Corn:
                prefab = CornBulletPrefab;
                rate = CornFireInterval > 0f ? 1f / CornFireInterval : 0f;
                break;
            default:
                prefab = BulletPrefab;
                rate = ShootSpeed;
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

