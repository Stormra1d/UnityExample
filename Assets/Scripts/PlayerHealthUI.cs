using TMPro;
using UnityEngine;

public class PlayerHealthUI : MonoBehaviour
{
    public Health playerHealth;
    public TextMeshProUGUI hpText;

    public void Update()
    {
        if (playerHealth != null && hpText != null)
        {
            hpText.text = $"HP: {Mathf.CeilToInt(playerHealth.CurrentHealth)}";
        }
    }
}
