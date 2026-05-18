#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

// Mixamo에서 받은 MR_Player_*.fbx 5종에 대해 Humanoid + Avatar 설정을 자동 적용한다.
// Docs/07 §15 — 제출 안정화: TPose는 Avatar 생성, 나머지는 TPose Avatar를 복사한다.
public class MR_PlayerModelPostprocessor : AssetPostprocessor
{
    private const string TargetFolder = "Assets/MemoryRecycler3D/ExternalAssets/Mixamo/Player";
    private const string TPoseFileName = "MR_Player_TPose.fbx";
    private const string TPoseAssetPath = TargetFolder + "/" + TPoseFileName;

    private static readonly string[] LoopingClipNames = { "MR_Player_Idle", "MR_Player_Walking", "MR_Player_Running" };

    private void OnPreprocessModel()
    {
        if (!IsTargetAsset(assetPath))
            return;

        ModelImporter importer = assetImporter as ModelImporter;
        if (importer == null)
            return;

        importer.animationType = ModelImporterAnimationType.Human;
        importer.importBlendShapes = true;
        importer.optimizeGameObjects = false;
        importer.importVisibility = true;
        importer.importCameras = false;
        importer.importLights = false;

        string fileName = Path.GetFileName(assetPath);
        bool isTPose = string.Equals(fileName, TPoseFileName, System.StringComparison.OrdinalIgnoreCase);

        if (isTPose)
        {
            // TPose는 자신의 Avatar를 생성한다.
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        }
        else
        {
            // 나머지 애니메이션 FBX는 TPose Avatar를 복사해 사용한다.
            Avatar sourceAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(TPoseAssetPath);
            if (sourceAvatar != null)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = sourceAvatar;
            }
            else
            {
                // TPose Avatar가 아직 임포트되지 않았다면 일단 자체 생성으로 두고,
                // TPose 임포트 완료 후 본 파일들을 재임포트하면 자동으로 Copy로 갱신된다.
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            }
        }
    }

    private void OnPostprocessModel(GameObject gameObject)
    {
        if (!IsTargetAsset(assetPath))
            return;

        ModelImporter importer = assetImporter as ModelImporter;
        if (importer == null)
            return;

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        if (clips == null || clips.Length == 0)
            return;

        bool changed = false;
        string baseName = Path.GetFileNameWithoutExtension(assetPath);

        for (int i = 0; i < clips.Length; i++)
        {
            ModelImporterClipAnimation clip = clips[i];
            bool wantsLoop = ClipShouldLoop(baseName);

            if (clip.loopTime != wantsLoop)
            {
                clip.loopTime = wantsLoop;
                changed = true;
            }

            if (clip.loopPose != wantsLoop)
            {
                clip.loopPose = wantsLoop;
                changed = true;
            }

            // Walking/Running은 코드 이동과 겹치지 않도록 XZ 루트 모션을 Bake Into Pose 한다.
            bool bakeIntoPoseXZ = !baseName.Equals("MR_Player_TPose", System.StringComparison.OrdinalIgnoreCase);
            if (clip.lockRootPositionXZ != bakeIntoPoseXZ)
            {
                clip.lockRootPositionXZ = bakeIntoPoseXZ;
                changed = true;
            }

            // Y(점프 높이)는 점프 클립에서는 살려두고, 그 외에는 잠근다.
            bool keepY = false;
            if (clip.lockRootHeightY != !keepY)
            {
                clip.lockRootHeightY = !keepY;
                changed = true;
            }

            if (clip.keepOriginalPositionXZ)
            {
                clip.keepOriginalPositionXZ = false;
                changed = true;
            }

            if (clip.keepOriginalPositionY)
            {
                clip.keepOriginalPositionY = false;
                changed = true;
            }

            if (clip.keepOriginalOrientation)
            {
                clip.keepOriginalOrientation = false;
                changed = true;
            }

            if (clip.heightFromFeet)
            {
                clip.heightFromFeet = false;
                changed = true;
            }

            // 회전은 코드가 처리하므로 항상 잠근다.
            if (!clip.lockRootRotation)
            {
                clip.lockRootRotation = true;
                changed = true;
            }

            clips[i] = clip;
        }

        if (changed)
            importer.clipAnimations = clips;
    }

    private static bool ClipShouldLoop(string baseName)
    {
        for (int i = 0; i < LoopingClipNames.Length; i++)
        {
            if (baseName.Equals(LoopingClipNames[i], System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static bool IsTargetAsset(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        string normalized = path.Replace('\\', '/');
        if (!normalized.StartsWith(TargetFolder, System.StringComparison.OrdinalIgnoreCase))
            return false;

        return normalized.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);
    }

    // 메뉴: TPose가 먼저 임포트된 뒤 나머지 클립이 Avatar를 복사하도록 강제 재임포트한다.
    [MenuItem("Tools/Memory Recycler 3D/Reimport MR_Player FBX Set")]
    private static void ReimportMixamoSet()
    {
        // 1) TPose 먼저 임포트해 Avatar 생성
        AssetDatabase.ImportAsset(TPoseAssetPath, ImportAssetOptions.ForceUpdate);

        // 2) 나머지 클립 임포트
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { TargetFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (path.EndsWith(TPoseFileName, System.StringComparison.OrdinalIgnoreCase))
                continue;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        AssetDatabase.Refresh();
        Debug.Log("[MR_PlayerModelPostprocessor] MR_Player FBX 5종 재임포트 완료");
    }
}
#endif
