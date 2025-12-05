using UnityEngine;

//TODO: Improve Set Animator
namespace SABI
{
    public static class AnimatorExtensions
    {
        /// Extension method for Animator that pauses all animations.
        /// Return this Animator for method chaining.
        public static Animator PauseAnimations(this Animator animator)
        {
            animator.speed = 0f;
            return animator;
        }

        /// Extension method for Animator that resumes all animations.
        /// Return this Animator for method chaining.
        public static Animator ResumeAnimations(this Animator animator)
        {
            animator.speed = 1f;
            return animator;
        }
    }
}
