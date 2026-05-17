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

    [Header("Collision")]
    // 카메라가 벽/건물에 파묻히지 않도록 충돌 검사용 LayerMask. 기본값(~0)은 모든 레이어 포함.
    public LayerMask collisionMask = ~0;
    public float collisionRadius = 0.32f;
    public float collisionPadding = 0.18f;
    public float collisionRecoverSmooth = 14f;

    private float yaw;
    private float pitch = 20f;
    private float currentDistanceRatio = 1f;

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
        // target.position에서 카메라까지의 광선을 기준으로 충돌 체크하기 위해 LookAt 기준점을 미리 계산한다.
        float lookUpAmount = Mathf.InverseLerp(maxPitch, minPitch, pitch);
        float lookHeight = Mathf.Lerp(lookDownHeight, lookUpHeight, lookUpAmount);
        lookHeight = Mathf.Max(lookHeight, baseLookHeight);
        Vector3 pivot = target.position + Vector3.up * Mathf.Min(lookHeight * 0.5f, 1.6f);
        Vector3 rawDesiredPosition = pivot + rotation * offset;

        // pivot → 원하는 카메라 위치 사이에 벽이 있으면 SphereCast로 안전한 거리로 당겨준다.
        Vector3 direction = rawDesiredPosition - pivot;
        float fullDistance = direction.magnitude;
        float targetRatio = 1f;
        if (fullDistance > 0.001f)
        {
            Vector3 dirNorm = direction / fullDistance;
            RaycastHit hit;
            if (Physics.SphereCast(pivot, collisionRadius, dirNorm, out hit, fullDistance + collisionPadding, collisionMask, QueryTriggerInteraction.Ignore))
            {
                float safeDistance = Mathf.Max(0.1f, hit.distance - collisionPadding);
                targetRatio = Mathf.Clamp01(safeDistance / fullDistance);
            }
        }

        // 벽에 막혔다 풀릴 때 자연스럽게 복귀하도록 보간.
        currentDistanceRatio = Mathf.Lerp(currentDistanceRatio, targetRatio,
            (targetRatio < currentDistanceRatio ? 30f : collisionRecoverSmooth) * Time.deltaTime);

        Vector3 desiredPosition = pivot + direction * currentDistanceRatio;
        float minCameraY = Mathf.Max(absoluteMinCameraY, target.position.y + minCameraHeightAboveTarget);
        desiredPosition.y = Mathf.Max(desiredPosition.y, minCameraY);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSmooth * Time.deltaTime);
        Vector3 clampedPosition = transform.position;
        clampedPosition.y = Mathf.Max(clampedPosition.y, minCameraY);
        transform.position = clampedPosition;
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }
}
