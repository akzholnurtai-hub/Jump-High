using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Level select in the Main Menu. Makes one button per LevelData in GameConfig
// (add a level asset to the list -> a new button appears) and shows locked / unlocked
// and the best score from the save file.
public class LevelSelect : MonoBehaviour
{
    [SerializeField] Button template;          // hidden button, copied once per level
    [SerializeField] Transform row;            // parent of the buttons (has a layout group)
    [SerializeField] TMP_Text totalText;
    [SerializeField] Color openColor = Color.white;
    [SerializeField] Color lockedColor = Color.gray;

    class Entry
    {
        public LevelData level;
        public Button button;
        public Image image;
        public TMP_Text label;
    }

    readonly List<Entry> entries = new List<Entry>();

    void Awake()
    {
        Build();
        SaveSystem.Changed += Refresh;
    }

    void OnDestroy()
    {
        SaveSystem.Changed -= Refresh;
    }

    void OnEnable()
    {
        Refresh();
    }

    void Build()
    {
        if (template == null || row == null || GameConfig.Get() == null) return;
        template.gameObject.SetActive(false);

        foreach (LevelData lvl in GameConfig.Get().levels)
        {
            if (lvl == null) continue;
            Button b = Instantiate(template, row);
            b.gameObject.SetActive(true);
            b.name = "Btn_" + lvl.name;

            LevelData target = lvl;
            b.onClick.AddListener(() => Play(target));

            entries.Add(new Entry
            {
                level = lvl,
                button = b,
                image = b.GetComponent<Image>(),
                label = b.GetComponentInChildren<TMP_Text>(true)
            });
        }
    }

    public void Refresh()
    {
        foreach (Entry e in entries)
        {
            int n = e.level.levelNumber;
            bool open = SaveSystem.IsUnlocked(n);
            e.button.interactable = open;
            if (e.image != null) e.image.color = open ? openColor : lockedColor;
            if (e.label != null)
                e.label.text = open
                    ? n + "\n<size=55%>" + e.level.displayName + "\nBest " + SaveSystem.BestScore(n) + "</size>"
                    : n + "\n<size=55%>LOCKED</size>";
        }

        if (totalText != null)
            totalText.text = "Best score: " + SaveSystem.Data.bestScore +
                             "     Coins: " + SaveSystem.Data.totalCoins +
                             "     Deaths: " + SaveSystem.Data.deaths;
    }

    void Play(LevelData lvl)
    {
        if (!SaveSystem.IsUnlocked(lvl.levelNumber)) return;   // a locked button is not clickable anyway
        LevelRunner.Load(lvl);
    }
}
