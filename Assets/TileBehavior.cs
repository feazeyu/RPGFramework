using UnityEngine;

public abstract class TileBehavior : ScriptableObject {
    public virtual void OnEnter(GameObject entity, Vector3Int position) { }
    public virtual void OnExit(GameObject entity, Vector3Int position) { }
    public virtual void OnEvent(string eventName, Vector3Int position) { }
}
