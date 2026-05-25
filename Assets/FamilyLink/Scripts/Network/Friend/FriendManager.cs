using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System;
using FamilyLink;

public class FriendManager : MonoBehaviour
{
    public static FriendManager instance;

    public List<FriendUser> friendList = new();
    public Dictionary<string, FriendStatus> friendStatuses = new();

    public Action OnFriendDataLoaded;

    void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
    }

    void OnEnable()
    {
        SocketManager.socketManager.OnFriendUpdate += HandleFriendUpdate;
    }

    void OnDisable()
    {
        if (SocketManager.socketManager != null)
            SocketManager.socketManager.OnFriendUpdate -= HandleFriendUpdate;
    }

    // 친구 목록 + 상태 한꺼번에 로드
    public void LoadAll() => StartCoroutine(LoadAllRoutine());

    IEnumerator LoadAllRoutine()
    {
        yield return StartCoroutine(LoadFriendListRoutine());
        yield return StartCoroutine(LoadFriendStatusesRoutine());
        OnFriendDataLoaded?.Invoke();
    }

    IEnumerator LoadFriendListRoutine()
    {
        using var req = UnityWebRequest.Get(AppConfig.FriendListUrl);
        req.SetRequestHeader("Authorization", "Bearer " + SessionManager.sessionManager.authToken);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            friendList = JsonConvert.DeserializeObject<List<FriendUser>>(req.downloadHandler.text) ?? new();
        }
        else Debug.LogError($"[FriendManager] 친구 목록 로드 실패: {req.error}");
    }

    IEnumerator LoadFriendStatusesRoutine()
    {
        using var req = UnityWebRequest.Get(AppConfig.FriendStatusesUrl);
        req.SetRequestHeader("Authorization", "Bearer " + SessionManager.sessionManager.authToken);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            friendStatuses = JsonConvert.DeserializeObject<Dictionary<string, FriendStatus>>(req.downloadHandler.text) ?? new();
        }
        else Debug.LogError($"[FriendManager] 친구 상태 로드 실패: {req.error}");
    }

    // 소켓 이벤트로 친구 상태 실시간 반영
    void HandleFriendUpdate(string fromId, string status)
    {
        if (string.IsNullOrEmpty(status))
        {
            friendStatuses.Remove(fromId);
            friendList.RemoveAll(f => f.id == fromId);
        }
        else if (status == "friend")
        {
            if (!friendStatuses.ContainsKey(fromId))
                friendStatuses[fromId] = new FriendStatus();
            friendStatuses[fromId].status = "friend";
        }
        else
        {
            if (!friendStatuses.ContainsKey(fromId))
                friendStatuses[fromId] = new FriendStatus();
            friendStatuses[fromId].status = status;
        }

        OnFriendDataLoaded?.Invoke();
    }

    // 편의 메서드
    public string GetStatus(string userId) =>
        friendStatuses.TryGetValue(userId, out var s) ? s.status : null;

    public bool IsFriend(string userId) => GetStatus(userId) == "friend";
}
