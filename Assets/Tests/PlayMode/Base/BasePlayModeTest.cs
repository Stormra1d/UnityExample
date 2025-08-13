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

    [UnitySetUp]
    public virtual void BaseSetUp()
    {
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 1f;

        if (!Object.FindFirstObjectByType<Camera>())
        { 
            var cam = new GameObject("TestCamera"); 
            cam.tag = "MainCamera"; cam.AddComponent<Camera>(); 
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
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            mb.StopAllCoroutines(); 
            if (mb.IsInvoking()) mb.CancelInvoke(); 
        }

        foreach (var go in _owned) if (go) Object.DestroyImmediate(go);
        _owned.Clear();

        Time.timeScale = _prevTimeScale;
        PlayerPrefs.DeleteAll();
    }
}
