using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ResultController : MonoBehaviour {
    public TMP_Text txtTitle;
    public TMP_Text txtDetails;
    public Button btnBack;

    void Start() {
        var s = StageConfigLoader.Load();
        int dmg  = PlayerPrefs.GetInt("dmgMargin", 0);
        int hp   = PlayerPrefs.GetInt("hpMargin", 0);
        int cost = PlayerPrefs.GetInt("totalCost", 0);

        bool win = (dmg >= 0 && hp >= 0);
        if (txtTitle) txtTitle.text = win ? "Victory" : "Defeat";

        if (txtDetails) {
            txtDetails.text =
                $"Total Cost: {cost} / {s.B}\n" +
                $"Damage Margin: {dmg}\n" +
                $"HP Margin: {hp}\n" +
                $"Turns Limit (T_max): {s.T_max}";
        }

        if (btnBack) btnBack.onClick.AddListener(() =>
            UnityEngine.SceneManagement.SceneManager.LoadScene("Loadout"));
    }
}
