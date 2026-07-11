using UnityEngine;
using System.Collections.Generic;

public class MonsterAnimationPreviewer : MonoBehaviour
{
    [System.Serializable]
    public class MonsterAnimData
    {
        public string name;
        public GameObject gameObject;
        public Animator animator;
        public List<string> states = new List<string>();
    }

    public List<MonsterAnimData> monsters = new List<MonsterAnimData>();
    public int selectedIndex = 0;

    private Vector2 scrollPosition = Vector2.zero;
    private Vector2 animScrollPosition = Vector2.zero;

    private void Start()
    {
        ScanMonsters();
        FocusOnSelected();
    }

    [ContextMenu("Scan Monsters")]
    public void ScanMonsters()
    {
        monsters.Clear();
        
        // Find all root objects that start with numbers
        var rootObjects = gameObject.scene.GetRootGameObjects();
        var tempMonsters = new List<GameObject>();
        for (int i = 0; i < rootObjects.Length; i++)
        {
            var obj = rootObjects[i];
            if (obj.name.Length >= 3 && char.IsDigit(obj.name[0]) && char.IsDigit(obj.name[1]))
            {
                tempMonsters.Add(obj);
            }
        }

        // Sort alphabetically by name
        tempMonsters.Sort((a, b) => string.Compare(a.name, b.name));

        foreach (var obj in tempMonsters)
        {
            var data = new MonsterAnimData();
            data.name = obj.name;
            data.gameObject = obj;
            data.animator = obj.GetComponent<Animator>();
            
            #if UNITY_EDITOR
            if (data.animator != null && data.animator.runtimeAnimatorController != null)
            {
                var controller = data.animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                if (controller == null && data.animator.runtimeAnimatorController is AnimatorOverrideController)
                {
                    controller = (data.animator.runtimeAnimatorController as AnimatorOverrideController).runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                }
                
                if (controller != null)
                {
                    foreach (var layer in controller.layers)
                    {
                        foreach (var state in layer.stateMachine.states)
                        {
                            if (!data.states.Contains(state.state.name))
                            {
                                data.states.Add(state.state.name);
                            }
                        }
                    }
                }
            }
            #endif

            // Default fallback states if nothing is found or not in editor
            if (data.states.Count == 0)
            {
                data.states.Add("Idle");
                data.states.Add("Run");
                data.states.Add("Walk");
                data.states.Add("Death");
            }

            monsters.Add(data);
        }
    }

    private void OnGUI()
    {
        // 1. Title Banner
        GUI.Box(new Rect(10, 10, Screen.width - 20, 30), "怪物动画预览工具 (Monster Animation Previewer)");

        // 2. Monster List Scroll Panel
        GUI.Box(new Rect(10, 50, 260, Screen.height - 70), "怪物列表 (" + monsters.Count + ")");
        
        GUILayout.BeginArea(new Rect(20, 80, 240, Screen.height - 110));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        for (int i = 0; i < monsters.Count; i++)
        {
            GUI.backgroundColor = (i == selectedIndex) ? Color.cyan : Color.white;
            if (GUILayout.Button(monsters[i].name, GUILayout.Height(30)))
            {
                selectedIndex = i;
                FocusOnSelected();
            }
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();

        GUI.backgroundColor = Color.white;

        // 3. Control Panel
        if (selectedIndex >= 0 && selectedIndex < monsters.Count)
        {
            var current = monsters[selectedIndex];
            GUI.Box(new Rect(280, 50, Screen.width - 290, Screen.height - 70), "控制面板: " + current.name);

            GUILayout.BeginArea(new Rect(290, 80, Screen.width - 310, Screen.height - 110));
            
            GUILayout.Label("当前选择: " + current.name, GUILayout.Height(20));
            if (GUILayout.Button("拉近摄像机观察 (Focus Camera)", GUILayout.Width(250), GUILayout.Height(30)))
            {
                FocusOnSelected();
            }

            GUILayout.Space(20);
            GUILayout.Label("可用动画列表 (点击直接播放):", GUILayout.Height(20));
            
            animScrollPosition = GUILayout.BeginScrollView(animScrollPosition);
            
            int buttonWidth = 200;
            int buttonHeight = 40;
            int buttonsPerRow = Mathf.Max(1, (Screen.width - 330) / (buttonWidth + 10));
            
            int count = 0;
            for (int i = 0; i < current.states.Count; i++)
            {
                if (count == 0) GUILayout.BeginHorizontal();

                string stateName = current.states[i];
                if (GUILayout.Button(stateName, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                {
                    if (current.animator != null)
                    {
                        current.animator.Play(stateName);
                        Debug.Log("Playing animation: " + stateName + " on " + current.name);
                    }
                }

                count++;
                if (count >= buttonsPerRow)
                {
                    GUILayout.EndHorizontal();
                    count = 0;
                }
            }
            if (count > 0) GUILayout.EndHorizontal();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }

    private void FocusOnSelected()
    {
        if (selectedIndex < 0 || selectedIndex >= monsters.Count) return;
        var current = monsters[selectedIndex];
        if (current.gameObject == null) return;

        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 monsterPos = current.gameObject.transform.position;
            // Position camera 4.5 units in front of the monster, looking slightly down at it
            mainCamera.transform.position = monsterPos + new Vector3(0f, 1.8f, -4.5f);
            mainCamera.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        }
    }
}
