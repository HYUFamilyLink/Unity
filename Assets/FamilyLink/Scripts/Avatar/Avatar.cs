using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//아바타 고유 정보 등을 처리 및 보유하는 스크립트
public class Avatar : MonoBehaviour
{
    public string id;

    public void SetID(string _id) { id = _id; return;}
}
