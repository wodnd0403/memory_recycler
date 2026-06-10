using UnityEngine;
using UnityEngine.SceneManagement;

// 씬 파일을 수정하지 않고 발표 안정화용 런타임 가드를 부착하는 부트스트랩.
// 현재 역할: PlayBoundary3D가 씬에 없으면 자동 생성.
// 씬에 이미 존재하면 아무 것도 하지 않는다(중복 방지).
public static class MR3D_Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        EnsurePlayBoundary();
        PlayerMemoryLog3D.Ensure();
        EnsureArchiveVisual();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsurePlayBoundary();
        PlayerMemoryLog3D.Ensure();
        EnsureArchiveVisual();
    }

    // 중앙 아카이브 터미널 장식 비주얼을 씬 수정 없이 자동 생성한다(중복 방지).
    private static void EnsureArchiveVisual()
    {
        if (ArchiveTerminalVisual3D.Instance != null)
            return;
        if (Object.FindFirstObjectByType<ArchiveTerminalVisual3D>() != null)
            return;

        ArchiveTerminal3D terminal = Object.FindFirstObjectByType<ArchiveTerminal3D>();
        if (terminal == null)
            return;

        GameObject host = new GameObject("MR3D_ArchiveTerminalVisual");
        ArchiveTerminalVisual3D visual = host.AddComponent<ArchiveTerminalVisual3D>();
        visual.Initialize(terminal.transform);
    }

    private static void EnsurePlayBoundary()
    {
        if (PlayBoundary3D.Instance != null)
            return;

        // 씬에 이미 컴포넌트가 직접 배치돼 있으면 그것을 우선 사용.
        PlayBoundary3D existing = Object.FindFirstObjectByType<PlayBoundary3D>();
        if (existing != null)
            return;

        GameObject host = new GameObject("MR3D_PlayBoundary");
        // DontDestroyOnLoad는 사용하지 않는다 - 씬과 함께 정리되어 다음 진입에서 다시 생성된다.
        host.AddComponent<PlayBoundary3D>();
    }
}
