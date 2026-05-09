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
        if (playerInside && Input.GetKeyDown(KeyCode.E))
            UIManager3D.Instance.ShowLore(terminalTitle, terminalBody, objectiveHint);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;
        UIManager3D.Instance.ShowPrompt("E : 도시 기록 조사 - " + terminalTitle);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;
        UIManager3D.Instance.HidePrompt();
    }
}
