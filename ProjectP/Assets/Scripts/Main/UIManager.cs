using UnityEngine;

namespace ProjectP.Main
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Show(GameObject obj)
        {
            obj?.SetActive(true);
        }

        public void Hide(GameObject obj)
        {
            obj?.SetActive(false);
        }
    }
}