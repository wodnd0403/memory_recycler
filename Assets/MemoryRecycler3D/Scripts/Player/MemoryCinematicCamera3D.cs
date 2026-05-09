using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MemoryCinematicCamera3D : MonoBehaviour
{
    public static MemoryCinematicCamera3D Instance { get; private set; }

    [Header("References")]
    public OrbitCamera3D orbitCamera;
    public ThirdPersonPlayer3D playerController;
    public Transform playerTarget;

    [Header("Timing")]
    public float memoryMoveTime = 0.55f;
    public float memoryHoldTime = 0.45f;
    public float archiveMoveTime = 0.9f;
    public float archiveHoldTime = 0.85f;
    public float returnTime = 0.45f;

    [Header("Framing")]
    public float memoryDistance = 4.8f;
    public float memoryHeight = 2.4f;
    public float archiveDistance = 12f;
    public float archiveHeight = 7.2f;
    public float minimumCameraY = 0.85f;

    private Coroutine currentShot;
    private bool restoreOrbitEnabled = true;
    private bool restorePlayerEnabled = true;

    private void Awake()
    {
        Instance = this;

        if (orbitCamera == null)
            orbitCamera = GetComponent<OrbitCamera3D>();

        ResolvePlayerReferences();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayMemoryPickup(Transform focusTarget)
    {
        if (focusTarget == null)
            return;

        Vector3 focus = focusTarget.position + Vector3.up * 0.65f;
        Vector3 shotPosition = BuildShotPosition(focus, memoryDistance, memoryHeight, 0.35f);
        PlayShot(shotPosition, focus, memoryMoveTime, memoryHoldTime);
    }

    public void PlayArchiveReveal(Transform archiveTarget, bool locked)
    {
        if (archiveTarget == null)
            return;

        Vector3 focus = archiveTarget.position + Vector3.up * (locked ? 3.0f : 5.6f);
        Vector3 shotPosition = BuildShotPosition(focus, archiveDistance, archiveHeight, locked ? -0.4f : 1.6f);
        PlayShot(shotPosition, focus, archiveMoveTime, archiveHoldTime);
    }

    public void PlayLoreGlance(Transform loreTarget)
    {
        if (loreTarget == null)
            return;

        Vector3 focus = loreTarget.position + Vector3.up * 1.4f;
        Vector3 shotPosition = BuildShotPosition(focus, 5.6f, 2.6f, -0.25f);
        PlayShot(shotPosition, focus, memoryMoveTime, memoryHoldTime);
    }

    private void PlayShot(Vector3 shotPosition, Vector3 lookTarget, float moveTime, float holdTime)
    {
        if (currentShot != null)
        {
            StopCoroutine(currentShot);
            RestoreControlState();
        }

        currentShot = StartCoroutine(ShotRoutine(shotPosition, lookTarget, moveTime, holdTime));
    }

    private IEnumerator ShotRoutine(Vector3 shotPosition, Vector3 lookTarget, float moveTime, float holdTime)
    {
        ResolvePlayerReferences();

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation((lookTarget - shotPosition).normalized, Vector3.up);
        restoreOrbitEnabled = orbitCamera != null && orbitCamera.enabled;
        restorePlayerEnabled = playerController != null && playerController.enabled;

        if (orbitCamera != null)
            orbitCamera.enabled = false;
        if (playerController != null)
            playerController.enabled = false;

        yield return BlendCamera(startPosition, startRotation, shotPosition, targetRotation, moveTime);
        yield return new WaitForSeconds(holdTime);
        yield return BlendCamera(transform.position, transform.rotation, startPosition, startRotation, returnTime);

        RestoreControlState();

        currentShot = null;
    }

    private IEnumerator BlendCamera(Vector3 fromPosition, Quaternion fromRotation, Vector3 toPosition, Quaternion toRotation, float duration)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            Vector3 position = Vector3.Lerp(fromPosition, toPosition, t);
            position.y = Mathf.Max(position.y, minimumCameraY);
            transform.position = position;
            transform.rotation = Quaternion.Slerp(fromRotation, toRotation, t);
            yield return null;
        }

        toPosition.y = Mathf.Max(toPosition.y, minimumCameraY);
        transform.position = toPosition;
        transform.rotation = toRotation;
    }

    private Vector3 BuildShotPosition(Vector3 focus, float distance, float height, float sideOffset)
    {
        ResolvePlayerReferences();

        Vector3 fromPlayer = Vector3.back;
        if (playerTarget != null)
        {
            fromPlayer = focus - playerTarget.position;
            fromPlayer.y = 0f;
            if (fromPlayer.sqrMagnitude < 0.01f)
                fromPlayer = -playerTarget.forward;
        }

        fromPlayer.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, fromPlayer).normalized;
        Vector3 position = focus - fromPlayer * distance + side * sideOffset + Vector3.up * height;
        position.y = Mathf.Max(position.y, minimumCameraY);
        return position;
    }

    private void ResolvePlayerReferences()
    {
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTarget = player.transform;
        }

        if (playerController == null && playerTarget != null)
            playerController = playerTarget.GetComponent<ThirdPersonPlayer3D>();
    }

    private void RestoreControlState()
    {
        if (playerController != null)
            playerController.enabled = restorePlayerEnabled;
        if (orbitCamera != null)
            orbitCamera.enabled = restoreOrbitEnabled;
    }
}
