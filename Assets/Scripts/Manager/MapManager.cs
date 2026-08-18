using UnityEngine;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    public GameObject CameraObj;
    public GameObject BackgroundPrefab;
    public GameObject CloudPrefab;
    public List<Sprite> CloudSprites;
    // 返回最后一段背景的起始 X。相机到达该位置时立即预生成下一段，
    // 保持首段生成后马上补齐前方地图，避免角色跑到背景空档。
    public float BackgroundLength => (_nextBackgroundId - 1) * _backgroundLength;
    private bool _isvalid = false;
    private Queue<GameObject> _backgroundQueue = new();
    private float _backgroundLength = 13f;
    private int _nextBackgroundId = 0;

    void Awake()
    {
        if (CameraObj == null && Camera.main != null)
            CameraObj = Camera.main.gameObject;

        if (CameraObj == null)
            Debug.LogError("CameraObj is null in MapManager");
        if (BackgroundPrefab == null)
            Debug.LogError("BackgroundPrefab is null in MapManager");

        _backgroundLength = Mathf.Max(0.1f, _backgroundLength);
        _isvalid = BackgroundPrefab != null;
    }

    public void CreateBackground()
    {
        if (!_isvalid || BackgroundPrefab == null)
        {
            Debug.LogError("MapManager is not valid. Cannot create background.");
            return;
        }

        //生成背景
        var pos = new Vector3(_nextBackgroundId * _backgroundLength, 0, 0);
        var backgroundObj = GameObject.Instantiate(BackgroundPrefab, pos, Quaternion.identity);
        _backgroundQueue.Enqueue(backgroundObj);
        _nextBackgroundId++;

        //生成云
        if (CloudPrefab != null && CloudSprites != null && CloudSprites.Count > 0)
        {
            for (int i = 0; i < 2; i++)
            {
                var cloudObj = GameObject.Instantiate(CloudPrefab, backgroundObj.transform);
                var renderer = cloudObj.GetComponent<SpriteRenderer>();
                if (renderer != null)
                    renderer.sprite = CloudSprites[Random.Range(0, CloudSprites.Count)];
                cloudObj.transform.localPosition = new Vector3(Random.Range((-1 + i) * _backgroundLength / 2, i * _backgroundLength / 2), Random.Range(0f, 2.5f), 0);
            }
        }

        //清理掉多余的背景
        if (_backgroundQueue.Count >= 4)
            Destroy(_backgroundQueue.Dequeue());
    }
}
