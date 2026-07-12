using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CardCameraManager : MonoBehaviour
{
    public static CardCameraManager Instance { get; private set; }

    [Header("卡牌渲染相机 (Overlay)")]
    public Camera cardCamera;

    [Header("主相机 (Base)")]
    public Camera mainCamera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnRuntimeMethodLoad()
    {
        // 自动创建 Manager 物体，这保证了在任何场景启动时都能自动配置双相机，无需手动在 scene 里摆放物体
        if (Instance == null)
        {
            GameObject go = new GameObject("CardCameraManager");
            go.AddComponent<CardCameraManager>();
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupCameraStack();
    }

    public void SetupCameraStack()
    {
        // 1. 获取/确定主相机
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            Debug.LogError("[CardCameraManager] 找不到主相机 Camera.main，请确保场景中有 Tag 为 MainCamera 的相机！");
            return;
        }

        int cardLayer = LayerMask.NameToLayer("Card");
        if (cardLayer == -1) cardLayer = 6;

        // 2. 检查或创建卡牌相机
        if (cardCamera == null)
        {
            // 尝试在主相机子物体中寻找
            Transform existingCam = mainCamera.transform.Find("CardCamera");
            if (existingCam != null)
            {
                cardCamera = existingCam.GetComponent<Camera>();
            }
            else
            {
                // 动态创建
                GameObject camGo = new GameObject("CardCamera");
                camGo.transform.SetParent(mainCamera.transform, false);
                cardCamera = camGo.AddComponent<Camera>();
            }
        }

        // 3. 配置卡牌相机属性 (与主相机同步，但只渲染 Card 层)
        cardCamera.clearFlags = CameraClearFlags.Depth;
        cardCamera.cullingMask = 1 << cardLayer;
        cardCamera.fieldOfView = mainCamera.fieldOfView;
        cardCamera.nearClipPlane = mainCamera.nearClipPlane;
        cardCamera.farClipPlane = mainCamera.farClipPlane;
        cardCamera.orthographic = mainCamera.orthographic;
        cardCamera.orthographicSize = mainCamera.orthographicSize;

        // 4. 配置 URP 相机堆叠
        var mainCamData = mainCamera.GetComponent<UniversalAdditionalCameraData>();
        if (mainCamData == null)
        {
            mainCamData = mainCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        }
        mainCamData.renderType = CameraRenderType.Base;

        // 主相机剔除 Card 层，避免双重渲染
        mainCamera.cullingMask &= ~(1 << cardLayer);

        var cardCamData = cardCamera.GetComponent<UniversalAdditionalCameraData>();
        if (cardCamData == null)
        {
            cardCamData = cardCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        }
        cardCamData.renderType = CameraRenderType.Overlay;

        // 将卡牌相机加入主相机的 Stack 列表中
        if (!mainCamData.cameraStack.Contains(cardCamera))
        {
            mainCamData.cameraStack.Add(cardCamera);
        }

        // 5. 确保 CardCamera 拥有 Raycaster 组件，以便接收拖拽和悬停事件 (EventSystem)
        // 导入 UnityEngine.EventSystems 命名空间以添加 Raycaster
        if (cardCamera.GetComponent<UnityEngine.EventSystems.Physics2DRaycaster>() == null)
        {
            var raycaster2D = cardCamera.gameObject.AddComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
            raycaster2D.eventMask = 1 << cardLayer; // 限制只检测卡牌层，提升射线检测性能
        }

        if (cardCamera.GetComponent<UnityEngine.EventSystems.PhysicsRaycaster>() == null)
        {
            var raycaster3D = cardCamera.gameObject.AddComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
            raycaster3D.eventMask = 1 << cardLayer;
        }

        Debug.Log("[CardCameraManager] URP 双相机叠加设置成功，并已绑定物理射线检测器！");
    }

    /// <summary>
    /// 递归设置 GameObject 及其所有子物体的 Layer
    /// </summary>
    public static void SetLayerRecursive(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, newLayer);
        }
    }
}
