# FamilyLink VR Karaoke

유니티 기반의 VR 노래방 협업 플랫폼 프로젝트입니다.

---

## 0. 사용 툴 & 폴더 구조

### 유니티 버전 2022.3.62f3(2022.3LTS)

```text
FamilyLink/
├── Avartar/             # 아바타 프리팹 및 에셋
├── Font/                # 폰트 리소스 (NexonBold, NexonReguler, TTF)
├── Scenes/              # 메인 씬 (Lobby, KaraokeRoom)
└── Scripts/
    ├── Avatar/
    │   └── AvatarManager.cs
    ├── Core/
    │   └── AppConfig.cs
    ├── Network/
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

---

## 2. 기타 설정 (Settings)

### **빌드 설정 (Build Settings)**
* **Platform**: Android
* **Texture Compression**: **ASTC** (VR 기기)

### **포톤 설정 (Photon Setup)**
* **경로**: `Window` > `Photon Unity Networking` > `PUN Wizard`
* **내용**: 본인의 **Photon App ID** 등록이 필요합니다. 

### **SoketIO 주소 설정**
* **AppConfig.cs**: 에서 연결할 Backend Sever 주소를 지정할 수 있습니다.
* **기본값** : http://localhost:4000

---

## 3. Ubiq 로컬 서버 구동

P2P 동기화를 위해 로컬 환경에서 랑데부 서버를 실행해야 합니다.

```bash
# 터미널에서 아래 명령 실행
npx @ucl-vr/ubiq-server
```
+)
SoketIO로 받아온 방 코드를 이용하여 방을 생성,참가합니다.
따라서 백엔드 서버의 실행도 선행되어야 합니다.