using UnityEngine.Tilemaps;
using UnityEngine;

public class TileEventDispatcher : MonoBehaviour {
    public Tilemap tilemap;

    void Start() {
        tilemap = GetComponent<Tilemap>();
    }

    public void HandleEnter(GameObject entity, Vector3Int tilePos) {
        var tile = tilemap.GetTile(tilePos) as ScriptableTile;
        tile?.behavior?.OnEnter(entity, tilePos);
    }

    public void TriggerEvent(Vector3Int tilePos, string eventName) {
        var tile = tilemap.GetTile(tilePos) as ScriptableTile;
        tile?.behavior?.OnEvent(eventName, tilePos);
    }
}
