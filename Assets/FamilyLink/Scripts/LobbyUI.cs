using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System.Collections.Generic;

public class LobbyUI : MonoBehaviour
{
    private string _roomCode; // 방 코드 입력값 저장용

    private void OnEnable()
    {
        if (SocketManager.socketManager?.socket == null) return;
        var socket = SocketManager.socketManager.socket;

        socket.OnUnityThread("room:state", (data) => {
            try {
                string rawJson = data.ToString();
                Debug.Log($"<color=white>[Raw Data]</color> {rawJson}");

                RoomStateResponse state = null;

                // 1. 데이터가 [ ] 로 시작하는 배열인 경우
                if (rawJson.Trim().StartsWith("[")) {
                    var list = JsonConvert.DeserializeObject<List<RoomStateResponse>>(rawJson);
                    if (list != null && list.Count > 0) state = list[0];
                } 
                // 2. 데이터가 { } 로 시작하는 단일 객체인 경우
                else {
                    state = JsonConvert.DeserializeObject<RoomStateResponse>(rawJson);
                }

                // 3. 데이터가 정상적으로 담겼다면 씬 전환
                if (state != null && !string.IsNullOrEmpty(state.roomId)) {
                    Debug.Log($"<color=cyan>[Lobby]</color> 검증 완료. 씬 이동: {state.joinCode}");
                    SceneManager.LoadScene("KaraokeRoom");
                }
            }
            catch (System.Exception e) {
                Debug.LogError($"파싱 실패 원인: {e.Message}");
            }
        });

        // 에러 리스너
        socket.Off("error");
        socket.OnUnityThread("error", (data) => {
            Debug.LogError($"<color=red>[Lobby]</color> 접속 에러: {data}");
        });
    }

    public void OnRandomMatchClick()
    {
        var socket = SocketManager.socketManager.socket;
        if (socket != null && socket.Connected) {
            Debug.Log("랜덤 매칭 시도: room:match");
            socket.Emit("room:match"); 
        }
    }

    public void OnJoinByCodeClick()
    {
        if (string.IsNullOrEmpty(_roomCode)) return;

        var socket = SocketManager.socketManager.socket;
        if (socket != null && socket.Connected) {
            Debug.Log($"코드 입장 시도: {_roomCode.ToUpper()}");
            
            // 명세서 및 프론트 규격: { joinCode: "ABCDE" } 객체 전달
            var payload = new { joinCode = _roomCode.ToUpper() };
            socket.Emit("room:join", payload);
        }
    }

    public void ChangeValueRoomCode(string v) => _roomCode = v;

    public void OnLogoutClick()
    {
        if (SocketManager.socketManager.socket != null)
            SocketManager.socketManager.socket.Disconnect();

        SessionManager.sessionManager.ClearSession();
        UIManager.uiManager.UIChange(); 
    }
}

[System.Serializable]
public class RoomStateResponse {
    public string roomId;
    public string joinCode;
    public string status;
}