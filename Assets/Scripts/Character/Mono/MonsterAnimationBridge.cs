using UnityEngine;

public class MonsterAnimationBridge : MonoBehaviour
{
    private Animator animator;

    [Header("动态绑定的 Animator")]
    public Animator targetAnimator;

    [Header("动画状态映射")]
    public string idleStateName = "Idle";
    public string attackStateName = "Attack";
    public string heavyAttackStateName = "HeavyAttack";
    public string getHitStateName = "GetHit";
    public string deathStateName = "Death";

    private void Awake()
    {
        ResolveAnimator();
    }

    public void ResolveAnimator()
    {
        animator = targetAnimator;
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
            
        AutoConfigureDefaults();
    }

    private void AutoConfigureDefaults()
    {
        string objectName = gameObject.name.ToLower();
        if (animator != null)
            objectName = animator.gameObject.name.ToLower();

        ConfigureNames(objectName);
    }

    public void BindTargetAnimator(Animator newAnimator, string monsterName)
    {
        targetAnimator = newAnimator;
        animator = newAnimator;
        ConfigureNames(monsterName.ToLower());
        PlayIdle();
    }

    private void ConfigureNames(string nameLower)
    {
        // Default to the new unified state names
        idleStateName = "Idle";
        attackStateName = "Attack";
        heavyAttackStateName = "HeavyAttack";
        getHitStateName = "GetHit";
        deathStateName = "Death";

        // Cavecrawler (2) has no death animation
        if (nameLower.Contains("cavecrawler") || nameLower.Contains("2"))
        {
            deathStateName = "";
        }
    }

    public void PlayIdle() => PlayState(idleStateName);
    public void PlayAttack() => PlayState(attackStateName);
    public void PlayHeavyAttack() => PlayState(heavyAttackStateName);
    public void PlayGetHit() => PlayState(getHitStateName);
    
    public void PlayDeath()
    {
        if (string.IsNullOrEmpty(deathStateName))
        {
            Debug.Log($"[MonsterAnim] {gameObject.name} 死亡动画留空，跳过播放。");
            return;
        }
        PlayState(deathStateName);
    }

    private void PlayState(string stateName)
    {
        if (animator != null && !string.IsNullOrEmpty(stateName))
        {
            animator.CrossFadeInFixedTime(stateName, 0.15f);
        }
    }
}
