using System.Collections;
using TMPro;
using UnityEngine;

public class AchievementUI : MonoBehaviour
{
    public TextMeshProUGUI achievementText;
    public float displayDuration = 5f;

    private Coroutine currentRoutine;

    public void ShowAchievement(string message)
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(DisplayMessage(message));
    }

    private IEnumerator DisplayMessage(string message)
    {
        achievementText.text = message;
        achievementText.alpha = 1f;

        yield return new WaitForSeconds(displayDuration);

        achievementText.alpha = 0f;
        achievementText.text = string.Empty;
    }
}
