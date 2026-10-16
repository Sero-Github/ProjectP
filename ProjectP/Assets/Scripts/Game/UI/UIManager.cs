using System;
using UnityEngine;

namespace ProjectP.Game
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject backpackPanel;
        [SerializeField] private GameObject mergePanel;
        [SerializeField] private GameObject mergeButton;

        private void Awake()
        {
            Instance = this;
        }

        public void OnClickBackpack()
        {
            inventoryPanel.SetActive(true);
            mergePanel.SetActive(false);
            backpackPanel.SetActive(true);
        }

        public void OnClickBack()
        {
            if (mergePanel.activeInHierarchy) {
                mergePanel.SetActive(false);
                mergeButton.SetActive(true);
                backpackPanel.SetActive(true);
            } 
            else {
                inventoryPanel.SetActive(false);
            }
        }

        public void OnClickMerge()
        {
            backpackPanel.SetActive(false);
            mergePanel.SetActive(true);
            mergeButton.SetActive(false);
        }
    }
}