using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class Xingqi : MonoBehaviour
{
    public Text CurrentTime;

    void Update()
    {
        DateTime NowTime = DateTime.Now.ToLocalTime();
        CurrentTime.text = NowTime.ToString("dddd");
    }
}
