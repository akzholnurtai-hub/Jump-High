using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.SceneManagement;

public class Movement : MonoBehaviour
{

    public Transform feet;
    public LayerMask groundMask;
    public Animator anim;
    public float speed = 6f;
    public float jumpForce = 7f;
    public float turnSpeed = 12f;

    Transform cam;
    bool grounded;

    public Animator gateAnimator;
    public float timeLimit = 30f;

    public TextMeshProUGUI coinText;
    public TextMeshProUGUI winText;
    public TextMeshProUGUI timerText;
    public AudioSource audioSource;
    public AudioClip coinSound;

    public float messageTime = 2f;

    public string nextScene = "Level2";
    public AudioClip jumpSound;

    private Coroutine messageRoutine;
    private Rigidbody rb;
    private Shooter shooter;
    private Vector2 move;
    private bool fireRequested;
    private int coinsCollected = 0;
    private int totalCoins;
    private float timeLeft;
    private bool gameOver = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        shooter = GetComponent<Shooter>();
    }

    void Start()
    {
        cam = Camera.main.transform;

        totalCoins = GameObject.FindGameObjectsWithTag("Coin").Length;
        Debug.Log("Coins in this level: " + totalCoins);

        timeLeft = timeLimit;
        winText.text = "";
        ShowCoins();
    }

    public void OnMove(InputValue value)
    {
        move = value.Get<Vector2>();
    }

    public void OnJump()
    {
        if (gameOver || Time.timeScale == 0f) return;

        if (grounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);

            if (audioSource != null && jumpSound != null)
                audioSource.PlayOneShot(jumpSound);
        }
    }

    public void OnFire()
    {
        fireRequested = true;
    }

    void Update()
    {

        grounded = Physics.CheckSphere(feet.position, 0.2f, groundMask);

        Vector3 flat = rb.linearVelocity;
        flat.y = 0;
        anim.SetFloat("Speed", flat.magnitude);
        anim.SetBool("Grounded", grounded);

        if (gameOver || Time.timeScale == 0f) return;

        if (fireRequested)
        {
            fireRequested = false;

            bool fromFireButton = Gamepad.current != null
                                  && Gamepad.current.buttonWest.isPressed;

            bool pointerOnUI = EventSystem.current != null
                               && EventSystem.current.IsPointerOverGameObject();

            if ((fromFireButton || !pointerOnUI) && shooter != null)
                shooter.Shoot(transform.forward);
        }

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0)
        {
            timeLeft = 0;
            Restart();
        }

        timerText.text = "Time: " + Mathf.CeilToInt(timeLeft);

    }

    void FixedUpdate()
    {
        if (gameOver) return;

        Vector3 camF = cam.forward; camF.y = 0; camF.Normalize();
        Vector3 camR = cam.right;   camR.y = 0; camR.Normalize();
        Vector3 dir = camF * move.y + camR * move.x;

        rb.linearVelocity = new Vector3(dir.x * speed, rb.linearVelocity.y, dir.z * speed);

        if (dir.sqrMagnitude > 0.01f)
            rb.MoveRotation(Quaternion.Slerp(rb.rotation,
                Quaternion.LookRotation(dir), turnSpeed * Time.fixedDeltaTime));
    }

    void OnTriggerEnter(Collider other)
    {
        if (gameOver) return;

        if (other.CompareTag("Coin"))
        {
            Destroy(other.gameObject);

            coinsCollected++;
            ShowCoins();

            if (audioSource != null && coinSound != null)
                audioSource.PlayOneShot(coinSound);

            Debug.Log("Coins: " + coinsCollected + " / " + totalCoins);

            if (coinsCollected >= totalCoins && gateAnimator != null)
            {
                gateAnimator.SetBool("Open", true);
                Debug.Log("Gate Opened!");
            }
        }

        if (other.CompareTag("Exit"))
        {
            if (coinsCollected >= totalCoins)
            {
                Debug.Log("Вы выиграли!");
                SceneManager.LoadScene(nextScene);
            }
            else
            {
                Debug.Log("Сначала соберите все монеты! " + coinsCollected + " / " + totalCoins);

                if (messageRoutine != null)
                    StopCoroutine(messageRoutine);

                messageRoutine = StartCoroutine(
                    ShowMessage("Collect all coins first!")
                );
            }
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Game Over");
            Restart();
        }
    }

    void EndGame(string message)
    {
        gameOver = true;
        winText.text = message;

        rb.linearVelocity = Vector3.zero;
    }

    void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void ShowCoins()
    {
        coinText.text = coinsCollected + " / " + totalCoins;
    }

    System.Collections.IEnumerator ShowMessage(string message)
    {
        winText.text = message;

        yield return new WaitForSeconds(messageTime);

        if (!gameOver)
            winText.text = "";
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}