using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Feazeyu.RPGSystems.Character;
using Feazeyu.RPGSystems.Inventory;
using Feazeyu.RPGSystems.Items;
using QuestGraph.Runtime;
using QuestGraph.Demo;

namespace QuestGraph.DemoEditor
{
    /// <summary>Builds the playable QuestDemo scene and saves it over the package sample.</summary>
    public static class QuestDemoSceneBuilder
    {
        private const string Dir = "Packages/com.feazeyu.rpgsystems/Samples/Quest Demo/";
        private const string ScenePath = Dir + "QuestDemo.unity";
        private const int AppleId = 10;
        private const int BurnId = 5;

        private static Sprite s_Square;

        [MenuItem("Tools/Quest Demo/Build Scene")]
        public static void BuildScene()
        {
            // Built-in sprite asset (persists across scene save/reload, unlike a runtime Sprite.Create).
            s_Square = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Camera ────────────────────────────────────────────────────────
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 10f;
            cam.backgroundColor = new Color(0.15f, 0.18f, 0.2f);
            camGo.transform.position = new Vector3(0, 0, -10);

            new GameObject("InventoryManager", typeof(InventoryManager));

            // ── UI canvas + event system (for the inventory list UI) ──────────
            var canvasGo = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // ── Player ────────────────────────────────────────────────────────
            var charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Character/Char.prefab");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(charPrefab);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = Vector3.zero;
            var rb = player.GetComponent<Rigidbody2D>();
            rb.gravityScale = 0; rb.freezeRotation = true;
            var pcol = player.AddComponent<CircleCollider2D>();
            pcol.isTrigger = true; pcol.radius = 1.5f;
            var interactor = player.AddComponent<Interactor>();
            var pc = player.GetComponent<PlayerController>();
            pc.interactor = interactor;

            // PlayerInventory: the data container (InventoryList) the quests/pickups use,
            // plus a configured generator that bakes a visible list UI onto the canvas.
            var invGo = new GameObject("PlayerInventory", typeof(InventoryList));
            invGo.transform.SetParent(player.transform, false);
            var invList = invGo.GetComponent<InventoryList>();
            var gen = invGo.AddComponent<InventoryListGenerator>();
            gen.list = invList;
            gen.targetCanvas = canvas;
            gen.slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Inventory/ListInventorySlot.prefab");
            gen.inventoryContainerOverride = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Inventory/PlayerListInventory.prefab");
            gen.inventoryName = "PlayerInventoryUI";
            gen.margin = new Vector2(0, 4);
            gen.anchorPosition = TextAnchor.LowerLeft;   // bottom-left HUD (was broken before the OriginBase fix)
            gen.DrawContents();                          // bakes the UI under the canvas
            InventoryHelper.GenerateDragLayer(canvas);   // enables drag-and-drop

            // ── NPC + chain + quest giver ─────────────────────────────────────
            var npc = MakeActor("NPC", new Vector3(5, 0, 0), new Color(0.3f, 0.7f, 1f), 1.6f);
            npc.AddComponent<Interactable>();
            var chain = npc.AddComponent<QuestChainRunner>();
            chain.Chain = Load("QuestChain.asset");
            // QuestGiver subscribes to the NPC's Interactable.OnInteract in code,
            // so no persistent UnityEvent listener is needed.
            var questGiver = npc.AddComponent<QuestGiver>();

            // ── Initial apples for the Find quest ─────────────────────────────
            var apples = new GameObject("FindApples");
            Vector2[] findSpots =
            {
                new(-4, 3), new(-6, -1), new(-3, -4), new(2, 4),
                new(4, -4), new(-7, 2), new(0, 5), new(6, 3),
            };
            foreach (var p in findSpots) MakePickup(apples.transform, p, AppleId, new Color(0.85f, 0.15f, 0.12f), "Apple");

            // ── Stage props (chest + apple groves) ────────────────────────────
            var chest = MakePickup(null, new Vector2(9, 6), BurnId, new Color(0.6f, 0.4f, 0.15f), "Chest");
            chest.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

            var near = new GameObject("NearArea");
            near.transform.position = new Vector3(9, 4, 0);
            foreach (var p in new Vector2[] { new(8, 4), new(10, 4), new(9, 3) })
                MakePickup(near.transform, p, AppleId, new Color(0.85f, 0.15f, 0.12f), "Apple");

            var far = new GameObject("FarArea");
            far.transform.position = new Vector3(-11, -7, 0);
            foreach (var p in new Vector2[] { new(-11, -7), new(-12, -6), new(-10, -8), new(-12, -8), new(-9, -6), new(-10, -6) })
                MakePickup(far.transform, p, AppleId, new Color(0.85f, 0.15f, 0.12f), "Apple");

            // ── Quest giver wiring (private serialized fields) ────────────────
            var qgo = new SerializedObject(questGiver);
            qgo.FindProperty("m_Chain").objectReferenceValue = chain;
            qgo.FindProperty("m_PlayerInventory").objectReferenceValue = invGo;
            qgo.FindProperty("m_StartingItemId").intValue = AppleId;
            qgo.FindProperty("m_StartingItemCount").intValue = 5;
            qgo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[QuestDemoSceneBuilder] Scene built and saved to " + ScenePath);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static GameObject MakeActor(string name, Vector3 pos, Color color, float radius)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s_Square; sr.color = color;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true; col.radius = radius;
            return go;
        }

        private static GameObject MakePickup(Transform parent, Vector2 pos, int itemId, Color color, string name)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            go.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s_Square; sr.color = color;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true; col.radius = 1.2f;
            var pickup = go.AddComponent<ItemPickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("itemId").intValue = itemId;
            so.FindProperty("amount").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        private static QuestGraphAsset Load(string file)
            => AssetDatabase.LoadAssetAtPath<QuestGraphAsset>(Dir + file);
    }
}
