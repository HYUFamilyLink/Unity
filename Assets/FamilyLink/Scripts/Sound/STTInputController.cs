using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_InputField))]
public class STTInputController : MonoBehaviour, IPointerClickHandler
{
    [Header("TTS 안내음 파일 (선택)")]
    public AudioClip sttStartClip;

    private AudioSource audioSource;
    private TMP_InputField inputField;
    private bool isProcessing = false;

    private void Awake()
    {
        // 1. 자기 자신에게 붙어있는 InputField 자동 할당
        inputField = GetComponent<TMP_InputField>();

        // 2. AudioSource 자동 탐색 및 생성
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            // 컴포넌트가 없으면 스크립트가 직접 추가합니다.
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; // 시작하자마자 재생되는 현상 방지
        }

        if (sttStartClip == null)
        {
            sttStartClip = Resources.Load<AudioClip>("stt_start"); 
        }
    }

    public async void OnPointerClick(PointerEventData eventData)
    {
        if (isProcessing) return; 
        await RunSTTFlow();
    }

    private async Task RunSTTFlow()
    {
        isProcessing = true;
        
        string originalText = inputField.text;
        inputField.interactable = false;

        // 음원 파일(Clip)이 인스펙터에 할당되어 있을 때만 자동 생성된 스피커로 재생
        if (sttStartClip != null)
        {
            audioSource.PlayOneShot(sttStartClip);
        }

        STTManager.sttManager.StartRecording();

        for (int i = 5; i > 0; i--)
        {
            inputField.text = $"음성인식 중... ({i})";
            await Task.Delay(1000);
        }

        inputField.text = "입력중...";
        string resultText = await STTManager.sttManager.StopRecording();

        if (!string.IsNullOrEmpty(resultText))
        {
            inputField.text = resultText;
        }
        else
        {
            inputField.text = originalText; 
        }

        inputField.interactable = true;
        inputField.Select();
        
        TouchScreenKeyboard.Open(inputField.text);

        isProcessing = false;
    }
}