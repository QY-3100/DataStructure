using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 引入UI命名空间


public class Anniu : MonoBehaviour
{
    public Image[] defaultImages; // 每个按钮的默认图片数组  
    public Image[] switchableImages; // 可切换的图片数组  
    public Button[] buttons; // 按钮数组  

    private Image currentImage; // 当前显示的图片  
    private int currentImageIndex = -1; // 当前显示的图片索引  

    // Start方法会在脚本实例被创建时调用  
    private void Start()
    {
        // 初始化，确保默认图片都是可见的  
        foreach (var image in defaultImages)
        {
            image.gameObject.SetActive(true);
        }

        // 隐藏所有可切换的图片  
        foreach (var image in switchableImages)
        {
            image.gameObject.SetActive(false);
        }

        // 为每个按钮添加点击事件监听器  
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onClick.AddListener(() => SwitchToImage(index));
        }
    }

    // 切换到指定索引的图片，并恢复其他按钮的默认图片  
    private void SwitchToImage(int index)
    {
        // 如果当前有图片显示，则隐藏它  
        if (currentImage != null)
        {
            currentImage.gameObject.SetActive(false);
        }

        // 恢复所有按钮的默认图片  
        foreach (var image in defaultImages)
        {
            image.gameObject.SetActive(true);
        }

        // 如果点击的按钮对应的图片不是当前显示的图片，则显示它  
        if (index >= 0 && index < switchableImages.Length)
        {
            switchableImages[index].gameObject.SetActive(true);
            currentImage = switchableImages[index];
            currentImageIndex = index;

            // 隐藏该按钮的默认图片  
            defaultImages[index].gameObject.SetActive(false);
        }
    }
}