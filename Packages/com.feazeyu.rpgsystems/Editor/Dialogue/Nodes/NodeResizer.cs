using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Feazeyu.RPGSystems.EditorTools
{
    /// <summary>
    /// Adds resize handles to a VisualElement (the node card).
    ///
    /// Seven hit zones are overlaid on the node edges and corners:
    ///
    /// Only right, bottom and the SE corner are exposed — top/left would
    /// fight with the GraphView's own drag-to-move gesture.
    ///
    /// Usage:
    ///   target.AddManipulator(new NodeResizer(onResized));
    /// </summary>
    public class NodeResizer : MouseManipulator
    {
        // ── Constants ──────────────────────────────────────────────────────────

        private const float HandleSize   = 6f;   // px width/height of edge handle zone
        private const float MinWidth     = 160f;
        private const float MinHeight    = 60f;

        // ── State ──────────────────────────────────────────────────────────────

        private enum ResizeEdge { None, E, S, SE }

        private ResizeEdge m_ActiveEdge = ResizeEdge.None;
        private Vector2    m_StartMouse;
        private Vector2    m_StartSize;
        private bool       m_Active;

        private readonly Action<Vector2> m_OnResized; // called every frame while dragging & on release

        // Handle overlay elements so we can set cursor properly.
        private VisualElement m_HandleE;
        private VisualElement m_HandleS;
        private VisualElement m_HandleSE;

        // ── Construction ───────────────────────────────────────────────────────

        public NodeResizer(Action<Vector2> onResized)
        {
            m_OnResized    = onResized;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        // ── Registration ───────────────────────────────────────────────────────

        protected override void RegisterCallbacksOnTarget()
        {
            // Build the three handle overlays inside the target.
            m_HandleE  = MakeHandle("node-resize-handle-e");
            m_HandleS  = MakeHandle("node-resize-handle-s");
            m_HandleSE = MakeHandle("node-resize-handle-se");

            target.Add(m_HandleE);
            target.Add(m_HandleS);
            target.Add(m_HandleSE);

            // Lay them out once the geometry is known.
            target.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            // Mouse events on the handles.
            m_HandleE.RegisterCallback<MouseDownEvent>(OnMouseDown);
            m_HandleS.RegisterCallback<MouseDownEvent>(OnMouseDown);
            m_HandleSE.RegisterCallback<MouseDownEvent>(OnMouseDown);

            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);

            m_HandleE?.RemoveFromHierarchy();
            m_HandleS?.RemoveFromHierarchy();
            m_HandleSE?.RemoveFromHierarchy();
        }

        // ── Handle layout ──────────────────────────────────────────────────────

        private static VisualElement MakeHandle(string ussClass)
        {
            var h = new VisualElement();
            h.AddToClassList("node-resize-handle");
            h.AddToClassList(ussClass);
            h.pickingMode = PickingMode.Position;
            return h;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            PositionHandles(target.layout.width, target.layout.height);
        }

        private void PositionHandles(float w, float h)
        {
            if (w <= 0 || h <= 0) return;

            // Right edge — full height, HandleSize wide.
            m_HandleE.style.position = Position.Absolute;
            m_HandleE.style.right    = 0;
            m_HandleE.style.top      = HandleSize;
            m_HandleE.style.width    = HandleSize;
            m_HandleE.style.height   = h - HandleSize * 2;

            // Bottom edge — full width, HandleSize tall.
            m_HandleS.style.position = Position.Absolute;
            m_HandleS.style.bottom   = 0;
            m_HandleS.style.left     = HandleSize;
            m_HandleS.style.width    = w - HandleSize * 2;
            m_HandleS.style.height   = HandleSize;

            // SE corner.
            m_HandleSE.style.position = Position.Absolute;
            m_HandleSE.style.right    = 0;
            m_HandleSE.style.bottom   = 0;
            m_HandleSE.style.width    = HandleSize * 2;
            m_HandleSE.style.height   = HandleSize * 2;
        }

        // ── Mouse handlers ─────────────────────────────────────────────────────

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (!CanStartManipulation(evt)) return;

            var handle = evt.target as VisualElement;
            if (handle == m_HandleE)       m_ActiveEdge = ResizeEdge.E;
            else if (handle == m_HandleS)  m_ActiveEdge = ResizeEdge.S;
            else if (handle == m_HandleSE) m_ActiveEdge = ResizeEdge.SE;
            else return;

            m_StartMouse = evt.mousePosition;
            m_StartSize  = new Vector2(target.resolvedStyle.width, target.resolvedStyle.height);
            m_Active     = true;

            target.CaptureMouse();
            evt.StopPropagation();
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (!m_Active) return;

            var delta = evt.mousePosition - m_StartMouse;
            var newW  = m_StartSize.x;
            var newH  = m_StartSize.y;

            if (m_ActiveEdge == ResizeEdge.E || m_ActiveEdge == ResizeEdge.SE)
                newW = Mathf.Max(MinWidth,  m_StartSize.x + delta.x);
            if (m_ActiveEdge == ResizeEdge.S || m_ActiveEdge == ResizeEdge.SE)
                newH = Mathf.Max(MinHeight, m_StartSize.y + delta.y);

            target.style.width  = newW;
            target.style.height = newH;

            PositionHandles(newW, newH);
            m_OnResized?.Invoke(new Vector2(newW, newH));

            evt.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (!m_Active) return;
            m_Active     = false;
            m_ActiveEdge = ResizeEdge.None;
            target.ReleaseMouse();
            evt.StopPropagation();
        }
    }
}
