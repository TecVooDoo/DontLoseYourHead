using UnityEngine;

namespace SABI
{
    public static class GameObjectExtensions
    {
        /// Extension method for GameObject that gets or adds a component of type T.
        /// Returns T component for further use.
        public static T GetOrAddComponent<T>(this GameObject gameObject)
            where T : Component
        {
            if (!gameObject.TryGetComponent<T>(out var attachedComponent))
            {
                attachedComponent = gameObject.AddComponent<T>();
            }

            return attachedComponent;
        }

        /// Extension method for GameObject that checks if it has a component of type T.
        /// Returns bool indicating presence of the component.
        public static bool HasComponent<T>(this GameObject gameObject)
            where T : Component => gameObject.TryGetComponent<T>(out _);

        /// Extension method for GameObject that toggles its active state.
        /// Return this GameObject for method chaining.
        public static GameObject ToggleActive(this GameObject gameObject)
        {
            gameObject.SetActive(!gameObject.activeSelf);
            return gameObject;
        }

        /// Extension method for GameObject that destroys all its child objects.
        /// Return this GameObject for method chaining.
        public static GameObject DestroyAllChildren(this GameObject gameObject)
        {
            foreach (Transform child in gameObject.transform)
            {
                if (Application.isPlaying)
                    GameObject.Destroy(child.gameObject);
                else
                    GameObject.DestroyImmediate(child.gameObject);
            }
            return gameObject;
        }

        /// Extension method for GameObject that adds a component of type T if missing.
        /// Return this GameObject for method chaining.
        public static GameObject AddComponentIfMissing<T>(this GameObject gameObject)
            where T : Component
        {
            if (gameObject.GetComponent<T>() == null)
                gameObject.AddComponent<T>();
            return gameObject;
        }

        /// Extension method for GameObject that tries to get a component of type T in its children.
        /// Returns bool indicating if the component was found.
        /// Arguments: bool includeInactive: Whether to include inactive children.
        public static bool TryGetComponentInChildren<T>(
            this GameObject gameObject,
            out T component,
            bool includeInactive = false
        )
            where T : Component
        {
            component = gameObject.GetComponentInChildren<T>(includeInactive);
            return component != null;
        }

        /// Extension method for GameObject that tries to get a component of type T in its parent.
        /// Returns bool indicating if the component was found.
        /// Arguments: bool includeInactive: Whether to include inactive parents.
        public static bool TryGetComponentInParent<T>(
            this GameObject gameObject,
            out T component,
            bool includeInactive = false
        )
            where T : Component
        {
            component = gameObject.GetComponentInParent<T>(includeInactive);
            return component != null;
        }

        /// Extension method for GameObject that enables it.
        /// Return this GameObject for method chaining.
        public static GameObject Enable(this GameObject gameObject)
        {
            gameObject.SetActive(true);
            return gameObject;
        }

        /// Extension method for GameObject that disables it.
        /// Return this GameObject for method chaining.
        public static GameObject Disable(this GameObject gameObject)
        {
            gameObject.SetActive(false);
            return gameObject;
        }

        /// Extension method for GameObject that enables it if currently disabled.
        /// Return this GameObject for method chaining.
        public static GameObject EnableIfDisabled(this GameObject gameObject)
        {
            if (!gameObject.activeInHierarchy)
                gameObject.SetActive(true);
            return gameObject;
        }

        /// Extension method for GameObject that disables it if currently enabled.
        /// Return this GameObject for method chaining.
        public static GameObject DisableIfEnabled(this GameObject gameObject)
        {
            if (gameObject.activeInHierarchy)
                gameObject.SetActive(false);
            return gameObject;
        }

        /// Extension method for GameObject that toggles its active state in hierarchy.
        /// Return this GameObject for method chaining.
        public static GameObject Toggle(this GameObject gameObject)
        {
            gameObject.SetActive(!gameObject.activeInHierarchy);
            return gameObject;
        }

        #region Distance

        public static float Distance(this GameObject gameObject, GameObject target) =>
            Vector3.Distance(gameObject.transform.position, target.transform.position);

        public static float Distance(this GameObject gameObject, Transform target) =>
            Vector3.Distance(gameObject.transform.position, target.position);

        public static float Distance(this GameObject gameObject, Vector3 target) =>
            Vector3.Distance(gameObject.transform.position, target);

        public static float DistanceWithoutHeight(this GameObject gameObject, GameObject target) =>
            Vector3.Distance(
                gameObject.transform.position.WithY(0),
                target.transform.position.WithY(0)
            );

        public static float DistanceWithoutHeight(this GameObject gameObject, Transform target) =>
            Vector3.Distance(gameObject.transform.position.WithY(0), target.position.WithY(0));

        public static float DistanceWithoutHeight(this GameObject gameObject, Vector3 target) =>
            Vector3.Distance(gameObject.transform.position.WithY(0), target.WithY(0));

        #endregion
    }
}
