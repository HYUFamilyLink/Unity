using System;
using UnityEngine;
using UnityEngine.Video;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using FamilyLink;
using System.Linq;
using Org.BouncyCastle.Crypto.Engines;

//Youtube Sync을 전체 구조에 맞게 리팩토링
public class VideoManager : MonoBehaviour
{
    [Header("UI & Components")]
    public VideoPlayer videoPlayer;   
    public GameObject loadingOverlay;   

    [Header("Room & User Config")]
    private string userId;         

    private YoutubeClient youtubeClient;
    private PlayingVideoData currentVideoData = null;
    private bool amISingingNow = false;
    private string currentSingerId = "";
    
    private float syncTimer = 0f;                 
    private bool isWaitingForInitialSync = false;

    private enum PlayerState { NONE = -1, ENDED = 0, PLAYING = 1, BUFFERING = 3 }
    private PlayerState currentState = PlayerState.NONE;

    // ✨ React 로직 이식을 위한 변수 추가
    private double currentOffsetSec = 0.05;  // offsetRef 역할 (기본 50ms)
    private bool isOffsetLocked = false;     // offsetLockedRef 역할
    private float lastSeekTime = 0f;         // lastSeekTimeRef 역할
    private const double BUFFER_MS = 0.300;  // 수신자 버퍼 (300ms = 0.3s)

    void Start()
    {
        Debug.Log("[VideoManager] Start() - 소켓 분리 리팩토링 버전");

        youtubeClient = new YoutubeClient();
        loadingOverlay.SetActive(false);
        videoPlayer.playOnAwake = false;
        videoPlayer.prepareCompleted += OnPlayerReady;

        videoPlayer.SetDirectAudioVolume(0, 0.3f);

        // SessionManager를 통해 내 ID 가져오기 (초기화 보장)
        if (SessionManager.sessionManager != null && SessionManager.sessionManager.currentUser != null)
        {
            userId = SessionManager.sessionManager.currentUser.id;
        }

        // SocketManager의 이벤트 구독
        SubscribeToSocketManager();
    }

    private void SubscribeToSocketManager()
    {
        if (SocketManager.socketManager == null) return;

        SocketManager.socketManager.OnVideoUpdate += SetVideoData;
        SocketManager.socketManager.OnSongStop += ClearVideo;
        SocketManager.socketManager.OnSyncRequested += HandleSyncRequested;
        SocketManager.socketManager.OnSyncReceived += HandleSyncReceived;
    }

    private void OnDestroy()
    {
        if (SocketManager.socketManager != null)
        {
            SocketManager.socketManager.OnVideoUpdate -= SetVideoData;
            SocketManager.socketManager.OnSongStop -= ClearVideo;
            SocketManager.socketManager.OnSyncRequested -= HandleSyncRequested;
            SocketManager.socketManager.OnSyncReceived -= HandleSyncReceived;
        }
        videoPlayer.prepareCompleted -= OnPlayerReady;
    }

    void Update()
    {
        // 💡 EnqueueMainThreadAction 관련 코드가 모두 제거되었습니다.

        if (currentVideoData != null && !string.IsNullOrEmpty(videoPlayer.url))
        {
            PlayerState newState = GetPlayerState();
            if (newState != currentState && newState != PlayerState.NONE)
            {
                HandleStateChange(newState);
                currentState = newState;
            }
        }

        // 방장일 경우 1초마다 동기화 데이터 발송
        if (amISingingNow && currentState == PlayerState.PLAYING)
        {
            syncTimer += Time.deltaTime;
            if (syncTimer >= 1.0f) 
            {
                syncTimer = 0f;
                EmitSyncData(videoPlayer.time);
            }
        }
    }

    // --- [소켓 이벤트 수신부 (Action 연결)] ---

    private async void SetVideoData(PlayingVideoData videoData)
    {
        if (videoData == null || string.IsNullOrEmpty(videoData.videoId))
        {
            ClearVideo();
            return;
        }

        bool isSameVideo = (currentVideoData != null && currentVideoData.videoId == videoData.videoId);

        currentVideoData = videoData;
        currentSingerId = (videoData.singerId ?? "").Trim();
        amISingingNow = currentSingerId == userId.Trim();
        string videoId = videoData.videoId;

        if(isSameVideo) return;

        isOffsetLocked = false;
        currentOffsetSec = 0.15; 
        lastSeekTime = 0f;

        videoPlayer.Stop();
        loadingOverlay.SetActive(true);
        isWaitingForInitialSync = false;

        try
        {
            var streamManifest = await youtubeClient.Videos.Streams.GetManifestAsync(videoId);
            var streamInfo = streamManifest.GetMuxedStreams()
                            .Where(s => s.Container == YoutubeExplode.Videos.Streams.Container.Mp4)
                            .OrderByDescending(s => s.VideoResolution.Height)
                            .FirstOrDefault(s => s.VideoResolution.Height <= 720);

            if (streamInfo != null)
            {
                videoPlayer.url = streamInfo.Url;
                Debug.Log($"[YouTube] 영상 URL 획득 성공!");
                Debug.Log($"[Video URL] {streamInfo.Url}");
                videoPlayer.Prepare(); 
            }
            else
            {
                Debug.LogError("[YouTube Error] 지원되는 스트림을 찾을 수 없습니다.");
                loadingOverlay.SetActive(false);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[YouTube Error] {ex.Message}");
            loadingOverlay.SetActive(false);
        }
    }

    private void HandleSyncRequested()
    {
        if (amISingingNow && currentState == PlayerState.PLAYING) 
        {
            EmitSyncData(videoPlayer.time);
        }
    }

    private void HandleSyncReceived(double targetTime)
    {
        if (!amISingingNow && currentVideoData != null)
        {
            double myTime = videoPlayer.time;

            // ✨ 1. 아고라 통계가 확보되면 딱 한 번만 갱신하고 잠금 (React의 offsetLockedRef 로직)
            if (!isOffsetLocked)
            {
                int delayMs = AgoraManager.agoraManager.GetAudioDelay(currentSingerId);
                if (delayMs != 50) // 50이 아니라는 건 실제 통계가 확보되었다는 뜻
                {
                    // 지연 시간(초 단위) + 버퍼 시간
                    currentOffsetSec = (delayMs / 1000.0) + BUFFER_MS;
                    isOffsetLocked = true; // 갱신 후 잠금!
                    Debug.Log($"[Sync] 🔒 네트워크 지연 확보 및 잠금: {delayMs}ms (총 오프셋: {currentOffsetSec:F2}s)");
                }
            }

            targetTime = Math.Max(0, targetTime - currentOffsetSec);

            if (isWaitingForInitialSync)
            {
                Debug.Log($"[Sync] 초기 로딩 완료! 방장 시간({targetTime:F2}s)부터 재생을 시작합니다.");
                videoPlayer.time = targetTime;
                videoPlayer.Play();
                isWaitingForInitialSync = false;
                lastSeekTime = Time.time; // 초기 동기화 시간 기록
            }
            else
            {
                //Debug.Log($"[Sync] 진행 중 보정: {myTime:F2}s → {targetTime:F2}s");
                //videoPlayer.time = targetTime;

                // ✨ 3. 드리프트 보정 (0.2초 이상 차이 나고, 마지막 보정 후 3초가 지났을 때만)
                double drift = Math.Abs(myTime - targetTime);
                float timeSinceLastSeek = Time.time - lastSeekTime;

                if (drift > 0.2 && timeSinceLastSeek > 3.0f)
                {
                    Debug.Log($"[Sync] 드리프트 보정: {myTime:F2}s → {targetTime:F2}s");
                    videoPlayer.time = targetTime;
                    lastSeekTime = Time.time; // 보정 시간 갱신
                }
            }
        }
    }

    // --- [소켓 데이터 송신부] ---

    private void EmitSyncData(double timeValue)
    {
        if (SocketManager.socketManager?.socket != null)
        {
            SocketManager.socketManager.socket.Emit("song:send_sync", new { time = timeValue });
        }
    }

    private void EmitEmptyEvent(string eventName)
    {
        if (SocketManager.socketManager?.socket != null)
        {
            SocketManager.socketManager.socket.Emit(eventName); 
        }
    }

    // --- [비디오 플레이어 제어 로직] ---

    private void OnPlayerReady(VideoPlayer vp)
    {
        if (!amISingingNow)
        {
            Debug.Log("[Sync] 영상 준비 완료! 방장의 실제 타임스탬프를 기다립니다...");
            isWaitingForInitialSync = true;
            EmitEmptyEvent("song:request_sync");
        }
        else
        {
            vp.Play(); 
        }
    }

    private void HandleStateChange(PlayerState state)
    {
        if (state == PlayerState.PLAYING)
        {
            loadingOverlay.SetActive(false);
            if (amISingingNow)
            {
                syncTimer = 0f; 
                EmitSyncData(videoPlayer.time);
            }
            else 
            {
                EmitEmptyEvent("song:request_sync");
            }
        }
        else if (state == PlayerState.BUFFERING)
        {
            loadingOverlay.SetActive(true); 
            if (amISingingNow) syncTimer = 0f;
        }
        else if (state == PlayerState.ENDED)
        {
            if (amISingingNow)
            {
                EmitEmptyEvent("song:end");
                ClearVideo();
            }
        }
    }

    private PlayerState GetPlayerState()
    {
        if (!videoPlayer.isPrepared) return PlayerState.BUFFERING;
        if (videoPlayer.isPlaying) return PlayerState.PLAYING;
        
        if (videoPlayer.isPrepared && !videoPlayer.isPlaying)
        {
            if (videoPlayer.length > 0 && Math.Abs(videoPlayer.time - videoPlayer.length) < 0.2f) return PlayerState.ENDED;
            return PlayerState.BUFFERING; 
        }
        return PlayerState.NONE;
    }

    private void ClearVideo()
    {
        videoPlayer.Stop();
        videoPlayer.url = "";
        currentVideoData = null;
        loadingOverlay.SetActive(false);
        currentState = PlayerState.NONE;
        syncTimer = 0f;
        isWaitingForInitialSync = false;
    }
}