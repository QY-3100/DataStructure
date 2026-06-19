using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Tiaozhuan : MonoBehaviour
{
    public Button switchButton; // 按钮引用
    public string targetSceneName; // 目标场景名称

    private string currentSceneName; // 当前场景名称

    void Start()
    {
        // 获取当前场景的名称
        currentSceneName = SceneManager.GetActiveScene().name;

        // 确保按钮已经分配
        if (switchButton != null)
        {
            switchButton.onClick.AddListener(OnButtonClick);
        }
    }

    void OnButtonClick()
    {
        // 获取当前场景的名称
        string activeSceneName = SceneManager.GetActiveScene().name;

        // 如果当前场景是目标场景，返回原始场景；否则，跳转到目标场景
        if (activeSceneName == targetSceneName)
        {
            SceneManager.LoadScene(currentSceneName, LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
        }
    }
}
