using System;
using Newtonsoft.Json;

namespace FamilyLink.Network
{
    [Serializable]
    public class NetworkUser
    {
        public string id;
        public string nickname;
        public string role = "vr"; // "phone" 또는 "vr"

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
}