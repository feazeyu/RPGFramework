using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueGraph.Runtime;

namespace DialogueGraph.Editor
{
    public class BlackboardPanel : VisualElement
    {
        // Variable types and their accent colours — only the colour is data-driven;
        // shape/size/layout comes from USS.
        private static readonly (string TypeName, Color Colour)[] VariableTypes =
        {
            ("Boolean",    new Color(0.88f, 0.31f, 0.44f)),
            ("Integer",    new Color(0.29f, 0.61f, 0.78f)),
            ("Float",      new Color(0.29f, 0.78f, 0.76f)),
            ("String",     new Color(0.94f, 0.65f, 0.20f)),
            ("GameObject", new Color(0.34f, 0.78f, 0.34f)),
            ("Transform",  new Color(0.69f, 0.42f, 0.97f)),
            ("Vector2",    new Color(0.88f, 0.62f, 0.29f)),
            ("Vector3",    new Color(0.88f, 0.47f, 0.29f)),
            ("Color",      new Color(0.98f, 0.60f, 0.80f)),
            ("Sprite",     new Color(0.80f, 0.78f, 0.29f)),
            ("AudioClip",  new Color(0.48f, 0.76f, 0.96f)),
        };

        // ── State ─────────────────────────────────────────────────────────────

        private GraphAsset m_Asset;
        private SerializedObject   m_SerializedAsset;
        private VisualElement      m_VariableList;
        private VisualElement      m_AddForm;
        private TextField          m_NewNameField;
        private DropdownField      m_NewTypeField;

        private readonly HashSet<string> m_ExpandedGuids = new HashSet<string>();

        /// <summary>
        /// Optional callback wired by <see cref="GraphEditorWindow"/> so
        /// the blackboard can refresh node views on the canvas (and any
        /// other dependent UI) after mutations that affect them — in
        /// particular, deleting a variable that nodes still reference.
        /// The callback receives the guid of each affected node.
        /// </summary>
        private System.Action<string> m_RefreshNodeView;

        // ── Construction ──────────────────────────────────────────────────────

        public BlackboardPanel()
        {
            AddToClassList("bb-panel");
            BuildHeader();
            BuildVariableList();
        }

        /// <summary>
        /// Supply a per-guid node-refresh callback so variable deletion
        /// can cascade into dependent UI (the graph canvas and, via the
        /// window, the inspector). Called by the owning window after
        /// construction.
        /// </summary>
        public void SetRefreshNodeViewCallback(System.Action<string> refresh)
        {
            m_RefreshNodeView = refresh;
        }

        // ── Header ────────────────────────────────────────────────────────────

        private void BuildHeader()
        {
            var header = new VisualElement();
            header.AddToClassList("bb-header");

            var icon = new Label("▦");
            icon.AddToClassList("bb-header-icon");
            header.Add(icon);

            var title = new Label("Blackboard");
            title.AddToClassList("bb-header-title");
            header.Add(title);

            var addBtn = new Button(ToggleAddForm) { text = "+" };
            addBtn.AddToClassList("bb-add-btn");
            header.Add(addBtn);

            Add(header);
            BuildAddForm();
        }

        private void BuildAddForm()
        {
            m_AddForm = new VisualElement();
            m_AddForm.AddToClassList("bb-add-form");

            var nameLabel = new Label("Name");
            nameLabel.AddToClassList("bb-form-label");
            m_AddForm.Add(nameLabel);

            m_NewNameField = new TextField { value = "NewVariable" };
            m_NewNameField.AddToClassList("bb-text-field");
            m_AddForm.Add(m_NewNameField);

            var typeLabel = new Label("Type");
            typeLabel.AddToClassList("bb-form-label-gap");
            m_AddForm.Add(typeLabel);

            var typeNames = new List<string>();
            foreach (var (name, _) in VariableTypes) typeNames.Add(name);
            foreach (var e in BlackboardVariableTypeRegistry.All)
                if (!typeNames.Contains(e.TypeName)) typeNames.Add(e.TypeName);
            m_NewTypeField = new DropdownField(typeNames, 0);
            m_NewTypeField.AddToClassList("bb-dropdown");
            m_AddForm.Add(m_NewTypeField);

            var confirmBtn = new Button(ConfirmAddVariable) { text = "Add Variable" };
            confirmBtn.AddToClassList("bb-primary-btn");
            m_AddForm.Add(confirmBtn);

            var cancelBtn = new Button(ToggleAddForm) { text = "Cancel" };
            cancelBtn.AddToClassList("bb-secondary-btn");
            m_AddForm.Add(cancelBtn);

            Add(m_AddForm);
        }

        // ── Variable list ─────────────────────────────────────────────────────

        private void BuildVariableList()
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            m_VariableList = scroll.contentContainer;
            m_VariableList.style.flexDirection = FlexDirection.Column;
            Add(scroll);
        }

        // ── Populate ──────────────────────────────────────────────────────────

        public void Populate(GraphAsset asset)
        {
            if (m_Asset != null)
                BlackboardPropertyBridge.Invalidate(m_Asset);

            m_Asset           = asset;
            m_SerializedAsset = asset != null
                ? BlackboardPropertyBridge.GetSerializedObject(asset)
                : null;

            m_ExpandedGuids.Clear();
            RebuildList();
        }

        private void RebuildList()
        {
            m_VariableList.Clear();

            if (m_Asset == null)
            {
                var empty = new Label("No graph loaded.");
                empty.AddToClassList("bb-unserialisable");
                empty.style.unityTextAlign = TextAnchor.MiddleCenter;
                empty.style.marginTop = 24;
                m_VariableList.Add(empty);
                return;
            }

            if (m_Asset.Blackboard.Variables.Count == 0)
            {
                var empty = new Label("No variables. Click + to add.");
                empty.AddToClassList("bb-unserialisable");
                empty.style.unityTextAlign = TextAnchor.MiddleCenter;
                empty.style.marginTop = 24;
                m_VariableList.Add(empty);
                return;
            }

            foreach (var variable in m_Asset.Blackboard.Variables)
                m_VariableList.Add(BuildVariableRow(variable));
        }

        // ── Variable row ──────────────────────────────────────────────────────

        private VisualElement BuildVariableRow(BlackboardVariable variable)
        {
            var typeColor = GetTypeColor(variable);
            bool expanded = m_ExpandedGuids.Contains(variable.Guid);

            var container = new VisualElement();
            container.AddToClassList("bb-var-container");

            var row = new VisualElement();
            row.AddToClassList("bb-var-row");
            if (expanded) row.AddToClassList("expanded");

            // Colour dot — background-color is data-driven, shape from USS.
            var dot = new VisualElement();
            dot.AddToClassList("bb-var-dot");
            dot.style.backgroundColor = new StyleColor(typeColor);
            row.Add(dot);

            var nameLabel = new Label(variable.Name);
            nameLabel.AddToClassList("bb-var-name");
            row.Add(nameLabel);

            // Type pill — color is data-driven, layout from USS.
            var typePill = new Label(GetShortTypeName(variable));
            typePill.AddToClassList("bb-type-pill");
            typePill.style.color           = new StyleColor(typeColor);
            typePill.style.backgroundColor = new StyleColor(
                new Color(typeColor.r, typeColor.g, typeColor.b, 0.12f));
            row.Add(typePill);

            var badgeContainer = new VisualElement();
            badgeContainer.AddToClassList("bb-badge-container");
            row.Add(badgeContainer);
            RefreshBadges(badgeContainer, variable);

            var chevron = new Label(expanded ? "▾" : "▸");
            chevron.AddToClassList("bb-chevron");
            row.Add(chevron);

            // Drag to node field.
            row.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0 || evt.clickCount != 1) return;
                row.RegisterCallback<MouseMoveEvent>(OnDragStartMove);

                void OnDragStartMove(MouseMoveEvent moveEvt)
                {
                    row.UnregisterCallback<MouseMoveEvent>(OnDragStartMove);
                    if (Vector2.Distance(evt.mousePosition, moveEvt.mousePosition) < 5f) return;

                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.SetGenericData("BlackboardVariableGuid", variable.Guid);
                    DragAndDrop.SetGenericData("BlackboardVariableName", variable.Name);
                    DragAndDrop.SetGenericData("BlackboardVariableType", GetShortTypeName(variable));
                    DragAndDrop.StartDrag(variable.Name);
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    evt.StopPropagation();
                }
            }, TrickleDown.NoTrickleDown);

            // Expand/collapse on click.
            row.RegisterCallback<ClickEvent>(_ =>
            {
                if (m_ExpandedGuids.Contains(variable.Guid))
                {
                    m_ExpandedGuids.Remove(variable.Guid);
                    row.RemoveFromClassList("expanded");
                }
                else
                {
                    m_ExpandedGuids.Add(variable.Guid);
                    row.AddToClassList("expanded");
                }
                RebuildList();
            });

            container.Add(row);

            if (expanded)
                container.Add(BuildVariableDetail(variable, nameLabel, badgeContainer));

            return container;
        }

        private static void RefreshBadges(VisualElement badgeContainer, BlackboardVariable variable)
        {
            badgeContainer.Clear();
            if (variable.Exposed)
            {
                var b = new Label("E");
                b.AddToClassList("bb-badge");
                b.AddToClassList("bb-badge-exposed");
                b.tooltip = "Exposed — visible on the agent Inspector";
                badgeContainer.Add(b);
            }
            if (variable.Shared)
            {
                var b = new Label("S");
                b.AddToClassList("bb-badge");
                b.AddToClassList("bb-badge-shared");
                b.tooltip = "Shared — one instance across all agents";
                badgeContainer.Add(b);
            }
        }

        // ── Variable detail ───────────────────────────────────────────────────

        private VisualElement BuildVariableDetail(BlackboardVariable variable,
            Label nameLabel, VisualElement badgeContainer)
        {
            var detail = new VisualElement();
            detail.AddToClassList("bb-detail");

            int idx = m_SerializedAsset != null
                ? BlackboardPropertyBridge.FindVariableIndex(m_SerializedAsset, variable.Guid)
                : -1;

            // Name
            detail.Add(MakeDetailLabel("Name"));
            if (idx >= 0)
            {
                var nameProp  = BlackboardPropertyBridge.FindVariableFieldAt(m_SerializedAsset, idx, "m_Name");
                var nameField = new PropertyField(nameProp, "") { label = "" };
                nameField.Bind(m_SerializedAsset);
                nameField.RegisterValueChangeCallback(_ =>
                {
                    m_SerializedAsset.ApplyModifiedProperties();
                    nameLabel.text = variable.Name;
                });
                detail.Add(nameField);
            }
            else
            {
                var nameField = new TextField { value = variable.Name };
                nameField.AddToClassList("bb-text-field");
                nameField.RegisterValueChangedCallback(evt =>
                {
                    variable.Name  = evt.newValue;
                    nameLabel.text = evt.newValue;
                    MarkDirty();
                });
                detail.Add(nameField);
            }

            // Type (read-only)
            detail.Add(MakeDetailLabel("Type"));
            var typeColor = GetTypeColor(variable);
            var typeLabel = new Label(GetShortTypeName(variable));
            typeLabel.AddToClassList("bb-type-pill");
            typeLabel.AddToClassList("bb-detail-type");
            typeLabel.style.color = new StyleColor(typeColor); // data-driven
            typeLabel.tooltip = "To change type, delete and re-add.";
            detail.Add(typeLabel);

            // Default Value
            detail.Add(MakeDetailLabel("Default Value"));
            if (idx >= 0)
            {
                var valueProp = BlackboardPropertyBridge.FindValuePropertyAt(m_SerializedAsset, idx);
                if (valueProp != null)
                {
                    if (valueProp.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        // PropertyField shows "type mismatch" for UnityEngine.Object fields
                        // inside [SerializeReference] elements — use ObjectField directly.
                        var objectType = variable.ValueType ?? typeof(UnityEngine.Object);
                        var objField = new ObjectField { objectType = objectType, allowSceneObjects = true };
                        objField.SetValueWithoutNotify(valueProp.objectReferenceValue);
                        objField.RegisterValueChangedCallback(evt =>
                        {
                            var prop = BlackboardPropertyBridge.FindValuePropertyAt(m_SerializedAsset, idx);
                            if (prop != null)
                            {
                                prop.objectReferenceValue = evt.newValue;
                                m_SerializedAsset.ApplyModifiedProperties();
                            }
                        });
                        detail.Add(objField);
                    }
                    else
                    {
                        var valueField = new PropertyField(valueProp, "");
                        valueField.Bind(m_SerializedAsset);
                        valueField.RegisterValueChangeCallback(_ => m_SerializedAsset.ApplyModifiedProperties());
                        detail.Add(valueField);
                    }
                }
                else { detail.Add(MakeUnserialisableLabel()); }
            }
            else { detail.Add(MakeUnserialisableLabel()); }

            // Exposed / Shared toggles
            detail.Add(MakeDetailLabel(""));
            detail.Add(BuildBoundToggleAt(idx,
                "Exposed", "Exposed variables appear in the agent Inspector.",
                "bb-toggle-exposed", variable, "m_Exposed", badgeContainer));
            detail.Add(BuildBoundToggleAt(idx,
                "Shared", "Shared variables have ONE value across all agents.",
                "bb-toggle-shared", variable, "m_Shared", badgeContainer));

            // GUID
            detail.Add(MakeDetailLabel("GUID"));
            var guidField = new TextField { value = variable.Guid, isReadOnly = true };
            guidField.AddToClassList("bb-text-field");
            guidField.style.fontSize = 8;
            detail.Add(guidField);

            // Delete
            var deleteBtn = new Button(() =>
            {
                if (m_Asset == null) return;
                Undo.RecordObject(m_Asset, "Delete Blackboard Variable");

                // Find every node field still linked to this variable
                // and clear the link. Without this, fields retain a
                // stale guid pointing at nothing and the node card
                // keeps rendering as "⟵ ?" indefinitely.
                var affectedNodeGuids = new HashSet<string>();
                foreach (var node in m_Asset.Nodes)
                {
                    if (node.Fields == null) continue;
                    foreach (var field in node.Fields)
                    {
                        if (field.LinkedVariableGuid == variable.Guid)
                        {
                            field.LinkedVariableGuid = null;
                            affectedNodeGuids.Add(node.Guid);
                        }
                    }
                }

                m_Asset.Blackboard.RemoveVariable(variable.Guid);
                m_ExpandedGuids.Remove(variable.Guid);
                EditorUtility.SetDirty(m_Asset);
                BlackboardPropertyBridge.Invalidate(m_Asset);
                m_SerializedAsset = BlackboardPropertyBridge.GetSerializedObject(m_Asset);

                // Refresh each affected canvas node view so the stale
                // "linked" visuals clear. Safe to call with a no-op
                // callback (checked via ?.Invoke).
                foreach (var guid in affectedNodeGuids)
                    m_RefreshNodeView?.Invoke(guid);

                RebuildList();
            }) { text = "Delete Variable" };
            deleteBtn.AddToClassList("bb-delete-btn");
            detail.Add(deleteBtn);

            return detail;
        }

        private static Label MakeUnserialisableLabel()
        {
            var l = new Label("⚠  Not serialisable — set value at runtime.");
            l.AddToClassList("bb-unserialisable");
            return l;
        }

        private Toggle BuildBoundToggleAt(int idx, string label, string tooltip,
            string cssClass, BlackboardVariable variable, string fieldName,
            VisualElement badgeContainer)
        {
            var toggle = new Toggle(label);
            toggle.AddToClassList(cssClass);
            toggle.tooltip = tooltip;

            if (m_SerializedAsset != null && idx >= 0)
            {
                var boolProp = BlackboardPropertyBridge.FindVariableFieldAt(m_SerializedAsset, idx, fieldName);
                if (boolProp != null)
                {
                    toggle.SetValueWithoutNotify(boolProp.boolValue);
                    toggle.RegisterValueChangedCallback(evt =>
                    {
                        var p = BlackboardPropertyBridge.FindVariableField(
                            m_SerializedAsset, variable.Guid, fieldName);
                        if (p != null)
                        {
                            p.boolValue = evt.newValue;
                            m_SerializedAsset.ApplyModifiedProperties();
                        }
                        RefreshBadges(badgeContainer, variable);
                    });
                    return toggle;
                }
            }

            toggle.RegisterValueChangedCallback(evt =>
            {
                if (fieldName == "m_Exposed") variable.Exposed = evt.newValue;
                else if (fieldName == "m_Shared") variable.Shared = evt.newValue;
                MarkDirty();
                RefreshBadges(badgeContainer, variable);
            });
            return toggle;
        }

        // ── Add-form logic ────────────────────────────────────────────────────

        private void ToggleAddForm()
        {
            bool visible = m_AddForm.style.display == DisplayStyle.Flex;
            m_AddForm.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
            if (!visible)
            {
                m_NewNameField.value = "NewVariable";
                m_NewNameField.Q(TextField.textInputUssName).Focus();
                m_NewNameField.SelectAll();
            }
        }

        private void ConfirmAddVariable()
        {
            if (m_Asset == null) return;
            var name = m_NewNameField.value.Trim();
            if (string.IsNullOrEmpty(name)) return;

            string typeName = m_NewTypeField.value;
            Undo.RecordObject(m_Asset, "Add Blackboard Variable");

            BlackboardVariable newVar = typeName switch
            {
                "Boolean"    => (BlackboardVariable) new BlackboardVariableBool(),
                "Integer"    => new BlackboardVariableInt(),
                "Float"      => new BlackboardVariableFloat(),
                "GameObject" => new BlackboardVariableGameObject(),
                "Transform"  => new BlackboardVariableTransform(),
                "Vector2"    => new BlackboardVariableVector2(),
                "Vector3"    => new BlackboardVariableVector3(),
                "Color"      => new BlackboardVariableColor(),
                "Sprite"     => new BlackboardVariableSprite(),
                "AudioClip"  => new BlackboardVariableAudioClip(),
                _            => CreateExtensionOrDefault(typeName),
            };

            newVar.Name    = name;
            newVar.Exposed = true;
            newVar.Shared  = false;

            m_Asset.Blackboard.AddVariable(newVar);
            EditorUtility.SetDirty(m_Asset);
            m_SerializedAsset?.Update();

            m_AddForm.style.display = DisplayStyle.None;
            RebuildList();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void MarkDirty() { if (m_Asset) EditorUtility.SetDirty(m_Asset); }

        private Color GetTypeColor(BlackboardVariable v)
        {
            var s = GetShortTypeName(v);
            foreach (var (name, col) in VariableTypes)
                if (name == s) return col;

            // Extension types (Quest, QuestGraph, etc.) carry their
            // accent on the registry entry.
            var ext = BlackboardVariableTypeRegistry.Find(s);
            if (ext.HasValue) return ext.Value.AccentColour;

            return Color.gray;
        }

        private static string GetShortTypeName(BlackboardVariable v)
        {
            if (v == null) return "Unknown";
            var builtIn = v switch
            {
                BlackboardVariableBool       _ => "Boolean",
                BlackboardVariableInt        _ => "Integer",
                BlackboardVariableFloat      _ => "Float",
                BlackboardVariableString     _ => "String",
                BlackboardVariableGameObject _ => "GameObject",
                BlackboardVariableTransform  _ => "Transform",
                BlackboardVariableVector2    _ => "Vector2",
                BlackboardVariableVector3    _ => "Vector3",
                BlackboardVariableColor      _ => "Color",
                BlackboardVariableSprite     _ => "Sprite",
                BlackboardVariableAudioClip  _ => "AudioClip",
                _                            => null,
            };
            if (builtIn != null) return builtIn;

            // Not a built-in — defer to the extension registry so Quest /
            // QuestGraph / etc. types contributed by other assemblies get
            // the right display name.
            var ext = BlackboardVariableTypeRegistry.FindFor(v);
            if (ext.HasValue) return ext.Value.TypeName;

            return v.ValueType?.Name ?? "Unknown";
        }

        /// <summary>
        /// Factory fallback for types not in the built-in switch. Looks
        /// the name up in <see cref="BlackboardVariableTypeRegistry"/>;
        /// if found, invokes the factory. Falls back to a String
        /// variable if nothing matches so the picker never produces
        /// <c>null</c>.
        /// </summary>
        private static BlackboardVariable CreateExtensionOrDefault(string typeName)
        {
            var entry = BlackboardVariableTypeRegistry.Find(typeName);
            if (entry.HasValue) return entry.Value.Factory();
            return new BlackboardVariableString();
        }

        private static Label MakeDetailLabel(string text)
        {
            var l = new Label(text);
            l.AddToClassList("bb-detail-label");
            return l;
        }
    }
}
