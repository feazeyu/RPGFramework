using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace Feazeyu.RPGSystems.Core
{
    /// <summary>
    /// Redirects UI pointer and drag events from this GameObject to a specified target GameObject.
    /// </summary>
    public class EventRedirector : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
        IScrollHandler
    {
        /// <summary>Redirect target.</summary>
        [Tooltip("Target GameObject to receive redirected events.")]
        public GameObject redirectTarget;

        /// <inheritdoc/>
        public void OnPointerEnter(PointerEventData eventData)
        {
            Redirect<IPointerEnterHandler>(eventData, ExecuteEvents.pointerEnterHandler);
        }

        /// <inheritdoc/>
        public void OnPointerExit(PointerEventData eventData)
        {
            Redirect<IPointerExitHandler>(eventData, ExecuteEvents.pointerExitHandler);
        }

        /// <inheritdoc/>
        public void OnPointerDown(PointerEventData eventData)
        {
            Redirect<IPointerDownHandler>(eventData, ExecuteEvents.pointerDownHandler);
        }

        /// <inheritdoc/>
        public void OnPointerUp(PointerEventData eventData)
        {
            Redirect<IPointerUpHandler>(eventData, ExecuteEvents.pointerUpHandler);
        }

        /// <inheritdoc/>
        public void OnPointerClick(PointerEventData eventData)
        {
            Redirect<IPointerClickHandler>(eventData, ExecuteEvents.pointerClickHandler);
        }

        /// <inheritdoc/>
        public void OnBeginDrag(PointerEventData eventData)
        {
            Redirect<IBeginDragHandler>(eventData, ExecuteEvents.beginDragHandler);
        }

        /// <inheritdoc/>
        public void OnDrag(PointerEventData eventData)
        {
            Redirect<IDragHandler>(eventData, ExecuteEvents.dragHandler);
        }

        /// <inheritdoc/>
        public void OnEndDrag(PointerEventData eventData)
        {
            Redirect<IEndDragHandler>(eventData, ExecuteEvents.endDragHandler);
        }

        /// <inheritdoc/>
        public void OnDrop(PointerEventData eventData)
        {
            Redirect<IDropHandler>(eventData, ExecuteEvents.dropHandler);
        }

        /// <inheritdoc/>
        public void OnScroll(PointerEventData eventData)
        {
            Redirect<IScrollHandler>(eventData, ExecuteEvents.scrollHandler);
        }

        private void Redirect<T>(PointerEventData eventData, ExecuteEvents.EventFunction<T> handler)
            where T : IEventSystemHandler
        {
            if (redirectTarget != null)
            {
                ExecuteEvents.Execute(redirectTarget, eventData, handler);
            }
        }

        /// <summary>
        /// Creates a transparent, full-rect raycast-blocking overlay under <paramref name="parent"/>
        /// that forwards all pointer events to <paramref name="target"/>.
        /// </summary>
        /// <param name="parent">GameObject the overlay is parented to and stretched over.</param>
        /// <param name="target">GameObject that receives the redirected events.</param>
        public static void AddEventRedirector(GameObject parent, GameObject target)
        {
            var raycastBlocker = new GameObject();
            raycastBlocker.name = "EventRedirector";
            var rectTransform = raycastBlocker.AddComponent<RectTransform>();
            rectTransform.SetParent(parent.transform, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            Image img = raycastBlocker.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            raycastBlocker.AddComponent<CanvasGroup>().blocksRaycasts = true;
            raycastBlocker.AddComponent<EventRedirector>().redirectTarget = target;
        }
    }
}
