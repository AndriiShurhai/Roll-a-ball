using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI countText;
    public GameObject gameOverScreen;
    public TextMeshProUGUI gameOverResultText;
    public TextMeshProUGUI countResultText;

    [Header("Movement")]
    public float speed = 600f;
    public float dashForce = 15f;
    public float dashCooldown = 2f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pickupSound;
    public AudioClip dashSound;
    public AudioClip loseSound;
    public AudioClip winSound;
    [Range(0f, 1f)] public float pickupVolume = 1f;
    [Range(0f, 1f)] public float dashVolume = 0.3f;
    [Range(0f, 1f)] public float loseVolume = 0.5f;
    [Range(0f, 1f)] public float winVolume = 0.5f;

    [Header("Effects")]
    public GameObject pickupParticlePrefab;

    private Rigidbody rb;
    private ParticleSystem dashParticles;
    private Vector2 moveInput;
    private int pickUpCount;
    private int pickUpAmountTarget;
    private float lastDashTime = -Mathf.Infinity;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        dashParticles = GetComponentInChildren<ParticleSystem>();
        pickUpAmountTarget = GameObject.FindGameObjectsWithTag("PickUp").Length;

        gameOverScreen.SetActive(false);
        UpdateCountText();
    }

    private void FixedUpdate()
    {
        rb.AddForce(new Vector3(moveInput.x, 0f, moveInput.y) * speed * Time.deltaTime);
    }

    private void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void OnFire()
    {
        if (Time.time < lastDashTime + dashCooldown)
        {
            Debug.Log("Dash on cooldown.");
            return;
        }

        Vector3 dashDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        if (dashDir == Vector3.zero) return;

        rb.AddForce(dashDir * dashForce, ForceMode.Impulse);
        lastDashTime = Time.time;

        PlaySound(dashSound, dashVolume);
        dashParticles?.Play();
        Debug.Log("Dashed!");
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Enemy")) return;

        Destroy(gameObject);
        PlaySound(loseSound, loseVolume);
        ShowGameOver(won: false);
        Debug.Log("Player lost.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PickUp"))
        {
            CollectPickup(other);
            pickUpCount++;
            UpdateCountText();
        }
        else if (other.CompareTag("Freeze PickUp"))
        {
            CollectPickup(other);
            StartCoroutine(FreezeEnemy(3f));
        }
    }

    private void CollectPickup(Collider pickup)
    {
        pickup.gameObject.SetActive(false);
        AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
        SpawnPickupParticle(pickup);
    }

    private void SpawnPickupParticle(Collider pickup)
    {
        if (pickupParticlePrefab == null) return;

        GameObject instance = Instantiate(pickupParticlePrefab, pickup.transform.position, Quaternion.identity);
        Renderer sourceRenderer = pickup.GetComponent<Renderer>();
        ParticleSystemRenderer particleRenderer = instance.GetComponent<ParticleSystemRenderer>();

        if (sourceRenderer != null && particleRenderer != null)
        {
            particleRenderer.material.mainTexture = sourceRenderer.material.mainTexture;
            particleRenderer.material.color = sourceRenderer.material.color;
        }
    }

    private void UpdateCountText()
    {
        countText.text = $"Count: {pickUpCount}";

        if (pickUpCount >= pickUpAmountTarget && pickUpAmountTarget > 0)
        {
            PlaySound(winSound, winVolume);
            Destroy(GameObject.FindGameObjectWithTag("Enemy"));
            ShowGameOver(won: true);
            Debug.Log("Player won — enemy destroyed.");
        }
    }

    private void ShowGameOver(bool won)
    {
        gameOverScreen.SetActive(true);
        gameOverResultText.text = won ? "You Win!" : "You Lose!";
        countResultText.text = $"You collected {pickUpCount}/{pickUpAmountTarget} pickups!";
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null) audioSource.PlayOneShot(clip, volume);
    }

    private IEnumerator FreezeEnemy(float duration)
    {
        GameObject enemy = GameObject.FindGameObjectWithTag("Enemy");
        NavMeshAgent agent = enemy?.GetComponentInParent<NavMeshAgent>();
        if (agent == null) yield break;

        float originalSpeed = agent.speed;
        agent.speed = 0f;
        yield return new WaitForSeconds(duration);
        if (agent != null) agent.speed = originalSpeed;
    }
}