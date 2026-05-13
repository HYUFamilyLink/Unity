using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Agora.Rtc;
using UnityEngine.Networking;
using FamilyLink;
using UnityEngine.Android;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using TMPro;

public class AgoraManager : MonoBehaviour
{
    public static AgoraManager agoraManager;
    private IRtcEngine rtcEngine;
    private ILocalSpatialAudioEngine spatialAudio;
    private bool isConnected = false;
    public int volume;

    // 현재 마이크 볼륨 레벨을 확인할 수 있는 변수 (0 ~ 255)
    public int currentMicLevel = 0;

    private string roomID => SessionManager.sessionManager.roomID;
    private string uid => SessionManager.sessionManager.currentUser.id;

    private void Awake()
    {
        if(agoraManager == null) agoraManager = this;
        else Destroy(gameObject);

        Debug.Log("실행1");
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
        ///
        rtcEngine.InitEventHandler(new AgoraEventHandler(this));
        /// 
        rtcEngine.SetAudioProfile(AUDIO_PROFILE_TYPE.AUDIO_PROFILE_MUSIC_STANDARD);
        rtcEngine.EnableAudio();

        ///
        rtcEngine.EnableAudioVolumeIndication(200, 3, true);
        /// 
        
        rtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);

        spatialAudio = rtcEngine.GetLocalSpatialAudioEngine();
        spatialAudio.Initialize();

        spatialAudio.SetAudioRecvRange(50f); // 가청 범위 설정
        spatialAudio.SetDistanceUnit(1f); // 단위 설정

        //rtcEngine.EnableInEarMonitoring(true, (int)EAR_MONITORING_FILTER_TYPE.EAR_MONITORING_FILTER_NONE);
        //rtcEngine.SetInEarMonitoringVolume(100);

        SocketManager.socketManager.OnTurnChanged += AgoraTrigger;
        isConnected = true;
    }

    public void SetAttenuation(uint uid, double amount)
    {
        spatialAudio.SetRemoteAudioAttenuation(uid, amount, false);
    }

    private uint GetAgoraUid(string strId)
    {
        if (string.IsNullOrEmpty(strId)) return 0;
    
        // 1. 초기값 설정 (JS와 동일하게 5381)
        uint hash = 5381;
    
        // 2. 문자열을 순회하며 해시 계산
        foreach (char c in strId)
        {
            // 3. 산술 오버플로우 발생 시 자동으로 하위 비트만 남기도록 unchecked 사용
            // (JS의 비트 연산과 동일한 효과를 냄)
            unchecked
            {
                // (hash << 5) + hash 는 수학적으로 hash * 33 과 동일함
                hash = ((hash << 5) + hash) + (uint)c;
            }
        }
    
        return hash;
    }
    private void JoinChannel(string token)
    {
        ChannelMediaOptions options = new ChannelMediaOptions();
        options.publishMicrophoneTrack.SetValue(true); 
        options.autoSubscribeAudio.SetValue(true);     

        rtcEngine.JoinChannel(token, roomID, GetAgoraUid(uid), options);
        Debug.Log($"아고라 연결 성공 - 채널 : {roomID}, 계정 : {GetAgoraUid(uid)}");
        rtcEngine.MuteLocalAudioStream(uid != SessionManager.sessionManager.currentTurnId);
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
        rtcEngine.MuteLocalAudioStream(uid != id);
        print("오디오 송신 : " + (uid == id));
    }

    private void Update()
    {
        if(!isConnected) return;
        rtcEngine.AdjustRecordingSignalVolume(volume);
        if(isSpeakerOn)rtcEngine.AdjustPlaybackSignalVolume(100);
    }

    private bool isMicOn = true;
    public void SetMute(TextMeshProUGUI text)
    {
        rtcEngine.MuteLocalAudioStream(isMicOn);
        isMicOn = !isMicOn;
        if(isMicOn) text.text = "마이크\n켜짐";
        else text.text = "마이크\n꺼짐";
        Debug.Log("마이크 상태 변경");
    }

    private bool isSpeakerOn = true;
    public void SetSpeakerOff(TextMeshProUGUI text)
    {
        isSpeakerOn = !isSpeakerOn;
        if (isSpeakerOn)
        {
            text.text = "음성 수신\n켜짐";
            rtcEngine.AdjustPlaybackSignalVolume(100);  // 스피커 켜기
        }
        else
        {
            text.text = "음성 수신\n꺼짐";
            rtcEngine.AdjustPlaybackSignalVolume(0); // 스피커 끄기
        }
    }

    public void UpdateRemotePosition(uint remoteUid, Vector3 pos, Vector3 forward)
    {
        if (spatialAudio != null && remoteUid != 0)
        {
            RemoteVoicePositionInfo posInfo = new RemoteVoicePositionInfo();
            // 위치 설정
            posInfo.position = new float[] { pos.x, pos.y, pos.z };
            // 방향 설정 (아바타 머리의 forward 벡터)
            posInfo.forward = new float[] { forward.x, forward.y, forward.z };

            spatialAudio.UpdateRemotePosition(remoteUid, posInfo);
        }
    }

    public void UpdateSelfPosition(Vector3 pos, Vector3 forward, Vector3 right, Vector3 up)
    {
        if (spatialAudio != null && isConnected)
        {
            float[] posArr = new float[] { pos.x, pos.y, pos.z };
            float[] fwdArr = new float[] { forward.x, forward.y, forward.z };
            float[] rgtArr = new float[] { right.x, right.y, right.z };
            float[] upArr = new float[] { up.x, up.y, up.z };

            // Listener(나)의 위치 정보를 업데이트해야 비로소 거리 계산이 시작됩니다.
            spatialAudio.UpdateSelfPosition(posArr, fwdArr, rgtArr, upArr);
        }
    }

    internal class AgoraEventHandler : IRtcEngineEventHandler
    {
        private AgoraManager _manager;

        public AgoraEventHandler(AgoraManager manager)
        {
            _manager = manager;
        }

        // 마이크/스피커 볼륨이 감지될 때마다 호출되는 콜백
        public override void OnAudioVolumeIndication(RtcConnection connection, AudioVolumeInfo[] speakers, uint speakerNumber, int totalVolume)
        {
            foreach (var speaker in speakers)
            {
                // uid가 0이면 '나 자신(Local User)'의 마이크 입력을 의미합니다.
                if (speaker.uid == 0)
                {
                    _manager.currentMicLevel = (int)speaker.volume; // 0 ~ 255 사이의 값
                    
                    // 테스트 용도: 볼륨이 어느 정도 감지되면 로그 출력
                    if (speaker.volume > 10) 
                    {
                        // Debug.Log($"🎤 내 마이크 입력 중... 볼륨: {speaker.volume}");
                    }
                }
            }
        }

        public override void OnUserJoined(RtcConnection connection, uint remoteUid, int elapsed)
        {
            UserInfo userInfo = new UserInfo();
            // 임의로 부여된 remoteUid를 던져주고, 원래 접속할 때 썼던 String ID(UserAccount)를 받아옵니다.
            int result = _manager.rtcEngine.GetUserInfoByUid(remoteUid, ref userInfo);

            if (result == 0 && !string.IsNullOrEmpty(userInfo.userAccount))
            {
                // 성공적으로 매칭된 경우
                Debug.Log($"[Agora] 상대방 접속! Socket ID: {userInfo.userAccount} (내부 UID: {remoteUid})");
            }
            else
            {
                // 네트워크 지연으로 접속 직후 찰나의 순간에 조회가 안 될 수도 있습니다.
                Debug.Log($"[Agora] 상대방 접속! 내부 UID: {remoteUid} (Socket ID 동기화 대기중...)");
            }
        }

        // OnUserJoined 시점에 정보가 안 넘어왔을 때를 대비해 유저 정보 갱신 콜백을 추가해 줍니다.
        public override void OnUserInfoUpdated(uint uid, UserInfo info)
        {
            Debug.Log($"[Agora] 유저 정보 갱신 완료! 내부 UID: {uid} -> Socket ID: {info.userAccount}");
        }
    }
}