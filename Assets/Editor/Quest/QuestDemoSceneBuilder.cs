using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DialogueGraph.Runtime;
using QuestGraph.Runtime;
using QuestGraph.Demo;
using Game.Character;

namespace QuestGraph.Editor
{
    public static class QuestDemoSceneBuilder
    {
        const string k_ScenePath = "Assets/Demo_scenes/QuestDemo.unity";
        const string k_GraphPath = "Assets/Demo_scenes/QuestDemoGraph.asset";

        [MenuItem("Demo/Build Quest Demo Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var graph = BuildGraph();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildScene(graph);
            EditorSceneManager.SaveScene(scene, k_ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[QuestDemoSceneBuilder] Quest demo scene saved to {k_ScenePath}");
        }

        // ── Quest graph asset ─────────────────────────────────────────────────

        static QuestGraphAsset BuildGraph()
        {
            var existing = AssetDatabase.LoadAssetAtPath<QuestGraphAsset>(k_GraphPath);
            if (existing != null) AssetDatabase.DeleteAsset(k_GraphPath);

            var g = ScriptableObject.CreateInstance<QuestGraphAsset>();

            var start    = MakeNode(g, QuestNodeRegistry.TypeStart,        "Start",          new Vector2(-200, 200));
            var kill     = MakeNode(g, QuestNodeRegistry.TypeObjKill,      "Kill Enemies",   new Vector2( 100, 200));
            var location = MakeNode(g, QuestNodeRegistry.TypeObjLocation,  "Reach Waypoint", new Vector2( 450, 200));
            var reward   = MakeNode(g, QuestNodeRegistry.TypeReward,       "Reward",         new Vector2( 750, 200));
            var complete = MakeNode(g, QuestNodeRegistry.TypeCompleteQuest,"Complete Quest",  new Vector2(1050, 200));
            var fail     = MakeNode(g, QuestNodeRegistry.TypeFailQuest,    "Fail Quest",     new Vector2( 350, 420));

            SetField(kill,     "Title",       "Kill 3 Goblins");
            SetField(kill,     "Description", "Defeat the goblins lurking nearby.");
            SetField(kill,     "Tag",         "Enemy");
            SetField(kill,     "Count",       "3");

            SetField(location, "Title",       "Reach the waypoint");
            SetField(location, "Description", "Head to the marked location.");
            SetField(location, "Target",      "Waypoint");
            SetField(location, "Radius",      "3");
            SetField(location, "Continuous",  "False");

            SetField(reward,   "XP",          "100");
            SetField(reward,   "Currency",    "50");
            SetField(reward,   "Quantity",    "1");

            SetField(fail,     "Reason",      "An objective failed.");

            g.AddEdge(start.Guid,    "Out",       kill.Guid,     "In");
            g.AddEdge(kill.Guid,     "Completed", location.Guid, "In");
            g.AddEdge(kill.Guid,     "Failed",    fail.Guid,     "In");
            g.AddEdge(location.Guid, "Completed", reward.Guid,   "In");
            g.AddEdge(location.Guid, "Failed",    fail.Guid,     "In");
            g.AddEdge(reward.Guid,   "Out",       complete.Guid, "In");

            AssetDatabase.CreateAsset(g, k_GraphPath);
            AssetDatabase.SaveAssets();
            return g;
        }

        static NodeData MakeNode(QuestGraphAsset g, string typeId, string displayName, Vector2 pos)
        {
            var node = g.AddNode(typeId, displayName, pos);
            var info = QuestNodeRegistry.Get(typeId);
            if (info == null) return node;

            node.Ports = new List<PortData>();
            foreach (var p in info.DefaultPorts ?? new List<PortData>())
                node.Ports.Add(new PortData { PortName = p.PortName, Direction = p.Direction, Capacity = p.Capacity });

            node.Fields = new List<FieldData>();
            foreach (var f in info.DefaultFields ?? new List<FieldData>())
                node.Fields.Add(new FieldData
                {
                    FieldName         = f.FieldName,
                    TypeName          = f.TypeName,
                    InlineValue       = f.InlineValue ?? "",
                    LinkedVariableGuid = "",
                });
            return node;
        }

        static void SetField(NodeData node, string fieldName, string value)
        {
            var f = node.Fields?.Find(x => x.FieldName == fieldName);
            if (f != null) f.InlineValue = value;
        }

        // ── Scene ─────────────────────────────────────────────────────────────

        static void BuildScene(QuestGraphAsset graph)
        {
            var charSprite  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Character/Char.png");
            var redSprite   = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Red.png");
            var greenSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/DarkGreen.png");

            // Camera
            var camGO = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam   = camGO.AddComponent<Camera>();
            cam.orthographic    = true;
            cam.orthographicSize = 8f;
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.10f, 0.16f);
            camGO.transform.position = new Vector3(0, 0, -10);

            // Player — add DemoPlayer first (auto-satisfies its RequireComponent),
            // then configure the auto-created Rigidbody2D.
            var playerGO  = new GameObject("Player") { tag = "Player" };
            playerGO.AddComponent<DialogueGraph.Demo.DemoPlayer>();
            var playerRB  = playerGO.GetComponent<Rigidbody2D>();
            playerRB.gravityScale  = 0f;
            playerRB.constraints   = RigidbodyConstraints2D.FreezeRotation;
            var playerCol = playerGO.AddComponent<CircleCollider2D>();
            playerCol.radius = 0.4f;
            var playerSR  = playerGO.AddComponent<SpriteRenderer>();
            if (charSprite != null) playerSR.sprite = charSprite;
            else playerSR.color = new Color(0.3f, 0.5f, 1f);

            // Enemies (killable via E-key interaction)
            Vector3[] enemyPos = { new(-4, 3, 0), new(0, 5, 0), new(4, 3, 0) };
            for (int i = 0; i < 3; i++)
            {
                var e = new GameObject($"Enemy{i + 1}") { tag = "Enemy" };
                e.transform.position = enemyPos[i];

                e.AddComponent<Entity>();
                var h = e.AddComponent<Health>();
                h.maxAmount = 10f;
                h.Points    = 10f;

                var rb = e.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;

                var col = e.AddComponent<CircleCollider2D>();
                col.radius    = 0.55f;
                col.isTrigger = true;

                e.AddComponent<Interactable>();
                e.AddComponent<DemoEnemy>();

                var sr = e.AddComponent<SpriteRenderer>();
                if (redSprite != null) sr.sprite = redSprite;
                else sr.color = new Color(0.9f, 0.2f, 0.2f);
            }

            // Waypoint (reach-location target — resolved by name in ReachLocationObjectiveHandler)
            var wp   = new GameObject("Waypoint");
            wp.transform.position = new Vector3(0, -5, 0);
            var wpSR = wp.AddComponent<SpriteRenderer>();
            if (greenSprite != null) wpSR.sprite = greenSprite;
            wpSR.color = new Color(1f, 0.85f, 0.1f);

            // QuestManager
            var qmGO   = new GameObject("QuestManager");
            var runner = qmGO.AddComponent<QuestRunner>();
            runner.Graph = graph;
            qmGO.AddComponent<QuestRewardHandler>();

            var autoStart = qmGO.AddComponent<AutoStartQuest>();
            var soAS = new SerializedObject(autoStart);
            soAS.FindProperty("m_Runner").objectReferenceValue = runner;
            soAS.ApplyModifiedProperties();

            BuildUI(runner);
        }

        static void BuildUI(QuestRunner runner)
        {
            // Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            // Quest panel (top-left)
            var panelGO = MakePanel(canvasGO.transform,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(0, 1),
                pivot:     new Vector2(0, 1),
                apos:      new Vector2(20, -20),
                size:      new Vector2(360, 190));
            panelGO.name = "QuestPanel";
            panelGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);

            // Panel title
            var titleGO  = new GameObject("TitleText");
            titleGO.transform.SetParent(panelGO.transform, false);
            var titleRT  = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin        = new Vector2(0, 1);
            titleRT.anchorMax        = new Vector2(1, 1);
            titleRT.pivot            = new Vector2(0.5f, 1);
            titleRT.anchoredPosition = new Vector2(0, -10);
            titleRT.sizeDelta        = new Vector2(0, 28);
            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text      = "DEMO QUEST";
            titleTMP.fontSize  = 18;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color     = new Color(1f, 0.82f, 0.2f);
            titleTMP.alignment = TextAlignmentOptions.Center;

            // Objectives list
            var objGO  = new GameObject("ObjectivesText");
            objGO.transform.SetParent(panelGO.transform, false);
            var objRT  = objGO.AddComponent<RectTransform>();
            objRT.anchorMin = Vector2.zero;
            objRT.anchorMax = Vector2.one;
            objRT.offsetMin = new Vector2(12, 12);
            objRT.offsetMax = new Vector2(-12, -50);
            var objTMP  = objGO.AddComponent<TextMeshProUGUI>();
            objTMP.text      = "";
            objTMP.fontSize  = 15;
            objTMP.color     = Color.white;
            objTMP.alignment = TextAlignmentOptions.TopLeft;

            // Status label (top centre — shown on quest end)
            var statusGO = new GameObject("StatusText");
            statusGO.transform.SetParent(canvasGO.transform, false);
            var statusRT  = statusGO.AddComponent<RectTransform>();
            statusRT.anchorMin        = new Vector2(0.5f, 1);
            statusRT.anchorMax        = new Vector2(0.5f, 1);
            statusRT.pivot            = new Vector2(0.5f, 1);
            statusRT.anchoredPosition = new Vector2(0, -20);
            statusRT.sizeDelta        = new Vector2(700, 70);
            var statusTMP = statusGO.AddComponent<TextMeshProUGUI>();
            statusTMP.text      = "";
            statusTMP.fontSize  = 28;
            statusTMP.fontStyle = FontStyles.Bold;
            statusTMP.color     = new Color(0.3f, 1f, 0.35f);
            statusTMP.alignment = TextAlignmentOptions.Center;

            // Controls hint (bottom centre)
            var hintGO = new GameObject("HintText");
            hintGO.transform.SetParent(canvasGO.transform, false);
            var hintRT  = hintGO.AddComponent<RectTransform>();
            hintRT.anchorMin        = new Vector2(0.5f, 0);
            hintRT.anchorMax        = new Vector2(0.5f, 0);
            hintRT.pivot            = new Vector2(0.5f, 0);
            hintRT.anchoredPosition = new Vector2(0, 18);
            hintRT.sizeDelta        = new Vector2(700, 36);
            var hintTMP = hintGO.AddComponent<TextMeshProUGUI>();
            hintTMP.text      = "WASD / Arrow Keys — Move    |    E — Attack / Interact";
            hintTMP.fontSize  = 15;
            hintTMP.color     = new Color(0.75f, 0.75f, 0.75f, 0.85f);
            hintTMP.alignment = TextAlignmentOptions.Center;

            // QuestDemoUI component
            var uiGO   = new GameObject("QuestDemoUI");
            uiGO.transform.SetParent(canvasGO.transform, false);
            uiGO.AddComponent<RectTransform>(); // keeps it a proper UI object
            var uiComp = uiGO.AddComponent<QuestDemoUI>();
            var soUI   = new SerializedObject(uiComp);
            soUI.FindProperty("m_ObjectivesText").objectReferenceValue = objTMP;
            soUI.FindProperty("m_StatusText").objectReferenceValue     = statusTMP;
            soUI.FindProperty("m_QuestRunner").objectReferenceValue    = runner;
            soUI.ApplyModifiedProperties();
        }

        static GameObject MakePanel(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                                    Vector2 pivot, Vector2 apos, Vector2 size)
        {
            var go = new GameObject("Panel");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = apos;
            rt.sizeDelta        = size;
            go.AddComponent<Image>();
            return go;
        }
    }
}
