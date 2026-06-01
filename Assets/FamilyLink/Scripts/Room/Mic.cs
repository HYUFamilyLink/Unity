using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Mic : MonoBehaviour
{
    public Transform traceTarget;
    
    private AudioEchoFilter avatarEcho;
    private AudioReverbFilter avatarReverb;
    private bool isAudioInitialized = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(traceTarget != null)
        {
            transform.position = traceTarget.position;
            transform.rotation = traceTarget.rotation;

            if (!isAudioInitialized)
            {
                avatarEcho = traceTarget.GetComponentInParent<AudioEchoFilter>();
                avatarReverb = traceTarget.GetComponentInParent<AudioReverbFilter>();

                isAudioInitialized = true; 
            }

            if (avatarEcho != null && !avatarEcho.enabled) avatarEcho.enabled = true;
            if (avatarReverb != null && !avatarReverb.enabled) avatarReverb.enabled = true;
        }
        else
        {
            if (avatarEcho != null && avatarEcho.enabled) avatarEcho.enabled = false;
            if (avatarReverb != null && avatarReverb.enabled) avatarReverb.enabled = false;
            
            if (isAudioInitialized)
            {
                avatarEcho = null;
                avatarReverb = null;
                isAudioInitialized = false;
            }
        }
    }
}
