#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

#nullable enable
namespace Feazeyu.RPGSystems.Core.Utilities
{
#if UNITY_EDITOR
    /// <summary>
    /// IMGUI convenience helpers for custom inspectors and property drawers:
    /// cached styles/icons and a set of <see cref="Rect"/> layout extensions.
    /// </summary>
    public static class EditorHelper
    {
        /// <summary>Height of a single editor line, from <see cref="EditorGUIUtility.singleLineHeight"/>.</summary>
        public static float LineHeight => EditorGUIUtility.singleLineHeight;

        /// <summary>Lazily resolved and cached built-in trash-can icon.</summary>
        public static Texture2D TrashIcon => _trashIcon = _trashIcon != null ? _trashIcon : EditorGUIUtility.FindTexture("TreeEditor.Trash");
        private static Texture2D? _trashIcon;

        /// <summary>A label style identical to <see cref="EditorStyles.label"/> but centered.</summary>
        public static readonly GUIStyle LabelCentered = new()
        {
            normal = EditorStyles.label.normal,
            alignment = TextAnchor.MiddleCenter
        };

        /// <summary>Returns a copy of <paramref name="rect"/> moved right by <paramref name="value"/>, shrinking its width to match.</summary>
        public static Rect PushRight(this Rect rect, float value)
        {
            value = Mathf.Min(rect.width, value);
            rect.x += value;
            rect.width -= value;
            return rect;
        }

        /// <summary>Returns a copy of <paramref name="rect"/> moved down by <paramref name="value"/>, shrinking its height to match.</summary>
        public static Rect PushDown(this Rect rect, float value)
        {
            value = Mathf.Min(rect.height, value);
            rect.y += value;
            rect.height -= value;
            return rect;
        }

        /// <summary>Returns a copy of <paramref name="rect"/> pushed right by <paramref name="x"/> and down by <paramref name="y"/>, shrinking accordingly.</summary>
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

        /// <summary>Returns a copy of <paramref name="rect"/> with its width clamped to <paramref name="maxWidth"/>.</summary>
        public static Rect CropWidth(this Rect rect, float maxWidth)
        {
            rect.width = Mathf.Min(rect.width, maxWidth);
            return rect;
        }

        /// <summary>Returns a copy of <paramref name="rect"/> with its height clamped to <paramref name="maxHeight"/>.</summary>
        public static Rect CropHeight(this Rect rect, float maxHeight)
        {
            rect.height = Mathf.Min(rect.height, maxHeight);
            return rect;
        }

        /// <summary>Returns a copy of <paramref name="rect"/> with width and height clamped independently.</summary>
        public static Rect Crop(this Rect rect, float maxWidth, float maxHeight)
        {
            rect.width = Mathf.Min(rect.width, maxWidth);
            rect.height = Mathf.Min(rect.height, maxHeight);
            return rect;
        }

        /// <summary>Returns a copy of <paramref name="rect"/> with both width and height clamped to <paramref name="size"/>.</summary>
        public static Rect Crop(this Rect rect, float size)
        {
            return Crop(rect, size, size);
        }

        /// <summary>Returns a <paramref name="width"/>-wide slice anchored to the right edge of <paramref name="rect"/>.</summary>
        public static Rect SliceRight(this Rect rect, float width)
        {
            width = Mathf.Min(rect.width, width);
            rect.x += (rect.width - width);
            rect.width = width;
            return rect;
        }

        /// <summary>Returns a <paramref name="width"/>-wide slice horizontally centered within <paramref name="rect"/>.</summary>
        public static Rect SliceCenter(this Rect rect, float width)
        {
            width = Mathf.Min(rect.width, width);
            rect.x += (rect.width - width) / 2f;
            rect.width = width;
            return rect;
        }

        /// <summary>Draws a label at <paramref name="rect"/> and returns the rendered text's size.</summary>
        public static Vector2 Label(Rect rect, string text)
        {
            var content = new GUIContent(text);
            EditorGUI.LabelField(rect, content, EditorStyles.label);
            return EditorStyles.label.CalcSize(content);
        }
    }
#endif
}
