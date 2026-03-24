using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "SO_UIventName", menuName = "ScriptableObject/SO_UIEvent")]
public class SO_UIEvent : ScriptableObject 
{
    public UnityEvent<bool> OnActiveToggle;

    public void Raise(bool active) => OnActiveToggle?.Invoke(active);
}
