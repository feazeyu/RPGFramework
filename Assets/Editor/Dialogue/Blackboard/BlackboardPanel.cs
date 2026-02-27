using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueGraph.Runtime;

namespace DialogueGraph.Editor
{
    /// <summary>
    /// Left-hand panel that displays and edits the Blackboard variables of a
    /// DialogueGraphAsset. Mirrors the Unity Behavior Blackboard window.
    ///
    /// Features:
    ///   • Add variable (name, type picker, Exposed/Shared toggles)
    ///   • Delete variable
    ///   • Inline-edit name, type and default value
    ///   • Visual indicators: E (Exposed), S (Shared), link count badge
    ///   • Colour-coded type pill per variable type
    /// </summary>
    public class BlackboardPanel : VisualElement
    {
        // ── Style constants ───────────────────────────────────────────────────

        private static readonly Color BgPanel      = new Color(0.13f, 0.14f, 0.18f);
        private static readonly Color BgHeader     = new Color(0.11f, 0.12f, 0.16f);
        private static readonly Color BgRow        = new Color(0.15f, 0.16f, 0.20f);
        private static readonly Color BgRowHover   = new Color(0.18f, 0.20f, 0.26f);
        private static readonly Color BgRowActive  = new Color(0.17f, 0.22f, 0.30f);
        private static readonly Color ColText      = new Color(0.82f, 0.86f, 0.95f);
        private static readonly Color ColMuted     = new Color(0.50f, 0.55f, 0.65f);
        private static readonly Color ColDivider   = new Color(0.10f, 0.11f, 0.15f);

        // Supported variable types and their accent colours.
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

        private DialogueGraphAsset   m_Asset;
        private SerializedObject     m_SerializedAsset;
        private VisualElement        m_VariableList;
        private VisualElement        m_AddForm;
        private TextField            m_NewNameField;
        private DropdownField        m_NewTypeField;

        // GUIDs of variables whose detail panel is currently expanded.
        // Persists across RebuildList() calls so rebuilds restore open rows.
        private readonly HashSet<string> m_ExpandedGuids = new HashSet<string>();

        // ── Construction ──────────────────────────────────────────────────────

        public BlackboardPanel()
        {
            style.backgroundColor = new StyleColor(BgPanel);
            style.flexDirection   = FlexDirection.Column;

            BuildHeader();
            BuildVariableList();
        }

        // ── Header ────────────────────────────────────────────────────────────

        private void BuildHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection   = FlexDirection.Row;
            header.style.alignItems      = Align.Center;
            header.style.paddingTop      = 8;
            header.style.paddingBottom   = 8;
            header.style.paddingLeft     = 10;
            header.style.paddingRight    = 10;
            header.style.backgroundColor = new StyleColor(BgHeader);
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new StyleColor(ColDivider);

            var icon = new Label("▦");
            icon.style.fontSize   = 12;
            icon.style.color      = new StyleColor(new Color(0.50f, 0.72f, 0.95f));
            icon.style.marginRight = 6;
            header.Add(icon);

            var title = new Label("Blackboard");
            title.style.fontSize  = 11;
            title.style.color     = new StyleColor(ColText);
            title.style.flexGrow  = 1;
            header.Add(title);

            // "+" add button.
            var addBtn = new Button(ToggleAddForm) { text = "+" };
            addBtn.AddToClassList("bb-add-btn");
            addBtn.style.fontSize         = 14;
            addBtn.style.color            = new StyleColor(new Color(0.34f, 0.78f, 0.34f));
            addBtn.style.backgroundColor  = new StyleColor(new Color(0.14f, 0.24f, 0.18f));
            addBtn.style.borderTopLeftRadius     = 4;
            addBtn.style.borderTopRightRadius    = 4;
            addBtn.style.borderBottomLeftRadius  = 4;
            addBtn.style.borderBottomRightRadius = 4;
            addBtn.style.paddingLeft   = 6;
            addBtn.style.paddingRight  = 6;
            addBtn.style.paddingTop    = 0;
            addBtn.style.paddingBottom = 2;
            addBtn.style.borderTopWidth = addBtn.style.borderBottomWidth =
            addBtn.style.borderLeftWidth = addBtn.style.borderRightWidth = 1;
            addBtn.style.borderTopColor = addBtn.style.borderBottomColor =
            addBtn.style.borderLeftColor = addBtn.style.borderRightColor =
                new StyleColor(new Color(0.22f, 0.38f, 0.26f));
            header.Add(addBtn);

            Add(header);

            // ── Add-variable form (hidden until "+" is pressed) ───────────────
            BuildAddForm();
        }

        private void BuildAddForm()
        {
            m_AddForm = new VisualElement();
            m_AddForm.style.display         = DisplayStyle.None;
            m_AddForm.style.flexDirection   = FlexDirection.Column;
            m_AddForm.style.paddingTop         = 10;
            m_AddForm.style.paddingBottom = 10;
            m_AddForm.style.paddingLeft = 10;
            m_AddForm.style.paddingRight = 10;
            m_AddForm.style.backgroundColor = new StyleColor(new Color(0.11f, 0.14f, 0.20f));
            m_AddForm.style.borderBottomWidth = 1;
            m_AddForm.style.borderBottomColor = new StyleColor(ColDivider);

            // Name field.
            m_NewNameField = MakeTextField("Variable Name", "NewVariable");
            m_AddForm.Add(new Label("Name") { style = { fontSize = 9, color = new StyleColor(ColMuted), marginBottom = 3 } });
            m_AddForm.Add(m_NewNameField);

            // Type dropdown.
            m_AddForm.Add(new Label("Type") { style = { fontSize = 9, color = new StyleColor(ColMuted), marginTop = 6, marginBottom = 3 } });
            var typeNames = new List<string>();
            foreach (var (name, _) in VariableTypes) typeNames.Add(name);
            m_NewTypeField = new DropdownField(typeNames, 0);
            StyleDropdown(m_NewTypeField);
            m_AddForm.Add(m_NewTypeField);

            // Confirm button.
            var confirmBtn = new Button(ConfirmAddVariable) { text = "Add Variable" };
            StylePrimaryButton(confirmBtn);
            m_AddForm.Add(confirmBtn);

            // Cancel link.
            var cancelBtn = new Button(ToggleAddForm) { text = "Cancel" };
            StyleSecondaryButton(cancelBtn);
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

        public void Populate(DialogueGraphAsset asset)
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
                empty.style.color     = new StyleColor(ColMuted);
                empty.style.fontSize  = 10;
                empty.style.unityTextAlign = TextAnchor.MiddleCenter;
                empty.style.marginTop = 24;
                m_VariableList.Add(empty);
                return;
            }

            if (m_Asset.Blackboard.Variables.Count == 0)
            {
                var empty = new Label("No variables. Click + to add.");
                empty.style.color    = new StyleColor(ColMuted);
                empty.style.fontSize = 10;
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
            container.style.borderBottomWidth = 1;
            container.style.borderBottomColor = new StyleColor(ColDivider);

            // ── Collapsed header row ──────────────────────────────────────────
            var row = new VisualElement();
            row.style.flexDirection   = FlexDirection.Row;
            row.style.alignItems      = Align.Center;
            row.style.paddingTop      = 7;
            row.style.paddingBottom   = 7;
            row.style.paddingLeft     = 10;
            row.style.paddingRight    = 10;
            row.style.backgroundColor = new StyleColor(expanded ? BgRowActive : BgRow);

            // Colour dot.
            var dot = new VisualElement();
            dot.style.width           = 7;
            dot.style.height          = 7;
            dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius =
            dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 4;
            dot.style.backgroundColor = new StyleColor(typeColor);
            dot.style.marginRight     = 7;
            dot.style.flexShrink      = 0;
            row.Add(dot);

            // Variable name.
            var nameLabel = new Label(variable.Name);
            nameLabel.style.fontSize = 11;
            nameLabel.style.color    = new StyleColor(ColText);
            nameLabel.style.flexGrow = 1;
            nameLabel.style.overflow = Overflow.Hidden;
            row.Add(nameLabel);

            // Type pill.
            var typePill = new Label(GetShortTypeName(variable));
            typePill.style.fontSize        = 9;
            typePill.style.color           = new StyleColor(typeColor);
            typePill.style.backgroundColor = new StyleColor(new Color(typeColor.r, typeColor.g, typeColor.b, 0.12f));
            typePill.style.borderTopLeftRadius = typePill.style.borderTopRightRadius =
            typePill.style.borderBottomLeftRadius = typePill.style.borderBottomRightRadius = 3;
            typePill.style.paddingLeft  = 4;
            typePill.style.paddingRight = 4;
            typePill.style.marginLeft   = 4;
            row.Add(typePill);

            // Badge container — holds E and S labels, updated in-place by toggles.
            var badgeContainer = new VisualElement();
            badgeContainer.style.flexDirection = FlexDirection.Row;
            badgeContainer.style.alignItems    = Align.Center;
            row.Add(badgeContainer);
            RefreshBadges(badgeContainer, variable);

            // Expand chevron.
            var chevron = new Label(expanded ? "▾" : "▸");
            chevron.style.fontSize   = 9;
            chevron.style.color      = new StyleColor(ColMuted);
            chevron.style.marginLeft = 6;
            row.Add(chevron);

            // Click header to toggle expand.
            row.RegisterCallback<ClickEvent>(_ =>
            {
                if (m_ExpandedGuids.Contains(variable.Guid))
                    m_ExpandedGuids.Remove(variable.Guid);
                else
                    m_ExpandedGuids.Add(variable.Guid);
                RebuildList();
            });

            row.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (!m_ExpandedGuids.Contains(variable.Guid))
                    row.style.backgroundColor = new StyleColor(BgRowHover);
            });
            row.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (!m_ExpandedGuids.Contains(variable.Guid))
                    row.style.backgroundColor = new StyleColor(BgRow);
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
                b.tooltip          = "Exposed — visible on the DialogueGraphAgent Inspector";
                b.style.fontSize   = 8;
                b.style.color      = new StyleColor(new Color(0.29f, 0.61f, 0.78f));
                b.style.marginLeft = 4;
                badgeContainer.Add(b);
            }
            if (variable.Shared)
            {
                var b = new Label("S");
                b.tooltip          = "Shared — one instance across all agents";
                b.style.fontSize   = 8;
                b.style.color      = new StyleColor(new Color(0.69f, 0.42f, 0.97f));
                b.style.marginLeft = 3;
                badgeContainer.Add(b);
            }
        }

        // ── Variable detail (expanded) ────────────────────────────────────────

        private VisualElement BuildVariableDetail(BlackboardVariable variable, Label nameLabel, VisualElement badgeContainer)
        {
            var detail = new VisualElement();
            detail.style.backgroundColor = new StyleColor(new Color(0.11f, 0.12f, 0.17f));
            detail.style.paddingLeft     = 14;
            detail.style.paddingRight    = 10;
            detail.style.paddingTop      = 8;
            detail.style.paddingBottom   = 10;

            // Look up the index once for this variable. All field accessors use
            // the index directly — no repeated GUID scans, no so.Update() calls.
            int idx = m_SerializedAsset != null
                ? BlackboardPropertyBridge.FindVariableIndex(m_SerializedAsset, variable.Guid)
                : -1;

            // ── Name ─────────────────────────────────────────────────────────
            detail.Add(MakeDetailLabel("Name"));
            if (idx >= 0)
            {
                var nameProp  = BlackboardPropertyBridge.FindVariableFieldAt(m_SerializedAsset, idx, "m_Name");
                var nameField = new PropertyField(nameProp, "") { label = "" };
                nameField.Bind(m_SerializedAsset);
                nameField.RegisterValueChangeCallback(_ =>
                {
                    // Apply and update the header label in-place.
                    // Do NOT call RebuildList — that would redraw on every keystroke.
                    m_SerializedAsset.ApplyModifiedProperties();
                    nameLabel.text = variable.Name;
                });
                detail.Add(nameField);
            }
            else
            {
                var nameField = MakeTextField("", variable.Name);
                nameField.RegisterValueChangedCallback(evt =>
                {
                    variable.Name  = evt.newValue;
                    nameLabel.text = evt.newValue;
                    MarkDirty();
                });
                detail.Add(nameField);
            }

            // ── Type (read-only) ──────────────────────────────────────────────
            detail.Add(MakeDetailLabel("Type"));
            var typeLabel = new Label(GetShortTypeName(variable));
            typeLabel.style.fontSize        = 10;
            typeLabel.style.color           = new StyleColor(GetTypeColor(variable));
            typeLabel.style.backgroundColor = new StyleColor(new Color(0.10f, 0.11f, 0.16f));
            typeLabel.style.borderTopLeftRadius = typeLabel.style.borderTopRightRadius =
            typeLabel.style.borderBottomLeftRadius = typeLabel.style.borderBottomRightRadius = 3;
            typeLabel.style.paddingLeft  = 5;
            typeLabel.style.paddingRight = 5;
            typeLabel.style.paddingTop   = typeLabel.style.paddingBottom = 3;
            typeLabel.tooltip = "To change type, delete this variable and add a new one.";
            detail.Add(typeLabel);

            // ── Default Value ─────────────────────────────────────────────────
            detail.Add(MakeDetailLabel("Default Value"));
            if (idx >= 0)
            {
                var valueProp = BlackboardPropertyBridge.FindValuePropertyAt(m_SerializedAsset, idx);
                if (valueProp != null)
                {
                    var valueField = new PropertyField(valueProp, "");
                    valueField.Bind(m_SerializedAsset);
                    // PropertyField writes through the SO automatically on edit;
                    // we just need to apply so the asset gets dirtied.
                    valueField.RegisterValueChangeCallback(_ =>
                        m_SerializedAsset.ApplyModifiedProperties());
                    detail.Add(valueField);
                }
                else
                {
                    detail.Add(MakeUnserialisableLabel());
                }
            }
            else
            {
                detail.Add(MakeUnserialisableLabel());
            }

            // ── Exposed / Shared toggles ──────────────────────────────────────
            detail.Add(MakeDetailLabel(""));
            detail.Add(BuildBoundToggleAt(idx,
                "Exposed",
                "Exposed variables appear in the agent Inspector and can be set per-instance.",
                new Color(0.29f, 0.61f, 0.78f),
                variable, "m_Exposed", badgeContainer));

            detail.Add(BuildBoundToggleAt(idx,
                "Shared",
                "Shared variables have ONE value across all agents running this graph.",
                new Color(0.69f, 0.42f, 0.97f),
                variable, "m_Shared", badgeContainer));

            // ── GUID (read-only) ──────────────────────────────────────────────
            detail.Add(MakeDetailLabel("GUID"));
            var guidField = new TextField { value = variable.Guid, isReadOnly = true };
            guidField.style.fontSize = 8;
            guidField.style.color    = new StyleColor(ColMuted);
            StyleTextField(guidField);
            detail.Add(guidField);

            // ── Delete ────────────────────────────────────────────────────────
            var deleteBtn = new Button(() =>
            {
                if (m_Asset == null) return;
                Undo.RecordObject(m_Asset, "Delete Blackboard Variable");
                m_Asset.Blackboard.RemoveVariable(variable.Guid);
                m_ExpandedGuids.Remove(variable.Guid);
                EditorUtility.SetDirty(m_Asset);
                BlackboardPropertyBridge.Invalidate(m_Asset);
                m_SerializedAsset = BlackboardPropertyBridge.GetSerializedObject(m_Asset);
                RebuildList();
            }) { text = "Delete Variable" };
            deleteBtn.style.marginTop       = 10;
            deleteBtn.style.color           = new StyleColor(new Color(0.88f, 0.31f, 0.44f));
            deleteBtn.style.backgroundColor = new StyleColor(new Color(0.22f, 0.10f, 0.14f));
            deleteBtn.style.borderTopWidth = deleteBtn.style.borderBottomWidth =
            deleteBtn.style.borderLeftWidth = deleteBtn.style.borderRightWidth = 1;
            deleteBtn.style.borderTopColor = deleteBtn.style.borderBottomColor =
            deleteBtn.style.borderLeftColor = deleteBtn.style.borderRightColor =
                new StyleColor(new Color(0.40f, 0.14f, 0.20f));
            deleteBtn.style.borderTopLeftRadius = deleteBtn.style.borderTopRightRadius =
            deleteBtn.style.borderBottomLeftRadius = deleteBtn.style.borderBottomRightRadius = 4;
            detail.Add(deleteBtn);

            return detail;
        }

        private static Label MakeUnserialisableLabel()
        {
            var l = new Label("⚠  Not serialisable — set value at runtime.");
            l.style.fontSize   = 9;
            l.style.color      = new StyleColor(new Color(0.94f, 0.65f, 0.20f));
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginTop  = 2;
            return l;
        }

        /// <summary>
        /// Toggle bound to a bool field. On change it updates the badge container
        /// in-place — no RebuildList, so the rest of the panel is untouched.
        /// </summary>
        private Toggle BuildBoundToggleAt(int idx, string label, string tooltip,
            Color colour, BlackboardVariable variable, string fieldName,
            VisualElement badgeContainer)
        {
            var toggle = new Toggle(label);
            toggle.tooltip        = tooltip;
            toggle.style.fontSize = 10;
            toggle.style.color    = new StyleColor(colour);
            toggle.style.marginTop = 5;

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
                        // Update badge row in-place — no full rebuild needed.
                        RefreshBadges(badgeContainer, variable);
                    });
                    return toggle;
                }
            }

            // Fallback: no SO available.
            toggle.RegisterValueChangedCallback(evt =>
            {
                if (fieldName == "m_Exposed") variable.Exposed = evt.newValue;
                else if (fieldName == "m_Shared") variable.Shared = evt.newValue;
                MarkDirty();
                RefreshBadges(badgeContainer, variable);
            });
            return toggle;
        }

        // ── Add variable form logic ───────────────────────────────────────────

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
                _            => new BlackboardVariableString(),
            };

            newVar.Name    = name;
            newVar.Exposed = true;
            newVar.Shared  = false;

            m_Asset.Blackboard.AddVariable(newVar);
            EditorUtility.SetDirty(m_Asset);

            // Sync the SerializedObject so the new variable's index is visible
            // to FindVariableIndex before RebuildList runs.
            m_SerializedAsset?.Update();

            m_AddForm.style.display = DisplayStyle.None;
            RebuildList();
        }

        // ── Utilities ─────────────────────────────────────────────────────────

        private void MarkDirty()
        {
            if (m_Asset) EditorUtility.SetDirty(m_Asset);
        }

        private Color GetTypeColor(BlackboardVariable v)
        {
            var short_ = GetShortTypeName(v);
            foreach (var (name, col) in VariableTypes)
                if (name == short_) return col;
            return Color.gray;
        }

        private static string GetShortTypeName(BlackboardVariable v)
        {
            if (v == null) return "Unknown";
            return v switch
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
                _                            => v.ValueType?.Name ?? "Unknown",
            };
        }

        // ── Style helpers ─────────────────────────────────────────────────────

        private static TextField MakeTextField(string placeholder, string value)
        {
            var tf = new TextField { value = value };
            StyleTextField(tf);
            return tf;
        }

        private static void StyleTextField(TextField tf)
        {
            tf.style.fontSize         = 10;
            tf.style.color            = new StyleColor(new Color(0.82f, 0.86f, 0.95f));
            tf.style.backgroundColor  = new StyleColor(new Color(0.10f, 0.11f, 0.16f));
            tf.style.borderTopWidth = tf.style.borderBottomWidth =
            tf.style.borderLeftWidth = tf.style.borderRightWidth = 1;
            tf.style.borderTopColor = tf.style.borderBottomColor =
            tf.style.borderLeftColor = tf.style.borderRightColor =
                new StyleColor(new Color(0.20f, 0.22f, 0.30f));
            tf.style.borderTopLeftRadius = tf.style.borderTopRightRadius =
            tf.style.borderBottomLeftRadius = tf.style.borderBottomRightRadius = 3;
            tf.style.paddingLeft   = 5;
            tf.style.paddingRight  = 5;
            tf.style.paddingTop    = 3;
            tf.style.paddingBottom = 3;
        }

        private static void StyleDropdown(DropdownField dd)
        {
            dd.style.fontSize        = 10;
            dd.style.color           = new StyleColor(new Color(0.82f, 0.86f, 0.95f));
            dd.style.backgroundColor = new StyleColor(new Color(0.10f, 0.11f, 0.16f));
            dd.style.borderTopWidth = dd.style.borderBottomWidth =
            dd.style.borderLeftWidth = dd.style.borderRightWidth = 1;
            dd.style.borderTopColor = dd.style.borderBottomColor =
            dd.style.borderLeftColor = dd.style.borderRightColor =
                new StyleColor(new Color(0.20f, 0.22f, 0.30f));
            dd.style.borderTopLeftRadius = dd.style.borderTopRightRadius =
            dd.style.borderBottomLeftRadius = dd.style.borderBottomRightRadius = 3;
        }

        private static Label MakeDetailLabel(string text)
        {
            var l = new Label(text);
            l.style.fontSize   = 9;
            l.style.color      = new StyleColor(new Color(0.50f, 0.55f, 0.65f));
            l.style.marginTop  = 6;
            l.style.marginBottom = 3;
            return l;
        }

        private static void StylePrimaryButton(Button btn)
        {
            btn.style.marginTop          = 10;
            btn.style.fontSize           = 10;
            btn.style.color              = new StyleColor(new Color(0.20f, 0.85f, 0.60f));
            btn.style.backgroundColor    = new StyleColor(new Color(0.12f, 0.28f, 0.22f));
            btn.style.borderTopWidth = btn.style.borderBottomWidth =
            btn.style.borderLeftWidth = btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = btn.style.borderBottomColor =
            btn.style.borderLeftColor = btn.style.borderRightColor =
                new StyleColor(new Color(0.18f, 0.42f, 0.32f));
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius =
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 4;
            btn.style.paddingTop = btn.style.paddingBottom = 5;
        }

        private static void StyleSecondaryButton(Button btn)
        {
            btn.style.marginTop          = 5;
            btn.style.fontSize           = 10;
            btn.style.color              = new StyleColor(new Color(0.55f, 0.60f, 0.70f));
            btn.style.backgroundColor    = new StyleColor(new Color(0.14f, 0.15f, 0.20f));
            btn.style.borderTopWidth = btn.style.borderBottomWidth =
            btn.style.borderLeftWidth = btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = btn.style.borderBottomColor =
            btn.style.borderLeftColor = btn.style.borderRightColor =
                new StyleColor(new Color(0.22f, 0.24f, 0.32f));
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius =
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 4;
            btn.style.paddingTop = btn.style.paddingBottom = 4;
        }
    }
}
