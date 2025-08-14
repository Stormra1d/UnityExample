using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public abstract class BasePlayModeTest
{
    float _prevTimeScale;
    readonly System.Collections.Generic.List<GameObject> _owned = new();
    private Scene? _originalScene;

    [UnitySetUp]
    public virtual void BaseSetUp()
    {
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 1f;

        _originalScene = SceneManager.GetActiveScene();

        if (!Object.FindFirstObjectByType<Camera>())
        { 
            var cam = new GameObject("TestCamera"); 
            cam.tag = "MainCamera"; 
            cam.AddComponent<Camera>(); 
            _owned.Add(cam); 
        }

        if (!Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>())
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            _owned.Add(es);
        }
    }

    [TearDown]
    public virtual void BaseTearDown()
    {
        var allObjects = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
        foreach (var mb in allObjects)
        {
            if (mb != null && mb.gameObject.scene.isLoaded)
            {
                try
                {
                    mb.StopAllCoroutines();
                    if (mb.IsInvoking()) mb.CancelInvoke();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to stop coroutines on {mb.name}: {e.Message}");
                }
            }
        }

        foreach (var go in _owned) if (go) Object.DestroyImmediate(go);
        _owned.Clear();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && _originalScene.HasValue && scene.handle != _originalScene.Value.handle)
            {
                SceneManager.UnloadSceneAsync(scene);
            }
        }

        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        Time.timeScale = _prevTimeScale;
        PlayerPrefs.DeleteAll();
    }
}
