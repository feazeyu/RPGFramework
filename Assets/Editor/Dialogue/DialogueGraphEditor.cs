using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Dialogue
{
    public class DialogueGraphEditor : EditorWindow
    {

        private DialogueGraphView _graphView;
        private string _fileName = "New Dialogue Graph";

        [MenuItem("RPGFramework/Dialogue/Dialogue Graph")]
        public static void CreateWindow()
        {
            var window = GetWindow<DialogueGraphEditor>();
            window.titleContent = new GUIContent("Dialogue Graph");
        }

        
        private void CreateView()
        {
            _graphView = new DialogueGraphView(this)
            {
                name = "Dialogue Graph",
            };
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);
        }

        private void GenerateToolbar()
        {
            var toolbar = new Toolbar();

            var fileNameTextField = new TextField("File Name:");
            fileNameTextField.SetValueWithoutNotify(_fileName);
            fileNameTextField.MarkDirtyRepaint();
            fileNameTextField.RegisterValueChangedCallback(evt => _fileName = evt.newValue);
            toolbar.Add(fileNameTextField);

            toolbar.Add(new Button(() => Debug.Log("TODO: Saving")) { text = "Save Data" });

            toolbar.Add(new Button(() => Debug.Log("TODO: Loading")) { text = "Load Data" });
            // toolbar.Add(new Button(() => _graphView.CreateNewDialogueNode("Dialogue Node")) {text = "New Node",});
            rootVisualElement.Add(toolbar);
        }


        private void OnEnable()
        {
            CreateView();
            GenerateToolbar();
            GenerateBlackBoard();
        }
        
        public void AddPropertyToBlackBoard(string property, bool loadMode = false)
        {
            var localPropertyName = property;
            if (!loadMode)
            {
                while (_graphView.ExposedProperties.Any(x => x == localPropertyName))
                    localPropertyName = $"{localPropertyName}(1)"; //TODO: Fix, it will end up in (1)(1)(1)...
            }

            var item = property;
            item = localPropertyName;
            _graphView.ExposedProperties.Add(item);

            var container = new VisualElement();
            var field = new BlackboardField { text = localPropertyName, typeText = "string" };
            container.Add(field);

            var propertyValueTextField = new TextField("Value:")
            {
                value = localPropertyName
            };
            propertyValueTextField.RegisterValueChangedCallback(evt =>
            {
                var index = _graphView.ExposedProperties.FindIndex(x => x == item);
                if (index != -1) { 
                    _graphView.ExposedProperties[index] = evt.newValue;
                }
            });
            var sa = new BlackboardRow(field, propertyValueTextField);
            container.Add(sa);
            _graphView.Blackboard.Add(container);
        }
        private void GenerateBlackBoard()
        {
            var blackboard = new Blackboard(_graphView);
            blackboard.Add(new BlackboardSection { title = "Exposed Variables" });
            blackboard.addItemRequested = _blackboard =>
            {
                AddPropertyToBlackBoard("Test", false);
            };
            blackboard.editTextRequested = (_blackboard, element, newValue) =>
            {
                ((BlackboardField)element).text = newValue;
            };
            blackboard.SetPosition(new Rect(10, 30, 200, 300));
            _graphView.Add(blackboard);
            _graphView.Blackboard = blackboard;
        }
        private void OnDisable()
        {
            rootVisualElement.Remove(_graphView);
        }
    }
}
