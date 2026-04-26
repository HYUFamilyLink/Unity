using UnityEngine;
using FamilyLink.Network;
using System.Collections.Generic;

//현재 세션 정보를 보유 및 관리하는 스크립트
//씬이 변해도 파괴되지 않게 지정
public class SessionManager : MonoBehaviour
{
    public static SessionManager sessionManager;

    [Header("Session Data")]
    public string authToken;
    public NetworkUser currentUser;
    public int valid_time;

    [Header("Room Data")]
    public string roomID;
    public List<NetworkUser> users;

    private void Awake()
    {
        if(sessionManager == null) {sessionManager = this; DontDestroyOnLoad(gameObject);}
        else Destroy(gameObject);
    }

    public void ClearSession()
    {
        authToken = null;
        currentUser = null;
    }

    public void SetSession(string token, NetworkUser user)
    {
        authToken = token;
        currentUser = user;
    }

    public void ClearRoomData()
    {
        roomID = string.Empty;
        users.Clear();
    }

    public void SetRoomID(string id)
    {
        roomID = id;
    }

    public void SetRoomUser(List<NetworkUser> _users)
    {
        users = _users;
    }
}