using System;
using System.Collections.Generic;
using UnityEngine;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Holds the master list of BlackboardVariables for a DialogueGraphAsset.
    /// At runtime, DialogueGraphAgent clones this for per-agent isolation,
    /// except for Shared variables which stay on the source asset.
    /// </summary>
    [Serializable]
    public class Blackboard
    {
        [SerializeReference]
        private List<BlackboardVariable> m_Variables = new List<BlackboardVariable>();

        public IReadOnlyList<BlackboardVariable> Variables => m_Variables;

        // ── Variable Management ─────────────────────────────────────────────

        public void AddVariable(BlackboardVariable variable)
        {
            if (string.IsNullOrEmpty(variable.Guid))
                variable.Guid = System.Guid.NewGuid().ToString();
            m_Variables.Add(variable);
        }

        public bool RemoveVariable(string guid)
        {
            int idx = m_Variables.FindIndex(v => v.Guid == guid);
            if (idx < 0) return false;
            m_Variables.RemoveAt(idx);
            return true;
        }

        public BlackboardVariable GetVariable(string guid)
            => m_Variables.Find(v => v.Guid == guid);

        public BlackboardVariable<T> GetVariable<T>(string guid)
            => GetVariable(guid) as BlackboardVariable<T>;

        public bool TryGetValue<T>(string guid, out T value)
        {
            var v = GetVariable<T>(guid);
            if (v != null) { value = v.Value; return true; }
            value = default;
            return false;
        }

        public bool SetValue<T>(string guid, T value)
        {
            var v = GetVariable<T>(guid);
            if (v == null) return false;
            v.Value = value;
            return true;
        }

        // ── Cloning (per-agent isolation) ───────────────────────────────────

        /// <summary>
        /// Returns a deep clone. Shared variables are NOT cloned — both the
        /// original and the clone reference the same BlackboardVariable object
        /// so all agents see the same value.
        /// </summary>
        public Blackboard Clone(Blackboard sharedSource)
        {
            var clone = new Blackboard();
            foreach (var v in m_Variables)
            {
                if (v.Shared)
                    // Point at the master variable so changes broadcast everywhere.
                    clone.m_Variables.Add(sharedSource.GetVariable(v.Guid) ?? v);
                else
                    clone.m_Variables.Add(v.Clone());
            }
            return clone;
        }
    }
}
