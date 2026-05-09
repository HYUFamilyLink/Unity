using UnityEngine;
using TMPro;
using UnityEngine.UI;
using FamilyLink;
using System.Collections.Generic;

public class RoomItemUI : MonoBehaviour
{
    public TextMeshProUGUI hostNameText;
    public TextMeshProUGUI participantCount;
    public TextMeshProUGUI currentSongText;
    public Button joinButton;
    public Image profileImg;
    private string roomJoinCode;

    public List<Sprite> profiles;

    public void SetRoomData(RoomListState data)
    {
        roomJoinCode = data.joinCode;
        hostNameText.text = $"{data.hostName}님의 방";
        participantCount.text = $"{data.participantCount} / 6명";
        currentSongText.text = $"{data.currentSong}";
        profileImg.sprite = profiles[data.hostProfileImage];

        // 버튼 클릭 이벤트 연결
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => {
            Debug.Log($"{roomJoinCode} 번 방으로 입장을 시도합니다.");
            SocketManager.socketManager.socket.Emit("room:join", new { joinCode = roomJoinCode });
        });
    }
}