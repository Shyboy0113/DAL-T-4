using System.Collections.Generic;
using UnityEngine;
using Eflatun.SceneReference;

[CreateAssetMenu(fileName = "SO_SceneGroupName", menuName = "ScriptableObject/SceneGroup")]
public class SO_SceneGroup : ScriptableObject
{
    [Header("메인 콘텐츠 씬 (하나만 등록)")]
    public SceneReference mainScene;

    [Header("함께 로드할 부가 씬들 (옵션 UI 등)")]
    public List<SceneReference> additiveScenes;
    
}
