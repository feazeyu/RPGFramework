using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Feazeyu.RPGSystems.Dialogue;
using Feazeyu.RPGSystems.Items;
using QuestGraph.Runtime;

namespace QuestGraph.DemoEditor
{
    /// <summary>
    /// One-shot builder for the Quest Demo assets: the Apple item and the four
    /// quest graphs (Deliver, Find, Final, and the Chain that sequences them).
    /// Runs through the real runtime API so GUIDs/SerializeReference are correct.
    /// </summary>
    public static class QuestDemoBuilder
    {
        private const string ItemInfoPath = "Assets/ScriptableObjects/Items/Apple.asset";
        private const string ItemPrefabPath = "Assets/Resources/Items/Apple.prefab";
        private const string Dir = "Packages/com.feazeyu.rpgsystems/Samples/Quest Demo/";

        private const int AppleId = 10;
        private const int BurnId = 5;
        private const string InventoryObjectName = "PlayerInventory";
        private const string NpcName = "NPC";
        private const string FarAreaName = "FarArea";

        [MenuItem("Tools/Quest Demo/Build Apple Item")]
        public static ItemInfo BuildApple()
        {
            var info = ScriptableObject.CreateInstance<ItemInfo>();
            info.id = AppleId;
            var so = new SerializedObject(info);
            so.FindProperty("_name").stringValue = "Apple";
            so.FindProperty("_tier").intValue = 1;
            so.FindProperty("_description").stringValue = "A crisp red apple.";
            var positions = so.FindProperty("_shape").FindPropertyRelative("Positions");
            positions.arraySize = 1;
            positions.GetArrayElementAtIndex(0).vector2IntValue = Vector2Int.zero;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(info, ItemInfoPath);

            var go = new GameObject("Apple", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
            go.GetComponent<Image>().color = new Color(0.85f, 0.15f, 0.12f, 1f);
            go.AddComponent<Item>().info = info;
            PrefabUtility.SaveAsPrefabAsset(go, ItemPrefabPath);
            Object.DestroyImmediate(go);

            AssetDatabase.SaveAssets();
            Debug.Log($"[QuestDemoBuilder] Apple item built (id={AppleId}).");
            return info;
        }

        [MenuItem("Tools/Quest Demo/Build Quest Graphs")]
        public static void BuildQuests()
        {
            var q1 = BuildDeliverQuest();
            var q2 = BuildFindQuest();
            var q3 = BuildFinalQuest();
            BuildChain(q1, q2, q3);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[QuestDemoBuilder] Quest graphs built.");
        }

        [MenuItem("Tools/Quest Demo/Build All")]
        public static void BuildAll()
        {
            BuildApple();
            BuildQuests();
        }

        // ── Q1: Deliver 5 apples to the NPC ───────────────────────────────────
        private static QuestGraphAsset BuildDeliverQuest()
        {
            var g = ScriptableObject.CreateInstance<QuestGraphAsset>();
            g.Kind = QuestKind.Single;
            string inv = AddInventoryVar(g);

            var start = Node(g, "Start", "Start", new Vector2(-600, 0), Ports(Out("Out")));
            var find = FindInventory(g, new Vector2(-380, 0), inv);
            var deliver = Node(g, "obj_deliver", "Deliver Apples", new Vector2(-80, 0),
                Ports(In("In"), Out("Completed"), Out("Failed")),
                F("Title", "S", "Deliver 5 apples"),
                F("Description", "S", "Hand 5 apples to the villager."),
                F("ItemId", "I", AppleId.ToString()),
                F("Count", "I", "5"),
                F("NPC", "S", NpcName),
                FLink("Inventory", "UnityEngine.GameObject", inv),
                F("Optional", "B", "False"));
            var done = Node(g, "CompleteQuest", "Complete Quest", new Vector2(260, -60), Ports(In("In")));
            var fail = Node(g, "FailQuest", "Fail Quest", new Vector2(260, 120), Ports(In("In")), F("Reason", "S", "Delivery failed."));

            Edge(g, start, "Out", find, "In");
            Edge(g, find, "Found", deliver, "In");
            Edge(g, find, "NotFound", fail, "In");
            Edge(g, deliver, "Completed", done, "In");
            Edge(g, deliver, "Failed", fail, "In");

            Save(g, "QuestDeliverApples.asset");
            return g;
        }

        // ── Q2: Accumulate 5 apples within 30s (reset on expiry) ──────────────
        private static QuestGraphAsset BuildFindQuest()
        {
            var g = ScriptableObject.CreateInstance<QuestGraphAsset>();
            g.Kind = QuestKind.Single;
            string inv = AddInventoryVar(g);

            var start = Node(g, "Start", "Start", new Vector2(-600, 0), Ports(Out("Out")));
            var find = FindInventory(g, new Vector2(-380, 0), inv);
            var acc = Node(g, "obj_accumulate", "Find Apples", new Vector2(-60, -40),
                Ports(In("In"), Out("Completed"), Out("Failed")),
                F("Title", "S", "Find 5 apples"),
                F("Description", "S", "Gather 5 apples before time runs out."),
                F("ItemId", "I", AppleId.ToString()),
                F("Count", "I", "5"),
                FLink("Inventory", "UnityEngine.GameObject", inv),
                F("Optional", "B", "False"));
            var timer = Node(g, "timer", "Window", new Vector2(-60, 160),
                Ports(In("Begin"), Out("Timeout")),
                F("Seconds", "Single", "30"),
                F("AutoRestart", "B", "True"));
            var reset = Node(g, "reset_progress", "Reset", new Vector2(240, 160),
                Ports(In("In"), Out("Target"), Out("Out")));
            var done = Node(g, "CompleteQuest", "Complete Quest", new Vector2(300, -40), Ports(In("In")));

            Edge(g, start, "Out", find, "In");
            Edge(g, find, "Found", acc, "In");
            Edge(g, find, "Found", timer, "Begin");   // fork: start the window
            Edge(g, acc, "Completed", done, "In");
            Edge(g, timer, "Timeout", reset, "In");
            Edge(g, reset, "Target", acc, "In");       // reference edge: resets acc's progress

            Save(g, "QuestFindApples.asset");
            return g;
        }

        // ── Q3: get Burn from chest, then 5 apples in the far area within 30s ──
        private static QuestGraphAsset BuildFinalQuest()
        {
            var g = ScriptableObject.CreateInstance<QuestGraphAsset>();
            g.Kind = QuestKind.Single;
            string inv = AddInventoryVar(g);

            var start = Node(g, "Start", "Start", new Vector2(-700, 0), Ports(Out("Out")));
            var find = FindInventory(g, new Vector2(-480, 0), inv);
            var burn = Node(g, "obj_accumulate", "Get Burn", new Vector2(-160, 0),
                Ports(In("In"), Out("Completed"), Out("Failed")),
                F("Title", "S", "Take the Burn from the chest"),
                F("Description", "S", "Open the chest and take the Burn item."),
                F("ItemId", "I", BurnId.ToString()),
                F("Count", "I", "1"),
                FLink("Inventory", "UnityEngine.GameObject", inv),
                F("Optional", "B", "False"));
            var apples = Node(g, "obj_accumulate", "Far Apples", new Vector2(200, -40),
                Ports(In("In"), Out("Completed"), Out("Failed")),
                F("Title", "S", "Gather 5 apples from the far grove"),
                F("Description", "S", "Only apples taken in the far grove count."),
                F("ItemId", "I", AppleId.ToString()),
                F("Count", "I", "5"),
                FLink("Inventory", "UnityEngine.GameObject", inv),
                F("Optional", "B", "False"));
            var gate = Node(g, "gate_location", "Far Grove", new Vector2(200, -220),
                Ports(Out("Out")),
                F("Target", "UnityEngine.Transform", FarAreaName),
                F("Radius", "Single", "6"),
                F("Subject", "S", "Player"));
            var timer = Node(g, "timer", "Window", new Vector2(200, 180),
                Ports(In("Begin"), Out("Timeout")),
                F("Seconds", "Single", "30"),
                F("AutoRestart", "B", "True"));
            var reset = Node(g, "reset_progress", "Reset", new Vector2(480, 180),
                Ports(In("In"), Out("Target"), Out("Out")));
            var done = Node(g, "CompleteQuest", "Complete Quest", new Vector2(540, -40), Ports(In("In")));

            Edge(g, start, "Out", find, "In");
            Edge(g, find, "Found", burn, "In");
            Edge(g, burn, "Completed", apples, "In");   // sequential: burn first
            Edge(g, burn, "Completed", timer, "Begin");  // then start the apple window
            Edge(g, gate, "Out", apples, "In");          // far-area gate decorates the apple objective
            Edge(g, apples, "Completed", done, "In");
            Edge(g, timer, "Timeout", reset, "In");
            Edge(g, reset, "Target", apples, "In");

            Save(g, "QuestFinal.asset");
            return g;
        }

        // ── Chain: Deliver → Find → Final ─────────────────────────────────────
        private static void BuildChain(QuestGraphAsset q1, QuestGraphAsset q2, QuestGraphAsset q3)
        {
            var g = ScriptableObject.CreateInstance<QuestGraphAsset>();
            g.Kind = QuestKind.Chain;

            string v1 = AddQuestVar(g, "Quest1", q1);
            string v2 = AddQuestVar(g, "Quest2", q2);
            string v3 = AddQuestVar(g, "Quest3", q3);

            var n1 = Node(g, "RunSubgraph", "Deliver", new Vector2(-300, 0),
                Ports(In("In"), Out("Out")), FLink("Quest", "QuestGraph.Runtime.QuestReference", v1));
            var n2 = Node(g, "RunSubgraph", "Find", new Vector2(0, 0),
                Ports(In("In"), Out("Out")), FLink("Quest", "QuestGraph.Runtime.QuestReference", v2));
            var n3 = Node(g, "RunSubgraph", "Final", new Vector2(300, 0),
                Ports(In("In"), Out("Out")), FLink("Quest", "QuestGraph.Runtime.QuestReference", v3));

            Edge(g, n1, "Out", n2, "In");  // Find depends on Deliver
            Edge(g, n2, "Out", n3, "In");  // Final depends on Find

            Save(g, "QuestChain.asset");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string AddInventoryVar(GraphAsset g)
        {
            var v = new BlackboardVariableGameObject { Name = "Inventory", Exposed = true };
            g.Blackboard.AddVariable(v);
            return v.Guid;
        }

        private static string AddQuestVar(GraphAsset g, string name, QuestGraphAsset quest)
        {
            var v = new BlackboardVariableQuestGraph(quest) { Name = name, Exposed = true };
            g.Blackboard.AddVariable(v);
            return v.Guid;
        }

        private static NodeData FindInventory(GraphAsset g, Vector2 pos, string invGuid)
        {
            return Node(g, "find_object", "Find Inventory", pos,
                Ports(In("In"), Out("Found"), Out("NotFound")),
                F("Mode", "S", "ByName"),
                F("Value", "S", InventoryObjectName),
                FLink("Target", "UnityEngine.GameObject", invGuid));
        }

        private static NodeData Node(GraphAsset g, string type, string name, Vector2 pos,
            PortData[] ports, params FieldData[] fields)
        {
            var n = g.AddNode(type, name, pos);
            n.Ports.AddRange(ports);
            if (fields != null) n.Fields.AddRange(fields);
            return n;
        }

        private static void Edge(GraphAsset g, NodeData o, string op, NodeData i, string ip)
            => g.AddEdge(o.Guid, op, i.Guid, ip);

        private static PortData[] Ports(params PortData[] p) => p;
        private static PortData In(string name) => new PortData { PortName = name, Direction = PortDirection.Input, Capacity = PortCapacity.Multi };
        private static PortData Out(string name) => new PortData { PortName = name, Direction = PortDirection.Output, Capacity = PortCapacity.Multi };

        private static FieldData F(string name, string type, string val)
            => new FieldData { FieldName = name, TypeName = Expand(type), InlineValue = val };

        private static FieldData FLink(string name, string type, string guid)
            => new FieldData { FieldName = name, TypeName = type, LinkedVariableGuid = guid };

        private static string Expand(string t) => t switch
        {
            "S" => "System.String",
            "I" => "System.Int32",
            "B" => "System.Boolean",
            _ => t,
        };

        private static void Save(QuestGraphAsset g, string file)
        {
            AssetDatabase.CreateAsset(g, Dir + file);
            EditorUtility.SetDirty(g);
        }
    }
}
