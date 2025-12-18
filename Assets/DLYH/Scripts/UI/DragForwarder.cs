// DragForwarder.cs
// Forwards drag events to a parent component that implements IDragHandler
// Created: December 18, 2025

using UnityEngine;
using UnityEngine.EventSystems;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Attach to a child UI element (like a grab bar) to forward drag events
    /// to a parent component that implements IBeginDragHandler and IDragHandler.
    /// </summary>
    public class DragForwarder : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private IBeginDragHandler _beginDragTarget;
        private IDragHandler _dragTarget;

        private void Awake()
        {
            // Find the drag handler in parent
            _beginDragTarget = GetComponentInParent<IBeginDragHandler>();
            _dragTarget = GetComponentInParent<IDragHandler>();

            // Skip self if this component also implements the interfaces
            if (_beginDragTarget == (IBeginDragHandler)this)
            {
                var parent = transform.parent;
                while (parent != null)
                {
                    _beginDragTarget = parent.GetComponent<IBeginDragHandler>();
                    _dragTarget = parent.GetComponent<IDragHandler>();
                    if (_beginDragTarget != null) break;
                    parent = parent.parent;
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _beginDragTarget?.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _dragTarget?.OnDrag(eventData);
        }
    }
}
