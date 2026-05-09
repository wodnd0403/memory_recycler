using UnityEngine;

public class DismissOnEnter3D : MonoBehaviour
{
    public GameObject target;

    private void Awake()
    {
        if (target == null)
            target = gameObject;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            target.SetActive(false);
    }
}
