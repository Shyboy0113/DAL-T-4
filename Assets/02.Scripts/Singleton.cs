using UnityEngine;

/*
    싱글톤 패턴을 위한 제네릭 클래스
    사용 클래스가 Monobehaviour를 상속받도록 where 절 사용

    사용 시 주의점:
    Class Name 과 GameObject Name은 일치해야 한다.
*/

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (!instance)
            {
                instance = FindFirstObjectByType<T>();
                if (instance) DontDestroyOnLoad(instance.gameObject);
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this as T;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}