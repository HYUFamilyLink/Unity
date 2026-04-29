using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    public static SpawnPointManager spawnPointManager;
    public List<SpawnPoint> points = new List<SpawnPoint>();

    private void Awake()
    {
        if(spawnPointManager == null) spawnPointManager = this;
        else Destroy(this);

        Init();
    }

    public int SetPoint(Avatar avatar)
    {
        int idx = 0;

        for(int i = 0; i < 6; i++)
        {
            if(points[i].avatar == avatar) return i;
        }
        for(idx = 0; idx < 6; idx++)
        {
            if(points[idx].avatar == null)
            {
                points[idx].avatar = avatar;
                avatar.transform.position = points[idx].point.transform.position;
                avatar.transform.rotation = points[idx].point.transform.rotation;
                break;
            }
        }
        return idx;
    }

    public void SetPoint(int idx, Avatar avatar)
    {
        if(points[idx].avatar != null) return;
        points[idx].avatar = avatar;
        avatar.transform.position = points[idx].point.transform.position;
        avatar.transform.rotation = points[idx].point.transform.rotation;
    }

    public void DelPoint(int idx)
    {
        points[idx].avatar = null;
    }

    public void DelPoint(Avatar avatar)
    {
        foreach(var p in points)
        {
            if(p.avatar == avatar || p.avatar == null) p.avatar = null;
        }
    }

    public int GetPoint(Avatar avatar)
    {
        int idx = 0;
        foreach(var p in points)
        {
            if(p.avatar == avatar) break;
            idx++;
        }
        return idx;
    }

    public void Init(){foreach(var p in points) p.avatar = null;}
}

[Serializable]
public class SpawnPoint
{
    public GameObject point;
    public Avatar avatar;
}