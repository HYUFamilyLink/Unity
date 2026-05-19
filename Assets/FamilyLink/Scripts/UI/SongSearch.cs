using System.Collections;
using System.Collections.Generic;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Unity.Tutorials.Core.Editor;
using UnityEngine;
using UnityEngine.Networking;
using FamilyLink;
using JetBrains.Annotations;
using UnityEngine.UIElements;

public class SongSearch : MonoBehaviour
{
    public static SongSearch songSearch;
    SocketIOUnity socket => SocketManager.socketManager.socket;
    string token => SessionManager.sessionManager.authToken;
    string songTitle = "";
    public List<SearchResponse> songs;

    [Header("필드")]
    public GameObject songItemPrefab;
    public Transform songListContent;

    void Start()
    {
        if(songSearch == null) songSearch = this;
        else Destroy(this);
    }

    void OnEnable()
    {
        songTitle = "";
        foreach (Transform child in songListContent)
        {
            Destroy(child.gameObject);
        }
    }

    public void OnSelect()
    {
        // 키보드, 음성인식 모듈 활성화
    }

    //OnValueChanged && OnEndEdit
    public void OnTitleget(string input)
    {
        songTitle = input;
    }

    //검색 버튼 클릭
    public void SearchSong()
    {
        if (songTitle.IsNullOrEmpty()) return;

        StartCoroutine(SearchSongRoutine(songTitle));
    }

    IEnumerator SearchSongRoutine(string title)
    {
        string url = AppConfig.SongUrl + UnityWebRequest.EscapeURL(title);

        using(UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.SetRequestHeader("Authorization", "Bearer " + token);

            yield return webRequest.SendWebRequest();

            if(webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;

                songs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SearchResponse>>(jsonResponse);
                ViewSelect();
            }
            else
            {
                Debug.LogError($"에러 : {webRequest.error}");
            }
        }
    }

    void ViewSelect()
    {
        foreach (Transform child in songListContent)
        {
            Destroy(child.gameObject);
        }

        foreach(var song in songs)
        {
            GameObject songObj = Instantiate(songItemPrefab, songListContent);
            SongItemUI songScript = songObj.GetComponent<SongItemUI>();
            if (songScript != null)
            {
                songScript.SetSong(song);
            }
        }
    }

    public void SelectEnd()
    {
        gameObject.SetActive(false);
    }
}
