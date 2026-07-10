using UnityEngine;

public class CardArrow : MonoBehaviour
{
    private LineRenderer lineRenderer;

    [Header("曲线平滑度")]
    public int pointsCount = 10;
    
    [Header("弯曲弧度控制(数值越大弯得越厉害)")]
    public float arcModifier = 2f;

    private Vector3 mousePos;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        Camera activeCam = (CardCameraManager.Instance != null && CardCameraManager.Instance.cardCamera != null)
            ? CardCameraManager.Instance.cardCamera
            : Camera.main;

        if (activeCam == null)
        {
            Debug.LogError("CardArrow: No active camera found for screen to world point conversion! 请确保场景中有 Tag 为 MainCamera 的相机。");
            return;
        }

        mousePos = activeCam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10));
        // 2. 实时计算并绘制贝塞尔曲线
        SetArrowPosition();
    }

      // 计算并绘制贝塞尔曲线箭头
    public void SetArrowPosition()
    {
        Vector3 cardPosition = transform.position; // 卡牌位置（起点 P0）
        Vector3 direction = mousePos - cardPosition; // 从卡牌指向鼠标的方向
        Vector3 normalizedDirection = direction.normalized; // 归一化方向
        // 计算垂直于卡牌到鼠标方向的向量（用于控制点侧向偏移）
        Vector3 perpendicular = new Vector3(-normalizedDirection.y, normalizedDirection.x, normalizedDirection.z);
        // 设置控制点的偏移量
        Vector3 offset = perpendicular * arcModifier;
        // 计算贝塞尔曲线的控制点（P1）
        Vector3 controlPoint = (cardPosition + mousePos) / 2 + offset;
        // 设置 LineRenderer 的顶点数量
        lineRenderer.positionCount = pointsCount;
        // 循环计算曲线上的每一个点并填入 LineRenderer
        for (int i = 0; i < pointsCount; i++)
        {
            float t = i / (float)(pointsCount - 1);
            Vector3 point = CalculateQuadraticBezierPoint(t, cardPosition, controlPoint, mousePos);
            lineRenderer.SetPosition(i, point);
        }
    }
    // 二次贝塞尔曲线公式计算点：B(t) = (1-t)²P0 + 2t(1-t)P1 + t²P2
    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        Vector3 p = uu * p0;    // 第一项
        p += 2 * u * t * p1;    // 第二项
        p += tt * p2;           // 第三项
        return p;
    }
}
