using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public struct SceneReference
{
    [SerializeField, Tooltip("에디터에서만 보이는 씬 에셋 참조")]
    private UnityEngine.Object sceneAsset;

    [SerializeField, Tooltip("런타임에서 실제로 로드할 씬 이름(자동 캐싱)")]
    private string sceneName;

    public string SceneName => sceneName;

#if UNITY_EDITOR
    // 인스펙터에서 씬 드롭 시 자동으로 이름 캐싱
    public void OnValidate()
    {
        if (sceneAsset != null)
        {
            var path = AssetDatabase.GetAssetPath(sceneAsset);
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            sceneName = asset != null ? asset.name : string.Empty;
        }
        else
        {
            sceneName = string.Empty;
        }
    }
#endif
}


public class SceneDirector : MonoBehaviour
{
    [Header("기본 씬 참조(인스펙터에서 드래그)")]
    [SerializeField] private SceneReference titleScene;
    [SerializeField] private SceneReference gameScene;

    public bool IsLoading { get; private set; }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // SceneReference 내부 캐싱 갱신
        titleScene.OnValidate();
        gameScene.OnValidate();
    }
#endif

    public string TitleSceneName => titleScene.SceneName;
    public string GameSceneName  => gameScene.SceneName;

    public void LoadTitle() => LoadByName(TitleSceneName);
    public void LoadGame()  => LoadByName(GameSceneName);
    public void ReloadActive() => LoadByName(SceneManager.GetActiveScene().name);

    public void LoadByName(string sceneName)
    {
        if (IsLoading || string.IsNullOrEmpty(sceneName)) return;
        StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        IsLoading = true;
        var async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!async.isDone) yield return null;
        IsLoading = false;
    }

    
}
