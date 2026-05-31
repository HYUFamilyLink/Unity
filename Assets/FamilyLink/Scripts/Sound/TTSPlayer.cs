using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using FamilyLink;

[RequireComponent(typeof(AudioSource))]
public class TTSPlayer : MonoBehaviour
{
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        // SocketManager의 TTS 안내 이벤트 구독
        if (SocketManager.socketManager != null)
        {
            SocketManager.socketManager.OnRoomAnnounce += HandleAnnounce;
        }
    }

    void OnDestroy()
    {
        // 구독 해제
        if (SocketManager.socketManager != null)
        {
            SocketManager.socketManager.OnRoomAnnounce -= HandleAnnounce;
        }
    }

    // 서버로부터 TTS 메시지와 오디오 데이터를 받았을 때 호출됨
    private void HandleAnnounce(string message, string base64Audio)
    {
        Debug.Log($"[TTS 안내] {message}"); // UI 자막용으로 사용 가능
        
        if (!string.IsNullOrEmpty(base64Audio))
        {
            StartCoroutine(PlayBase64Audio(base64Audio));
        }
    }

    private IEnumerator PlayBase64Audio(string base64Data)
    {
        // 1. Base64 문자열을 byte 배열로 변환
        byte[] audioBytes = Convert.FromBase64String(base64Data);

        // 2. 임시 파일로 저장 (UnityWebRequestMultimedia는 파일 경로나 URL이 필요함)
        // 구글 TTS는 기본적으로 MP3 형식이므로 .mp3 확장자 사용
        string tempPath = Path.Combine(Application.temporaryCachePath, "temp_tts.mp3");
        File.WriteAllBytes(tempPath, audioBytes);

        // 3. 임시 파일 경로를 uri로 변환하여 AudioClip 로드
        string uri = "file://" + tempPath;
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                
                // 음성 재생
                audioSource.clip = clip;
                audioSource.Play();
            }
            else
            {
                Debug.LogError($"<color=red>[TTS Error]</color> 오디오 로드 실패: {www.error}");
            }
        }
    }
}