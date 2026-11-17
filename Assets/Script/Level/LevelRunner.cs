using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// The same script in every level scene. It does not know any numbers itself:
// it takes a LevelData asset and builds the run from it -
// spawns the enemies, gives the timer and goal to the player (Movement reads them),
// adds up the score and, when the level is won, saves and loads the next level.
[DefaultExecutionOrder(-100)]          // Awake before Movement / Spawner / Health read the level
public class LevelRunner : MonoBehaviour
{
    [SerializeField] LevelData level;  // empty = found by the scene name in GameConfig

    static LevelData requested;        // set by Load(), so one scene could serve several LevelData

    public static LevelRunner Current { get; private set; }
    public static LevelData CurrentLevel => Current != null ? Current.level : null;

    public LevelData Level => level;
    public int Score { get; private set; }

    Movement player;
    HUD hud;
    bool finished;

    // Start a level from the menu / level select / previous level.
    public static void Load(LevelData lvl)
    {
        requested = lvl;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(lvl.sceneName);
    }

    void Awake()
    {
        Current = this;
        string scene = SceneManager.GetActiveScene().name;

        if (requested != null && requested.sceneName == scene) level = requested;
        if (level == null && GameConfig.Get() != null) level = GameConfig.Get().FindByScene(scene);
        if (level == null) Debug.LogError("No LevelData for scene " + scene + ". Add it to GameConfig > Levels.");

        SaveSystem.StartLevel(level);
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    IEnumerator Start()
    {
        player = FindAnyObjectByType<Movement>();
        hud = FindAnyObjectByType<HUD>();
        if (level == null) yield break;

        SpawnEnemies();

        yield return null;               // one frame: Movement.Start clears the message text first
        if (hud != null && !finished)
            hud.ShowMessage(level.levelNumber + ". " + level.displayName + "   " + GoalText());
    }

    void SpawnEnemies()
    {
        foreach (EnemySpawn s in level.enemies)
        {
            if (s == null || s.type == null || s.type.prefab == null)
            {
                Debug.LogWarning(level.name + ": an enemy entry has no type or the type has no prefab.");
                continue;
            }
            GameObject go = Instantiate(s.type.prefab, s.position, Quaternion.Euler(0f, s.yaw, 0f));
            go.name = s.type.displayName;
            EnemyBase enemy = go.GetComponent<EnemyBase>();
            if (enemy != null) enemy.Init(s.type, s.route);
        }
    }

    string GoalText()
    {
        return level.goal == LevelGoal.DefeatBoss ? "Defeat the boss!" : "Collect the coins!";
    }

    public void AddScore(int points)
    {
        if (!finished) Score += points;
    }

    // Exit door (Movement) or boss death (Boss) calls this.
    public void Complete()
    {
        if (finished || level == null) return;
        finished = true;

        float timeLeft = player != null ? player.TimeLeft : 0f;
        Score += level.clearBonus + Mathf.CeilToInt(timeLeft) * level.timeBonusPerSecond;

        bool newBest = SaveSystem.CompleteLevel(level, Score, level.timeLimit - timeLeft);

        string text = "LEVEL CLEAR!\nScore " + Score + (newBest ? "   NEW BEST!" : "   Best " + SaveSystem.BestScore(level.levelNumber));
        if (player != null) player.Finish(text);

        StartCoroutine(LoadNext());
    }

    IEnumerator LoadNext()
    {
        yield return new WaitForSecondsRealtime(GameConfig.Get().levelEndDelay);
        if (level.nextLevel != null) Load(level.nextLevel);
        else SceneManager.LoadScene(GameConfig.Get().winScene);
    }

    // Scene view: show where the data will spawn enemies (they are not in the scene any more).
    void OnDrawGizmos()
    {
        if (level == null) return;
        const float Marker = 0.6f;       // m, size of the gizmo only
        foreach (EnemySpawn s in level.enemies)
        {
            if (s == null) continue;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(s.position, Marker);
            Gizmos.DrawRay(s.position, Quaternion.Euler(0f, s.yaw, 0f) * Vector3.forward * Marker * 2f);
            Gizmos.color = Color.white;
            for (int k = 0; k < s.route.Count; k++)
                Gizmos.DrawLine(s.route[k], s.route[(k + 1) % s.route.Count]);
        }
    }
}
