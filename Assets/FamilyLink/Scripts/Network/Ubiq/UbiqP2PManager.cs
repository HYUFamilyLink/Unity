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

        AgoraManager.agoraManager.AgoraConnect();
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