using System.Collections.Generic;
using UnityEngine;

namespace SABI
{
    public static class ColliderExtensions
    {
        public static bool IsCollidingWith(this Collider collider, Collider otherCollider) =>
            collider.bounds.Intersects(otherCollider.bounds);

        /// <summary>
        /// Extension method for Collider that disables collision with another Collider.
        /// Returns bool.
        /// Arguments: Collider otherCollider
        /// </summary>
        public static Collider DisableCollisionWith(this Collider collider, Collider otherCollider)
        {
            Physics.IgnoreCollision(collider, otherCollider, true);
            return collider;
        }

        /// <summary>
        /// Extension method for Collider that enables collision with another Collider.
        /// Returns Collider for chaining.
        /// Arguments: Collider otherCollider
        /// </summary>
        public static Collider EnableCollisionWith(this Collider collider, Collider otherCollider)
        {
            Physics.IgnoreCollision(collider, otherCollider, false);
            return collider;
        }

        /// <summary>
        /// Extension method for Collider that disables collision with a list of Colliders.
        /// Returns Collider for chaining.
        /// Arguments: List&lt;Collider&gt; otherColliders
        /// </summary>
        public static Collider DisableCollisionWith(
            this Collider collider,
            List<Collider> otherColliders
        )
        {
            if (collider == null || otherColliders == null)
                return null;
            for (int i = 0; i < otherColliders.Count; i++)
            {
                collider.DisableCollisionWith(otherColliders[i]);
            }
            return collider;
        }

        /// <summary>
        /// Extension method for Collider that enables collision with a list of Colliders.
        /// Returns Collider for chaining.
        /// Arguments: List&lt;Collider&gt; otherColliders
        /// </summary>
        public static Collider EnableCollisionWith(
            this Collider collider,
            List<Collider> otherColliders
        )
        {
            if (collider == null || otherColliders == null)
                return null;
            for (int i = 0; i < otherColliders.Count; i++)
            {
                collider.EnableCollisionWith(otherColliders[i]);
            }
            return collider;
        }

        /// <summary>
        /// Extension method for Collider that checks collision with a specific LayerMask.
        /// Returns bool.
        /// Arguments: LayerMask layerMask
        /// </summary>
        public static bool IsCollidingWithLayer(this Collider collider, LayerMask layerMask)
        {
            if (collider == null)
                return false;
            return Physics
                    .OverlapBox(
                        collider.bounds.center,
                        collider.bounds.extents,
                        collider.transform.rotation,
                        layerMask
                    )
                    .Length > 0;
        }
    }
}
