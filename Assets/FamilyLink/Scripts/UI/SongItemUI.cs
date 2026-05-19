using System.Collections;
using System.Collections.Generic;
using FamilyLink;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.XR.CoreUtils;

public class SongItemUI : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI artiestText;
    public Button button;

    [Header("tag")]
    public GameObject tagPrefab;
    public Transform tagContenet;


    public void SetSong(SearchResponse song)
    {
        titleText.text = $"{song.title}";
        artiestText.text = song.artist;

        foreach(Transform child in tagContenet) Destroy(child);

        if(song.tags != null && song.tags.Count > 0)
        {
            foreach(string tag in song.tags)
            {
                GameObject tagObj = Instantiate(tagPrefab, tagContenet);
                tagObj.GetComponentInChildren<TextMeshProUGUI>().text = tag;
            }
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => {
            Debug.Log($"{song.title} 재생 시도");
            SocketManager.socketManager.socket.Emit("song:select", new { videoId = song.id, title = song.title, artist = song.artist });
            SongSearch.songSearch.SelectEnd();
        });
    }
}
