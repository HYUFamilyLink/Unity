using UnityEngine;
using SocketIOClient;
using System;
using System.Collections.Generic;
using FamilyLink;
using Newtonsoft.Json;

public class SocketManager : MonoBehaviour
{
    public static SocketManager socketManager;
    public SocketIOUnity socket; // 여기서 직접 Emit/On

    public Action<NetworkUser> OnUserJoined;
    public Action<string> OnUserLeft;

    //music
    public Action<string> OnTurnChanged; //턴 변경시 트리거되는건 여기에
    public Action<PlayingVideoData> OnVideoUpdate; //
    public Action OnSongStop;
    public Action OnSyncRequested;
    public Action<double> OnSyncReceived;

    //ID,Emoji
    public Action<string, string> OnReactionReceived;
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
        SessionManager.sessionManager.SetAction();
        //입장 이벤트 수신(소켓)
        socket.OnUnityThread("room:user_joined", (data) =>
        {
            var newUser = JsonConvert.DeserializeObject<NetworkUser>(data.ToString());
            OnUserJoined?.Invoke(newUser);
        });
        
        //퇴장 이벤트 수신(소켓).Trim('[', ']')
        socket.OnUnityThread("room:user_left", (data) =>
        {
            var userId = JsonConvert.DeserializeObject<_LeftData_>(data.ToString().Trim('[', ']')).userId;
            OnUserLeft?.Invoke(userId);
        });

        socket.OnUnityThread("user:reaction", (data) =>
        {
           var reactionData = JsonConvert.DeserializeObject<_ReactionData_>(data.ToString().Trim('[', ']'));
           OnReactionReceived?.Invoke(reactionData.userId, reactionData.emoji);
        });

        socket.OnUnityThread("room:state", (data) =>
        {
           //정상적인 종료시에도 송신됨
           var vidData = JsonConvert.DeserializeObject<_VidData_>(data.ToString().Trim('[', ']'));
           OnVideoUpdate?.Invoke(vidData.playingVideo);
           OnTurnChanged?.Invoke(vidData.currentTurnId);
        });

        socket.OnUnityThread("song:receive_sync", (data) =>{
           var syncData = JsonConvert.DeserializeObject<_SyncData_>(data.ToString().Trim('[', ']'));
           OnSyncReceived?.Invoke(syncData.time);
        });

        socket.OnUnityThread("song:play", (data) =>
        {
            var vidData = JsonConvert.DeserializeObject<PlayingVideoData>(data.ToString().Trim('[', ']'));
            OnVideoUpdate?.Invoke(vidData);
        });

        socket.OnUnityThread("song:stop", (data) =>
        {
            //{} (아무 데이터도 안옴)
            //룸 상태, 턴 제어용 스위치
            //노래가 중단/종료될때 수신
            OnSongStop?.Invoke();
        });

        socket.OnUnityThread("song:request_sync", (data) =>
        {
            //내가 가수일때, 서버가 지금 내 재생 시간을 요청할때 수신
            //실제로는 유튜브 모듈을 통해 시간을 얻어야한다
            //지금은 디버깅을 위해 sessionManager의 StartAt 송신
            OnSyncRequested?.Invoke();
        });
    }

    public void LeftEvenet()
    {
        //구독 해제

        //room
        socket.Off("room:user_joined");
        socket.Off("room:user_left");
        socket.Off("room:state");
        //user
        socket.Off("user:reaction");
        //song
        socket.Off("song:receive_sync");
        socket.Off("song:request_sync");
        socket.Off("song:play");
        socket.Off("song:stop");

        //퇴장 송신
        socket.Emit("room:leave");
    }
}