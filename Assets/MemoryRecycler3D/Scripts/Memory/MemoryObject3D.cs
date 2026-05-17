using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MemoryObject3D : MonoBehaviour
{
    public MemoryData3D memoryData;
    public float rotateSpeed = 35f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.2f;

    private Vector3 startPosition;
    private bool playerInside;

    private void Start()
    {
        startPosition = transform.position;
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        if (MemoryManager3D.Instance != null)
            MemoryManager3D.Instance.RegisterMemoryObject(this);
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);

        // UI 패널 열려 있는 동안에는 E 상호작용이 다시 호출되지 않도록 차단.
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
        string title = memoryData != null ? memoryData.memoryTitle : "기억";
        if (UIManager3D.Instance != null)
            UIManager3D.Instance.ShowPrompt("E : 기억 회수 - " + title);
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
        // memoryData 또는 매니저가 비어 있는 상태에서 클릭되면 조용히 무시한다.
        if (memoryData == null || MemoryManager3D.Instance == null)
            return;

        playerInside = false;
        if (UIManager3D.Instance != null)
            UIManager3D.Instance.HidePrompt();
        if (MemoryCinematicCamera3D.Instance != null)
            MemoryCinematicCamera3D.Instance.PlayMemoryPickup(transform);
        if (MemoryAudio3D.Instance != null)
            MemoryAudio3D.Instance.PlayMemoryFound();
        MemoryManager3D.Instance.CollectMemory(memoryData);
        gameObject.SetActive(false);
    }

    public void SetCollectedFromSave(bool collected)
    {
        gameObject.SetActive(!collected);
    }
}
