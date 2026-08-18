using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public GameObject TargetPosObj;
    public GameObject PlayerPrefab;
    private bool _isvalid = false;
    void Awake()
    {
        if (PlayerPrefab == null)
            Debug.LogError("PlayerPrefab is null in PlayerManager");
        if (TargetPosObj == null)
            Debug.LogWarning("TargetPosObj is not assigned; the player will keep its spawn position.", this);
        _isvalid = PlayerPrefab != null;
    }

    void Start()
    {
        if (!_isvalid)
        {
            Debug.LogError("PlayerManager is not valid. Cannot create player.", this);
            return;
        }

        var obj = Instantiate(PlayerPrefab, new Vector3(-3.4f, -1.8f, 0), Quaternion.identity);
        var player = obj.GetComponent<Player>();
        if (player == null)
        {
            Debug.LogError("PlayerPrefab must contain a Player component.", obj);
            Destroy(obj);
            return;
        }

        player.SetTargetPosObj(TargetPosObj);
    }
}
