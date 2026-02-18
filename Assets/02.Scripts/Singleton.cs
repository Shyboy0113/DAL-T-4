using UnityEngine;

/*
    싱글톤 패턴을 위한 제네릭 클래스
    사용 클래스가 Monobehaviour를 상속받도록 where 절 사용

    사용 시 주의점:
    Class Name 과 GameObject Name은 일치해야 한다.
*/

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static readonly object lockObject = new();
    
    private static T instance;

    public static T Instance
    {
        get
        {
            // Thread safe
            lock (lockObject)
            {
                // 만약 객체가 저장되지 않았다면,
                if (!instance)
                {
                    // 씬에서 객체를 찾는다.
                    instance = (T)FindObjectOfType(typeof(T));

                    // 못찾았다면 객체 생성이 안 된 것이기 때문에 Resources 폴더에서 Prefab을 가져온다.
                    if (!instance)
                    {
                        instance = Instantiate(Resources.Load<T>("Singleton/" + typeof(T).Name));
                    }
                    DontDestroyOnLoad(instance.gameObject);
                }
            }
            return instance;
        }
    }
    
    private void OnDestroy()
    {
        instance = null;
    }
   

}