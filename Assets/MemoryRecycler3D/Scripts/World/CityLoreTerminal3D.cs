using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CityLoreTerminal3D : MonoBehaviour
{
    public string terminalTitle = "도시 기록";
    [TextArea(3, 8)] public string terminalBody;
    [TextArea(1, 3)] public string objectiveHint;

    private bool playerInside;

    private void Start()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        // UI 패널이 열려 있으면 E 상호작용을 차단한다.
        if (UIManager3D.Instance != null && UIManager3D.Instance.IsGameplayInputBlocked())
            return;

        if (playerInside && Input.GetKeyDown(KeyCode.E))
        {
            if (MemoryCinematicCamera3D.Instance != null)
                MemoryCinematicCamera3D.Instance.PlayLoreGlance(transform);
            if (UIManager3D.Instance != null)
                UIManager3D.Instance.ShowLore(terminalTitle, terminalBody, objectiveHint);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;
        if (UIManager3D.Instance != null)
            UIManager3D.Instance.ShowPrompt("E : 도시 기록 조사 - " + terminalTitle);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;
        if (UIManager3D.Instance != null)
            UIManager3D.Instance.HidePrompt();
    }
}
