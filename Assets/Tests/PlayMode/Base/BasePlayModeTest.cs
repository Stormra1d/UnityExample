using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public abstract class BasePlayModeTest
{
    private float _prevTimeScale;
    private Scene _baselineScene; 
    private readonly List<Scene> _scenesLoaded = new();
    private bool _baselineSceneUnloadedByTest;
    private const string SafetySceneName = "__Test_Safety__";

    protected virtual bool ClearAllPlayerPrefs => false;

    [UnitySetUp]
    public virtual IEnumerator BaseSetUp()
    {
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 1f;

        _baselineScene = SceneManager.CreateScene($"Test_{TestContext.CurrentContext.Test.ID}");
        SceneManager.SetActiveScene(_baselineScene);

        var safety = SceneManager.GetSceneByName(SafetySceneName);
        if (safety.IsValid() && safety.isLoaded)
        {
            var op = SceneManager.UnloadSceneAsync(safety);
            while (op != null && !op.isDone) yield return null;
        }

        EnsureCamera();
        EnsureEventSystem();

        SceneManager.sceneLoaded += OnSceneLoaded;
        yield return null;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        _scenesLoaded.Add(s);
        if (mode == LoadSceneMode.Single)
            _baselineSceneUnloadedByTest = true;
    }

    [UnityTearDown]
    public virtual IEnumerator BaseTearDown()
    {
        var safety = SceneManager.GetSceneByName(SafetySceneName);
        if (!safety.IsValid() || !safety.isLoaded)
            safety = SceneManager.CreateScene(SafetySceneName);
        SceneManager.SetActiveScene(safety);
        yield return null;

        foreach (var scene in ScenesToClean())
        {
            if (!scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null) continue;
                    try
                    {
                        mb.StopAllCoroutines();
                        if (mb.IsInvoking()) mb.CancelInvoke();
                    }
                    catch {}
                }
            }
        }
        yield return null;

        yield return ClearDontDestroyOnLoad();

        foreach (var s in _scenesLoaded.Where(s => s.isLoaded))
        {
            var op = SceneManager.UnloadSceneAsync(s);
            while (op != null && !op.isDone) yield return null;
        }
        _scenesLoaded.Clear();

        if (!_baselineSceneUnloadedByTest && _baselineScene.IsValid() && _baselineScene.isLoaded)
        {
            var op = SceneManager.UnloadSceneAsync(_baselineScene);
            while (op != null && !op.isDone) yield return null;
        }

        var unload = Resources.UnloadUnusedAssets();
        while (!unload.isDone) yield return null;
        System.GC.Collect();

        Time.timeScale = _prevTimeScale;
        if (ClearAllPlayerPrefs) PlayerPrefs.DeleteAll();

        yield return null;
    }

    private IEnumerable<Scene> ScenesToClean()
    {
        if (!_baselineSceneUnloadedByTest && _baselineScene.IsValid()) yield return _baselineScene;
        foreach (var s in _scenesLoaded) yield return s;
    }


    protected IEnumerator LoadTestScene(string sceneName,
                                        LoadSceneMode mode = LoadSceneMode.Additive,
                                        bool setActive = true)
    {
        var already = SceneManager.GetSceneByName(sceneName);
        if (!already.isLoaded)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, mode);
            while (!op.isDone) yield return null;
            already = SceneManager.GetSceneByName(sceneName);
        }
        else
        {
            if (!_scenesLoaded.Any(s => s.handle == already.handle))
                _scenesLoaded.Add(already);
        }

        if (setActive) SceneManager.SetActiveScene(already);
        yield return null;
    }

    protected static GameObject Spawn(string name = "Temp")
    {
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
        return go;
    }

    public static IEnumerator ClearDontDestroyOnLoad()
    {
        var probe = new GameObject("DDOL_Probe");
        Object.DontDestroyOnLoad(probe);
        var ddol = probe.scene;
        foreach (var go in ddol.GetRootGameObjects())
            if (go != probe) Object.Destroy(go);
        Object.Destroy(probe);
        yield return null;
    }

    private static void EnsureCamera()
    {
        if (Object.FindFirstObjectByType<Camera>() == null)
        {
            var cam = new GameObject("TestCamera");
            cam.tag = "MainCamera";
            cam.AddComponent<Camera>();
        }
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
}
