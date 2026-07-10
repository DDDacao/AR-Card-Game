using UnityEngine;
using UnityEngine.Pool;

public class PoolTool : MonoBehaviour
{
    public GameObject objPrefab;
    private ObjectPool<GameObject> pool;

    private void Awake()
    {
        // 初始化对象池
        pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(objPrefab, transform),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: false,
            defaultCapacity: 10,
            maxSize: 20
        );
        // 预填充对象池
        PreFillPoll(10);
    }

    private void PreFillPoll(int count){

        var preFillArray = new GameObject[count];

        for(int i = 0; i < count; i++){
            preFillArray[i] = pool.Get();
        }
        foreach(var item in preFillArray){
            pool.Release(item);
        }
    }

    public GameObject GetObj(){
        return pool.Get();
    }
    public void ReleaseObj(GameObject obj){
        pool.Release(obj);
    }
}