using UnityEngine;
using FamilyLink.Network;
using System.Collections.Generic;
using UnityEditor.Rendering;

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
    public string currentTurnId;
    public PlayingVideoData currentVideo;

    private void Awake()
    {
        if(sessionManager == null) {sessionManager = this; DontDestroyOnLoad(gameObject);}
        else Destroy(gameObject);
    }

    public void SetAction()
    {
        SocketManager.socketManager.OnVideoUpdate += SetPlaying;
        SocketManager.socketManager.OnTurnChanged += SetTurnId;
    }

    public void ClearSession()
    {
        authToken = null;
        currentUser = null;
        ClearRoomData();
    }

    public void SetSession(string token, NetworkUser user)
    {
        authToken = token;
        currentUser = user;
    }

    public void SetRoom(string roomId, string turnId, List<NetworkUser> users, PlayingVideoData vidData)
    {
        SetRoomID(roomId);
        SetRoomUser(users);
        SetTurnId(turnId);
        SetPlaying(vidData);
    }

    public void ClearRoomData()
    {
        roomID = string.Empty;
        users.Clear();
        currentTurnId = string.Empty;
        currentVideo = null;
        SocketManager.socketManager.OnVideoUpdate -= SetPlaying;
        SocketManager.socketManager.OnTurnChanged -= SetTurnId;
    }

    public void SetPlaying(PlayingVideoData videoData)
    {
        currentVideo = videoData;
    }
    public void SetTurnId(string turnId)
    {
        currentTurnId = turnId;
    }

    public void SetRoomID(string id)
    {
        roomID = id;
    }

    public void SetRoomUser(List<NetworkUser> _users)
    {
        users = _users; 
    }

    public void JoinUser(NetworkUser user)
    {
        if(users.Contains(user)) return;
        users.Add(user);
    }

    public void ExitUser(string id)
    {
        users.RemoveAll(u => u.id == id);
    }
}