using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Mic : MonoBehaviour
{
    public Transform traceTarget;

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
        }
    }
}
