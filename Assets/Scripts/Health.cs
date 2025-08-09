using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;
    public float CurrentHealth => currentHealth;

    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;

    private Coroutine flashCoroutine;

    public float knockbackDuration = 0.2f;
    private float knockbackTimer = 0f;
    private Vector3 currentKnockbackDisplacementPerSecond;

    private CharacterController characterController;
    private PlayerHealthUI playerHealthUI;

    public event System.Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (knockbackTimer > 0)
        {
            if (characterController != null)
            {
                characterController.Move(currentKnockbackDisplacementPerSecond * Time.deltaTime);
            } else
            {
                transform.position += currentKnockbackDisplacementPerSecond * Time.deltaTime;
            }

            currentKnockbackDisplacementPerSecond = Vector3.Lerp(currentKnockbackDisplacementPerSecond, Vector3.zero, Time.deltaTime * 5f);

            knockbackTimer -= Time.deltaTime;

            if (knockbackTimer <= 0)
            {
                knockbackTimer = 0;
                currentKnockbackDisplacementPerSecond = Vector3.zero;
            }
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashDamage());

        if (currentHealth <= 0)
        {
            OnDeath?.Invoke();

            if (CompareTag("Enemy"))
            {
                AchievementManager.Instance?.EnemyKilled();
            }

            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    public void Die()
    {
        OnDeath?.Invoke();

        if (CompareTag("Player"))
        {
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator FlashDamage()
    {

        if (bodyRenderer == null) yield break;

        Color original = bodyRenderer.material.color;
        bodyRenderer.material.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        bodyRenderer.material.color = original;
    }

    public void ApplyKnockback(Vector3 direction, float force)
    {
        currentKnockbackDisplacementPerSecond = direction.normalized * force;
        knockbackTimer = knockbackDuration;
    }
}