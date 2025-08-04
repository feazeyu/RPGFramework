using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace Game.Utils
{
    /// <summary>
    /// Redirects UI pointer and drag events from this GameObject to a specified target GameObject.
    /// </summary>
    public class EventRedirector : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IScrollHandler
    {
        [Tooltip("Target GameObject to receive redirected events.")]
        public GameObject redirectTarget;

        public void OnPointerEnter(PointerEventData eventData)
        {
            Redirect<IPointerEnterHandler>(eventData, ExecuteEvents.pointerEnterHandler);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Redirect<IPointerExitHandler>(eventData, ExecuteEvents.pointerExitHandler);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Redirect<IPointerDownHandler>(eventData, ExecuteEvents.pointerDownHandler);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Redirect<IPointerUpHandler>(eventData, ExecuteEvents.pointerUpHandler);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Redirect<IPointerClickHandler>(eventData, ExecuteEvents.pointerClickHandler);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Redirect<IBeginDragHandler>(eventData, ExecuteEvents.beginDragHandler);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Redirect<IDragHandler>(eventData, ExecuteEvents.dragHandler);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Redirect<IEndDragHandler>(eventData, ExecuteEvents.endDragHandler);
        }

        public void OnScroll(PointerEventData eventData)
        {
            Redirect<IScrollHandler>(eventData, ExecuteEvents.scrollHandler);
        }

        private void Redirect<T>(PointerEventData eventData, ExecuteEvents.EventFunction<T> handler)
            where T : IEventSystemHandler
        {
            //Debug.Log($"Redirecting {typeof(T).Name} event from {gameObject.name} to {redirectTarget?.name} at position {eventData.position} with delta {eventData.delta}");
            if (redirectTarget != null)
            {
                ExecuteEvents.Execute(redirectTarget, eventData, handler);
            }
        }


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
