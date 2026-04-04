using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

//Lobby에서 UI 변화를 관장하는 스크립트
public class UIManager : MonoBehaviour
{
    public static UIManager uiManager;
    string _name;
    string _birth;

    [Header("LoginUI Slots")]
    public Canvas login;
    public TMP_InputField nameInput;
    public TMP_InputField birthInput;

    [Header("LobbyUI Slots")]
    public Canvas lobby;

    private void Awake()
    {
        if (uiManager == null) uiManager = this;
        else { Destroy(gameObject); }
    }
    public void UIChange()
    {
        if (login.gameObject.activeSelf)
        {
            login.gameObject.SetActive(false);
            lobby.gameObject.SetActive(true);
        }
        else
        {
            lobby.gameObject.SetActive(false);
            login.gameObject.SetActive(true);
        }
    }

    public void OnRegisterClick()
    {
        Debug.Log($"<color=yellow>[Auth]</color> 회원가입 시도: 이름({_name}), 생일({_birth})");

        string json = JsonUtility.ToJson(new RegisterData
        {
            name = _name,
            email = _name + "@familylink.com",
            password = _birth,
            role = "vr"
        });
        
        AuthManager.authManager.Register(json);
    }

    public void OnLoginClick()
    {
        Debug.Log($"<color=yellow>[Auth]</color> 로그인 시도: 이름({_name}), 생일({_birth})");

        string json = JsonUtility.ToJson(new LoginData
        {
            name = _name,
            password = _birth
        });

        AuthManager.authManager.Login(json);
    }

    public void ChangeValueName(string value) => _name = value;
    public void ChangeValueBirth(string value) => _birth = value;
}

[Serializable]
public class RegisterData {
    public string name;
    public string email;
    public string password;
    public string role;
}

[Serializable]
public class LoginData {
    public string name;
    public string password;
}