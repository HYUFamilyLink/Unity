using Ubiq.Rooms;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Ubiq.Messaging;
using UnityEngine.Events;

public class UbiqRoomManager : MonoBehaviour
{
    public string roomID;
    private RoomClient roomClient;
    private List<IRoom> _rooms = new List<IRoom>();
    private UnityAction<List<IRoom>, RoomsDiscoveredRequest> _joinAction;

    void Start()
    {
        roomClient = gameObject.GetComponent<RoomClient>();
        roomID = SessionManager.sessionManager.roomID;
        _joinAction = (rooms, req) => JoinRoom(roomID);
        if (roomClient != null)
        {
            roomClient.OnRooms.AddListener(HandleRoomsFound);
            roomClient.OnRooms.AddListener(_joinAction);
            StartCoroutine(InitRoomClientRoutine());
        } 
    }

    IEnumerator InitRoomClientRoutine()
    {
        yield return new WaitUntil(() => NetworkScene.Find(roomClient) != null);
        var scene = NetworkScene.Find(roomClient);
        yield return new WaitUntil(() => scene.connectionCount > 0);
        RequestRoomList();
    }

    public void RequestRoomList()
    {
        roomClient.DiscoverRooms();
    }

    private void HandleRoomsFound(List<IRoom> rooms, RoomsDiscoveredRequest request)
    {
        Debug.Log($"방 목록 수신 완료: {rooms.Count}개");
        _rooms = rooms;
    }

    public void JoinRoom(string roomID)
    {
        if(_rooms == null) return;
        if(_rooms.Count == 0) roomClient.Join(roomID, true);
        else
        {
            foreach(IRoom r in _rooms)
            {
                if(r.Name == roomID)
                {
                    roomClient.Join(r.JoinCode);
                    roomClient.OnRooms.RemoveListener(HandleRoomsFound);
                    roomClient.OnRooms.RemoveListener(_joinAction);
                    return;
                }   
            }
            roomClient.Join(roomID, true);
        }
        
        roomClient.OnRooms.RemoveListener(HandleRoomsFound);
        roomClient.OnRooms.RemoveListener(_joinAction);
    }
}