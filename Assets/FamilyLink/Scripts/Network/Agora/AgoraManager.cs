using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Agora.Rtc;
using UnityEngine.Networking;
using FamilyLink;
using UnityEngine.Android;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;

public class AgoraManager : MonoBehaviour
{
    public static AgoraManager agoraManager;
    private IRtcEngine rtcEngine;
    private bool isConnected = false;
    public int volume;

    private string roomID => SessionManager.sessionManager.roomID;
    private string uid => SessionManager.sessionManager.currentUser.id;
    private void Awake()
    {
        if(agoraManager == null) agoraManager = this;
        else Destroy(gameObject);

        //권한 요청
        #if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            Debug.Log("🎤 마이크 권한을 요청했습니다.");
        }
        #endif
    }
    //동기 처리 문제때문에 소켓 등 다른 세팅 완료 후 호출되도록 구성
    //이 함수가 Start와 유사한 역할을 담당한다
    public void AgoraConnect()
    {
        StartCoroutine(GetTokenAndJoin());
    }

    IEnumerator GetTokenAndJoin()
    {
        string _url = AppConfig.AgoraUrl + $"/token?roomId={roomID}";
        Debug.Log($"접속 시도 : {_url}");
        using(UnityWebRequest request = UnityWebRequest.Get(_url))
        {
            string jwtToken = SessionManager.sessionManager.authToken;
            request.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

            yield return request.SendWebRequest();

            if(request.result == UnityWebRequest.Result.Success)
            {
                AgoraTokenResponse response = JsonUtility.FromJson<AgoraTokenResponse>(request.downloadHandler.text);
                InitAgora();
                JoinChannel(response.token);
            }
            else
            {
                Debug.LogError($"Agora 연결 실패 : {request.error}");
            }
        }
    }

    private void InitAgora()
    {
        if(rtcEngine != null) return;

        rtcEngine = RtcEngine.CreateAgoraRtcEngine();
        RtcEngineContext context = new RtcEngineContext();
        context.appId = AppConfig.AgoraAppID;
        context.channelProfile = CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING;
        context.audioScenario = AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_CHORUS;

        rtcEngine.Initialize(context);

        rtcEngine.SetAudioProfile(AUDIO_PROFILE_TYPE.AUDIO_PROFILE_MUSIC_STANDARD);
        rtcEngine.EnableAudio();
        rtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
        rtcEngine.SetAudioEffectPreset(AUDIO_EFFECT_PRESET.ROOM_ACOUSTICS_KTV);
        SocketManager.socketManager.OnTurnChanged += AgoraTrigger;
        isConnected = true;
    }

    private void JoinChannel(string token)
    {
        ChannelMediaOptions options = new ChannelMediaOptions();
        options.publishMicrophoneTrack.SetValue(true); 
        options.autoSubscribeAudio.SetValue(true);     

        rtcEngine.JoinChannelWithUserAccount(token, roomID, uid, options);
        Debug.Log($"아고라 연결 성공 - 채널 : {roomID}, 계정 : {uid}");
        rtcEngine.MuteLocalAudioStream(false);
    }

    public void QuitChannel()
    {
        isConnected = false;
        if(rtcEngine != null)
        {
            rtcEngine.LeaveChannel();
            rtcEngine.Dispose();
            rtcEngine = null;
        }
    }

    //아고라 입출력 여부 전환 스위치, 마이크 아님
    public void AgoraTrigger(string id)
    {
        rtcEngine.MuteLocalAudioStream(uid == id);
        print(uid == id);
    }

    private void Update()
    {
        if(!isConnected) return;
        rtcEngine.AdjustRecordingSignalVolume(volume);
        rtcEngine.AdjustPlaybackSignalVolume(100);
    }
}