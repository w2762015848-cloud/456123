using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSystem
{
    public class SkillExecutor : MonoBehaviour
    {
        [Header("系统引用")]
        public BattleController battleController;

        [Header("伤害数字设置")]
        public DamageTextPop damageTextPrefab;
        public Canvas damageCanvas;
        public Transform damageTextParent;

        [Header("状态弹窗设置")]
        public GameObject statusPopupPrefab;
        public float popupDuration = 2f;

        [Header("伤害计算设置")]
        public bool enableCriticalHits = true;
        public float baseCriticalChance = 0.1f;
        public float criticalMultiplier = 1.5f;

        [Header("调试设置")]
        public bool debugLogs = true;

        private ObjectPool<DamageTextPop> _damageTextPool;

        void Awake()
        {
            // 初始化对象池
            InitializeObjectPool();
        }

        void Start()
        {
            // 自动查找画布
            if (damageCanvas == null)
            {
                damageCanvas = GameObject.Find("Damage_Canvas")?.GetComponent<Canvas>();
                Debug.Log($"自动找到 Damage_Canvas: {damageCanvas != null}");
            }
        }

        private void InitializeObjectPool()
        {
            if (damageTextPrefab != null && damageTextParent != null)
            {
                _damageTextPool = new ObjectPool<DamageTextPop>(
                    createFunc: () => Instantiate(damageTextPrefab, damageTextParent),
                    actionOnGet: (obj) => obj.gameObject.SetActive(true),
                    actionOnRelease: (obj) => obj.gameObject.SetActive(false),
                    actionOnDestroy: (obj) => Destroy(obj.gameObject),
                    defaultCapacity: 10,
                    maxSize: 50
                );

                if (debugLogs) Debug.Log("伤害数字对象池初始化完成");
            }
        }

        // 执行技能
        public void ExecuteSkill(PetEntity attacker, PetEntity target, SkillData skill)
        {
            if (attacker == null || target == null || skill == null)
            {
                Debug.LogError("技能执行失败: 参数为空");
                return;
            }

            if (debugLogs) Debug.Log($"{attacker.petName} 使用 {skill.skillName}");

            StartCoroutine(ExecuteSkillCoroutine(attacker, target, skill));
        }

        private IEnumerator ExecuteSkillCoroutine(PetEntity attacker, PetEntity target, SkillData skill)
        {
            // 等待一小段时间（用于动画或效果）
            yield return new WaitForSeconds(0.2f);

            // 根据技能类型执行不同的效果
            switch (skill.effectType)
            {
                case SkillEffectType.Damage:
                    ExecuteDamageSkill(attacker, target, skill);
                    break;

                case SkillEffectType.Heal:
                    ExecuteHealSkill(attacker, target, skill);
                    break;

                case SkillEffectType.Status:
                    ExecuteStatusSkill(attacker, target, skill);
                    break;

                case SkillEffectType.Buff:
                case SkillEffectType.Debuff:
                    ExecuteBuffSkill(attacker, target, skill);
                    break;

                default:
                    Debug.LogWarning($"未知的技能类型: {skill.effectType}");
                    break;
            }

            // 等待技能效果完成
            yield return new WaitForSeconds(0.5f);

            // 结束回合
            StartCoroutine(EndTurnAfterDelay(attacker));
        }

        // 执行伤害技能
        private void ExecuteDamageSkill(PetEntity attacker, PetEntity target, SkillData skill)
        {
            // 计算基础伤害
            int baseDamage = CalculateBaseDamage(attacker, target, skill);

            // 应用暴击
            bool isCritical = false;
            int finalDamage = baseDamage;

            if (enableCriticalHits && Random.value < baseCriticalChance)
            {
                isCritical = true;
                finalDamage = Mathf.RoundToInt(baseDamage * criticalMultiplier);
            }

            // 应用伤害
            target.TakeDamage(finalDamage);

            // 显示伤害数字
            ShowDamagePopup(target, finalDamage, 1f, skill.skillName,
                isCritical ? DamageTextPop.DamageType.Critical : DamageTextPop.DamageType.Normal);

            // 触发伤害事件
            BattleEvents.TriggerDamageDealt(attacker, target, skill, finalDamage);

            // 检查状态效果
            ApplyStatusEffectIfNeeded(attacker, target, skill);

            if (debugLogs) Debug.Log($"{attacker.petName} 对 {target.petName} 造成 {finalDamage} 点伤害{(isCritical ? " (暴击!)" : "")}");
        }

        // 执行治疗技能
        private void ExecuteHealSkill(PetEntity attacker, PetEntity target, SkillData skill)
        {
            // 计算治疗量
            int healAmount = CalculateHealAmount(attacker, target, skill);

            // 应用治疗
            target.Heal(healAmount);

            // 显示治疗数字
            ShowHealPopup(target, healAmount);

            if (debugLogs) Debug.Log($"{attacker.petName} 治疗 {target.petName} {healAmount} 点生命值");
        }

        // 执行状态技能
        private void ExecuteStatusSkill(PetEntity attacker, PetEntity target, SkillData skill)
        {
            if (skill.statusCondition != StatusCondition.None && skill.statusChance > 0)
            {
                // 检查状态是否成功施加
                if (Random.value <= skill.statusChance)
                {
                    // 施加状态效果
                    StatusEffect effect = new StatusEffect(skill.statusCondition, skill.statusDuration, 1);
                    target.AddStatusEffect(effect);

                    // 显示状态弹窗
                    ShowStatusPopup(target, $"{skill.statusCondition}!");

                    if (debugLogs) Debug.Log($"{attacker.petName} 对 {target.petName} 施加了 {skill.statusCondition} 状态");
                }
                else
                {
                    if (debugLogs) Debug.Log($"{skill.skillName} 的状态效果未命中");
                }
            }
            else
            {
                Debug.LogWarning($"{skill.skillName} 没有配置状态效果");
            }
        }

        // 执行增益/减益技能
        private void ExecuteBuffSkill(PetEntity attacker, PetEntity target, SkillData skill)
        {
            // 这里可以添加属性修改的逻辑
            // 由于基础版本没有StatModifier，我们暂时只显示信息

            string buffType = skill.effectType == SkillEffectType.Buff ? "增益" : "减益";
            ShowStatusPopup(target, $"{buffType}效果!");

            if (debugLogs) Debug.Log($"{attacker.petName} 对 {target.petName} 施加了{buffType}效果");
        }

        // 计算基础伤害
        private int CalculateBaseDamage(PetEntity attacker, PetEntity target, SkillData skill)
        {
            // 简单伤害计算公式
            float damage = skill.power;

            // 考虑攻击者和防御者的属性
            damage *= (attacker.attack / 100f);
            damage /= (target.defense / 100f);

            // 应用随机波动 (0.85-1.0)
            damage *= Random.Range(0.85f, 1.0f);

            // 确保最小伤害为1
            return Mathf.Max(1, Mathf.RoundToInt(damage));
        }

        // 计算治疗量
        private int CalculateHealAmount(PetEntity attacker, PetEntity target, SkillData skill)
        {
            // 基于技能威力计算治疗量
            float heal = skill.power;

            // 应用随机波动
            heal *= Random.Range(0.9f, 1.1f);

            return Mathf.RoundToInt(heal);
        }

        // 如果需要，应用状态效果
        private void ApplyStatusEffectIfNeeded(PetEntity attacker, PetEntity target, SkillData skill)
        {
            // 如果技能有状态效果，并且概率成功
            if (skill.statusCondition != StatusCondition.None && skill.statusChance > 0)
            {
                if (Random.value <= skill.statusChance)
                {
                    StatusEffect effect = new StatusEffect(skill.statusCondition, skill.statusDuration, 1);
                    target.AddStatusEffect(effect);

                    if (debugLogs) Debug.Log($"{target.petName} 获得了 {skill.statusCondition} 状态");
                }
            }
        }

        // 显示伤害数字
        public void ShowDamagePopup(PetEntity target, int damage, float scale, string skillName, DamageTextPop.DamageType damageType)
        {
            if (damageTextPrefab == null || damageCanvas == null)
            {
                Debug.LogWarning("伤害数字显示: 缺少预制体或画布引用");
                return;
            }

            if (_damageTextPool == null)
            {
                InitializeObjectPool();
            }

            // 从对象池获取伤害文本
            DamageTextPop damageText = _damageTextPool.Get();

            // 设置位置
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(target.transform.position);

            // 添加一些随机偏移，使数字不会完全重叠
            float randomX = Random.Range(-20f, 20f);
            float randomY = Random.Range(20f, 40f);

            damageText.transform.position = screenPosition + new Vector3(randomX, randomY, 0);
            damageText.transform.SetAsLastSibling();

            // 显示伤害
            damageText.ShowDamage(damage, damageType);

            // 延迟后放回对象池
            StartCoroutine(ReturnToPoolAfterDelay(damageText, 1.5f));
        }

        // 显示治疗数字
        private void ShowHealPopup(PetEntity target, int healAmount)
        {
            if (damageTextPrefab == null || damageCanvas == null) return;

            if (_damageTextPool == null)
            {
                InitializeObjectPool();
            }

            DamageTextPop healText = _damageTextPool.Get();

            Vector3 screenPosition = Camera.main.WorldToScreenPoint(target.transform.position);
            float randomX = Random.Range(-20f, 20f);
            float randomY = Random.Range(20f, 40f);

            healText.transform.position = screenPosition + new Vector3(randomX, randomY, 0);
            healText.transform.SetAsLastSibling();

            healText.ShowDamage(healAmount, DamageTextPop.DamageType.Heal);

            StartCoroutine(ReturnToPoolAfterDelay(healText, 1.5f));
        }

        // 显示状态弹窗
        private void ShowStatusPopup(PetEntity target, string message)
        {
            if (statusPopupPrefab == null || damageCanvas == null) return;

            GameObject popup = Instantiate(statusPopupPrefab, damageCanvas.transform);

            // 设置位置
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(target.transform.position);
            popup.transform.position = screenPosition + new Vector3(0, 80f, 0);

            // 设置文本
            TextMeshProUGUI text = popup.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = message;
            }

            // 自动销毁
            Destroy(popup, popupDuration);

            if (debugLogs) Debug.Log($"{target.petName} {message}");
        }

        // 应用状态效果（供外部调用）
        public void ApplyStatusEffect(PetEntity attacker, PetEntity target, StatusCondition condition, int duration, float chance)
        {
            if (target == null || condition == StatusCondition.None) return;

            if (Random.value <= chance)
            {
                StatusEffect effect = new StatusEffect(condition, duration, 1);
                target.AddStatusEffect(effect);

                if (debugLogs) Debug.Log($"{target.petName} 获得 {condition} 状态，持续 {duration} 回合");

                // 显示状态弹窗
                ShowStatusPopup(target, $"{condition}!");
            }
        }

        private IEnumerator ReturnToPoolAfterDelay(DamageTextPop damageText, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (damageText != null && _damageTextPool != null)
            {
                _damageTextPool.Release(damageText);
            }
        }

        private IEnumerator EndTurnAfterDelay(PetEntity attacker)
        {
            yield return new WaitForSeconds(1f);

            if (battleController != null)
            {
                battleController.EndTurn(attacker);
            }
            else
            {
                Debug.LogError("BattleController 为空，无法结束回合");
            }
        }
    }
}