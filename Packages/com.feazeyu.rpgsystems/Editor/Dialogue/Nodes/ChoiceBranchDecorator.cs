using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueGraph.Runtime;

namespace DialogueGraph.Editor
{
    /// <summary>
    /// Injects the "Add Choice / Remove Choice" UI into ChoiceBranch node cards.
    /// Registered automatically when the editor loads via [InitializeOnLoadMethod].
    ///
    /// This class contains no state — it only builds VisualElements and mutates
    /// NodeData + GraphAsset directly, then calls rebuild() to sync the
    /// node view's ports.
    /// </summary>
    [InitializeOnLoad]
    public static class ChoiceBranchDecorator
    {
        static ChoiceBranchDecorator()
        {
            NodeViewDecoratorRegistry.Register(NodeRegistry.TypeChoiceBranch, Decorate);
        }

        private static void Decorate(
            VisualElement      container,
            NodeData           node,
            GraphAsset asset,
            System.Action      rebuild)
        {
            int choiceCount = node.Ports.FindAll(p => p.Direction == PortDirection.Output).Count;
            RebuildChoiceRows(container, node, asset, choiceCount, rebuild);

            var addBtn = new Button(() => { AddChoice(node, asset); rebuild(); }) { text = "+ Add Choice" };
            addBtn.AddToClassList("choice-add-btn");
            container.Add(addBtn);
        }

        // ── Choice row rebuild ────────────────────────────────────────────────

        private static void RebuildChoiceRows(
            VisualElement      container,
            NodeData           node,
            GraphAsset asset,
            int                choiceCount,
            System.Action      rebuild)
        {
            // Remove existing choice rows (they were built without remove buttons).
            var toRemove = new List<VisualElement>();
            foreach (var child in container.Children())
            {
                // Identify choice rows by USS class.
                if (child.ClassListContains("node-field-row") &&
                    child.userData is string fn &&
                    fn.StartsWith("Choice ") && fn.EndsWith(" Text"))
                {
                    toRemove.Add(child);
                }
            }
            foreach (var el in toRemove) container.Remove(el);

            // Re-add them with remove buttons.
            foreach (var field in node.Fields)
            {
                if (!field.FieldName.StartsWith("Choice ") ||
                    !field.FieldName.EndsWith(" Text")) continue;

                container.Add(BuildChoiceRow(field, node, asset, choiceCount, rebuild));
            }
        }

        private static VisualElement BuildChoiceRow(
            FieldData          field,
            NodeData           node,
            GraphAsset asset,
            int                totalChoices,
            System.Action      rebuild)
        {
            var row = new VisualElement();
            row.AddToClassList("node-field-row");
            row.userData = field.FieldName; // used for identification during rebuild

            // "×" remove button — hidden when only one choice remains.
            var removeBtn = new Button(() =>
            {
                RemoveChoice(field, node, asset);
                rebuild();
            }) { text = "×" };

            removeBtn.AddToClassList("choice-remove-btn");
            removeBtn.style.display = totalChoices > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            row.Add(removeBtn);

            var nameLabel = new Label(field.FieldName);
            nameLabel.AddToClassList("node-field-name");
            row.Add(nameLabel);

            if (!string.IsNullOrEmpty(field.LinkedVariableGuid))
            {
                var bbVar = asset.Blackboard.GetVariable(field.LinkedVariableGuid);
                var linked = new Label("⟵ " + (bbVar?.Name ?? "?"));
                linked.AddToClassList("node-field-linked");
                row.Add(linked);
            }
            else
            {
                var valueField = new TextField { value = field.InlineValue ?? "" };
                valueField.AddToClassList("node-field-value");
                valueField.RegisterValueChangedCallback(evt =>
                {
                    field.InlineValue = evt.newValue;
                    EditorUtilityHelper.SetDirty(asset);
                });
                row.Add(valueField);
            }

            // Link dot with drag-and-drop — same as regular field rows.
            var dot = new VisualElement();
            dot.AddToClassList("node-field-link-dot");
            dot.tooltip = string.IsNullOrEmpty(field.LinkedVariableGuid)
                ? "Drag a Blackboard variable here to link"
                : "Linked — click to unlink";
            if (!string.IsNullOrEmpty(field.LinkedVariableGuid))
                dot.AddToClassList("linked");

            dot.RegisterCallback<ClickEvent>(evt =>
            {
                if (string.IsNullOrEmpty(field.LinkedVariableGuid)) return;
                field.LinkedVariableGuid = null;
                EditorUtilityHelper.SetDirty(asset);
                rebuild();
                evt.StopPropagation();
            });

            row.RegisterCallback<DragEnterEvent>(_ =>
            {
                if (DragAndDrop.GetGenericData("BlackboardVariableGuid") is string)
                    row.AddToClassList("drag-over");
            });
            row.RegisterCallback<DragLeaveEvent>(_ => row.RemoveFromClassList("drag-over"));
            row.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (DragAndDrop.GetGenericData("BlackboardVariableGuid") is string)
                { DragAndDrop.visualMode = DragAndDropVisualMode.Link; evt.StopPropagation(); }
            });
            row.RegisterCallback<DragPerformEvent>(evt =>
            {
                if (DragAndDrop.GetGenericData("BlackboardVariableGuid") is not string guid) return;
                DragAndDrop.AcceptDrag();
                row.RemoveFromClassList("drag-over");
                field.LinkedVariableGuid  = guid;
                EditorUtilityHelper.SetDirty(asset);
                rebuild();
                evt.StopPropagation();
            });
            row.RegisterCallback<DragExitedEvent>(_ => row.RemoveFromClassList("drag-over"));

            row.Add(dot);
            return row;
        }

        // ── Data mutations ────────────────────────────────────────────────────

        private static void AddChoice(NodeData node, GraphAsset asset)
        {
            int next = 1;
            foreach (var p in node.Ports)
            {
                if (p.Direction == PortDirection.Output &&
                    p.PortName.StartsWith("Choice ") &&
                    int.TryParse(p.PortName.Substring(7), out int n))
                    next = Mathf.Max(next, n + 1);
            }

            Undo.RecordObject(asset, "Add Choice");
            node.Ports.Add(new PortData
            {
                PortName  = $"Choice {next}",
                Direction = PortDirection.Output,
                Capacity  = PortCapacity.Single,
            });
            node.Fields.Add(new FieldData
            {
                FieldName = $"Choice {next} Text",
                TypeName  = "System.String",
            });
            EditorUtilityHelper.SetDirty(asset);
        }

        private static void RemoveChoice(FieldData field, NodeData node, GraphAsset asset)
        {
            var portName = field.FieldName.EndsWith(" Text")
                ? field.FieldName.Substring(0, field.FieldName.Length - 5)
                : null;

            Undo.RecordObject(asset, "Remove Choice");

            if (portName != null)
                node.Ports.RemoveAll(p => p.PortName == portName && p.Direction == PortDirection.Output);
            node.Fields.Remove(field);

            if (portName != null)
            {
                var toRemove = new List<string>();
                foreach (var e in asset.Edges)
                    if (e.OutputNodeGuid == node.Guid && e.OutputPortName == portName)
                        toRemove.Add(e.Guid);
                foreach (var guid in toRemove)
                    asset.RemoveEdge(guid);
            }

            EditorUtilityHelper.SetDirty(asset);
        }

    }
}
