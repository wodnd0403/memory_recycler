using UnityEngine;

public class OrbitCamera3D : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 3.5f, -6f);
    public float mouseSensitivity = 1.55f;
    public float minPitch = -75f;
    public float maxPitch = 85f;
    public float baseLookHeight = 1.45f;
    public float lookUpHeight = 5.2f;
    public float lookDownHeight = 1.05f;
    public float followSmooth = 12f;
    public float targetFollowSmooth = 18f;
    public float verticalFollowSmooth = 4.2f;
    public float jumpVerticalFollowSmooth = 7.5f;
    public float verticalDeadZone = 0.72f;
    public float verticalSnapDistance = 3.0f;
    public float minCameraHeightAboveTarget = 0.25f;
    public float absoluteMinCameraY = 0.35f;

    [Header("Collision")]
    // 카메라가 벽/건물에 파묻히지 않도록 충돌 검사용 LayerMask. 기본값(~0)은 모든 레이어 포함.
    public LayerMask collisionMask = ~0;
    public float collisionRadius = 0.32f;
    public float collisionPadding = 0.18f;
    public float minimumCollisionDistance = 2.65f;
    public float collisionRecoverSmooth = 14f;
    public bool ignoreTargetCollision = true;

    private float yaw;
    private float pitch = 20f;
    private float currentDistanceRatio = 1f;
    private bool hasSmoothedPivot;
    private Vector3 smoothedPivot;
    private readonly RaycastHit[] cameraHits = new RaycastHit[16];
    private bool pitchClampLogged;

    private void Start()
    {
        LogPitchClamp();

        // 시작 메뉴가 떠 있는 동안 마우스를 잠그면 클릭이 불가능해진다.
        // UIManager3D가 메뉴 닫힐 때 LockCursor()를 호출하므로 여기서는 강제 잠금하지 않는다.
        if (UIManager3D.Instance != null && UIManager3D.Instance.IsGameplayInputBlocked())
            return;

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
        // 상체 높이의 고정 기준점을 중심으로 돌게 해서 벽 근처에서도 시점 높이가 튀지 않게 한다.
        float aimHeight = Mathf.Max(0.1f, baseLookHeight);
        Vector3 targetPivot = target.position + Vector3.up * aimHeight;
        if (!hasSmoothedPivot)
        {
            smoothedPivot = targetPivot;
            hasSmoothedPivot = true;
        }
        else
        {
            smoothedPivot.x = Mathf.Lerp(smoothedPivot.x, targetPivot.x, Damp(targetFollowSmooth));
            smoothedPivot.z = Mathf.Lerp(smoothedPivot.z, targetPivot.z, Damp(targetFollowSmooth));
            smoothedPivot.y = SmoothVerticalPivot(smoothedPivot.y, targetPivot.y);
        }

        Vector3 pivot = smoothedPivot;
        Vector3 rawDesiredPosition = pivot + rotation * offset;

        // pivot → 원하는 카메라 위치 사이에 벽이 있으면 SphereCast로 안전한 거리로 당겨준다.
        Vector3 direction = rawDesiredPosition - pivot;
        float fullDistance = direction.magnitude;
        float targetRatio = 1f;
        if (fullDistance > 0.001f)
        {
            Vector3 dirNorm = direction / fullDistance;
            float hitDistance;
            if (TryGetNearestCameraHit(pivot, dirNorm, fullDistance + collisionPadding, out hitDistance))
            {
                float safeDistance = Mathf.Max(minimumCollisionDistance, hitDistance - collisionPadding);
                targetRatio = Mathf.Clamp01(safeDistance / fullDistance);
            }
        }

        // 벽에 막혔다 풀릴 때 자연스럽게 복귀하도록 보간.
        currentDistanceRatio = Mathf.Lerp(currentDistanceRatio, targetRatio,
            Damp(targetRatio < currentDistanceRatio ? 30f : collisionRecoverSmooth));

        Vector3 desiredPosition = pivot + direction * currentDistanceRatio;
        float smoothedTargetY = pivot.y - aimHeight;
        float minCameraY = Mathf.Max(absoluteMinCameraY, smoothedTargetY + minCameraHeightAboveTarget);
        desiredPosition.y = Mathf.Max(desiredPosition.y, minCameraY);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, Damp(followSmooth));
        Vector3 clampedPosition = transform.position;
        clampedPosition.y = Mathf.Max(clampedPosition.y, minCameraY);
        transform.position = clampedPosition;
        transform.LookAt(pivot);
    }

    private float SmoothVerticalPivot(float currentY, float targetY)
    {
        float delta = targetY - currentY;
        float deadZone = Mathf.Max(0f, verticalDeadZone);
        if (Mathf.Abs(delta) <= deadZone)
            return currentY;

        float goalY = targetY - Mathf.Sign(delta) * deadZone;
        float smooth = Mathf.Abs(delta) >= verticalSnapDistance ? jumpVerticalFollowSmooth : verticalFollowSmooth;
        return Mathf.Lerp(currentY, goalY, Damp(smooth));
    }

    private bool TryGetNearestCameraHit(Vector3 origin, Vector3 direction, float distance, out float hitDistance)
    {
        hitDistance = 0f;
        int hitCount = Physics.SphereCastNonAlloc(origin, collisionRadius, direction, cameraHits, distance, collisionMask, QueryTriggerInteraction.Ignore);
        bool found = false;
        float nearest = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = cameraHits[i];
            if (hit.collider == null || ShouldIgnoreCollisionHit(hit.collider.transform))
                continue;

            if (hit.distance < nearest)
            {
                nearest = hit.distance;
                found = true;
            }
        }

        if (!found)
            return false;

        hitDistance = nearest;
        return true;
    }

    private bool ShouldIgnoreCollisionHit(Transform hitTransform)
    {
        if (!ignoreTargetCollision || target == null || hitTransform == null)
            return false;

        return hitTransform == target || hitTransform.IsChildOf(target) || target.IsChildOf(hitTransform);
    }

    private float Damp(float smooth)
    {
        return 1f - Mathf.Exp(-Mathf.Max(0f, smooth) * Time.deltaTime);
    }

    private void LogPitchClamp()
    {
        if (pitchClampLogged)
            return;

        pitchClampLogged = true;
        Debug.Log("[MR3D Camera] pitch clamp range min=" + minPitch.ToString("0.0") +
            " max=" + maxPitch.ToString("0.0") +
            " current=" + pitch.ToString("0.0"));
    }
}
