using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mic : MonoBehaviour
{
    public GameObject traceTarget;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(traceTarget != null)
        {
            transform.position = traceTarget.transform.position;
        }
    }
}
