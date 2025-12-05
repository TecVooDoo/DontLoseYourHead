using UnityEngine;

namespace SABI
{
    public static class ComponentExtensions
    {
        /// Extension method for Component that adds a new component of type T to the GameObject.
        /// Returns T component added for further use.
        public static T AddComponent<T>(this Component component)
            where T : Component => component.gameObject.AddComponent<T>();

        /// Extension method for Component that gets or adds a component of type T to the GameObject.
        /// Returns T component found or added for further use.
        public static T GetOrAddComponent<T>(this Component component)
            where T : Component
        {
            if (!component.TryGetComponent<T>(out var attachedComponent))
                attachedComponent = component.AddComponent<T>();

            return attachedComponent;
        }

        /// Extension method for Component that checks if a component of type T exists on the GameObject.
        /// Returns bool indicating if the component is present.
        public static bool HasComponent<T>(this Component component)
            where T : Component => component.TryGetComponent<T>(out _);

        /// Extension method for Component that destroys a component of type T on the GameObject if it exists.
        /// Returns void, no value.
        public static void DestroyComponent<T>(this Component component)
            where T : Component
        {
            if (component.TryGetComponent<T>(out var componentToDestroy))
                Object.Destroy(componentToDestroy);
        }

        /// Extension method for Component that tries to get a component of type T in parent GameObjects.
        /// Returns bool indicating if the component was found.
        /// bool includeInactive: Whether to include inactive GameObjects in the search.
        public static bool TryGetComponentInParent<T>(
            this Component component,
            out T componentFound,
            bool includeInactive = false
        )
            where T : Component
        {
            componentFound = component.gameObject.GetComponentInParent<T>(includeInactive);
            return componentFound != null;
        }

        /// Extension method for Component that tries to get a component of type T in child GameObjects.
        /// Returns bool indicating if the component was found.
        /// bool includeInactive: Whether to include inactive GameObjects in the search.
        public static bool TryGetComponentInChildren<T>(
            this Component component,
            out T componentFound,
            bool includeInactive = false
        )
            where T : Component
        {
            componentFound = component.gameObject.GetComponentInChildren<T>(includeInactive);
            return componentFound != null;
        }
    }
}
