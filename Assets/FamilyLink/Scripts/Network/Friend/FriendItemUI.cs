using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FriendItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI nicknameText;
    public Image profileImage;
    public Button actionButton;
    public TextMeshProUGUI actionButtonText;

    private string currentTargetId;

    // LobbyUI.cs에서 호출하는 데이터 세팅 메서드
    public void SetData(string targetId, string nickname, int profileImageIdx, string status)
    {
        currentTargetId = targetId;
        nicknameText.text = nickname;

        // LobbyUI에 있는 프로필 이미지 리스트를 참조하여 이미지 설정
        if (LobbyUI.lobbyUI != null && LobbyUI.lobbyUI.profiles != null && LobbyUI.lobbyUI.profiles.Count > profileImageIdx)
        {
            profileImage.sprite = LobbyUI.lobbyUI.profiles[profileImageIdx];
        }

        // 상태(friend, sent, received, null)에 따른 버튼 텍스트 처리
        actionButton.interactable = true;
        if (status == "friend")
        {
            actionButtonText.text = "삭제";
        }
        else if (status == "sent")
        {
            actionButtonText.text = "대기중";
            actionButton.interactable = false; // 이미 보낸 요청은 비활성화 처리
        }
        else if (status == "received")
        {
            actionButtonText.text = "수락";
        }
        else // null 인 경우 (친구 아님)
        {
            actionButtonText.text = "친추";
        }

        // 버튼 클릭 이벤트 리스너 재할당
        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnActionButtonClicked);
    }

    private void OnActionButtonClicked()
    {
        if (LobbyUI.lobbyUI != null)
        {
            // LobbyUI에 이미 구현된 친구 처리 로직을 호출
            LobbyUI.lobbyUI.OnFriendActionClick(currentTargetId);
        }
    }
}