using UnityEngine;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    public GameObject CameraObj;
    public GameObject BackgroundPrefab;
    public GameObject CloudPrefab;
    public List<Sprite> CloudSprites;
    public float BackgroundSegmentLength = 13f;
    public int CloudsPerSegment = 2;
    public float CloudMinY = 0f;
    public float CloudMaxY = 2.5f;
    public int MaxBackgroundCount = 4;
    // 返回最后一段背景的起始 X。相机到达该位置时立即预生成下一段，
    // 保持首段生成后马上补齐前方地图，避免角色跑到背景空档。
    public float BackgroundLength => (_nextBackgroundId - 1) * BackgroundSegmentLength;
    private bool _isvalid = false;
    private Queue<GameObject> _backgroundQueue = new();
    private int _nextBackgroundId = 0;

    void Awake()
    {
        if (CameraObj == null && Camera.main != null)
            CameraObj = Camera.main.gameObject;

        if (CameraObj == null)
            Debug.LogError("CameraObj is null in MapManager");
        if (BackgroundPrefab == null)
            Debug.LogError("BackgroundPrefab is null in MapManager");

        BackgroundSegmentLength = Mathf.Max(0.1f, BackgroundSegmentLength);
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
        var pos = new Vector3(_nextBackgroundId * BackgroundSegmentLength, 0, 0);
        var backgroundObj = GameObject.Instantiate(BackgroundPrefab, pos, Quaternion.identity);
        _backgroundQueue.Enqueue(backgroundObj);
        _nextBackgroundId++;

        //生成云
        if (CloudPrefab != null && CloudSprites != null && CloudSprites.Count > 0)
        {
            int cloudCount = Mathf.Max(0, CloudsPerSegment);
            float cloudSegmentLength = BackgroundSegmentLength / Mathf.Max(1, cloudCount);
            for (int i = 0; i < cloudCount; i++)
            {
                var cloudObj = GameObject.Instantiate(CloudPrefab, backgroundObj.transform);
                var renderer = cloudObj.GetComponent<SpriteRenderer>();
                if (renderer != null)
                    renderer.sprite = CloudSprites[Random.Range(0, CloudSprites.Count)];
                float minX = -BackgroundSegmentLength * 0.5f + i * cloudSegmentLength;
                float maxX = minX + cloudSegmentLength;
                cloudObj.transform.localPosition = new Vector3(
                    Random.Range(minX, maxX),
                    Random.Range(Mathf.Min(CloudMinY, CloudMaxY), Mathf.Max(CloudMinY, CloudMaxY)),
                    0);
            }
        }

        //清理掉多余的背景
        if (_backgroundQueue.Count >= Mathf.Max(1, MaxBackgroundCount))
            Destroy(_backgroundQueue.Dequeue());
    }
}
