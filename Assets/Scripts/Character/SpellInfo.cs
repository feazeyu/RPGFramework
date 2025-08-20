using Game.Core.Utilities;
using UnityEngine;
namespace Game.Character
{
    public class SpellInfo : ScriptableObject
    {
        [Header("Projectile Settings")]
        public GameObject prefab;
        public float speed = 10f;
        public float range = 20f;
        public float damage = 5f;
        public float cooldown = 1f;
        public SerializableDictionary<ResourceTypes, float> resourceCosts;
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Assets/Create/Abilities/SpellInfo")]
        public static void CreateSpell()
        {
            var asset = ScriptableObject.CreateInstance<SpellInfo>();
            string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath("Assets/NewSpell.asset");
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.EditorUtility.FocusProjectWindow();
            UnityEditor.Selection.activeObject = asset;
        }
#endif
    }
}
