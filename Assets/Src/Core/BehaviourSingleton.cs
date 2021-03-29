/// <summary>
/// Generic Mono singleton.
/// </summary>
using UnityEngine;

public abstract class BehaviourSingleton<T> : MonoBehaviour where T : BehaviourSingleton<T>
{

    private static T m_Instance = null;

    public static T instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = GameObject.FindObjectOfType(typeof(T)) as T;
                if (m_Instance == null)
                {
                    GameObject sinObj = GameObject.Find("Single_" + typeof(T).ToString());
                    if (sinObj == null)
                        m_Instance = new GameObject("Single_" + typeof(T).ToString()).AddComponent<T>();
                    else
                        m_Instance = sinObj.AddComponent<T>();
                }
            }
            return m_Instance;
        }
    }

    public static void Destroy()
    {
        if (m_Instance != null)
        {
            GameObject.Destroy(m_Instance.gameObject);
            m_Instance = null;
        }
    }

    protected virtual void Awake()
    {
        if (m_Instance == null)
        {
            m_Instance = this as T;
            DontDestroyOnLoad(m_Instance);
        }
    }

    protected virtual void OnDestroy()
    {
    }
    private void OnApplicationQuit()
    {
        m_Instance = null;
    }
}