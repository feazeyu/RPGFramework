using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using QuestGraph.Runtime;

namespace QuestGraph.Editor
{
    /// <summary>
    /// Adds a second entry under <b>Assets → Create → Quest → Quest Chain</b>
    /// that creates a <see cref="QuestGraphAsset"/> with
    /// <see cref="QuestGraphAsset.Kind"/> pre-set to
    /// <see cref="QuestKind.Chain"/>.
    ///
    /// The default <c>[CreateAssetMenu]</c> on <see cref="QuestGraphAsset"/>
    /// produces a Single-kind asset; this class covers the Chain case by
    /// mimicking Unity's built-in create-new-asset flow
    /// (<see cref="ProjectWindowUtil.StartNameEditingIfProjectWindowExists"/>)
    /// so the name field opens inline in the Project view, matching the
    /// native UX.
    /// </summary>
    public static class QuestChainCreateMenu
    {
        [MenuItem("Assets/Create/Quest/Quest Chain", priority = 2)]
        private static void CreateQuestChain()
        {
            var icon = EditorGUIUtility.IconContent("d_ScriptableObject Icon").image as Texture2D;

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<CreateChainEndAction>(),
                "NewQuestChain.asset",
                icon,
                null);
        }

        /// <summary>
        /// Callback invoked by the Project view once the user has typed a
        /// name and pressed Enter. Creates the asset with the correct
        /// <see cref="QuestKind"/> and registers it with the AssetDatabase.
        /// </summary>
        private class CreateChainEndAction : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var asset = ScriptableObject.CreateInstance<QuestGraphAsset>();
                asset.Kind = QuestKind.Chain;
                AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(pathName));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Select and ping the new asset to match native UX.
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }
    }
}
