using UnityEngine;

namespace ProjectP.Game
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private GameObject backpackPanel;
        [SerializeField] private GameObject mergePanel;

        private void Awake()
        {
            Instance = this;
        }

        // [병합] 버튼: 가방 화면을 닫고 병합 화면을 연다
        public void OnClickMerge()
        {
            SetActive(backpackPanel, false);
            SetActive(mergePanel, true);
        }

        // 병합 화면을 닫고 가방 화면으로 돌아간다
        public void OnClickBackpack()
        {
            SetActive(mergePanel, false);
            SetActive(backpackPanel, true);
        }

        private void SetActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
    }
}