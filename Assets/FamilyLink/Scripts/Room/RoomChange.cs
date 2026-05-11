using UnityEngine;
using TMPro;

public class ThemeManager : MonoBehaviour
{
    [System.Serializable]
    public struct ThemeData
    {
        public string themeName;
        public Material skyboxMaterial;
        public GameObject roomObject; 
    }

    public System.Collections.Generic.List<ThemeData> themes;
    public TextMeshProUGUI titleText;
    
    private int currentIndex = 0;

    void Start()
    {
        UpdateTheme();
    }

    public void NextTheme()
    {
        currentIndex = (currentIndex + 1) % themes.Count;
        UpdateTheme();
    }

    public void PrevTheme()
    {
        currentIndex = (currentIndex - 1 + themes.Count) % themes.Count;
        UpdateTheme();
    }

    private void UpdateTheme()
    {
        for (int i = 0; i < themes.Count; i++)
        {
            if (themes[i].roomObject != null)
            {
                themes[i].roomObject.SetActive(i == currentIndex);
            }
        }

        RenderSettings.skybox = themes[currentIndex].skyboxMaterial;

        if (titleText != null)
        {
            titleText.text = themes[currentIndex].themeName;
        }

        DynamicGI.UpdateEnvironment();
    }
}