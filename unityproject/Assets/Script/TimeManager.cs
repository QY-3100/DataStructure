using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Time Scale Settings")]
    public float timeScale = 60f; 
    public float minTimeScale = 1f;
    public float maxTimeScale = 120f;

    private float _systemTime = 0f;
    public float SystemTime => _systemTime;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        _systemTime += Time.deltaTime * timeScale;
    }

    public void SetTimeScale(float scale)
    {
        timeScale = Mathf.Clamp(scale, minTimeScale, maxTimeScale);
        Debug.Log($"Time scale set to: {timeScale}x (1s real = {timeScale}s system)");
    }

    public float GetDeltaTime()
    {
        return Time.deltaTime * timeScale;
    }

    public long GetSystemTimestamp()
    {
        return (long)_systemTime;
    }
}
