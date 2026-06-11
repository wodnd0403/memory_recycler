using UnityEngine;

// 중앙 아카이브 접속 판정.
// 예전에는 터미널 큐브의 얇은 트리거 박스(깊이 1.6m) 안에 들어가야만 상호작용이 됐다.
// 그런데 터미널 메시는 비활성(보이지 않음)이고 비주얼 정면(라벨/렌즈)은 트리거 박스보다
// 앞(-Z)에 있어서, 플레이어가 "정면이라고 느끼는 위치"에 서면 트리거 밖이라 접속이 안 됐다
// (뒤쪽에서만 박스 안으로 들어가져서 되는 것처럼 보임).
// 해결: 방향/얇은 트리거가 아니라 "터미널 중심 기준 수평 거리"로 판정해 정면/측면/후면 어디서나 접속 가능하게 한다.
[RequireComponent(typeof(Collider))]
public class ArchiveTerminal3D : MonoBehaviour
{
    // 인스펙터에서 임시 조정 가능. 기본값은 UIManager3D.RequiredDecisionsForEnding과 동기화된다.
    public int requiredDecisions = UIManager3D.RequiredDecisionsForEnding;

    // 터미널 중심에서 이 수평 거리(XZ) 안이면 정면/측면/후면 어디서든 E 접속 가능.
    public float interactRadius = 5f;

    private Transform player;
    private bool playerNearby;
    private bool promptShown;

    private void Start()
    {
        // 콜라이더는 기존대로 트리거로 유지한다(플레이어가 통과 가능, 물리 비간섭). 구조 변경 없음.
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        // 씬에 직렬화된 값이 옛 임계값(예: 5)을 가지고 있어도, 시연 임계값(상수)을 항상 정답으로 사용.
        requiredDecisions = UIManager3D.RequiredDecisionsForEnding;

        Debug.Log("[MR3D ArchiveTerminal] terminal initialized pos=" + transform.position +
            " radius=" + interactRadius.ToString("0.0") + " required=" + requiredDecisions);
    }

    private void Update()
    {
        Transform p = ResolvePlayer();
        if (p == null)
            return;

        float distance = HorizontalDistanceTo(p);
        bool inRange = distance <= interactRadius;
        bool blocked = UIManager3D.Instance != null && UIManager3D.Instance.IsGameplayInputBlocked();

        UpdatePresence(inRange, distance, p);

        // 안내 UI는 범위 안 + 다른 패널이 열려있지 않을 때만 표시(정면에서 접근하면 항상 뜬다).
        bool shouldPrompt = inRange && !blocked;
        if (shouldPrompt && !promptShown)
        {
            promptShown = true;
            if (UIManager3D.Instance != null)
                UIManager3D.Instance.ShowPrompt("E : 중앙 아카이브 접속 / Tab : 수집 기록");
        }
        else if (!shouldPrompt && promptShown)
        {
            promptShown = false;
            if (UIManager3D.Instance != null)
                UIManager3D.Instance.HidePrompt();
        }

        if (inRange && !blocked && Input.GetKeyDown(KeyCode.E))
            Interact(p, distance);
    }

    private void UpdatePresence(bool inRange, float distance, Transform p)
    {
        if (inRange == playerNearby)
            return;

        playerNearby = inRange;
        // 아카이브 체류 시간 기록(기존 동작 유지).
        PlayerMemoryLog3D.Ensure().SetArchivePresence(inRange);
        Debug.Log("[MR3D ArchiveTerminal] canInteract=" + inRange +
            " distance=" + distance.ToString("0.0") + " side=" + GetSide(p));
    }

    private Transform ResolvePlayer()
    {
        if (player != null)
            return player;

        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
            player = go.transform;
        return player;
    }

    private float HorizontalDistanceTo(Transform p)
    {
        // 터미널 중심이 높이 3.9에 있으므로 Y를 무시한 수평 거리로 판정한다.
        Vector3 a = transform.position;
        Vector3 b = p.position;
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // 로그용 방향 판정. 비주얼 정면은 -transform.forward(-Z) 쪽.
    private string GetSide(Transform p)
    {
        Vector3 toPlayer = p.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f)
            return "center";
        toPlayer.Normalize();

        Vector3 front = -transform.forward;
        front.y = 0f;
        front.Normalize();

        float dot = Vector3.Dot(toPlayer, front);
        if (dot > 0.5f)
            return "front";
        if (dot < -0.5f)
            return "back";
        return "side";
    }

    private void Interact(Transform p, float distance)
    {
        int required = Mathf.Max(1, requiredDecisions);
        int decided = MemoryManager3D.Instance != null ? MemoryManager3D.Instance.CountDecidedMemories() : 0;

        Debug.Log("[MR3D ArchiveTerminal] E pressed from " + GetSide(p) +
            " distance=" + distance.ToString("0.0") + " decided=" + decided + "/" + required);

        if (decided < required)
        {
            Debug.Log("[MR3D ArchiveTerminal] blocked: processed " + decided + " < " + required);
            if (MemoryCinematicCamera3D.Instance != null)
                MemoryCinematicCamera3D.Instance.PlayArchiveReveal(transform, true);
            if (UIManager3D.Instance != null)
                UIManager3D.Instance.ShowArchiveLocked(decided, required);
            return;
        }

        Debug.Log("[MR3D ArchiveTerminal] starting interrogation/ending from " + GetSide(p));
        if (MemoryCinematicCamera3D.Instance != null)
            MemoryCinematicCamera3D.Instance.PlayArchiveReveal(transform, false);
        PlayerMemoryLog3D.Ensure().MarkEndingReached();
        if (UIManager3D.Instance != null)
            UIManager3D.Instance.ShowEnding();
    }
}
