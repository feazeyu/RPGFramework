using UnityEngine;
using Feazeyu.RPGSystems.Dialogue;

namespace QuestGraph.Runtime
{
    /// <summary>
    /// Drives a boolean blackboard variable from a trigger collider: true while a
    /// tagged actor (the player by default) is inside, false when it leaves. Pair
    /// with a Flag Gate reading the same variable to express "while in this zone"
    /// objectives without any per-frame distance checks — physics does the work.
    ///
    /// Setup:
    ///   1. Put this on a GameObject with a trigger Collider2D shaped like the zone.
    ///   2. Point <see cref="runner"/> at the QuestRunner driving the quest.
    ///   3. Set <see cref="variableName"/> to a Bool blackboard variable's name.
    ///   4. Add a Flag Gate (Variable = that variable, == True) to the objective.
    /// </summary>
    [AddComponentMenu("Quest/Zone Flag")]
    [RequireComponent(typeof(Collider2D))]
    public class ZoneFlag : MonoBehaviour
    {
        [Tooltip("The runner whose runtime blackboard variable this zone toggles.")]
        [SerializeField] private GraphRunner runner;

        [Tooltip("Name of a Bool blackboard variable to set true inside / false outside.")]
        [SerializeField] private string variableName;

        [Tooltip("Only actors with this tag toggle the flag.")]
        [SerializeField] private string actorTag = "Player";

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(actorTag)) Set(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag(actorTag)) Set(false);
        }

        private void Set(bool value)
        {
            if (runner == null || string.IsNullOrEmpty(variableName)) return;
            runner.SetBlackboardValue(variableName, value);
        }
    }
}
