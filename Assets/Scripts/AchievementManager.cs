using NUnit.Framework.Internal;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance;

    public AchievementUI achievementUI;

    private int totalKills = 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void EnemyKilled()
    {
        totalKills++;

        switch (totalKills)
        {
            case 10:
                achievementUI.ShowAchievement("10 Kills - Merciless");
                break;
            case 20:
                achievementUI.ShowAchievement("20 Kills - Relentless");
                break;
            case 50:
                achievementUI.ShowAchievement("50 Kills - Nuclear");
                break;
        }
    }
}
