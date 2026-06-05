using UnityEngine;

// 발표 안정화용 비파괴 플레이 영역 가드.
// 씬에 직접 배치하거나 MR3D_Bootstrap이 런타임에 부착해 사용한다.
// 트리거/메쉬 콜라이더를 추가하지 않으므로 기존 씬 충돌 환경을 일절 변경하지 않는다.
public class PlayBoundary3D : MonoBehaviour
{
    [Header("Boundary")]
    public Vector3 center = Vector3.zero;
    // 발표 맵의 기본 평면 크기(약 240m)를 기준으로, 외곽 이탈을 너무 늦게 잡지 않도록 설정.
    public float radius = 125f;
    // 낙하 사고 방지용 바닥 한계. 발표 중 공허로 떨어지면 빠르게 복귀시킨다.
    public float floorY = -8f;
    // 천장 한계(점프 + 외부 도구로 떠밀려 올라간 경우 대비).
    public float ceilingY = 120f;

    [Header("Failsafe")]
    public Vector3 safeRespawnPosition = new Vector3(0f, 0f, -22f);
    public float safeRespawnYaw = 0f;
    public float checkInterval = 0.25f;
    // 같은 프레임에 복귀가 연쇄로 발생해 토스트가 도배되지 않도록 쿨다운.
    public float toastCooldown = 2.0f;

    private static PlayBoundary3D instance;
    public static PlayBoundary3D Instance => instance;

    private Transform tracked;
    private float nextCheckTime;
    private float nextToastTime;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void Configure(Vector3 boundaryCenter, float boundaryRadius, Vector3 respawn, float respawnYaw)
    {
        center = boundaryCenter;
        radius = Mathf.Max(10f, boundaryRadius);
        safeRespawnPosition = respawn;
        safeRespawnYaw = respawnYaw;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCheckTime)
            return;
        nextCheckTime = Time.unscaledTime + checkInterval;

        Transform player = ResolvePlayer();
        if (player == null)
            return;

        Vector3 position = player.position;
        Vector3 planar = position - center;
        planar.y = 0f;

        bool outsideRadius = planar.sqrMagnitude > radius * radius;
        bool belowFloor = position.y < floorY;
        bool aboveCeiling = position.y > ceilingY;

        if (!outsideRadius && !belowFloor && !aboveCeiling)
            return;

        // 안전 위치로 강제 복귀. CharacterController가 있으면 잠시 비활성화해 텔레포트 충돌을 피한다.
        Vector3 target = safeRespawnPosition;
        Quaternion targetRotation = Quaternion.Euler(0f, safeRespawnYaw, 0f);
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;
        player.position = target;
        player.rotation = targetRotation;
        if (controller != null)
            controller.enabled = true;

        if (Time.unscaledTime >= nextToastTime && UIManager3D.Instance != null)
        {
            UIManager3D.Instance.ShowToast("플레이 영역 밖입니다. 안전 위치로 복귀합니다.");
            nextToastTime = Time.unscaledTime + toastCooldown;
        }
    }

    private Transform ResolvePlayer()
    {
        if (tracked != null)
            return tracked;

        GameObject byName = GameObject.Find("Player_Recycler");
        if (byName != null)
        {
            tracked = byName.transform;
            return tracked;
        }

        GameObject byTag = GameObject.FindGameObjectWithTag("Player");
        if (byTag != null)
            tracked = byTag.transform;
        return tracked;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        const int segments = 48;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float t = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(t) * radius, 0f, Mathf.Sin(t) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
        Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.4f);
        Gizmos.DrawWireCube(center + Vector3.up * (floorY * 0.5f), new Vector3(radius * 2f, 0.1f, radius * 2f));
    }
#endif
}
