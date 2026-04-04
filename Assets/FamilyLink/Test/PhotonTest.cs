using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonTest : MonoBehaviourPunCallbacks
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("포톤 연결 시도중");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("연결 성공, 입장 시도");
        PhotonNetwork.JoinOrCreateRoom("TestVoiceRoom", new RoomOptions{MaxPlayers = 4}, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("포톤 음성 접속 성공");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"연결 끊김 사유 {cause}");
    }
}
