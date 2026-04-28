# FamilyLink VR Karaoke

유니티 기반의 VR 노래방 협업 플랫폼 프로젝트입니다.

---

## 0. 사용 툴 & 폴더 구조

### 유니티 버전 2022.3.62f3(2022.3LTS)

```text
FamilyLink/
├── Font/                # 폰트 리소스 (NexonBold, NexonReguler, TTF)
├── Graphic/
│   ├── Avartar/         # 아바타 프리팹 및 에셋
│   ├── Background/      # 배경 프리팹 및 에셋
│   ├── Light/           # 광원 효과에 사용되는 에셋
│   └── Objects/         # 사물 프리팹 및 에셋
├── Scenes/              # 메인 씬 (Lobby, KaraokeRoom)
└── Scripts/
    ├── Avatar/
    │   └── AvatarManager.cs
    ├── Core/
    │   └── AppConfig.cs
    ├── Network/
    │   ├── Agora/
    │   │   └── AgoraManager.cs
    │   ├── Auth/
    │   │   ├── AuthManager.cs
    │   │   └── SessionManager.cs
    │   ├── SoketIO/
    │   │   ├── NetworkUser.cs
    │   │   └── SoketManager.cs
    │   └── Ubiq/
    │       ├── ObjSync.cs
    │       └── UbiqP2PManager.cs
    └── UI/
        ├── LobbyUI.cs
        └── UIManager.cs
```

---

## 1. 사용 패키지 (Unity Package Manager)

아래 패키지들을 순서대로 설치해야 프로젝트가 정상 작동합니다.

| 패키지명 | 설치 경로 / URL | 비고 |
| :--- | :--- | :--- |
| **Ubiq** | `https://github.com/UCL-VR/ubiq.git#upm` | P2P 아바타 및 오브젝트 동기화 |
| **Newtonsoft JSON** | `com.unity.nuget.newtonsoft-json` | JSON 데이터 파싱 (Socket.io 필수) |
| **SocketIOUnity** | `https://github.com/itisnajim/SocketIOUnity.git` | 실시간 시그널링 서버 연결 |
| **AgoraUnitySDK** | `https://docs.agora.io/en/sdks?platform=unity` | 실시간 음성 채팅 |
| **XR Hands** | `Unity Registry` > `XR Hands` | 핸드 트래킹 지원 |
| **ParrelSync** | `https://github.com/VeriorPies/ParrelSync` | (선택) 멀티플레이 디버깅 |
| **Unity glTFast** | `com.unity.cloud.gltfast` | glTF 파일 사용 |

---

## 2. 기타 설정 (Settings)

### **빌드 설정 (Build Settings)**
* **Platform**: Android
* **Texture Compression**: **ASTC** (VR 기기)

### **AppConfig.cs 설정**
* **AppConfig.cs 대신** **AppConfig.cs.example**가 있습니다
* **BaseUrl**: 연결할 SoketIO Sever 주소 지정, **기본값** : http://localhost:4000
* **AgoraAppID**: 아고라 앱 ID
* **YoutubeAPI**: 유튜브 API 키


---

## 3. Ubiq 로컬 서버 구동

P2P 동기화를 위해 로컬 환경에서 랑데부 서버를 실행해야 합니다.

```bash
# 터미널에서 아래 명령 실행
npx @ucl-vr/ubiq-server
```
---

## 4. 사용 Asset 모음

| 사용에셋 | 설치 경로 / URL | 저작권 |
| :--- | :--- | :--- |
| **White Modern Living Room** | `https://skfb.ly/oCoML` | dylanheyes, CC Attribution |
| **Speaker** | `https://skfb.ly/66KzR` | Andulil, CC Attribution |
| **Tamborine from Poly by Google** | `https://skfb.ly/6XSXG` | IronEqual, CC Attribution |
| **elephant mic 1 retexture 3** | `https://skfb.ly/667uZ` | shaylastewart, CC Attribution |
| **Ultimate House Interior Pack** | `https://quaternius.com/packs/ultimatehomeinterior.html` | CC0 |
