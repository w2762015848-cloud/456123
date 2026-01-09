using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BattleSystem
{
    public class StatusIconManager : MonoBehaviour
    {
        [Header("UI引用")]
        public RectTransform statusIconContainer;
        public GameObject statusIconPrefab;

        [Header("位置设置")]
        public Vector2 allyIconOffset = new Vector2(-46f, 590f);
        public Vector2 enemyIconOffset = new Vector2(-6f, -240f);

        private Dictionary<PetEntity, RectTransform> _petIconContainers = new Dictionary<PetEntity, RectTransform>();
        private Dictionary<PetEntity, bool> _petIsAlly = new Dictionary<PetEntity, bool>();

        void Awake()
        {
            // 确保容器存在
            if (statusIconContainer == null)
            {
                Debug.LogError("StatusIconContainer未设置！");
            }
        }

        // 设置宠物阵营
        public void SetPetFaction(PetEntity pet, bool isAlly)
        {
            if (pet == null) return;

            _petIsAlly[pet] = isAlly;
            Debug.Log($"设置阵营: {pet.petName} 为 {(isAlly ? "我方" : "敌方")}");
        }

        // 获取宠物阵营
        private bool IsPetAlly(PetEntity pet)
        {
            if (pet == null) return true;

            if (_petIsAlly.ContainsKey(pet))
            {
                return _petIsAlly[pet];
            }

            // 默认判断：根据名称
            string name = pet.petName.ToLower();
            if (name.Contains("enemy") || name.Contains("boss") || name.Contains("敌人"))
            {
                return false;
            }

            return true;
        }

        // 创建状态图标
        public void CreateStatusIcon(PetEntity pet, StatusCondition condition, int duration)
        {
            if (pet == null || condition == StatusCondition.None) return;

            // 确保有容器
            if (!_petIconContainers.ContainsKey(pet))
            {
                CreatePetIconContainer(pet);
            }

            RectTransform container = _petIconContainers[pet];
            if (container == null || statusIconPrefab == null) return;

            // 创建图标
            GameObject iconObj = Instantiate(statusIconPrefab, container);
            iconObj.name = $"{condition}_Icon";

            // 设置图标大小
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            if (iconRect != null)
            {
                iconRect.sizeDelta = new Vector2(40, 40);
            }

            Debug.Log($"为 {pet.petName} 创建 {condition} 状态图标");
        }

        // 创建宠物的图标容器
        private void CreatePetIconContainer(PetEntity pet)
        {
            if (pet == null || statusIconContainer == null) return;

            GameObject containerObj = new GameObject($"{pet.petName}_StatusContainer");
            RectTransform containerRect = containerObj.AddComponent<RectTransform>();

            // 设置为statusIconContainer的子物体
            containerRect.SetParent(statusIconContainer, false);
            containerRect.localScale = Vector3.one;

            // 根据阵营设置位置
            bool isAlly = IsPetAlly(pet);

            if (isAlly)
            {
                // 我方：设置到上方
                containerRect.anchorMin = new Vector2(0.5f, 1f);
                containerRect.anchorMax = new Vector2(0.5f, 1f);
                containerRect.pivot = new Vector2(0.5f, 1f);
                containerRect.anchoredPosition = allyIconOffset;
            }
            else
            {
                // 敌方：设置到下方
                containerRect.anchorMin = new Vector2(0.5f, 0f);
                containerRect.anchorMax = new Vector2(0.5f, 0f);
                containerRect.pivot = new Vector2(0.5f, 0f);
                containerRect.anchoredPosition = enemyIconOffset;
            }

            _petIconContainers[pet] = containerRect;

            Debug.Log($"创建图标容器: {pet.petName}, 阵营: {(isAlly ? "我方" : "敌方")}, 位置: {containerRect.anchoredPosition}");
        }

        // 移除宠物的所有图标
        public void ClearPetIcons(PetEntity pet)
        {
            if (pet == null || !_petIconContainers.ContainsKey(pet)) return;

            RectTransform container = _petIconContainers[pet];
            if (container != null)
            {
                // 销毁所有子物体
                foreach (Transform child in container)
                {
                    Destroy(child.gameObject);
                }

                // 销毁容器
                Destroy(container.gameObject);
            }

            _petIconContainers.Remove(pet);
            if (_petIsAlly.ContainsKey(pet))
            {
                _petIsAlly.Remove(pet);
            }
        }
    }
}