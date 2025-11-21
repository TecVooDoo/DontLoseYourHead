using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace UIColorSystem.Demo
{
    public class TabManager : MonoBehaviour
    {
        [SerializeField] private Tab[] tabs;

        private void Start()
        {
            tabs = GetComponentsInChildren<Tab>();
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i; // Capture the index for the lambda
                tabs[i].Deselect();
                tabs[i].GetComponent<Button>().onClick.AddListener(() => SelectTab(index));
            }

            if (tabs.Length > 0) {
                tabs[0].Select();
            }

        }

        public void SelectTab(int index)
        {
            if (index < 0 || index >= tabs.Length) return;

            foreach (var tab in tabs)
            {
                tab.Deselect();
            }

            tabs[index].Select();
        }
    }
}