using UnityEngine;
using SocketIOClient;
using System;
using System.Collections.Generic;
using FamilyLink.Network;
using Newtonsoft.Json;

public class SocketManager : MonoBehaviour
{
    public static SocketManager socketManager;
    public SocketIOUnity socket; // 여기서 직접 Emit/On

    public Action<NetworkUser> OnUserJoined;
    public Action<string> OnUserLeft;
    void Awake()
    {
        if (socketManager == null) socketManager = this;
        //SessionManager하고 같은 오브젝트에 들어가므로 파괴방지, 중복방지는 따로 안한다
    }

    // AuthManager에서 로그인 성공시 이 함수 호출
    public void Connect()
    {
        if (socket != null && socket.Connected) return;
    
        var uri = new System.Uri(AppConfig.SocketUrl);
        
        var options = new SocketIOClient.SocketIOOptions
        {
            Auth = new { token = SessionManager.sessionManager.authToken },
            
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        };
    
        socket = new SocketIOUnity(uri, options);
    
        // 디버깅
        socket.OnConnected += (sender, e) => Debug.Log("<color=cyan>[Socket]</color> 연결 성공!");
        socket.OnUnityThread("connect_error", (data) => Debug.LogError($"소켓 인증 실패: {data}"));

        socket.Connect();
    }

    public void SetupEvenet()
    {
        //입장 이벤트 수신(소켓)
        socket.OnUnityThread("room:user_joined", (data) =>
        {
            var newUser = JsonConvert.DeserializeObject<NetworkUser>(data.ToString().Trim('[', ']'));
            OnUserJoined?.Invoke(newUser);
        });

        //퇴장 이벤트 수신(소켓)
        socket.OnUnityThread("room:user_left", (data) =>
        {
            var userId = JsonConvert.DeserializeObject<_LeftData_>(data.ToString().Trim('[', ']')).userId;
            OnUserLeft?.Invoke(userId);
        });
    }

    public void LeftEvenet()
    {
        socket.Off("room:user_joined");
        socket.Off("room:user_left");
        socket.Emit("room:leave");
    }
}

public struct _LeftData_
{
    public string userId;
}