using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ArchiveTerminal3D : MonoBehaviour
{
    // 인스펙터에서 임시 조정 가능. 기본값은 UIManager3D.RequiredDecisionsForEnding과 동기화된다.
    public int requiredDecisions = UIManager3D.RequiredDecisionsForEnding;

    private bool playerInside;

    private void Start()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        // UI가 입력을 막는 동안에는 E 상호작용이 중복 실행되지 않도록 차단.
        if (UIManager3D.Instance != null && UIManager3D.Instance.IsGameplayInputBlocked())
            return;

        if (playerInside && Input.GetKeyDown(KeyCode.E))
            Interact();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;
        if (UIManager3D.Instance != null)
            UIManager3D.Instance.ShowPrompt("E : 중앙 아카이브 접속 / Tab : 수집 기록");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;
        if (UIManager3D.Instance != null)
            UIManager3D.Instance.HidePrompt();
    }

    private void Interact()
    {
        int required = Mathf.Max(1, requiredDecisions);
        int decided = MemoryManager3D.Instance != null ? MemoryManager3D.Instance.CountDecidedMemories() : 0;
        if (decided < required)
        {
            if (MemoryCinematicCamera3D.Instance != null)
                MemoryCinematicCamera3D.Instance.PlayArchiveReveal(transform, true);
            if (UIManager3D.Instance != null)
                UIManager3D.Instance.ShowArchiveLocked(decided, required);
            return;
        }

        if (MemoryCinematicCamera3D.Instance != null)
            MemoryCinematicCamera3D.Instance.PlayArchiveReveal(transform, false);
        if (UIManager3D.Instance != null)
            UIManager3D.Instance.ShowEnding();
    }
}
