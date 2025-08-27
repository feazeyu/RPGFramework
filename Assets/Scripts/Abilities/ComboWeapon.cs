using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.Abilities
{
    [Serializable]
    public class ComboWeapon : MonoBehaviour
    {
        public Combo currentCombo;
        public int currentStep = 0;
        private float lastAttackTime = 0f;   
        public Animator animator;

        public float ComboCancelTime
        {
            get
            {
                if (currentStep > 0 && currentCombo != null && currentCombo.steps.Count >= currentStep)
                {
                    return currentCombo.steps[currentStep - 1].comboCancelTime;
                }
                return 0f;
            }
        }

        private void Update()
        {
            if (currentStep > 0 && Time.time > lastAttackTime + ComboCancelTime)
            {
                CancelCombo();
            }
        }

        public void StartAttack()
        {
            // Call this when an attack starts
            lastAttackTime = Time.time;
        }

        private void CancelCombo()
        {
            currentStep = 0;
            Debug.Log("Combo canceled.");
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
            foreach (var step in combo.steps)
            {
                if (step.motion != null)
                {
                    var state = rootStateMachine.AddState(step.motion.name);
                    state.motion = step.motion;
                }
            }

            Animator animator = comboWeapon.GetComponent<Animator>();
            if (animator != null)
            {
                animator.runtimeAnimatorController = animatorController;
                Debug.Log($"[{comboWeapon.name}] Animator assigned.");
            }
            else
            {
                Debug.LogWarning($"[{comboWeapon.name}] No Animator component found.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
