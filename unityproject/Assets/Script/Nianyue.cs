using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class Nianyue : MonoBehaviour
{
    // Start is called before the first frame update
    public Text CurrentTime;

    void Update()
    {
        DateTime NowTime = DateTime.Now.ToLocalTime();
        CurrentTime.text = NowTime.ToString("yyyy.MM.dd");
    }
}
