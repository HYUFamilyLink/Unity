using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using FamilyLink.Network;
using System;
using Newtonsoft.Json;

//lobby에서 로그인/회원가입 등의 인증만을 담당하는 스크립트
public class AuthManager : MonoBehaviour
{
    public static AuthManager authManager;

    void Awake() {
        if (authManager == null) authManager = this;
        else { Destroy(gameObject); }
    }

    // 회원가입 실행
    public void Register(string json)
    {
        StartCoroutine(AuthRequest(AppConfig.RegisterUrl, json));
    }

    // 로그인 실행
    public void Login(string json)
    {
        StartCoroutine(AuthRequest(AppConfig.LoginUrl, json));
    }

    private IEnumerator AuthRequest(string url, String json)
    {
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if(request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log(JsonConvert.DeserializeObject(request.downloadHandler.text));

                //데이터 전달 시점
                AuthResponse response = JsonConvert.DeserializeObject<AuthResponse>(request.downloadHandler.text);
                SessionManager.sessionManager.SetSession(response.token, response.user);
                SocketManager.socketManager.Connect();

                UIManager.uiManager.UIChange(); // login > lobby
            }
            else
            {
                //TODO : 로그인 실패 인디케이트
            }
        }
    }
}

public class AuthResponse
{
    public string token;
    public NetworkUser user;
}