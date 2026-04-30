using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System.Collections.Generic;
using FamilyLink.Network;
using UnityEngine.Networking;
using Org.BouncyCastle.Ocsp;
using System.Text;
using NUnit.Framework.Constraints;
using System.Collections;

public class LobbyUI : MonoBehaviour
{
    private string _roomCode; // 방 코드 입력값 저장용
    public static LobbyUI lobbyUI;
    SocketIOUnity socket => SocketManager.socketManager.socket;

    [Header("Profile")]
    public GameObject profileImg;

    void Awake()
    {
        if (lobbyUI == null) lobbyUI = this;
        else { Destroy(gameObject); }
    }

    private void OnEnable()
    {
        if (SocketManager.socketManager?.socket == null) return;

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
                    SessionManager.sessionManager.SetRoom(state.joinCode, state.currentTurnId, state.participants, state.playingVideo);
                    //씬 이동이 포함되어 끊고 가야한다
                    socket.Off("room:state");
                    SocketManager.socketManager.SetupEvenet();
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
        if (socket != null && socket.Connected) {
            Debug.Log("랜덤 매칭 시도: room:match");
            socket.Emit("room:match"); 
        }
    }

    public void OnJoinByCodeClick()
    {
        if (string.IsNullOrEmpty(_roomCode)) return;

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
        if (socket != null) socket.Disconnect();

        SessionManager.sessionManager.ClearSession();
        UIManager.uiManager.UIChange(); 
    }

    public void SetProfileImg()
    {
        if(socket == null) return;
        profileImg.SetActive(true);
    }

    public void _ImgButton(int num)
    {
        StartCoroutine(ImgPost(num));
    }

    IEnumerator ImgPost(int num)
    {
        string url = AppConfig.AuthApi + "/profile";
        var payload = new { profileImage = num };
        string jsonPayload = JsonConvert.SerializeObject(payload);
        using(UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            string token = SessionManager.sessionManager.authToken;
            request.SetRequestHeader("Authorization", "Bearer " + token);

            yield return request.SendWebRequest();
            Debug.Log(request.result);

            if(request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<UpdateResponse>(request.downloadHandler.text);

                if (response.success)
                {
                    Debug.Log("변경 성공" + response.token);
                    SessionManager.sessionManager.SetSession(response.token, response.user);

                    profileImg.SetActive(false);
                    Debug.Log("변경 완료");
                }
            }
            else
            {
                Debug.LogError("업데이트 실패" + request.error);
            }
        }
    }
}

[System.Serializable]
public class RoomStateResponse {
    public string roomId;
    public string joinCode;
    public string status;
    public List<NetworkUser> participants;
    public string currentTurnId;
    public PlayingVideoData playingVideo;
}

[System.Serializable]
public class UpdateResponse
{
    public bool success;
    public NetworkUser user;
    public string token;
}

[System.Serializable]
public class PlayingVideoData {
    public string videoId;
    public string title;
    public string artist;
    public string singerId;
    public long startAt;
}