using System;
using System.Collections.Generic;
using UnityEngine;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Holds the master (authored) list of BlackboardVariables for a DialogueGraphAsset.
    /// At runtime a runner builds its own working copy via <see cref="CloneForRuntime"/>:
    /// non-shared variables are deep-cloned per runner, Shared variables resolve to a
    /// global instance in <see cref="SharedBlackboardStore"/>. The authored values here
    /// are never mutated at runtime — they are the defaults restored when play mode ends.
    /// </summary>
    [Serializable]
    public class Blackboard
    {
        [SerializeReference]
        private List<BlackboardVariable> m_Variables = new List<BlackboardVariable>();

        /// <summary>Variables.</summary>
        public IReadOnlyList<BlackboardVariable> Variables => m_Variables;


        /// <summary>Add variable.</summary>
        public void AddVariable(BlackboardVariable variable)
        {
            if (string.IsNullOrEmpty(variable.Guid))
                variable.Guid = System.Guid.NewGuid().ToString();
            m_Variables.Add(variable);
        }

        /// <summary>Remove variable.</summary>
        public bool RemoveVariable(string guid)
        {
            int idx = m_Variables.FindIndex(v => v.Guid == guid);
            if (idx < 0) return false;
            m_Variables.RemoveAt(idx);
            return true;
        }

        /// <summary>Get variable.</summary>
        public BlackboardVariable GetVariable(string guid)
            => m_Variables.Find(v => v.Guid == guid);

        /// <summary>Guid.</summary>
        public BlackboardVariable<T> GetVariable<T>(string guid)
            => GetVariable(guid) as BlackboardVariable<T>;

        /// <summary>T.</summary>
        public bool TryGetValue<T>(string guid, out T value)
        {
            var v = GetVariable<T>(guid);
            if (v != null) { value = v.Value; return true; }
            value = default;
            return false;
        }

        /// <summary>T.</summary>
        public bool SetValue<T>(string guid, T value)
        {
            var v = GetVariable<T>(guid);
            if (v == null) return false;
            v.Value = value;
            return true;
        }


        /// <summary>
        /// Builds the working blackboard for a single runner from these authored values.
        ///
        /// Non-shared variables are deep-cloned, giving the runner its own isolated
        /// copy. A runner keeps this copy for its whole lifetime, so a non-shared value
        /// set during one dialogue persists into later dialogues with the same runner;
        /// it resets to the authored default only when the runner (and play mode) ends.
        ///
        /// Shared variables resolve to a single global instance per Guid via
        /// <see cref="SharedBlackboardStore"/>, so all runners read and write the same
        /// value across dialogues.
        ///
        /// In neither case is the authored asset variable mutated.
        /// </summary>
        public Blackboard CloneForRuntime()
        {
            var clone = new Blackboard();
            foreach (var v in m_Variables)
            {
                clone.m_Variables.Add(v.Shared
                    ? SharedBlackboardStore.Resolve(v)
                    : v.Clone());
            }
            return clone;
        }
    }
}
