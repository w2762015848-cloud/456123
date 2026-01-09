using System;

namespace BattleSystem
{
    public static class BattleEvents
    {
        // 宠物生命值变化
        public static event Action<PetEntity, int, int> OnPetHPChanged;
        public static void TriggerPetHPChanged(PetEntity pet, int currentHP, int maxHP)
        {
            OnPetHPChanged?.Invoke(pet, currentHP, maxHP);
        }

        // 状态效果应用
        public static event Action<PetEntity, StatusCondition> OnStatusEffectApplied;
        public static void TriggerStatusEffectApplied(PetEntity pet, StatusCondition condition)
        {
            OnStatusEffectApplied?.Invoke(pet, condition);
        }

        // 伤害造成
        public static event Action<PetEntity, PetEntity, SkillData, int> OnDamageDealt;
        public static void TriggerDamageDealt(PetEntity attacker, PetEntity target, SkillData skill, int damage)
        {
            OnDamageDealt?.Invoke(attacker, target, skill, damage);
        }

        // 治疗接收
        public static event Action<PetEntity, int> OnHealReceived;
        public static void TriggerHealReceived(PetEntity pet, int amount)
        {
            OnHealReceived?.Invoke(pet, amount);
        }

        // UI更新
        public static event Action<PetEntity> OnPetUIUpdateNeeded;
        public static void TriggerPetUIUpdateNeeded(PetEntity pet)
        {
            OnPetUIUpdateNeeded?.Invoke(pet);
        }
    }
}