//서버 주소 등 상수값들
public static class AppConfig
{
    // --- [서버 주소 설정] ---
    public const string BaseUrl = "http://localhost:4000"; 

    // --- [API 경로] ---
    public const string AuthApi = BaseUrl + "/api/auth";
    public const string RegisterUrl = AuthApi + "/register";
    public const string LoginUrl = AuthApi + "/login";

    // --- [소켓 경로] ---
    public const string SocketUrl = BaseUrl;

    // --- [기타 설정] ---
}