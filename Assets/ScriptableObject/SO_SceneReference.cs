using UnityEngine;
using Eflatun.SceneReference;

// 데이터 중앙 집중화
// 런타임과 상관없는 에디터에서의 접근성 - 경로 기반 로드 (AssetDatabase.LoadAssetAtPath)
// 

[CreateAssetMenu(fileName = "SO_SceneReference", menuName = "ScriptableObject/SceneReference")]
public class SO_SceneReference : ScriptableObject 
{
    public SceneReference scene;
}