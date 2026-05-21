using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CentralArchiveMusicZone : MonoBehaviour
{
    private bool playerInside;

    private void Reset()
    {
        Collider zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    private void Awake()
    {
        Collider zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    private void OnDisable()
    {
        if (playerInside && MemoryRecyclerMusicManager.Instance != null)
            MemoryRecyclerMusicManager.Instance.SetArchiveZone(false);

        playerInside = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;
        if (MemoryRecyclerMusicManager.Instance != null)
            MemoryRecyclerMusicManager.Instance.SetArchiveZone(true);
    }

    private void OnTriggerStay(Collider other)
    {
        if (playerInside || !other.CompareTag("Player"))
            return;

        playerInside = true;
        if (MemoryRecyclerMusicManager.Instance != null)
            MemoryRecyclerMusicManager.Instance.SetArchiveZone(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;
        if (MemoryRecyclerMusicManager.Instance != null)
            MemoryRecyclerMusicManager.Instance.SetArchiveZone(false);
    }
}
