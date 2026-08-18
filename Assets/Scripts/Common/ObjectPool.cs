using System.Collections.Generic;
using UnityEngine;

// 通用 GameObject 对象池，减少子弹/特效等高频生成对象的 GC 开销
public class ObjectPool
{
    private readonly GameObject _prefab;
    private readonly Transform _root;
    private readonly Stack<GameObject> _inactive = new();

    public ObjectPool(GameObject prefab, int prewarm = 0, Transform root = null)
    {
        _prefab = prefab;
        _root = root;
        for (int i = 0; i < prewarm; i++)
        {
            var obj = Object.Instantiate(_prefab, _root);
            obj.SetActive(false);
            _inactive.Push(obj);
        }
    }

    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject obj = _inactive.Count > 0 ? _inactive.Pop() : Object.Instantiate(_prefab, _root);
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    public void Release(GameObject obj)
    {
        obj.SetActive(false);
        _inactive.Push(obj);
    }
}
