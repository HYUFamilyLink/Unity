using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.Timeline;

namespace FamilyLink
{
    [Serializable]
    public class NetworkUser
    {
        public string id;
        public string nickname;
        public string role = "vr"; // "phone" 또는 "vr"
        public int profileimage = 0;
        // 유닛 테스트나 디버깅용 편의 기능
        public override string ToString() => $"[{role}] {nickname}";
    }

    // 로그인/회원가입 시 서버가 주는 응답 양식
    [Serializable]
    public class AuthResponse
    {
        public NetworkUser user;
        public string token;
    }

    [Serializable]
    public class UpdateResponse
    {
        public bool success;
        public NetworkUser user;
        public string token;
    }
    
    [Serializable]
    public class SearchResponse
    {
        public string id;
        public string title;
        public string artist;
        public string thumbnail;
        public string songNo;
        public List<string> tags;
    }

    [Serializable]
    public class PlayingVideoData {
        public string videoId;
        public string title;
        public string artist;
        public string singerId;
        public long startAt;
    }

    [Serializable]
    public class RoomStateResponse {
        public string roomId;
        public string joinCode;
        public string status;
        public List<NetworkUser> participants;
        public string currentTurnId;
        public PlayingVideoData playingVideo;
    }

    [Serializable]
    public class RoomListState
    {
        public string id;
        public string joinCode;
        public string status;
        public string hostName;
        public int hostProfileImage;
        public int participantCount;
        public string currentSong;
        public string currentTurnId;
    }

    [Serializable]
    public class AgoraTokenResponse
    {
        public string token;
        public string uid;
        public string appId;
    }

    [Serializable]
    public class RegisterData {
        public string name;
        public string email;
        public string password;
        public string role;
    }

    [Serializable]
    public class LoginData {
        public string name;
        public string password;
        public string role;
    }

    public struct _LeftData_
    {
        public string userId;
    }

    public struct _ReactionData_
    {
        public string userId;
        public string nickname;
        public string emoji;
    }
    public struct _VidData_
    {
        public string currentTurnId;
        public PlayingVideoData playingVideo;
    }

    public struct _SyncData_
    {
        public double time;
    }
}