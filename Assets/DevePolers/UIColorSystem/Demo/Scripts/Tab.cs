using UnityEngine;

namespace UIColorSystem.Demo
{
    public class Tab : MonoBehaviour
    {
        [SerializeField] private GameObject selected;
        
        public void Select()
        {
            selected.SetActive(true);
        }

        public void Deselect()
        {
            selected.SetActive(false);
        }
    }
}