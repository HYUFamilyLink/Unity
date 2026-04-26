This file is a merged representation of the entire codebase, combined into a single document by Repomix.

# File Summary

## Purpose
This file contains a packed representation of the entire repository's contents.
It is designed to be easily consumable by AI systems for analysis, code review,
or other automated processes.

## File Format
The content is organized as follows:
1. This summary section
2. Repository information
3. Directory structure
4. Repository files (if enabled)
5. Multiple file entries, each consisting of:
  a. A header with the file path (## File: path/to/file)
  b. The full contents of the file in a code block

## Usage Guidelines
- This file should be treated as read-only. Any changes should be made to the
  original repository files, not this packed version.
- When processing this file, use the file path to distinguish
  between different files in the repository.
- Be aware that this file may contain sensitive information. Handle it with
  the same level of security as you would the original repository.

## Notes
- Some files may have been excluded based on .gitignore rules and Repomix's configuration
- Binary files are not included in this packed representation. Please refer to the Repository Structure section for a complete list of file paths, including binary files
- Files matching patterns in .gitignore are excluded
- Files matching default ignore patterns are excluded
- Files are sorted by Git change count (files with more changes are at the bottom)

# Directory Structure
```
Avatar.meta
Avatar/Avatar.cs
Avatar/Avatar.cs.meta
Avatar/AvatarManager.cs
Avatar/AvatarManager.cs.meta
Core.meta
Core/AppConfig.cs.example
Core/AppConfig.cs.example.meta
Core/AppConfig.cs.meta
Network.meta
Network/Auth.meta
Network/Auth/AuthManager.cs
Network/Auth/AuthManager.cs.meta
Network/Auth/SessionManager.cs
Network/Auth/SessionManager.cs.meta
Network/SoketIO.meta
Network/SoketIO/NetworkUser.cs
Network/SoketIO/NetworkUser.cs.meta
Network/SoketIO/SoketManager.cs
Network/SoketIO/SoketManager.cs.meta
Network/Ubiq.meta
Network/Ubiq/ObjSync.cs
Network/Ubiq/ObjSync.cs.meta
Network/Ubiq/UbiqP2PManager.cs
Network/Ubiq/UbiqP2PManager.cs.meta
UI.meta
UI/LobbyUI.cs
UI/LobbyUI.cs.meta
UI/UIManager.cs
UI/UIManager.cs.meta
```

# Files

## File: Avatar.meta
```
fileFormatVersion: 2
guid: 8d7ff9507ac442f4c99e2ec2e43a1855
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Avatar/Avatar.cs
```csharp
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UniVRM10;

//아바타 고유 정보 등을 처리 및 보유하는 스크립트
public class Avatar : MonoBehaviour
{
    public string id;
    public string role = "phone";


    public void SetID(string _id) { id = _id; return;}

    void Start()
    {
        string networkId = gameObject.GetComponent<ObjSync>().NetworkId.ToString().Split('.')[0];
        var room = AvatarManager.avatarManager.spawnManager.roomClient.Room;

        foreach(var entry in room)
        {
            if (entry.Key.Contains(networkId))
            {
                var data = JsonUtility.FromJson<ExtendedSpawnMessage>(entry.Value);
                if(!string.IsNullOrEmpty(data.targetSocketId))
                {
                    AvatarManager.avatarManager.ReigsterAvatar(data.targetSocketId, this);
                    id = data.targetSocketId;
                }
                break;
            }
        }
        if(role == "phone") AvatarManager.avatarManager.SetWebSync(this);
    }


    void Update() {
    // 아래 화살표 누르면 blink 표정 적용
        if (Input.GetKeyDown(KeyCode.DownArrow)) {
            Debug.Log("asdfsafd");
            var vrm10 = GetComponent<Vrm10Instance>(); // [cite: 99, 113]
            if (vrm10 != null) {
                // UI가 없어도 코드로는 강제 실행됩니다
                vrm10.Runtime.Expression.SetWeight(ExpressionKey.Blink, 1.0f); // [cite: 111, 128]
                Debug.Log("표정 강제 변경 완료!");
            }
        }
    }
}
```

## File: Avatar/Avatar.cs.meta
```
fileFormatVersion: 2
guid: 5e5687a919bca274caa24602ee38ec95
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Avatar/AvatarManager.cs
```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FamilyLink.Network;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Spawning;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Hands.Gestures;

//아바타 생성, 관리, 소켓-ubiq간 매핑
public class AvatarManager : MonoBehaviour
{
    //---생성 규칙---
    //1. RoomClient의 peer중 번호가 가장 빠른 사람이 마스터 peer가 된다.
    //2. 마스터 peer는 백엔드 서버의 호스트와는 무관하다.
    //3. VR : 본인의 아바타는 본인이 직접 생성 및 등록한다.
    //4. 웹 : 마스터 peer가 모든 웹 유저의 아바타를 생성 및 등록한다.
    //5. 마스터 peer가 나갈 경우 다음 번호가 생성 및 릴레이 권한을 이어받는다.
    public static AvatarManager avatarManager;

    //soket - ubiq peer 매핑된 딕셔너리
    //키-soket id(uuid), value - 대상 오브젝트
    public Dictionary<string, Avatar> userDict = new Dictionary<string, Avatar>();

    //soketIO에서 받아온 데이터
    private List<NetworkUser> users => SessionManager.sessionManager.users;
    private NetworkUser currentUser => SessionManager.sessionManager.currentUser;

    //ubiq
    public NetworkSpawnManager spawnManager;
    RoomClient roomClient => spawnManager.roomClient;
    NetworkSpawner spawner;
    [SerializeField]
    public PrefabCatalogue avatarCatalogue;
    public bool isMaster;
    string cachedMasterId;
    private const string MASTER_ID = "MASTERID";
    private const string ISMASTERIN = "MASTERINFLAG";

    void Awake()
    {
        if(avatarManager == null) avatarManager = this;
    }

    public void StartSetup()
    {
        if (spawnManager == null) spawnManager = FindObjectOfType<NetworkSpawnManager>();
        spawnManager.OnSpawned.AddListener(OnAvatarSpawned);
        Debug.Log(currentUser.role + currentUser.nickname);

        if(SocketManager.socketManager != null)
        {
            SocketManager.socketManager.OnUserJoined += HandleUserJoin;
            SocketManager.socketManager.OnUserLeft += HandleUserLeft;
        }

        RecalulateMasterPeer();

        var field = typeof(NetworkSpawnManager).GetField(
            "spawner", System.Reflection.BindingFlags.NonPublic 
            | System.Reflection.BindingFlags.Instance);
        spawner = (NetworkSpawner)field.GetValue(spawnManager);
    }

    public void RecalulateMasterPeer()
    {
        if(roomClient == null || roomClient.Me == null || roomClient.Room == null) return;

        string currentMasterId = roomClient.Room[MASTER_ID];
        if(roomClient.Room[ISMASTERIN] == "T") return;

        //현재 마스터 피어가 존재하지 않을 경우
        var allPeer = roomClient.Peers
                    .Append(roomClient.Me)
                    .OrderBy(p => p["id"])
                    .ToList();

        foreach(var peer in allPeer)
        {
            if(peer["id"] == currentMasterId)
            {
                allPeer.Remove(peer);
                break;
            }
        }

        if(allPeer.Count > 0)
        {
            cachedMasterId = allPeer[0]["id"];
            if(cachedMasterId == currentUser.id)
            {
                roomClient.Room[MASTER_ID] = cachedMasterId;
                roomClient.Room[ISMASTERIN] = "T";
                isMaster = true;
            }
            else
            {
                isMaster = false;
            }
        }
        SetWebSync();
        Debug.Log($"마스터 연산 결과 : {isMaster}");
    }

    //본인 오브젝트 생성
    public IEnumerator SpawnAvatarRoutine()
    {
        yield return new WaitUntil(() => roomClient.Me != null && roomClient.Me.networkId.Valid);

        if(isMaster)
        {
            foreach(NetworkUser user in users)
            {
                if(user.id == currentUser.id) continue;
                HandleUserJoin(user);
            }
        }

        //본인 아바타 생성
        if (!userDict.ContainsKey(currentUser.id))
        {
            userDict[currentUser.id] = null;
            GameObject myAvatar = spawnManager.SpawnWithPeerScope(avatarCatalogue.prefabs[currentUser.profileimage]);
            myAvatar.GetComponent<ObjSync>().enabled = true;
            myAvatar.GetComponent<ObjSync>().SetOwner(true);
            myAvatar.GetComponent<Avatar>().SetID(currentUser.id);
        }
    }

    //본인 아바타 퇴장
    //soket call > 구독 해제/등록 > 디스폰 > 룸클라이언트 수동파괴 > 씬전환
    //마스터 피어 권한의 이월은 이 오브젝트가 나가면서 발생한 ubiq 이벤트로 처리한다
    public void DespawnMyAvatar()
    {
        StartCoroutine(DespawnMyAvatarRoutine());
    }
    public IEnumerator DespawnMyAvatarRoutine()
    {
        if(isMaster) roomClient.Room[ISMASTERIN] = "F";
        //1. socket에 퇴장 call, 세션 정보 정리
        SocketManager.socketManager.LeftEvenet();
        SessionManager.sessionManager.ClearRoomData();

        //2. 리스너/액션 구독 및 구독 해제
        SocketManager.socketManager.OnUserJoined -= HandleUserJoin;
        SocketManager.socketManager.OnUserLeft -= HandleUserLeft;
        spawnManager.OnSpawned.RemoveAllListeners();
        UbiqP2PManager.ubiqManager.ClearListener();

        //3. 이 오브젝트 디스폰
        if(roomClient.Peers.Count() > 0)
        {
            yield return new WaitUntil(() => roomClient.Room[ISMASTERIN] == "T");
            spawnManager.Despawn(gameObject);   
        }
        else
        {
            yield return null;
            foreach(var user in userDict.Values) spawnManager.Despawn(user.gameObject);
        }

        //4. 룸클라이언트 파괴(스폰매니저 있는 곳에 다들어있다)
        if(spawnManager != null)
        {
            Destroy(spawnManager.gameObject);
        }

        //5. 씬 전환
        SceneManager.LoadScene("Lobby");
    }

    //타 유저 오브젝트 생성
    //마스터 피어만 이 작업을 수행하며, 웹 유저에 대해서만 이 작업을 수행한다
    public void HandleUserJoin(NetworkUser user)
    {
        if(user.role == "vr" || !isMaster) return;
        SpawnAvatarWithID(avatarCatalogue.prefabs[user.profileimage], user.id);
    }

    //타 유저 삭제 루틴(웹)
    public void HandleUserLeft(string _id)
    {
        if(!userDict.ContainsKey(_id)) return;

        bool isWeb = userDict[_id].role != "vr" ;
        StartCoroutine(OnAvatarOutRoutine(_id,isWeb));
    }

    //오브젝트 생성 완료시 호출
    private void OnAvatarSpawned(GameObject go, IRoom room, IPeer peer, NetworkSpawnOrigin origin)
    {
        go.GetComponent<ObjSync>().enabled = true;
        //vr 유저 매핑
        if(peer != null)
        {
            string id = peer["id"];
            if (!string.IsNullOrEmpty(id))
            {
                userDict[id] = go.GetComponent<Avatar>();
                userDict[id].role = "vr";
            }
        }
        //웹 유저 매핑 함수는 Avatar에서 호출
    }

    //오브젝트 퇴장시 호출
    public IEnumerator OnAvatarOutRoutine(string id, bool isWeb)
    {
        if(!userDict.ContainsKey(id)) yield break;
        yield return new WaitUntil(() => userDict[id].gameObject != null);
        yield return new WaitUntil(() => roomClient.Me != null && roomClient.Me.networkId.Valid);
        RecalulateMasterPeer();
        if(isMaster && isWeb) spawnManager.Despawn(userDict[id].gameObject);

        userDict.Remove(id);
    }

    public void ReigsterAvatar(string id, Avatar avatar)
    {
        if(string.IsNullOrEmpty(id) || userDict.ContainsKey(id)) return;
        userDict[id] = avatar;
    }
    
    //기존 roomScope를 이용한 생성은 매핑 과정에서 온갖 스파게티를 유발하므로
    //roomScope의 생성 방식을 참고하여 재설계
    public void SpawnAvatarWithID(GameObject prefab, string socketId)
    {
        // 1. 미래에 생성될 객체의 주민번호(NetworkId)를 먼저 발급
        NetworkId futureNetId = NetworkId.Unique();
        PrefabCatalogue catalogue = spawnManager.catalogue;

        // 2. NetworkSpawner가 감시하는 규칙에 맞게 Key 생성 
        // 예: "ubiq.spawner.발급한ID"
        string roomPropertyKey = $"{spawner.propertyPrefix}{futureNetId}";

        // 3. 확장 정보를 담은 패키지(JSON) 작성
        // JsonUtility는 ExtendedSpawnMessage를 직렬화해도 
        // 나중에 NetworkSpawner가 기본 Message로 읽을 때 추가 필드를 무시하므로 안전합니다.
        ExtendedSpawnMessage identityPkg = new ExtendedSpawnMessage
        {
            creatorPeer = roomClient.Me.networkId,
            catalogueIndex = catalogue.IndexOf(prefab),
            targetSocketId = socketId
        };

        string jsonPayload = JsonUtility.ToJson(identityPkg);

        // 4. 룸 클라이언트에 명령 하달 (이 순간 전 네트워크에 생성이 트리거됨)
        roomClient.Room[roomPropertyKey] = jsonPayload;

        Debug.Log($"[RoomSpawn] ID {futureNetId}번에 {socketId}의 자아를 주입하여 생성을 요청했습니다.");
    }

    public void SetWebSync(Avatar avatar = null)
    {
        if(!isMaster) return;
        if(avatar != null) avatar.GetComponent<ObjSync>().SetOwner(isMaster);
        else
        {
            foreach(var user in userDict)
            {
                if(user.Value.role == "vr" || user.Value.IsDestroyed()) continue;
                user.Value.GetComponent<ObjSync>().SetOwner(isMaster);
            }
        }

    }
}

[Serializable]
public struct ExtendedSpawnMessage
{
    // NetworkSpawner가 파싱에 사용하는 필수 필드
    public NetworkId creatorPeer;
    public int catalogueIndex;

    // 추가로 주입할 '자아(Identity)' 데이터
    public string targetSocketId;
}
```

## File: Avatar/AvatarManager.cs.meta
```
fileFormatVersion: 2
guid: fc5913ea459f1b34489be0a486740cb0
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Core.meta
```
fileFormatVersion: 2
guid: f067b536efc976842a5d8ec018d93de1
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Core/AppConfig.cs.example
```
//서버 주소 등 상수값들
public static class AppConfig
{
    // --- [백엔드 서버 주소 설정] ---
    public const string BaseUrl = "http://localhost:4000"; 

    // --- [API 경로] ---
    public const string AuthApi = BaseUrl + "/api/auth";
    public const string RegisterUrl = AuthApi + "/register";
    public const string LoginUrl = AuthApi + "/login";

    // --- [소켓 경로] ---
    public const string SocketUrl = BaseUrl;

    // --- [Ubiq P2P 포트] ---
    public const int UbiqPort = 8009;

    // --- [Agora App ID] ---
    public const string AgoraAppID = "your-app-id-here";

    // --- [Youtube API] ---
    public const string YoutubeAPI = "your-youtube-api-key-here";
}
```

## File: Core/AppConfig.cs.example.meta
```
fileFormatVersion: 2
guid: bcec1e8cf8ed4d64b960e96bb3bbf62c
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Core/AppConfig.cs.meta
```
fileFormatVersion: 2
guid: b967c91e04f798a41847a7f6940953c3
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Network.meta
```
fileFormatVersion: 2
guid: 52b52b25727e592438a5ff7b2cfe77c2
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Network/Auth.meta
```
fileFormatVersion: 2
guid: 76c66542033a0494095720015a52d285
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Network/Auth/AuthManager.cs
```csharp
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using FamilyLink.Network;
using System;
using Newtonsoft.Json;

//lobby에서 로그인/회원가입 등의 인증만을 담당하는 스크립트
public class AuthManager : MonoBehaviour
{
    public static AuthManager authManager;

    void Awake() {
        if (authManager == null) authManager = this;
        else { Destroy(gameObject); }
    }

    // 회원가입 실행
    public void Register(string json)
    {
        StartCoroutine(AuthRequest(AppConfig.RegisterUrl, json));
    }

    // 로그인 실행
    public void Login(string json)
    {
        StartCoroutine(AuthRequest(AppConfig.LoginUrl, json));
    }

    private IEnumerator AuthRequest(string url, String json)
    {
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if(request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log(JsonConvert.DeserializeObject(request.downloadHandler.text));

                //데이터 전달 시점
                AuthResponse response = JsonConvert.DeserializeObject<AuthResponse>(request.downloadHandler.text);
                SessionManager.sessionManager.SetSession(response.token, response.user);
                SocketManager.socketManager.Connect();

                UIManager.uiManager.UIChange(); // login > lobby
                if(SessionManager.sessionManager.currentUser.profileimage == 0) LobbyUI.lobbyUI.SetProfileImg();
            }
            else
            {
                //TODO : 로그인 실패 인디케이트
            }
        }
    }
}

public class AuthResponse
{
    public string token;
    public NetworkUser user;
}
```

## File: Network/Auth/AuthManager.cs.meta
```
fileFormatVersion: 2
guid: e2989f2dcdd7e4b4d97cf73818160ea1
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Network/Auth/SessionManager.cs
```csharp
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
```

## File: Network/Auth/SessionManager.cs.meta
```
fileFormatVersion: 2
guid: 4b2b276ce077e1749b1976847a03591b
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Network/SoketIO.meta
```
fileFormatVersion: 2
guid: c2421c201a129f245adebfc3b0eacf3e
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Network/SoketIO/NetworkUser.cs
```csharp
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
}
```

## File: Network/SoketIO/NetworkUser.cs.meta
```
fileFormatVersion: 2
guid: 90d39705d0009874ea5dcd12f74600cf
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Network/SoketIO/SoketManager.cs
```csharp
using UnityEngine;
using SocketIOClient;
using System;
using System.Collections.Generic;
using FamilyLink.Network;
using Newtonsoft.Json;

public class SocketManager : MonoBehaviour
{
    public static SocketManager socketManager;
    public SocketIOUnity socket; // 여기서 직접 Emit/On

    public Action<NetworkUser> OnUserJoined;
    public Action<string> OnUserLeft;
    void Awake()
    {
        if (socketManager == null) socketManager = this;
        //SessionManager하고 같은 오브젝트에 들어가므로 파괴방지, 중복방지는 따로 안한다
    }

    // AuthManager에서 로그인 성공시 이 함수 호출
    public void Connect()
    {
        if (socket != null && socket.Connected) return;
    
        var uri = new System.Uri(AppConfig.SocketUrl);
        
        var options = new SocketIOClient.SocketIOOptions
        {
            Auth = new { token = SessionManager.sessionManager.authToken },
            
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        };
    
        socket = new SocketIOUnity(uri, options);
    
        // 디버깅
        socket.OnConnected += (sender, e) => Debug.Log("<color=cyan>[Socket]</color> 연결 성공!");
        socket.OnUnityThread("connect_error", (data) => Debug.LogError($"소켓 인증 실패: {data}"));

        socket.Connect();
    }

    public void SetupEvenet()
    {
        //입장 이벤트 수신(소켓)
        socket.OnUnityThread("room:user_joined", (data) =>
        {
            var newUser = JsonConvert.DeserializeObject<NetworkUser>(data.ToString().Trim('[', ']'));
            OnUserJoined?.Invoke(newUser);
        });

        //퇴장 이벤트 수신(소켓)
        socket.OnUnityThread("room:user_left", (data) =>
        {
            var userId = JsonConvert.DeserializeObject<_LeftData_>(data.ToString().Trim('[', ']')).userId;
            OnUserLeft?.Invoke(userId);
        });
    }

    public void LeftEvenet()
    {
        socket.Off("room:user_joined");
        socket.Off("room:user_left");
        socket.Emit("room:leave");
    }
}

public struct _LeftData_
{
    public string userId;
}
```

## File: Network/SoketIO/SoketManager.cs.meta
```
fileFormatVersion: 2
guid: e94087325c680334fa29e1c6e27da9ee
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Network/Ubiq.meta
```
fileFormatVersion: 2
guid: b47c2c9a24efe934ebe3c4bf3a5f6eff
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Network/Ubiq/ObjSync.cs
```csharp
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Spawning;

//p2p 통신간 오브젝트 동기화를 담당하는 코드
//ubiq로 움직임 동기화가 필요한 모든 오브젝트는 이 스크립트를 포함해야 한다.
public class ObjSync : MonoBehaviour, INetworkSpawnable
{
    private NetworkContext context;
    public bool isOwner = false;

    private NetworkId _networkId;
    public NetworkId NetworkId
    {
        get => _networkId;
        set
        {
            _networkId = value;
            if(context.Id == NetworkId.Null) context = NetworkScene.Register(this, _networkId);
        }
    }

    // 동기화할 데이터 구조체
    struct Message
    {
        public Vector3 position;
        public Quaternion rotation;
        public string id;
    }

    // 소유권 설정 (예: 로컬 플레이어가 생성한 경우 true)
    public void SetOwner(bool _isOwner)
    {
        isOwner = _isOwner;
    }

    void Update()
    {
        if (isOwner && context.Id != NetworkId.Null)
        {
            // 소유자라면 자신의 위치 정보를 다른 클라이언트에게 전송
            context.Send(JsonUtility.ToJson(new Message
            {
                position = transform.position,
                rotation = transform.rotation,
                id = gameObject.GetComponent<Avatar>().id
            }));
        }
    }

    // 네트워크를 통해 메시지를 받았을 때 실행되는 함수
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        if (!isOwner)
        {
            // 소유자가 아니라면 받은 데이터를 바탕으로 위치/회전 업데이트
            var m = JsonUtility.FromJson<Message>(message.ToString());
            transform.position = m.position;
            transform.rotation = m.rotation;

            var avatar = gameObject.GetComponent<Avatar>();
            avatar.SetID(m.id);
            AvatarManager.avatarManager.ReigsterAvatar(m.id, avatar);
        }
    }
}
```

## File: Network/Ubiq/ObjSync.cs.meta
```
fileFormatVersion: 2
guid: 6733b5ff84b1f4540ae0ab0d77afe028
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: Network/Ubiq/UbiqP2PManager.cs
```csharp
using Ubiq.Rooms;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Ubiq.Messaging;
using UnityEngine.Events;
using Ubiq.Spawning;
using System.Linq;

public class UbiqP2PManager : MonoBehaviour
{
    public static UbiqP2PManager ubiqManager = null;
    public string roomID;
    private RoomClient roomClient;
    private List<IRoom> _rooms = new List<IRoom>();
    private bool isDiscoverFin;

    void Start()
    {
        if(ubiqManager == null) ubiqManager = this;
        else Destroy(this);

        roomClient = gameObject.GetComponent<RoomClient>();
        roomID = SessionManager.sessionManager.roomID;
        if (roomClient != null)
        {
            Debug.Log("ㅁㅇㄴㄻㄴ");
            roomClient.OnRooms.AddListener(HandleRoomsFound);
            roomClient.OnJoinedRoom.AddListener((rooms) => OnJoinAction());
            roomClient.OnPeerRemoved.AddListener((peer) => OnPeerOutAction(peer));

            StartCoroutine(InitRoomClientRoutine());
        } 
    }

    IEnumerator InitRoomClientRoutine()
    {
        yield return new WaitUntil(() => NetworkScene.Find(roomClient) != null);
        var scene = NetworkScene.Find(roomClient);

        yield return new WaitUntil(() => scene.connectionCount > 0);
        yield return new WaitUntil(() => roomClient.Me != null && roomClient.Me.networkId.Valid);

        var spawnManager = NetworkSpawnManager.Find(this);
        yield return new WaitUntil(() => spawnManager != null);
        yield return new WaitUntil(() => spawnManager.catalogue != null && spawnManager.catalogue.prefabs.Count > 0);
        
        isDiscoverFin = false;
        roomClient.DiscoverRooms();

        yield return new WaitUntil(() => isDiscoverFin);
        Debug.Log("asdfsda");
        JoinRoom(roomID);
    }

    private void HandleRoomsFound(List<IRoom> rooms, RoomsDiscoveredRequest request)
    {
        Debug.Log($"방 목록 수신 완료: {rooms.Count}개");
        _rooms = rooms;
        isDiscoverFin = true;
    }

    public void JoinRoom(string roomID)
    {
        //방 목록 순회
        foreach(IRoom r in _rooms)
        {
            //roomID가 일치하는 방이 있으면 거기에 접속하고 리턴
            if(r.Name == roomID)
            {
                roomClient.Join(r.JoinCode);
                return;
            }   
        }

        //없으면 방을 생성해서 접속
        roomClient.Join(roomID, true);
    }

    public void OnJoinAction()
    {
        roomClient.OnRooms.RemoveListener(HandleRoomsFound);

        roomClient.Me["id"] = SessionManager.sessionManager.currentUser.id;

        if(AvatarManager.avatarManager != null)
        {
            AvatarManager.avatarManager.StartSetup();
            StartCoroutine(AvatarManager.avatarManager.SpawnAvatarRoutine());
        } 
    }

    //얘는 vr 유저의 아웃만 감지한다
    public void OnPeerOutAction(IPeer peer)
    {
        StartCoroutine(AvatarManager.avatarManager.OnAvatarOutRoutine(peer["id"], false));

    }
    public void ClearListener()
    {
        //어짜피 방 나갈때 호출할거라 싹 날려버려도 상관없다
        roomClient.OnJoinedRoom.RemoveAllListeners();
        roomClient.OnPeerRemoved.RemoveAllListeners();
    }
}
```

## File: Network/Ubiq/UbiqP2PManager.cs.meta
```
fileFormatVersion: 2
guid: d6d68e1d989b87c4cb24aefe314db1f1
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: UI.meta
```
fileFormatVersion: 2
guid: d91b65ae023b5e141b03b30389b56336
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: UI/LobbyUI.cs
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System.Collections.Generic;
using FamilyLink.Network;

public class LobbyUI : MonoBehaviour
{
    private string _roomCode; // 방 코드 입력값 저장용
    public static LobbyUI lobbyUI;
    SocketIOUnity socket => SocketManager.socketManager.socket;

    [Header("Profile")]
    public GameObject ProfileImg;

    void Awake()
    {
        if (lobbyUI == null) lobbyUI = this;
        else { Destroy(gameObject); }
    }

    private void OnEnable()
    {
        if (SocketManager.socketManager?.socket == null) return;

        socket.OnUnityThread("room:state", (data) => {
            try {
                string rawJson = data.ToString();
                Debug.Log($"<color=white>[Raw Data]</color> {rawJson}");

                RoomStateResponse state = null;

                // 1. 데이터가 [ ] 로 시작하는 배열인 경우
                if (rawJson.Trim().StartsWith("[")) {
                    var list = JsonConvert.DeserializeObject<List<RoomStateResponse>>(rawJson);
                    if (list != null && list.Count > 0) state = list[0];
                } 
                // 2. 데이터가 { } 로 시작하는 단일 객체인 경우
                else {
                    state = JsonConvert.DeserializeObject<RoomStateResponse>(rawJson);
                }

                // 3. 데이터가 정상적으로 담겼다면 씬 전환
                if (state != null && !string.IsNullOrEmpty(state.roomId)) {
                    Debug.Log($"<color=cyan>[Lobby]</color> 검증 완료. 씬 이동: {state.joinCode}");
                    SessionManager.sessionManager.SetRoomID(state.joinCode);
                    SessionManager.sessionManager.SetRoomUser(state.participants);
                    socket.Off("room:state");
                    SocketManager.socketManager.SetupEvenet();
                    SceneManager.LoadScene("KaraokeRoom");
                }
            }
            catch (System.Exception e) {
                Debug.LogError($"파싱 실패 원인: {e.Message}");
            }
        });

        // 에러 리스너
        socket.Off("error");
        socket.OnUnityThread("error", (data) => {
            Debug.LogError($"<color=red>[Lobby]</color> 접속 에러: {data}");
        });
    }

    public void OnRandomMatchClick()
    {
        if (socket != null && socket.Connected) {
            Debug.Log("랜덤 매칭 시도: room:match");
            socket.Emit("room:match"); 
        }
    }

    public void OnJoinByCodeClick()
    {
        if (string.IsNullOrEmpty(_roomCode)) return;

        if (socket != null && socket.Connected) {
            Debug.Log($"코드 입장 시도: {_roomCode.ToUpper()}");
            
            // 명세서 및 프론트 규격: { joinCode: "ABCDE" } 객체 전달
            var payload = new { joinCode = _roomCode.ToUpper() };
            socket.Emit("room:join", payload);
        }
    }

    public void ChangeValueRoomCode(string v) => _roomCode = v;

    public void OnLogoutClick()
    {
        if (socket != null) socket.Disconnect();

        SessionManager.sessionManager.ClearSession();
        UIManager.uiManager.UIChange(); 
    }

    public void SetProfileImg()
    {
        if(socket == null) return;
        ProfileImg.SetActive(true);
    }

    public void _ImgButton(int num)
    {
        var user = SessionManager.sessionManager.currentUser;
        user.profileimage = num;
        var payload = new { profileImage = num };
        socket.Emit("updateProfile", payload);
        Debug.Log("변경완료");
        ProfileImg.SetActive(false);
    }
}

[System.Serializable]
public class RoomStateResponse {
    public string roomId;
    public string joinCode;
    public string status;
    public List<NetworkUser> participants;
}
```

## File: UI/LobbyUI.cs.meta
```
fileFormatVersion: 2
guid: 53fd1e94fdda7e74fbdc516682def221
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```

## File: UI/UIManager.cs
```csharp
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

//Lobby에서 UI 변화를 관장하는 스크립트
public class UIManager : MonoBehaviour
{
    public static UIManager uiManager;
    string _name;
    string _birth;

    [Header("LoginUI Slots")]
    public Canvas login;
    public TMP_InputField nameInput;
    public TMP_InputField birthInput;

    [Header("LobbyUI Slots")]
    public Canvas lobby;

    private void Awake()
    {
        if (uiManager == null) uiManager = this;
        else { Destroy(gameObject); }
    }
    public void UIChange()
    {
        if (login.gameObject.activeSelf)
        {
            login.gameObject.SetActive(false);
            lobby.gameObject.SetActive(true);
        }
        else
        {
            lobby.gameObject.SetActive(false);
            login.gameObject.SetActive(true);
        }
    }

    public void OnRegisterClick()
    {
        Debug.Log($"<color=yellow>[Auth]</color> 회원가입 시도: 이름({_name}), 생일({_birth})");

        string json = JsonUtility.ToJson(new RegisterData
        {
            name = _name,
            email = _name + "@familylink.com",
            password = _birth,
            role = "vr"
        });
        
        AuthManager.authManager.Register(json);
    }

    public void OnLoginClick()
    {
        Debug.Log($"<color=yellow>[Auth]</color> 로그인 시도: 이름({_name}), 생일({_birth})");

        string json = JsonUtility.ToJson(new LoginData
        {
            name = _name,
            password = _birth,
            role = "vr"
        });

        AuthManager.authManager.Login(json);
    }

    public void KeyboardFloat(string inputField)
    {
        TouchScreenKeyboard.Open(inputField);
        Debug.Log("함수는실행됨@@");
    }

    public void ChangeValueName(string value) => _name = value;
    public void ChangeValueBirth(string value) => _birth = value;
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
```

## File: UI/UIManager.cs.meta
```
fileFormatVersion: 2
guid: ca0e0181b28db1645bb68422c991c408
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant:
```
