using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingEffect : MonoBehaviour
{
    public float amplitude = 10f; // 揺れの振幅
    public float frequency = 1f; // 揺れの周波数
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        transform.localPosition = startPos + new Vector3(0.0f, Mathf.Sin(Time.time * frequency) * amplitude, 0.0f);
    }
}
