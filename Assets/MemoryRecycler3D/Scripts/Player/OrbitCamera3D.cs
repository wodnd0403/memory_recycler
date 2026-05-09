using UnityEngine;

public class OrbitCamera3D : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 3.5f, -6f);
    public float mouseSensitivity = 1.55f;
    public float minPitch = -35f;
    public float maxPitch = 70f;
    public float baseLookHeight = 1.45f;
    public float lookUpHeight = 5.2f;
    public float lookDownHeight = 1.05f;
    public float followSmooth = 12f;
    public float minCameraHeightAboveTarget = 0.8f;
    public float absoluteMinCameraY = 0.55f;

    private float yaw;
    private float pitch = 20f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = target.position + rotation * offset;
        float minCameraY = Mathf.Max(absoluteMinCameraY, target.position.y + minCameraHeightAboveTarget);
        desiredPosition.y = Mathf.Max(desiredPosition.y, minCameraY);

        float lookUpAmount = Mathf.InverseLerp(maxPitch, minPitch, pitch);
        float lookHeight = Mathf.Lerp(lookDownHeight, lookUpHeight, lookUpAmount);
        lookHeight = Mathf.Max(lookHeight, baseLookHeight);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSmooth * Time.deltaTime);
        Vector3 clampedPosition = transform.position;
        clampedPosition.y = Mathf.Max(clampedPosition.y, minCameraY);
        transform.position = clampedPosition;
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }
}
