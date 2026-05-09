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

        if (playerInside && Input.GetKeyDown(KeyCode.E))
            Interact();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;
        string title = memoryData != null ? memoryData.memoryTitle : "기억";
        UIManager3D.Instance.ShowPrompt("E : 기억 회수 - " + title);
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
        playerInside = false;
        UIManager3D.Instance.HidePrompt();
        MemoryManager3D.Instance.CollectMemory(memoryData);
        gameObject.SetActive(false);
    }

    public void SetCollectedFromSave(bool collected)
    {
        gameObject.SetActive(!collected);
    }
}
