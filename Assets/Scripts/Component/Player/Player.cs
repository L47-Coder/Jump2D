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
    public float CornFireInterval = 1.2f;
    public SpriteRenderer MouthSprite;
    public Sprite DefaultMouthSprite;
    public Sprite MachineGunMouthSprite;
    public Sprite CornMouthSprite;
    private bool _isvalid = false;
    private GameObject _targetPosObj;
    private int _hasJumpCount = 2;
    private float _shootTimer;
    private WeaponType _weaponType = WeaponType.Pea;
    private Coroutine _weaponRoutine;
    private Vector3 _bodyRestScale;
    private const float FollowSpeed = 5f;
    private const float JumpImpulse = 7f;
    private const float RisingVelocityThreshold = 3f;
    private const float RisingGravityScale = 1f;
    private const float FallingGravityScale = 5f;

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

        _bodyRestScale = BodyRoot.transform.localScale;
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
    }

    void Update()
    {
        if (!_isvalid || _targetPosObj == null || Rigidbody2D == null)
            return;

        var targetPos = new Vector3(_targetPosObj.transform.position.x, transform.position.y, 0);
        transform.position = Vector3.Lerp(transform.position, targetPos, FollowSpeed * Time.deltaTime);

        TryJump();
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
        bool wasAirborne = _hasJumpCount < 2;
        _hasJumpCount = 2;
        if (wasAirborne && BodyRoot != null)
            Tween.Punch(this, BodyRoot.transform, _bodyRestScale, 1.08f, 0.16f);
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
            if (BodyRoot != null)
                Tween.Punch(this, BodyRoot.transform, _bodyRestScale, 0.92f, 0.12f);
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
    }

    void OnDisable()
    {
        if (_weaponRoutine != null)
        {
            StopCoroutine(_weaponRoutine);
            _weaponRoutine = null;
        }
    }
}

