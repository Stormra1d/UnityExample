using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class BasePlayModeTest
{
    float _prevTimeScale;

    [SetUp]
    public virtual void BaseSetUp()
    {
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 1f;

        var s = SceneManager.CreateScene($"TestScene_{TestContext.CurrentContext.Test.ID}");
        SceneManager.SetActiveScene(s);
    }

    [TearDown]
    public virtual void BaseTearDown()
    {
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            mb.StopAllCoroutines();
            if (mb.IsInvoking()) mb.CancelInvoke();
        }

        var probe = new GameObject("DDOL_Probe");
        Object.DontDestroyOnLoad(probe);
        var ddol = probe.scene;
        foreach (var go in ddol.GetRootGameObjects())
        {
            if (go != probe) Object.DestroyImmediate(go);
        }
        Object.DestroyImmediate(probe);

        TryUnload("GameAITest");
        TryUnload("Game");
        TryUnload("MainMenu");
        TryUnload("TestGame");
        TryUnload("TestMainMenu");
        TryUnload("PerformanceTestScene");

        Time.timeScale = _prevTimeScale;
        PlayerPrefs.DeleteAll();
        Resources.UnloadUnusedAssets();
    }

    static void TryUnload(string name)
    {
        var scene = SceneManager.GetSceneByName(name);
        if (scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
    }
}
