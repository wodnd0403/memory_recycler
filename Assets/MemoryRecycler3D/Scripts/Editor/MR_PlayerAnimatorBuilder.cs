#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// MR_Player.controller를 코드로 빌드한다.
// 구조: Locomotion(BlendTree: Idle ↔ Walk ↔ Run, MoveSpeed로 보간) + Jump + Falling.
// Docs/07 §15 — MR_Player_Running 추가로 Walk/Run 실제 클립 사용.
public static class MR_PlayerAnimatorBuilder
{
    private const string MixamoFolder = "Assets/MemoryRecycler3D/ExternalAssets/Mixamo/Player";
    private const string ControllerFolder = "Assets/MemoryRecycler3D/Animations/Player";
    private const string ControllerPath = ControllerFolder + "/MR_Player.controller";

    [MenuItem("Tools/Memory Recycler 3D/Advanced/Build MR_Player Animator Controller")]
    public static AnimatorController BuildController()
    {
        EnsureFolder("Assets/MemoryRecycler3D/Animations");
        EnsureFolder(ControllerFolder);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        EnsureParameter(controller, "MoveSpeed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "Forward", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "Side", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "IsRunning", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "VerticalSpeed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "JumpTrigger", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine root = controller.layers[0].stateMachine;
        ClearStateMachine(root);

        AnimationClip idleClip = LoadAnimationClip("MR_Player_Idle");
        AnimationClip walkClip = LoadAnimationClip("MR_Player_Walking");
        AnimationClip runClip = LoadAnimationClip("MR_Player_Running");
        AnimationClip jumpClip = LoadAnimationClip("MR_Player_Jumping");
        AnimationClip fallClip = jumpClip;

        // Locomotion BlendTree: MoveSpeed 0=Idle, 0.5=Walk, 1=Run.
        BlendTree locomotionTree = new BlendTree();
        locomotionTree.name = "Locomotion";
        locomotionTree.blendType = BlendTreeType.Simple1D;
        locomotionTree.blendParameter = "MoveSpeed";
        locomotionTree.useAutomaticThresholds = false;
        AssetDatabase.AddObjectToAsset(locomotionTree, controller);
        locomotionTree.AddChild(idleClip, 0f);
        locomotionTree.AddChild(walkClip, 0.5f);
        if (runClip != null)
            locomotionTree.AddChild(runClip, 1f);

        AnimatorState locomotion = root.AddState("Locomotion", new Vector3(260f, 100f, 0f));
        locomotion.motion = locomotionTree;
        locomotion.writeDefaultValues = true;

        AnimatorState jump = root.AddState("Jump", new Vector3(520f, 200f, 0f));
        jump.motion = jumpClip;

        AnimatorState fall = root.AddState("Falling", new Vector3(260f, 300f, 0f));
        fall.motion = fallClip;
        fall.speed = 0.75f;

        root.defaultState = locomotion;

        // Locomotion → Jump (트리거)
        AnimatorStateTransition locoToJump = locomotion.AddTransition(jump);
        ConfigureTransition(locoToJump, 0.05f);
        locoToJump.AddCondition(AnimatorConditionMode.If, 0f, "JumpTrigger");

        // Jump → Falling (하강 시작)
        AnimatorStateTransition jumpToFall = jump.AddTransition(fall);
        ConfigureTransition(jumpToFall, 0.10f);
        jumpToFall.AddCondition(AnimatorConditionMode.Less, 0f, "VerticalSpeed");
        jumpToFall.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");

        // Falling → Locomotion (착지)
        AnimatorStateTransition fallToLoco = fall.AddTransition(locomotion);
        ConfigureTransition(fallToLoco, 0.12f);
        fallToLoco.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");

        // Jump → Locomotion (혹시 점프 클립 끝나기 전에 접지)
        AnimatorStateTransition jumpToLoco = jump.AddTransition(locomotion);
        ConfigureTransition(jumpToLoco, 0.15f);
        jumpToLoco.hasExitTime = true;
        jumpToLoco.exitTime = 0.85f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[MR_PlayerAnimatorBuilder] " + ControllerPath + " 빌드 완료 (BlendTree Idle/Walk/Run + Jump/Falling)");
        return controller;
    }

    private static void ClearStateMachine(AnimatorStateMachine sm)
    {
        var children = sm.states;
        for (int i = 0; i < children.Length; i++)
            sm.RemoveState(children[i].state);

        var anyTransitions = sm.anyStateTransitions;
        for (int i = 0; i < anyTransitions.Length; i++)
            sm.RemoveAnyStateTransition(anyTransitions[i]);

        var entryTransitions = sm.entryTransitions;
        for (int i = 0; i < entryTransitions.Length; i++)
            sm.RemoveEntryTransition(entryTransitions[i]);

        // 이전에 추가했던 sub-asset(BlendTree)도 정리.
        Object[] subs = AssetDatabase.LoadAllAssetRepresentationsAtPath(ControllerPath);
        for (int i = 0; i < subs.Length; i++)
        {
            if (subs[i] is BlendTree)
                Object.DestroyImmediate(subs[i], true);
        }
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        var parameters = controller.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == name)
                return;
        }
        controller.AddParameter(name, type);
    }

    private static void ConfigureTransition(AnimatorStateTransition transition, float duration)
    {
        transition.hasExitTime = false;
        transition.exitTime = 0f;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
    }

    private static AnimationClip LoadAnimationClip(string baseName)
    {
        string fbxPath = MixamoFolder + "/" + baseName + ".fbx";
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        for (int i = 0; i < subAssets.Length; i++)
        {
            AnimationClip clip = subAssets[i] as AnimationClip;
            if (clip == null)
                continue;
            if (clip.name.StartsWith("__preview__"))
                continue;
            return clip;
        }
        Debug.LogWarning("[MR_PlayerAnimatorBuilder] AnimationClip 누락: " + fbxPath);
        return null;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(folder);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
