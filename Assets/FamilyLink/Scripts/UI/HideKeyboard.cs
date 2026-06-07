using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HideKeyboard : MonoBehaviour
{
    private TMP_InputField inputField;
    // Start is called before the first frame update
    void Awake()
    {
        inputField = gameObject.GetComponent<TMP_InputField>();
        HideKey();
    }

    public void HideKey()
    {
        inputField.shouldHideMobileInput = true;
    }
}
