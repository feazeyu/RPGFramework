using Game.Core.Utilities;
using System;
using UnityEditor;
using UnityEngine;

#nullable enable
namespace Game.Items
{
    [Serializable]
    public struct ItemShape
    {
        public Vector2Int[] Positions;

        public bool[,] GetBoolGrid(int dimension = 5) {
            bool[,] bools = new bool[dimension,dimension];
            foreach (Vector2Int i in Positions)
            {
                bools[i.x,i.y] = true;
            }
            return bools;
        }
        public readonly Vector2Int GetOffset()
        {
            if (Positions is null or { Length: 0 })
            {
                return Vector2Int.zero;
            }

            Vector2Int offset = new(int.MaxValue, int.MaxValue);
            for (int i = 0; i < Positions.Length; i++)
            {
                offset = Vector2Int.Min(offset, Positions[i]);
            }
            return offset;
        }

        public readonly Vector2 GetCenter()
        {
            if (Positions is null or { Length: 0 })
            {
                return Vector2.zero;
            }

            Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
            for (int i = 0; i < Positions.Length; i++)
            {
                min = Vector2.Min(min, Positions[i]);
                max = Vector2.Max(max, Positions[i]);
            }
            return min + (max - min) / 2;
        }

        public readonly void Trim()
        {
            if (Positions is null or { Length: 0 })
            {
                return;
            }

            Vector2Int min = new(int.MaxValue, int.MaxValue);
            for (int i = 0; i < Positions.Length; i++)
            {
                min = Vector2Int.Min(min, Positions[i]);
            }

            if (min == Vector2Int.zero)
            {
                return;
            }

            for (int i = 0; i < Positions.Length; i++)
            {
                Positions[i] -= min;
            }
        }
    }

    [CustomPropertyDrawer(typeof(ItemShape))]
    public sealed class ShapeDrawer : PropertyDrawer
    {
        private static readonly float lineHeight = EditorGUIUtility.singleLineHeight;
        private static readonly float maxTileSize = lineHeight * 1.4f;

        private bool HasValidTarget(SerializedProperty property)
        {
            if (property.serializedObject.targetObject is not ItemInfo)
            {
                Debug.LogWarning($"Shape property is for Item objects only (got {property.serializedObject.GetType().Name}).");
                return false;
            }
            return true;
        }

        private int GetTier(SerializedProperty property)
        {
            return property.serializedObject.FindProperty("_tier").intValue;
        }

        private float GetTileSize(float width, int tileCount, out float offset)
        {
            offset = 0f;
            width = Mathf.Clamp(width - 30f, 50f, 500f);

            if (tileCount * maxTileSize <= width)
            {
                offset += (width - (tileCount * maxTileSize)) / 2f;
                return maxTileSize;
            }

            return width / tileCount;
        }

        private ReadOnlySpan<Vector2Int> GetSpan(SerializedProperty property)
        {
            return ((ItemShape)property.boxedValue).Positions.AsSpan();
        }

        private void CropShapeIfOutsideGrid(SerializedProperty property, int tier)
        {
            int invalidCount = 0;
            ReadOnlySpan<Vector2Int> shape = GetSpan(property);
            for (int i = 0; i < shape.Length; i++)
            {
                if ((uint)shape[i].x >= (uint)tier || (uint)shape[i].y >= (uint)tier)
                {
                    invalidCount++;
                }
            }

            if (invalidCount == 0)
            {
                return;
            }

            int destination = 0;
            var cropped = new Vector2Int[shape.Length - invalidCount];
            for (int i = 0; i < shape.Length; i++)
            {
                if ((uint)shape[i].x < (uint)tier && (uint)shape[i].y < (uint)tier)
                {
                    cropped[destination++] = shape[i];
                }
            }

            property.boxedValue = new ItemShape() { Positions = cropped };
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!HasValidTarget(property))
            {
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            int tier = GetTier(property);
            CropShapeIfOutsideGrid(property, tier);

            position = position.PushDown(lineHeight / 2f);
            if (position.width >= 300)
            {
                EditorGUI.LabelField(position, label);
            }

            Color originalColor = GUI.backgroundColor;
            ReadOnlySpan<Vector2Int> shape = GetSpan(property);
            float tileSize = GetTileSize(position.width, tier, out float offset);
            for (int y = 0; y < tier; y++)
            {
                for (int x = 0; x < tier; x++)
                {
                    Rect rect = position.Push(offset + x * tileSize, y * tileSize).Crop(tileSize);

                    int index = shape.IndexOf(new Vector2Int(x, y));
                    bool enabled = index >= 0;
                    GUI.backgroundColor = enabled ? Color.green : Color.gray;
                    if (GUI.Button(rect, string.Empty))
                    {
                        Vector2Int[] updated;
                        if (enabled)
                        {
                            updated = new Vector2Int[shape.Length - 1];
                            var span = updated.AsSpan();
                            shape[..index].CopyTo(span);
                            shape[(index + 1)..].CopyTo(span[index..]);
                        }
                        else
                        {
                            updated = new Vector2Int[shape.Length + 1];
                            shape.CopyTo(updated);
                            updated[^1] = new Vector2Int(x, y);
                        }
                        property.boxedValue = new ItemShape() { Positions = updated };
                    }
                }
            }
            GUI.backgroundColor = originalColor;

            Rect buttonRect = position
                .Push(offset, tileSize * tier + (lineHeight / 3f))
                .Crop(tileSize * tier, lineHeight);
            ItemShape itemShape = (ItemShape)property.boxedValue;
            GUI.enabled = itemShape.GetOffset() != Vector2Int.zero;
            if (GUI.Button(buttonRect, "Normalize"))
            {
                itemShape.Trim();
                property.boxedValue = itemShape;
            }
            GUI.enabled = true;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!HasValidTarget(property))
            {
                return 0f;
            }

            int tier = GetTier(property);
            return (2 * lineHeight) + tier * GetTileSize(EditorGUIUtility.currentViewWidth, tier, out _);
        }
    }
}
