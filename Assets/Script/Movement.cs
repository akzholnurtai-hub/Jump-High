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

    const float GroundCheckRadius = 0.2f;   // m, size of the feet sphere (shape of the model, not balance)

    // speed, jump and turn come from PlayerStats; time limit, coin goal and next level from LevelData
    static PlayerStats P => GameConfig.Get().player;

    Transform cam;
    bool grounded;

    public Animator gateAnimator;

    public TextMeshProUGUI coinText;
    public TextMeshProUGUI winText;
    public TextMeshProUGUI timerText;
    public AudioSource audioSource;
    public AudioClip coinSound;

    public float messageTime;               // s a short message stays on screen (UI, not balance)

    public AudioClip jumpSound;

    private Coroutine messageRoutine;
    private Rigidbody rb;
    private Shooter shooter;
    private Vector2 move;
    private bool fireRequested;
    private int coinsCollected = 0;
    private int totalCoins;
    private int coinsNeeded;
    private float timeLeft;
    private bool gameOver = false;

    public float TimeLeft => timeLeft;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        shooter = GetComponent<Shooter>();
    }

    void Start()
    {
        cam = Camera.main.transform;

        totalCoins = GameObject.FindGameObjectsWithTag("Coin").Length;

        LevelData level = LevelRunner.CurrentLevel;
        coinsNeeded = level != null && level.coinsRequired > 0 ? Mathf.Min(level.coinsRequired, totalCoins) : totalCoins;
        timeLeft = level != null ? level.timeLimit : Mathf.Infinity;
        Debug.Log("Coins in this level: " + totalCoins + ", needed: " + coinsNeeded);

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
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, P.jumpForce, rb.linearVelocity.z);

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

        grounded = Physics.CheckSphere(feet.position, GroundCheckRadius, groundMask);

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

            if ((fromFireButton || !pointerOnUI || Cursor.lockState == CursorLockMode.Locked) && shooter != null)
            {
                // shoot where the camera looks, and turn the player that way
                Vector3 aim = cam.forward; aim.y = 0f;
                if (aim.sqrMagnitude < 0.01f) aim = transform.forward;
                aim.Normalize();
                transform.rotation = Quaternion.LookRotation(aim);
                rb.rotation = transform.rotation;
                shooter.Shoot(aim);
            }
        }

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0)
        {
            timeLeft = 0;
            gameOver = true;
            SaveSystem.AddDeath();
            Restart();
        }

        timerText.text = float.IsInfinity(timeLeft) ? "" : "Time: " + Mathf.CeilToInt(timeLeft);

    }

    void FixedUpdate()
    {
        if (gameOver) return;

        Vector3 camF = cam.forward; camF.y = 0; camF.Normalize();
        Vector3 camR = cam.right;   camR.y = 0; camR.Normalize();
        Vector3 dir = camF * move.y + camR * move.x;

        rb.linearVelocity = new Vector3(dir.x * P.moveSpeed, rb.linearVelocity.y, dir.z * P.moveSpeed);

        if (dir.sqrMagnitude > 0.01f)
            rb.MoveRotation(Quaternion.Slerp(rb.rotation,
                Quaternion.LookRotation(dir), P.turnSpeed * Time.fixedDeltaTime));
    }

    void OnTriggerEnter(Collider other)
    {
        if (gameOver) return;

        if (other.CompareTag("Coin"))
        {
            Destroy(other.gameObject);

            coinsCollected++;
            ShowCoins();
            SaveSystem.AddCoin();
            if (LevelRunner.Current != null) LevelRunner.Current.AddScore(GameConfig.Get().items.coinScore);

            if (audioSource != null && coinSound != null)
                audioSource.PlayOneShot(coinSound);

            Debug.Log("Coins: " + coinsCollected + " / " + totalCoins);

            if (coinsCollected >= coinsNeeded && gateAnimator != null)
            {
                gateAnimator.SetBool("Open", true);
                Debug.Log("Gate Opened!");
            }
        }

        if (other.CompareTag("Exit"))
        {
            if (coinsCollected >= coinsNeeded)
            {
                Debug.Log("Level clear!");
                if (LevelRunner.Current != null) LevelRunner.Current.Complete();   // score, save, next level
                else SceneManager.LoadScene(GameConfig.Get().menuScene);
            }
            else
            {
                Debug.Log("Collect the coins first! " + coinsCollected + " / " + coinsNeeded);

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
        if (other.gameObject.CompareTag("Enemy") && !gameOver)
        {
            gameOver = true;
            Debug.Log("Game Over");
            SaveSystem.AddDeath();
            Restart();
        }
    }

    // LevelRunner calls this when the level is won: stop the player and show the result.
    public void Finish(string message)
    {
        EndGame(message);
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
        coinText.text = coinsCollected + " / " + coinsNeeded;
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
        SceneManager.LoadScene(GameConfig.Get().menuScene);
    }
}