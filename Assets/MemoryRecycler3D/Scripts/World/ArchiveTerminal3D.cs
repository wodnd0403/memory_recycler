using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ArchiveTerminal3D : MonoBehaviour
{
    public int requiredDecisions = 5;

    private bool playerInside;

    private void Start()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (playerInside && Input.GetKeyDown(KeyCode.E))
            Interact();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;
        UIManager3D.Instance.ShowPrompt("E : 중앙 아카이브 접속 / Tab : 수집 기록");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;
        UIManager3D.Instance.HidePrompt();
    }

    private void Interact()
    {
        int decided = MemoryManager3D.Instance != null ? MemoryManager3D.Instance.CountDecidedMemories() : 0;
        if (decided < requiredDecisions)
        {
            if (MemoryCinematicCamera3D.Instance != null)
                MemoryCinematicCamera3D.Instance.PlayArchiveReveal(transform, true);
            UIManager3D.Instance.ShowArchiveLocked(decided, requiredDecisions);
            return;
        }

        if (MemoryCinematicCamera3D.Instance != null)
            MemoryCinematicCamera3D.Instance.PlayArchiveReveal(transform, false);
        UIManager3D.Instance.ShowEnding();
    }
}
