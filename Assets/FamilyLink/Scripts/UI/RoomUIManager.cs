using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FamilyLink.Network;

// KaraokeRoom 씬의 UI를 관리하는 스크립트
// 디버깅용 UI에 가깝다
public class RoomUIManager : MonoBehaviour
{
    public static RoomUIManager roomUIManager;

    [Header("UI Elements")]
    public TextMeshProUGUI currentSingerText; // 현재 노래 부르는 사람 표시
    public TextMeshProUGUI CurrentSongText;

    [Header("Data")]
    private string title;

    private void Awake()
    {
        if (roomUIManager == null) roomUIManager = this;
        else Destroy(this);
    }

    private void Start()
    {
        // 소켓 이벤트 구독
        if (SocketManager.socketManager != null)
        {
            SocketManager.socketManager.OnVideoUpdate += UpdateVideo;
            SocketManager.socketManager.OnTurnChanged += UpdateTurnUI;
            UpdateVideo(SessionManager.sessionManager.currentVideo);
            UpdateTurnUI(SessionManager.sessionManager.currentTurnId);
        }
    }

    private void OnDestroy()
    {
        if (SocketManager.socketManager != null)
        {
            SocketManager.socketManager.OnVideoUpdate -= UpdateVideo;
            SocketManager.socketManager.OnTurnChanged -= UpdateTurnUI;
        }
    }

    // --- [소켓 수신 UI 업데이트 로직] ---
    private void UpdateVideo(PlayingVideoData videoData)
    {
        string text = "";
        if(videoData == null) text = "재생중인 곡 없음";
        else text = $"{videoData.artist} - {videoData.title}\n재생위치 - {videoData.startAt}";
        CurrentSongText.text = text;
    }

    private void UpdateTurnUI(string turnId)
    {
        if (turnId == null || string.IsNullOrEmpty(turnId))
        {
            currentSingerText.text = "현재 부르는 사람: 없음";
            return;
        }

        NetworkUser currentTurn = SessionManager.sessionManager.users.Find(u => u.id == turnId);
        
        currentSingerText.text = $"현재 턴: {currentTurn.nickname}";
    }

    // --- [사용자 액션 발송 로직] ---

    // UI 버튼 이벤트에 연결 (내 차례 혹은 현재 곡 넘기기)
    public void SkipTurn()
    {
        Debug.Log("차례 넘기기 요청");
        SocketManager.socketManager.socket.Emit("turn:skip");
    }
}
