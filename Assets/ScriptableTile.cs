using UnityEngine.Tilemaps;
using UnityEngine;

[CreateAssetMenu(menuName = "2D/Tiles/Scriptable Tile")]
public class ScriptableTile : Tile {
    public TileBehavior behavior;

    public override void RefreshTile(Vector3Int position, ITilemap tilemap) {
        base.RefreshTile(position, tilemap);
        // Optionally refresh behavior here
    }
}
