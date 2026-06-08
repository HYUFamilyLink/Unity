using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Android;
using FamilyLink;
using Newtonsoft.Json;
using UnityEngine.Video;

public class STTManager : MonoBehaviour
{
    public static STTManager sttManager;

    private string micDevice;
    private AudioClip recordingClip;
    private bool isRecording = false;

    private const int MAX_RECORD_TIME = 10; 
    private const int SAMPLE_RATE = 16000;

    private VideoPlayer activeVideoPlayer;
    private bool wasVideoPlaying;

    private void Awake()
    {
        // ★ 기존 스크립트들과 동일한 초기화 및 중복 파괴 로직
        if (sttManager == null)
        {
            sttManager = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(this); 
        
        #if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
        #endif

        if (Microphone.devices.Length > 0)
            micDevice = Microphone.devices[0];
    }

    public async Task StartRecording()
    {
        if (string.IsNullOrEmpty(micDevice)) return;

        activeVideoPlayer = FindObjectOfType<VideoPlayer>();
        if (activeVideoPlayer != null && activeVideoPlayer.isPlaying)
        {
            wasVideoPlaying = true;
            activeVideoPlayer.Pause();
        }

        if (AgoraManager.agoraManager != null)
        {
            AgoraManager.agoraManager.PauseMicForSTT(true);
        }

        await Task.Delay(300);

        isRecording = true;
        recordingClip = Microphone.Start(micDevice, false, MAX_RECORD_TIME, SAMPLE_RATE);
        Debug.Log("<color=green>[STT]</color> 녹음 시작...");
    }

    // 콜백 없이 직관적으로 Task<string> 반환
    public async Task<string> StopRecording()
    {
        if (!isRecording) return "";
        isRecording = false;

        int position = Microphone.GetPosition(micDevice);
        Microphone.End(micDevice);
        Debug.Log("<color=green>[STT]</color> 녹음 종료. 변환 및 전송 중...");

        // 1. 비디오 플레이어 원상복구
        if (wasVideoPlaying && activeVideoPlayer != null)
        {
            activeVideoPlayer.Play();
            wasVideoPlaying = false;
        }

        if (AgoraManager.agoraManager != null)
        {
            AgoraManager.agoraManager.PauseMicForSTT(false);
        }

        if (position > 0)
        {
            byte[] wavData = WavUtility.FromAudioClip(recordingClip, position);
            return await UploadAudioAsync(wavData);
        }
        
        return "";
    }

    // 코루틴 대신 비동기 Task 사용
    private async Task<string> UploadAudioAsync(byte[] audioData)
    {
        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", audioData, "record.wav", "audio/wav");
        using (UnityWebRequest www = UnityWebRequest.Post(AppConfig.STTUrl, form))
        {
            // 필요 시 주석 해제하여 토큰 연동
            // www.SetRequestHeader("Authorization", "Bearer " + SessionManager.sessionManager.authToken);

            var operation = www.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield(); 
            }

            if (www.result == UnityWebRequest.Result.Success)
            {
                string textResult = www.downloadHandler.text;
                _STTData_ data = JsonConvert.DeserializeObject<_STTData_>(textResult.ToString().Trim('[', ']'));
                Debug.Log("<color=yellow>[STT 성공]</color> 인식 결과: " + data.text);
                return data.text; 
            }
            else
            {
                Debug.LogError("<color=red>[STT 실패]</color> " + www.error);
                return ""; 
            }
        }
    }
}