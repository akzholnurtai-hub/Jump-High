using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Put on a Button, Toggle or Dropdown: it plays the UI click sound.
// A component (not a listener added from code) is copied by Instantiate, so the
// level buttons that LevelSelect makes from a template click too.
[DisallowMultipleComponent]
public class ClickSound : MonoBehaviour
{
    void Awake()
    {
        if (TryGetComponent(out Button b)) b.onClick.AddListener(Click);
        else if (TryGetComponent(out Toggle t)) t.onValueChanged.AddListener(_ => Click());
        else if (TryGetComponent(out TMP_Dropdown d)) d.onValueChanged.AddListener(_ => Click());
    }

    void Click() => AudioManager.PlayUI(Sfx.UIClick);
}
