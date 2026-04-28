using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Agora.Rtc;
using UnityEngine.Networking;
using Org.BouncyCastle.Ocsp;

public class AgoraManager : MonoBehaviour
{
    public static AgoraManager agoraManager;
    private IRtcEngine rtcEngine;
    private string roomID => SessionManager.sessionManager.roomID;
    private string uid => SessionManager.sessionManager.currentUser.id;

    private void Awake()
    {
        if(agoraManager == null) agoraManager = this;
        else Destroy(gameObject);
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
    }

    private void JoinChannel(string token)
    {
        rtcEngine.JoinChannelWithUserAccount(token, roomID, uid);
        Debug.Log($"아고라 연결 성공 - 채널 : {roomID}, 계정 : {uid}");
    }

    public void QuitChannel()
    {
        if(rtcEngine != null)
        {
            rtcEngine.LeaveChannel();
            rtcEngine.Dispose();
            rtcEngine = null;
        }
    }
}

[SerializeField]
public class AgoraTokenResponse
{
    public string token;
    public string uid;
    public string appId;
}