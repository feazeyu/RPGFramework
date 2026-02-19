

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Game.Dialogue {
    public class DialogueGraphView : GraphView
    {
        public Blackboard Blackboard;
        private DialogueGraphEditor _editor;
        public List<string> ExposedProperties = new List<string>(); //TODO
        public DialogueGraphView(DialogueGraphEditor editor)
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            _editor = editor;
        }
    }
}