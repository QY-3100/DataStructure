using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Jiemian : MonoBehaviour
{
    //定义UI
    public GameObject Zuoce1_img;
    public GameObject Youce1_img;
    public GameObject Xiace1_img;

    public GameObject Youce2_img;
    public GameObject Xiace2_img;

    public GameObject Youce3_img;
    public GameObject Xiace3_img;
    public GameObject Zuoce3;

    public GameObject Youce4_img;
    public GameObject Xiace4_img;
    public GameObject nenghao_img;

    public GameObject jiankong;

    private CanvasGroup nenghaoCanvasGroup; // nenghao_img 的 CanvasGroup 组件




    void Start()
    {
        // 获取 nenghao_img 的 CanvasGroup 组件
        nenghaoCanvasGroup = nenghao_img.GetComponent<CanvasGroup>();
        // 初始化 nenghao_img
        nenghao_img.SetActive(false);
        nenghaoCanvasGroup.alpha = 0;
        ResetUI();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void ResetUI()
    {
        //引用dotween插件，移动左侧栏消失
        Zuoce1_img.transform.DOLocalMoveX(-2500, 0);
        Youce1_img.transform.DOLocalMoveX(2500, 0);
        Xiace1_img.transform.DOLocalMoveY(-1500, 0);

        Youce2_img.transform.DOLocalMoveX(2500, 0);
        Xiace2_img.transform.DOLocalMoveY(-1500, 0);

        Youce3_img.transform.DOLocalMoveX(2500, 0);
        Xiace3_img.transform.DOLocalMoveY(-1500, 0);

        Youce4_img.transform.DOLocalMoveX(2500, 0);
        Xiace4_img.transform.DOLocalMoveY(-1500, 0);

        // 隐藏 nenghao_img
        HideNenghao();

        Zuoce3.transform.DOScale(0, 0);//楼层按钮默认不出现
        jiankong.transform.DOScale(0, 0);//监控小标签默认不出现
    }


    public void Func1_Click()
    {

        ResetUI();
        Zuoce1_img.transform.DOLocalMoveX(-1550, 1);
        Youce1_img.transform.DOLocalMoveX(1550, 1);
        Xiace1_img.transform.DOLocalMoveY(-860, 1);
        HideNenghao();



    }
    public void Func2_Click()
    {

        ResetUI();
        Youce2_img.transform.DOLocalMoveX(1550, 1);
        Xiace2_img.transform.DOLocalMoveY(-905, 1);
        HideNenghao();


    }
    public void Func3_Click()
    {

        ResetUI();
        Youce3_img.transform.DOLocalMoveX(1550, 1);
        Xiace3_img.transform.DOLocalMoveY(-905, 1);
        HideNenghao();

    }
    public void Func4_Click()
    {

        ResetUI();
        Youce4_img.transform.DOLocalMoveX(1550, 1);
        Xiace4_img.transform.DOLocalMoveY(-905, 1);
        HideNenghao();


    }

    public void Func5_Click()
    {
        StartCoroutine(ShowNenghao());
    }

    IEnumerator ShowNenghao()
    {
        // 隐藏其他页面
        ResetUI();

        // 逐渐显示 nenghao_img
        nenghao_img.SetActive(true);
        while (nenghaoCanvasGroup.alpha < 1)
        {
            nenghaoCanvasGroup.alpha += Time.deltaTime;
            yield return null;
        }
    }

    void HideNenghao()
    {
        // 隐藏 nenghao_img
        nenghao_img.SetActive(false);
        nenghaoCanvasGroup.alpha = 0;
    }


    public void Jiankong_Show(bool b)
    {
        if (b)
        {
            jiankong.transform.DOScale(1, 0);
        }
        else
        {
            jiankong.transform.DOScale(0, 0);
        }
    }
    public void Zuoce3_Show(bool b)
    {
        if (b)
        {
            Zuoce3.transform.DOScale(1, 0);
        }
        else
        {
            Zuoce3.transform.DOScale(0, 0);
        }
    }
}