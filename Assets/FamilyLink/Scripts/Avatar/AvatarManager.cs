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
    public Avatar avatarBase;
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
            GameObject myAvatar = spawnManager.SpawnWithPeerScope(avatarBase.gameObject);
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
        SpawnAvatarWithID(avatarBase.gameObject, user.id);
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

