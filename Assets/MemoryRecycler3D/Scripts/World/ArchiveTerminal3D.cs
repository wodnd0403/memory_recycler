using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ArchiveTerminal3D : MonoBehaviour
{
    private bool playerInside;

    private void Start()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (playerInside && Input.GetKeyDown(KeyCode.E))
            UIManager3D.Instance.ShowEnding();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;
        UIManager3D.Instance.ShowPrompt("E : 중앙 아카이브 접속 / Tab : 기억 목록");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;
        UIManager3D.Instance.HidePrompt();
    }
}
