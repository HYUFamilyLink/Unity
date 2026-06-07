using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager themeManager;
    public string defaultThemeName = "Theme_Classic";
    public Action ChangeThemeAcion;
    private string currentTheme = "";

    void Start()
    {
        if(themeManager == null) themeManager = this;
        else Destroy(this);
        // 시작하자마자 기본 테마 씬을 불러오라고 명령합니다.
        if (!string.IsNullOrEmpty(defaultThemeName))
        {
            ChangeTheme(defaultThemeName);
        }
    }
    public void ChangeTheme(string themeSceneName)
    {
        StartCoroutine(SwitchThemeRoutine(themeSceneName));
    }

    // [새로 추가] 드롭다운에서 전달하는 '숫자(index)'를 받아서 처리하는 함수
    public void ChangeThemeByIndex(int index)
    {
        string targetSceneName = "";

        // 드롭다운의 순서에 맞춰서 불러올 씬 이름을 지정해줍니다.
        if (index == 0) targetSceneName = "Theme_Classic"; // 0번: 기본
        else if (index == 1) targetSceneName = "Theme_Old";     // 1번: 옛날
        else if (index == 2) targetSceneName = "Theme_Hanok";   // 2번: 한옥

        // 씬 이름이 비어있지 않다면 기존 테마 변경 함수를 실행합니다.
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            ChangeTheme(targetSceneName);
        }
    }

    private IEnumerator SwitchThemeRoutine(string themeSceneName)
    {
        if (!string.IsNullOrEmpty(currentTheme))
        {
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentTheme);
            while (!unloadOp.isDone) { yield return null; }
        }

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(themeSceneName, LoadSceneMode.Additive);
        while (!loadOp.isDone) { yield return null; }

        currentTheme = themeSceneName;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(themeSceneName));
        Debug.Log($"[{themeSceneName}] 테마로 변경 완료!");
        ChangeThemeAcion?.Invoke();
    }
}