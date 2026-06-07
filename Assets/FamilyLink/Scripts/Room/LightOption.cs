using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightOption : MonoBehaviour
{
    public float rotationSpeed = 15f;

    private Light light = new Light();

    public Color[] colors = new Color[] { Color.magenta, Color.cyan, Color.yellow, Color.green, Color.red, Color.blue };
    public float colorChangeInterval = 3f;
    private float timer = 0f;
    private int index = 0;

    void Start()
    {
        light = gameObject.GetComponent<Light>();
        light.color = colors[index];
    }

    void Update()
    {
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
        if (light != null && colors.Length > 0)
        {
            timer += Time.deltaTime;
            if(timer >= colorChangeInterval)
            {
                timer = 0f;
                index = (index+1) % colors.Length;
                light.color = colors[index];
            }
        }
    }
}
