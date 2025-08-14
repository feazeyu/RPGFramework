using Game.Core.Utilities;
using UnityEngine;
using Game.Character;
using UnityEditor;
namespace Game.Abilities
{
    internal class SpellComponent : ScriptableObject
    {
        public float speed;
        public float damage;
        public SerializableDictionary<ResourceTypes, float> resourceCosts;
        public float cooldown;
        public float range;
        //Todo add optional aoe

#if UNITY_EDITOR
        [MenuItem("Assets/Create/Abilities/Spell Component")]
        public static void CreateSpellComponent()
        {
            var asset = CreateInstance<SpellComponent>();
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/SpellComponent.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
#endif
    }
}
