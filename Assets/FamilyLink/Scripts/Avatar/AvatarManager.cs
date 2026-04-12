using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using FamilyLink.Network;
using Ubiq.Rooms;
using Ubiq.Spawning;
using UnityEngine;
using UnityEngine.InputSystem.iOS;

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
    public Dictionary<string, GameObject> userDict = new Dictionary<string, GameObject>();

    //soketIO에서 받아온 데이터
    private List<NetworkUser> users;
    private NetworkUser currentUser;
    private string lastInId;

    //ubiq
    public NetworkSpawnManager spawnManager;
    RoomClient roomClient => spawnManager.roomClient;

    //TODO : 전용 Avatar class로 변경
    public GameObject avatarBase;
    bool isMaster => spawnManager.roomClient.Peers.All(p => p.networkId.GetHashCode() > spawnManager.roomClient.Me.networkId.GetHashCode());
    
    void Awake()
    {
        if(avatarManager == null) avatarManager = this;
    }

    public void StartSetup()
    {
        if (spawnManager == null) spawnManager = FindObjectOfType<NetworkSpawnManager>();
        spawnManager.OnSpawned.AddListener(OnAvatarSpawned);

        users = SessionManager.sessionManager.users;
        currentUser = SessionManager.sessionManager.currentUser;

        if(SocketManager.socketManager != null)
        {
            Debug.Log("asdfdasfasfdsfasdfasfdsafsa");
            SocketManager.socketManager.OnUserJoined += HandleUserJoin;
            SocketManager.socketManager.OnUserLeft += HandleUserLeft;
        }
    }

    //본인 오브젝트 생성
    public IEnumerator SpawnAvatarRoutine()
    {

        yield return new WaitUntil(() => roomClient.Me != null && roomClient.Me.networkId.Valid);

        //본인 아바타 생성
        if (!userDict.ContainsKey(currentUser.id))
        {
            userDict[currentUser.id] = null;
            GameObject myAvatar = spawnManager.SpawnWithPeerScope(avatarBase);
            myAvatar.GetComponent<ObjSync>().enabled = true;
            myAvatar.GetComponent<ObjSync>().SetOwner(true);
        }

        Debug.Log(roomClient.Peers.Count() + "생성ㅇㅇㅇ" + isMaster);
    }

    //타 유저 오브젝트 생성
    //마스터 피어만 이 작업을 수행하며, 웹 유저에 대해서만 이 작업을 수행한다
    public void HandleUserJoin(NetworkUser user)
    {
        if(user.role == "vr" || !isMaster) return;
        Debug.Log("호출ㄹㄹㄹㄹㅁㄴㅇㄻㄴㄹㅇ");
        spawnManager.SpawnWithRoomScope(avatarBase);
        lastInId = user.id;
    }

    //타 유저 삭제 루틴
    public void HandleUserLeft(string _id)
    {
        if(!userDict.ContainsKey(_id)) return;
        Debug.Log("퇴장ㅇ");

        //임시 하드코딩
        bool isWeb = true;
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
            if(!string.IsNullOrEmpty(id)) userDict[id] = go;
        }
        //웹 유저 매핑
        else
        {
            Debug.Log(go.name + "asdfasd");
            //roomScope로 생성된 유저는 peer 정보가 없어서 오브젝트에 id를 담을 스크립트가 필요하다
            var webUser = users.FirstOrDefault(u => !userDict.ContainsKey(u.id));
            Debug.Log(webUser);
            if(webUser == null) {userDict[lastInId] = go; Debug.Log("등록");}
        }
    }

    //오브젝트 퇴장시 호출
    public IEnumerator OnAvatarOutRoutine(string id, bool isWeb)
    {
        Debug.Log("삭제로직1");
        if(!userDict.ContainsKey(id)) yield break;
        Debug.Log("삭제로직2");
        yield return new WaitUntil(() => roomClient.Me != null && roomClient.Me.networkId.Valid);
        if(isMaster && isWeb) spawnManager.Despawn(userDict[id]);
        Debug.Log("삭제로직3");
        userDict.Remove(id);
        Debug.Log("삭제로직4");
    }
}