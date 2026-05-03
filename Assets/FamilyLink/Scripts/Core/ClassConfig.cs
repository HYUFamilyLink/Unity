using System;
using System.Collections.Generic;

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
}