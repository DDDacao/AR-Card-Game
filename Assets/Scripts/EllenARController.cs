using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Animator))]
public class EllenARController : MonoBehaviour
{
    private Animator animator;

    [Header("UI Buttons")]
    public Button idleButton;
    public Button walkButton;
    public Button runButton;
    public Button deathButton;

    void Start()
    {
        animator = GetComponent<Animator>();

        // 如果没有手动在面板拖拽赋值，则尝试通过名字自动查找按钮
        if (idleButton == null) { GameObject go = GameObject.Find("IdleButton"); if (go) idleButton = go.GetComponent<Button>(); }
        if (walkButton == null) { GameObject go = GameObject.Find("WalkButton"); if (go) walkButton = go.GetComponent<Button>(); }
        if (runButton == null) { GameObject go = GameObject.Find("RunButton"); if (go) runButton = go.GetComponent<Button>(); }
        if (deathButton == null) { GameObject go = GameObject.Find("DeathButton"); if (go) deathButton = go.GetComponent<Button>(); }

        // 绑定按钮事件
        if (idleButton != null) idleButton.onClick.AddListener(PlayIdle);
        if (walkButton != null) walkButton.onClick.AddListener(PlayWalk);
        if (runButton != null) runButton.onClick.AddListener(PlayRun);
        if (deathButton != null) deathButton.onClick.AddListener(PlayDeath);
    }

    public void PlayIdle()
    {
        if (animator != null) animator.SetTrigger("ToIdle");
    }

    public void PlayWalk()
    {
        if (animator != null) animator.SetTrigger("ToWalk");
    }

    public void PlayRun()
    {
        if (animator != null) animator.SetTrigger("ToRun");
    }

    public void PlayDeath()
    {
        if (animator != null) animator.SetTrigger("ToDeath");
    }
}
