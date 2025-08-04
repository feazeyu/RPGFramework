using UnityEditor;
using UnityEngine;

#nullable enable
namespace Game.Core.Utilities
{
    public static class EditorHelper
    {
        public static float LineHeight => EditorGUIUtility.singleLineHeight;

        public static Texture2D TrashIcon => _trashIcon = _trashIcon != null ? _trashIcon : EditorGUIUtility.FindTexture("TreeEditor.Trash");
        private static Texture2D? _trashIcon;

        public static readonly GUIStyle LabelCentered = new()
        {
            normal = EditorStyles.label.normal,
            alignment = TextAnchor.MiddleCenter
        };

        public static Rect PushRight(this Rect rect, float value)
        {
            value = Mathf.Min(rect.width, value);
            rect.x += value;
            rect.width -= value;
            return rect;
        }

        public static Rect PushDown(this Rect rect, float value)
        {
            value = Mathf.Min(rect.height, value);
            rect.y += value;
            rect.height -= value;
            return rect;
        }

        public static Rect Push(this Rect rect, float x, float y)
        {
            x = Mathf.Min(rect.width, x);
            y = Mathf.Min(rect.height, y);
            rect.x += x;
            rect.y += y;
            rect.width -= x;
            rect.height -= y;
            return rect;
        }

        public static Rect CropWidth(this Rect rect, float maxWidth)
        {
            rect.width = Mathf.Min(rect.width, maxWidth);
            return rect;
        }

        public static Rect CropHeight(this Rect rect, float maxHeight)
        {
            rect.height = Mathf.Min(rect.height, maxHeight);
            return rect;
        }

        public static Rect Crop(this Rect rect, float maxWidth, float maxHeight)
        {
            rect.width = Mathf.Min(rect.width, maxWidth);
            rect.height = Mathf.Min(rect.height, maxHeight);
            return rect;
        }

        public static Rect Crop(this Rect rect, float size)
        {
            return Crop(rect, size, size);
        }

        public static Rect SliceRight(this Rect rect, float width)
        {
            width = Mathf.Min(rect.width, width);
            rect.x += (rect.width - width);
            rect.width = width;
            return rect;
        }

        public static Rect SliceCenter(this Rect rect, float width)
        {
            width = Mathf.Min(rect.width, width);
            rect.x += (rect.width - width) / 2f;
            rect.width = width;
            return rect;
        }

        public static Vector2 Label(Rect rect, string text)
        {
            var content = new GUIContent(text);
            EditorGUI.LabelField(rect, content, EditorStyles.label);
            return EditorStyles.label.CalcSize(content);
        }
    }
}
