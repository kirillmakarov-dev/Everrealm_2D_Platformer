#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LetterHunter.Editor
{
    public static class PlayerAnimationControllerBuilder
    {
        private const string ControllerPath = "Assets/Player model/Walking.controller";
        private const string Folder = "Assets/Player model/";
        private const string StateParameter = "CharacterState";
        private const string SpeedParameter = "MoveSpeed";
        private const string DeadParameter = "IsDead";

        [MenuItem("Tools/Everrealm/Build Player Animator")]
        public static void Build()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) { Debug.LogError($"Missing controller: {ControllerPath}"); return; }

            var idle = Load("Idle.anim");
            var walk = Load("Walk.anim");
            var run = Load("Run.anim");
            var jump = Load("Jump.anim");
            var fall = Load("Fall.anim");
            var shooting = Load("Shooting.anim");
            var death = Load("Death.anim");
            if (idle == null || walk == null || run == null || jump == null || fall == null || shooting == null || death == null) return;

            EnsureParameter(controller, StateParameter, AnimatorControllerParameterType.Int);
            EnsureParameter(controller, SpeedParameter, AnimatorControllerParameterType.Float);
            EnsureParameter(controller, DeadParameter, AnimatorControllerParameterType.Bool);

            var machine = controller.layers[0].stateMachine;
            foreach (var child in machine.states) machine.RemoveState(child.state);
            RemoveOldBlendTrees(controller);

            var locomotionTree = new BlendTree
            {
                name = "Locomotion Blend Tree",
                blendType = BlendTreeType.Simple1D,
                blendParameter = SpeedParameter,
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy
            };
            locomotionTree.AddChild(idle, 0f);
            locomotionTree.AddChild(walk, .5f);
            locomotionTree.AddChild(run, 1f);
            AssetDatabase.AddObjectToAsset(locomotionTree, controller);

            var locomotion = AddState(machine, "Blend Tree", locomotionTree, new Vector3(320, 0));
            var jumpState = AddState(machine, "Jump", jump, new Vector3(580, -120));
            var fallState = AddState(machine, "Fall", fall, new Vector3(580, 0));
            var shootingState = AddState(machine, "Shooting", shooting, new Vector3(580, 120));
            var deathState = AddState(machine, "Death", death, new Vector3(800, 120));
            machine.defaultState = locomotion;

            Add(locomotion, jumpState, 0f, Condition(StateParameter, AnimatorConditionMode.Equals, 2));
            Add(locomotion, fallState, 0f, Condition(StateParameter, AnimatorConditionMode.Equals, 3));
            Add(locomotion, shootingState, 0f, Condition(StateParameter, AnimatorConditionMode.Equals, 4));
            Add(locomotion, deathState, 0f, Condition(DeadParameter, AnimatorConditionMode.If));

            // Physics owns ascent/descent; clip exit time must not start a fall early.
            Add(jumpState, fallState, .08f, Condition(StateParameter, AnimatorConditionMode.Equals, 3));
            Add(jumpState, locomotion, .04f, Condition(StateParameter, AnimatorConditionMode.Equals, 0));
            Add(jumpState, shootingState, 0f, Condition(StateParameter, AnimatorConditionMode.Equals, 4));
            Add(jumpState, deathState, 0f, Condition(DeadParameter, AnimatorConditionMode.If));

            Add(fallState, locomotion, .04f, Condition(StateParameter, AnimatorConditionMode.Equals, 0));
            Add(fallState, locomotion, .04f, Condition(StateParameter, AnimatorConditionMode.Equals, 1));
            Add(fallState, jumpState, .04f, Condition(StateParameter, AnimatorConditionMode.Equals, 2));
            Add(fallState, shootingState, 0f, Condition(StateParameter, AnimatorConditionMode.Equals, 4));
            Add(fallState, deathState, 0f, Condition(DeadParameter, AnimatorConditionMode.If));

            Add(shootingState, locomotion, .1f, true, .8f, Condition(StateParameter, AnimatorConditionMode.Equals, 0));
            Add(shootingState, jumpState, .08f, true, .8f, Condition(StateParameter, AnimatorConditionMode.Equals, 2));
            Add(shootingState, fallState, .08f, true, .8f, Condition(StateParameter, AnimatorConditionMode.Equals, 3));
            Add(shootingState, deathState, 0f, Condition(DeadParameter, AnimatorConditionMode.If));

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("Everrealm Animator built with a 1D MoveSpeed Blend Tree: Idle 0, Walk 0.5, Run 1.", controller);
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion, Vector3 position)
        {
            var state = machine.AddState(name, position);
            state.motion = motion;
            state.writeDefaultValues = false;
            return state;
        }

        private static void Add(AnimatorState from, AnimatorState to, float duration, params AnimatorCondition[] conditions) => Add(from, to, duration, false, 0f, conditions);

        private static void Add(AnimatorState from, AnimatorState to, float duration, bool hasExitTime, float exitTime, params AnimatorCondition[] conditions)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = hasExitTime;
            transition.exitTime = exitTime;
            transition.duration = duration;
            transition.interruptionSource = TransitionInterruptionSource.None;
            foreach (var condition in conditions) transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
        }

        private static AnimatorCondition Condition(string parameter, AnimatorConditionMode mode, float threshold = 0f) =>
            new AnimatorCondition { parameter = parameter, mode = mode, threshold = threshold };

        private static AnimationClip Load(string fileName)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + fileName);
            if (clip == null) Debug.LogError($"Missing animation clip: {Folder + fileName}");
            return clip;
        }

        private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (var parameter in controller.parameters) if (parameter.name == name) return;
            controller.AddParameter(name, type);
        }

        private static void RemoveOldBlendTrees(AnimatorController controller)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))
            {
                if (asset is not BlendTree tree) continue;
                AssetDatabase.RemoveObjectFromAsset(tree);
                Object.DestroyImmediate(tree, true);
            }
        }
    }
}
#endif
