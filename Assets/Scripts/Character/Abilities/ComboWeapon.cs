using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.Character
{
    [Serializable]
    public class ComboWeapon : Weapon
    {
        private bool triedToGetOffsetController = false;
        private WeaponOffsetController offsetController = null;
        public Combo currentCombo;
        private bool recovering = false;
        [SerializeField]
        private int currentStep;
        public int CurrentStep
        {
            get => currentStep;
            set
            {
                currentStep = value;
                UpdateAnimatorParameters();
            }
        }
        private float lastAttackEndTime = 0f;
        public Animator animator;

        public float ComboCancelTime
        {
            get
            {
                if (CurrentStep > 0 && currentCombo != null && currentCombo.steps.Count >= CurrentStep)
                {
                    return currentCombo.steps[CurrentStep - 1].comboCancelTime;
                }
                return 0f;
            }
        }

        private void Update()
        {
            if (CurrentStep > 0 && Time.time > lastAttackEndTime + ComboCancelTime)
            {
                CancelCombo();
            }
        }

        public void OnDestroy()
        {
            if (offsetController != null)
            {
                offsetController.onRecovered -= () => { recovering = false; };
            }
        }

        public override void Attack()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            //If step is 0, we can start the combo unless recovering
            if (currentStep != 0 || recovering)
                // Shouldn't attack if the character is mid animation, or if we've reached the end of the combo, or is recovering
                if ((animator.IsInTransition(0) ||
               (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f &&
                animator.GetCurrentAnimatorStateInfo(0).length > 0) ||
                currentStep == currentCombo.steps.Count ||
                recovering)
                )
            {
                return;
            }

            // Increment CurrentStep if not playing
            if (currentCombo != null && CurrentStep < currentCombo.steps.Count)
            {
                CurrentStep++;
            }
            lastAttackEndTime = Time.time + animator.GetCurrentAnimatorStateInfo(0).length;
        }

        private void CancelCombo()
        {
            CurrentStep = 0;
            if (!triedToGetOffsetController && offsetController == null)
            {
                triedToGetOffsetController = true;
                offsetController = GetComponent<WeaponOffsetController>();
                if (offsetController != null)
                {
                    offsetController.onRecovered += () => { recovering = false; };
                }
            }
            if (offsetController != null)
            {
                offsetController.BeginRecovery();
                recovering = true;
            }
            UpdateAnimatorParameters();
        }
        private void UpdateAnimatorParameters()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            animator.SetInteger("currentStep", CurrentStep);
        }
    }

    [CustomEditor(typeof(ComboWeapon))]
    [CanEditMultipleObjects]
    internal sealed class ComboWeaponEditor : Editor
    {
        private SerializedProperty currentComboProp;
        private SerializedProperty currentStepProp;
        private SerializedProperty animatorProp;

        private void OnEnable()
        {
            currentComboProp = serializedObject.FindProperty("currentCombo");
            currentStepProp = serializedObject.FindProperty("currentStep");
            animatorProp = serializedObject.FindProperty("animator");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(currentComboProp);
            EditorGUILayout.PropertyField(currentStepProp);
            EditorGUILayout.PropertyField(animatorProp);

            if (GUILayout.Button("Generate Animator from ComboSteps"))
            {
                foreach (UnityEngine.Object obj in targets)
                {
                    GenerateAnimator((ComboWeapon)obj);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }


        private void GenerateAnimator(ComboWeapon comboWeapon)
        {
            var combo = comboWeapon.currentCombo;
            if (combo == null || combo.steps == null || combo.steps.Count == 0)
            {
                Debug.LogWarning($"[{comboWeapon.name}] Combo or ComboSteps are missing.");
                return;
            }

            string folderPath = "Assets/GeneratedAnimators";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "GeneratedAnimators");
            }

            string path = $"{folderPath}/{combo.name}_Animator.controller";
            AnimatorController animatorController = AnimatorController.CreateAnimatorControllerAtPath(path);

            var rootStateMachine = animatorController.layers[0].stateMachine;

            // Add idle state 
            var idleState = rootStateMachine.AddState("Idle");
            rootStateMachine.defaultState = idleState;

            // Add currentStep parameter
            animatorController.AddParameter("currentStep", AnimatorControllerParameterType.Int);

            // Create states for each combo step
            var stepStates = new List<AnimatorState>();
            for (int i = 0; i < combo.steps.Count; i++)
            {
                var step = combo.steps[i];
                if (step.motion != null)
                {
                    var state = rootStateMachine.AddState(step.motion.name);
                    state.motion = step.motion;
                    stepStates.Add(state);
                }
                else
                {
                    stepStates.Add(null);
                }
            }

            // Add transitions between states based on currentStep
            for (int i = 0; i < stepStates.Count; i++)
            {
                var state = stepStates[i];
                if (state == null) continue;

                // Transition from Idle to first step
                if (i == 0)
                {
                    var trans = idleState.AddTransition(state);
                    trans.hasExitTime = false;
                    trans.AddCondition(AnimatorConditionMode.Equals, 1, "currentStep");
                }
                // Transition from previous step to current step
                if (i > 0 && stepStates[i - 1] != null)
                {
                    var trans = stepStates[i - 1].AddTransition(state);
                    trans.hasExitTime = false;
                    trans.AddCondition(AnimatorConditionMode.Equals, i + 1, "currentStep");
                }
            }

            // Transition from any step back to Idle if currentStep == 0
            foreach (var state in stepStates)
            {
                if (state == null) continue;
                var trans = state.AddTransition(idleState);
                trans.hasExitTime = false;
                trans.AddCondition(AnimatorConditionMode.Equals, 0, "currentStep");
            }

            Animator animator = comboWeapon.GetComponent<Animator>();
            if (animator == null)
            {
                animator = comboWeapon.gameObject.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = animatorController;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
