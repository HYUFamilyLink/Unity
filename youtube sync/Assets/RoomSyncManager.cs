using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection; 
using UnityEngine;
using UnityEngine.Video;
using SocketIOClient;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

public class RoomSyncManager : MonoBehaviour
{
    [Header("UI & Components")]
    public VideoPlayer videoPlayer;   
    public GameObject loadingOverlay;   

    [Header("Room & User Config")]
    public string serverUrl = "http://127.0.0.1:3000"; 
    public string userId = "unity_user_123";         
    public string roomId = "test_room";        

    private SocketIO socket;
    private YoutubeClient youtubeClient;

    private VideoDataDTO currentVideoData = null;
    private bool amISingingNow = false;
    private string currentSingerId = "";
    
    private const double AGORA_OFFSET_SEC = 0.15; 
    private float syncTimer = 0f;                 

    // ✨ 초기 로딩 후 타임스탬프를 기다리는 상태인지 체크하는 변수
    private bool isWaitingForInitialSync = false;

    private enum PlayerState { NONE = -1, ENDED = 0, PLAYING = 1, BUFFERING = 3 }
    private PlayerState currentState = PlayerState.NONE;

    private readonly Queue<Action> _mainThreadActions = new Queue<Action>();

    void Start()
    {
        Debug.Log("====================================");
        Debug.Log(" RoomSyncManager Start() 정상 실행됨!");
        Debug.Log("====================================");

        youtubeClient = new YoutubeClient();
        
        loadingOverlay.SetActive(false);
        videoPlayer.playOnAwake = false;
        videoPlayer.prepareCompleted += OnPlayerReady;

        InitializeSocket();
    }

    void Update()
    {
        lock (_mainThreadActions)
        {
            while (_mainThreadActions.Count > 0)
            {
                _mainThreadActions.Dequeue().Invoke();
            }
        }

        if (currentVideoData != null && !string.IsNullOrEmpty(videoPlayer.url))
        {
            PlayerState newState = GetPlayerState();
            if (newState != currentState && newState != PlayerState.NONE)
            {
                HandleStateChange(newState);
                currentState = newState;
            }
        }

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

    private async void InitializeSocket()
    {
        try 
        {
            Debug.Log($"[Socket] 서버({serverUrl})에 연결 시도 중...");

            var options = new SocketIOOptions();

            try 
            {
                var transportProp = options.GetType().GetProperty("Transport");
                if (transportProp != null) 
                {
                    var enumValue = Enum.Parse(transportProp.PropertyType, "WebSocket");
                    transportProp.SetValue(options, enumValue);
                }
            } 
            catch { Debug.LogWarning("[Socket] Transport 옵션 변경 실패 (무시 가능)"); }

            socket = new SocketIO(new Uri(serverUrl), options);

            socket.OnConnected += OnSocketConnected;

            socket.On("room:state", response =>
            {
                try 
                {
                    var state = response.GetValue<RoomStateDTO>(0);
                    EnqueueMainThreadAction(() =>
                    {
                        if (state != null && state.playingVideo != null && !string.IsNullOrEmpty(state.playingVideo.videoId)) 
                        {
                            SetVideoData(state.playingVideo);
                        }
                        else 
                        {
                            ClearVideo();
                        }
                    });
                }
                catch (Exception ex) { Debug.LogWarning($"[Parse Error] room:state - {ex.Message}"); }
                return Task.CompletedTask; 
            });

            socket.On("song:play", response =>
            {
                try 
                {
                    var videoInfo = response.GetValue<VideoDataDTO>(0);
                    EnqueueMainThreadAction(() => SetVideoData(videoInfo));
                }
                catch (Exception ex) { Debug.LogWarning($"[Parse Error] song:play - {ex.Message}"); }
                return Task.CompletedTask;
            });

            socket.On("song:stop", response =>
            {
                EnqueueMainThreadAction(ClearVideo);
                return Task.CompletedTask;
            });

            socket.On("song:request_sync", response =>
            {
                EnqueueMainThreadAction(() =>
                {
                    if (amISingingNow && currentState == PlayerState.PLAYING) EmitSyncData(videoPlayer.time);
                });
                return Task.CompletedTask;
            });

            socket.On("song:receive_sync", response =>
            {
                try 
                {
                    var syncData = response.GetValue<SyncDataDTO>(0);
                    double time = syncData.time;

                    EnqueueMainThreadAction(() =>
                    {
                        if (!amISingingNow && currentVideoData != null)
                        {
                            double myTime = videoPlayer.time;
                            double targetTime = Math.Max(0, time - AGORA_OFFSET_SEC);

                            // ✨ 변경점: 초기 동기화 대기 중이면 바로 시간을 맞추고 재생을 시작합니다.
                            if (isWaitingForInitialSync)
                            {
                                Debug.Log($"[Sync] 초기 로딩 완료! 방장 시간({targetTime:F2}s)부터 재생을 시작합니다.");
                                videoPlayer.time = targetTime;
                                videoPlayer.Play();
                                isWaitingForInitialSync = false;
                            }
                            // 이미 재생 중인 경우 0.1초 이상 차이나면 보정합니다.
                            else if (Math.Abs(myTime - targetTime) > 0.1)
                            {
                                Debug.Log($"[Sync] 진행 중 보정: {myTime:F2}s → {targetTime:F2}s");
                                videoPlayer.time = targetTime;
                            }
                        }
                    });
                }
                catch (Exception ex) { Debug.LogWarning($"[Parse Error] song:receive_sync - {ex.Message}"); }
                return Task.CompletedTask;
            });

            await socket.ConnectAsync();
            Debug.Log("[Socket] 소켓 연결 대기 완료!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Socket Error] 치명적 오류:\n{ex.Message}\n{ex.StackTrace}");
        }
    }

    private async void OnSocketConnected(object sender, EventArgs e)
    {
        Debug.Log("[Socket] Connected! 방 입장 요청 발송...");

        var joinData = new { roomId = this.roomId };
        await socket.EmitAsync("room:join", new object[] { joinData });

        var reqData = new { roomId = this.roomId };
        await socket.EmitAsync("room:request_state", new object[] { reqData });
    }

    private async void EmitSyncData(double timeValue)
    {
        if (socket != null && socket.Connected)
        {
            var syncData = new { time = timeValue };
            await socket.EmitAsync("song:send_sync", new object[] { syncData });
        }
    }

    private async void EmitEmptyEvent(string eventName)
    {
        if (socket != null && socket.Connected)
        {
            await socket.EmitAsync(eventName, new object[0]); 
        }
    }


    private async void SetVideoData(VideoDataDTO videoData)
    {
        currentVideoData = videoData;
        currentSingerId = (videoData.singerId ?? "").Trim();
        amISingingNow = (currentSingerId == userId.Trim());
        string videoId = videoData.videoId;

        videoPlayer.Stop();
        loadingOverlay.SetActive(true);
        isWaitingForInitialSync = false; // 새로운 곡을 틀 때마다 초기화

        try
        {
            var streamManifest = await youtubeClient.Videos.Streams.GetManifestAsync(videoId);
            var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

            if (streamInfo != null)
            {
                videoPlayer.url = streamInfo.Url;
                Debug.Log($"[YouTube] 영상 URL 획득 성공!");
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

    private void OnPlayerReady(VideoPlayer vp)
    {
        if (!amISingingNow)
        {
            // ✨ 변경점: 로딩이 끝나면 부정확한 시간 계산을 하지 않고, 방장에게 현재 시간을 요청한 채 대기합니다.
            Debug.Log("[Sync] 영상 준비 완료! 방장의 실제 타임스탬프를 기다립니다...");
            isWaitingForInitialSync = true;
            EmitEmptyEvent("song:request_sync");
            
            // vp.Play()를 여기서 호출하지 않습니다! (receive_sync가 호출해줌)
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

    private void EnqueueMainThreadAction(Action action)
    {
        lock (_mainThreadActions) { _mainThreadActions.Enqueue(action); }
    }

    private async void OnDestroy()
    {
        if (socket != null) await socket.DisconnectAsync();
        videoPlayer.prepareCompleted -= OnPlayerReady;
    }
}

public class VideoDataDTO
{
    public string videoId { get; set; }
    public string singerId { get; set; }
    public string title { get; set; }
    public string artist { get; set; }
    public long startAt { get; set; }
}

public class RoomStateDTO
{
    public VideoDataDTO playingVideo { get; set; }
}

public class SyncDataDTO
{
    public double time { get; set; }
}