using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public abstract class ComponentPool<T> : MonoBehaviour where T : PoolableBehaviour
{
    [SerializeField]
    private int defaultCapacity = 16;
    [SerializeField]
    private int maxSize = 64;

    private readonly Dictionary<T, ObjectPool<T>> pools = new();
    private Transform poolRoot;

    private void Awake()
    {
        GameObject root = new GameObject($"{name}_{typeof(T)}ProjectilePool");
        poolRoot = root.transform;
        poolRoot.SetParent(transform);
    }

    private void OnDestroy()
    {
        pools.Clear();
    }

    /// <summary>
    /// 지정한 prefab의 UnityPool를 반환
    /// </summary>
    /// <param name="prefab">Pool에서 관리한 Componenet Prefab</param>
    /// <returns></returns>
    private ObjectPool<T> GetOrCreatePool(T prefab)
    {
        if (pools.TryGetValue(prefab, out ObjectPool<T> pool))
            return pool;

        ObjectPool<T> newPool = null;

        newPool = new ObjectPool<T>(createFunc: () => CreateInstance(prefab, newPool), actionOnGet: null, actionOnRelease: ReleaseInstance, actionOnDestroy: DestroyedInstance, 
            collectionCheck: true, defaultCapacity: defaultCapacity, maxSize: maxSize);

        pools.Add(prefab, newPool);

        return newPool;
    }

    /// <summary>
    /// 새로운 인스턴스를 생성하고 Pool 반환을 연결
    /// </summary>
    /// <param name="prefab">생성할 prefab</param>
    /// <param name="ownerPool">생선된 prefab을 소유할 Pool</param>
    /// <returns></returns>
    private T CreateInstance(T prefab, IObjectPool<T> ownerPool)
    {
        T newObject = Instantiate(prefab, poolRoot);

        newObject.BindRelease(() => ownerPool.Release(newObject));
        newObject.gameObject.SetActive(false);

        return newObject;
    }


    /// <summary>
    /// 반환한 인스턴스를 초기화하고 비활성화
    /// </summary>
    /// <param name="prefab">반환된 인스턴스</param>
    private void ReleaseInstance(T prefab)
    {
        prefab.ResetPoolState();
        prefab.transform.SetParent(poolRoot);
        prefab.gameObject.SetActive(false);
    }

    /// <summary>
    /// Pool 최대 크기를 초과한 인스턴스 제거
    /// </summary>
    /// <param name="prefab"></param>
    private void DestroyedInstance(T prefab)
    {
        Destroy(prefab.gameObject);
    }

    /// <summary>
    /// 지정한 Prefab의 인스턴스를 Pool에서 가져옴
    /// </summary>
    /// <param name="prefab">가져올 Component prefab</param>
    /// <param name="pos">배치할 world 위치</param>
    /// <param name="rotation">배치할 world 회전 값</param>
    /// <returns></returns>
    public T Get(T prefab, Vector3 pos, Quaternion rotation)
    {
        ObjectPool<T> pool = GetOrCreatePool(prefab);
        T newPool = pool.Get();

        newPool.transform.SetParent(poolRoot, false);
        newPool.transform.SetPositionAndRotation(pos, rotation);
        newPool.gameObject.SetActive(true);

        return newPool;
    }
}
