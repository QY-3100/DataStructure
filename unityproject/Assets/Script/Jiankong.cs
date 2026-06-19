using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Jiankong : MonoBehaviour
{
    //定义UI
    public GameObject jiankongtk;

    void Start()
    {
          ResetUI();
    }

    // Update is called once per frame
    void Update()
    {

    }
    void ResetUI()
    {
        jiankongtk.transform.DOScale(0, 0);//监控弹窗默认不出现
    }
    public void Jiankongtk_Show(bool b)
    {
        if (b)
        {
            jiankongtk.transform.DOScale(1, 0);
        }
        else
        {
            jiankongtk.transform.DOScale(0, 0);
        }
    }
}