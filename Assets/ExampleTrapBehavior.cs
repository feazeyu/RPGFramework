using UnityEngine;

[CreateAssetMenu(menuName = "Tiles/Behaviors/Spike Trap")]
public class SpikeTrapBehavior : TileBehavior {
    public int damage = 10;

    public override void OnEnter(GameObject entity, Vector3Int position) {
        Debug.Log($"Entity {entity.name} entered spike trap at {position}. Inflicting {damage} damage.");
        
    }
}