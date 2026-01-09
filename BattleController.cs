using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace BattleSystem
{
    // 添加Turn枚举
    public enum Turn
    {
        Player,
        Enemy
    }

    public class BattleController : MonoBehaviour
    {
        [Header("基本战斗设置")]
        public PetEntity playerPet;
        public PetEntity enemyPet;
        public SkillExecutor skillExecutor;
        public BattleUIController uiController;
        public StatusEffectManager statusManager;

        [Header("战斗状态")]
        private Turn currentTurn = Turn.Player;

        void Start()
        {
            Debug.Log("=== BattleController Start ===");

            if (playerPet != null) Debug.Log($"PlayerPet: {playerPet.petName}");
            if (enemyPet != null) Debug.Log($"EnemyPet: {enemyPet.petName}");

            InitializeBattle();
        }

        // 最简单的初始化方法
        private void InitializeBattle()
        {
            Debug.Log("=== 战斗开始初始化 ===");

            // 1. 重置宠物生命值
            if (playerPet != null)
            {
                playerPet.currentHealth = playerPet.maxHealth;
                Debug.Log($"重置玩家宠物: {playerPet.petName}");
            }

            if (enemyPet != null)
            {
                enemyPet.currentHealth = enemyPet.maxHealth;
                Debug.Log($"重置敌人宠物: {enemyPet.petName}");
            }

            // 2. 重置技能PP
            ResetAllSkillPP(playerPet, "玩家");
            ResetAllSkillPP(enemyPet, "敌人");

            // 3. 设置阵营（修复图标位置的核心）
            SetPetFactions();

            // 4. 更新UI
            if (uiController != null && playerPet != null)
            {
                uiController.InitializeUI(playerPet);
            }

            // 5. 开始玩家回合
            currentTurn = Turn.Player;
            if (uiController != null)
            {
                uiController.SetSkillButtonsInteractable(true);
            }

            Debug.Log("战斗初始化完成");
        }

        // 最简单版本的技能PP重置
        private void ResetAllSkillPP(PetEntity pet, string faction)
        {
            if (pet == null || pet.skills == null)
            {
                Debug.LogWarning($"{faction}宠物或技能列表为空");
                return;
            }

            foreach (var skill in pet.skills)
            {
                if (skill != null && skill.maxPP > 0)
                {
                    skill.currentPP = skill.maxPP;
                    Debug.Log($"重置{faction}技能PP: {skill.skillName} -> {skill.currentPP}/{skill.maxPP}");
                }
            }
        }

        // 设置宠物阵营（解决图标位置问题）
        private void SetPetFactions()
        {
            // 查找StatusIconManager
            StatusIconManager statusIconManager = FindObjectOfType<StatusIconManager>();
            if (statusIconManager == null)
            {
                Debug.LogError("找不到StatusIconManager！请确保场景中有一个StatusIconManager对象");
                return;
            }

            // 设置玩家宠物为我方
            if (playerPet != null)
            {
                statusIconManager.SetPetFaction(playerPet, true);
                Debug.Log($"设置阵营: {playerPet.petName} = 我方");
            }

            // 设置敌人宠物为敌方
            if (enemyPet != null)
            {
                statusIconManager.SetPetFaction(enemyPet, false);
                Debug.Log($"设置阵营: {enemyPet.petName} = 敌方");
            }
        }

        // 玩家使用技能
        public void OnPlayerUseSkill(int skillIndex)
        {
            Debug.Log($"尝试使用技能: 索引{skillIndex}");

            // 基本验证
            if (currentTurn != Turn.Player)
            {
                Debug.LogWarning("现在不是玩家回合");
                return;
            }

            if (playerPet == null || enemyPet == null)
            {
                Debug.LogError("宠物为空");
                return;
            }

            if (skillIndex < 0 || playerPet.skills == null || skillIndex >= playerPet.skills.Count)
            {
                Debug.LogError("无效的技能索引");
                return;
            }

            SkillData skill = playerPet.skills[skillIndex];
            if (skill == null)
            {
                Debug.LogError("技能为空");
                return;
            }

            if (skill.currentPP <= 0)
            {
                Debug.LogWarning($"技能{skill.skillName} PP不足");
                return;
            }

            // 消耗PP
            skill.currentPP--;

            Debug.Log($"使用技能: {skill.skillName} (剩余PP: {skill.currentPP}/{skill.maxPP})");

            // 禁用技能按钮
            if (uiController != null)
            {
                uiController.SetSkillButtonsInteractable(false);
            }

            // 执行技能
            if (skillExecutor != null)
            {
                skillExecutor.ExecuteSkill(playerPet, enemyPet, skill);
            }
            else
            {
                Debug.LogError("SkillExecutor为空");
            }
        }

        // 结束回合
        public void EndTurn(PetEntity attacker)
        {
            Debug.Log($"结束回合: {attacker.petName}");

            // 切换回合
            if (currentTurn == Turn.Player)
            {
                currentTurn = Turn.Enemy;
                Debug.Log("切换到敌人回合");
                StartCoroutine(EnemyTurn());
            }
            else
            {
                currentTurn = Turn.Player;
                Debug.Log("切换到玩家回合");
                if (uiController != null)
                {
                    uiController.SetSkillButtonsInteractable(true);
                }
            }
        }

        // 敌人回合
        private IEnumerator EnemyTurn()
        {
            Debug.Log("敌人回合开始");
            yield return new WaitForSeconds(1f);

            if (enemyPet != null && playerPet != null && enemyPet.skills != null)
            {
                // 简单AI: 使用第一个可用的技能
                SkillData enemySkill = null;
                foreach (var skill in enemyPet.skills)
                {
                    if (skill != null && skill.currentPP > 0)
                    {
                        enemySkill = skill;
                        break;
                    }
                }

                if (enemySkill != null)
                {
                    Debug.Log($"敌人使用技能: {enemySkill.skillName}");
                    enemySkill.currentPP--;

                    if (skillExecutor != null)
                    {
                        skillExecutor.ExecuteSkill(enemyPet, playerPet, enemySkill);
                    }
                }
                else
                {
                    Debug.Log("敌人没有可用技能");
                    EndTurn(enemyPet);
                }
            }
            else
            {
                EndTurn(enemyPet);
            }
        }
    }
}