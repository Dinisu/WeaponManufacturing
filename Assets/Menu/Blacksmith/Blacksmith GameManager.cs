using App.BaseSystem.DataStores.ScriptableObjects.Status;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static MenuManager;

public class BlacksmithGameManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject SystemBanner;
    [SerializeField] private GameObject miniGameUI;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI finishedProductText;

    [Header("各システム")]
    [SerializeField] private BlacksmithManager blacksmithManager;
    [SerializeField] private GaugeController gaugeController;
    [SerializeField] private SuccessAreaController successAreaController;
    [SerializeField] private JudgeController judgeController;

    [Header("制限時間")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private float forgingTime = 10f;

    [Header("判定後待機時間")]
    [SerializeField] private float resultDisplayTime = 0.3f;

    private bool canJudge;

    private float timer;
    private bool isPlaying;

    private D_It_StatusData material1;
    private D_It_StatusData material2;

    private int successCount;
    private int greatCount;

    private Db_Eq_StatusDataBase db_PossessedEq;
    private Dss_Eq_StatusDataStores dss_Eq_StatusDataStores;

    private void Awake()
    {
        dss_Eq_StatusDataStores = FindAnyObjectByType<Dss_Eq_StatusDataStores>();

        // FindDatabaseWithName を使用して Player_Item データベースを取得
        db_PossessedEq = dss_Eq_StatusDataStores.FindDatabaseWithName("Possessed Equipment");
    }

    /// <summary>
    /// 鍛冶開始
    /// </summary>
    public void Blacksmith(D_It_StatusData material1, D_It_StatusData material2)
    {
        this.material1 = material1;
        this.material2 = material2;

        successCount = 0;
        greatCount = 0;

        timer = forgingTime;

        isPlaying = true;
        canJudge = true;

        gaugeController.ResetPointer();

        resultText.text = "";
        finishedProductText.text = "";

        miniGameUI.SetActive(true);
        SystemBanner.SetActive(false);

        successAreaController.CreateArea();
        gaugeController.StartMove();
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        timer -= Time.deltaTime;

        timeText.text = $"残り時間 : {timer:F1}";

        if (timer <= 0)
        {
            timer = 0;
            StartCoroutine(FinishForging());
            return;
        }

        if (canJudge &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartCoroutine(JudgeCoroutine());
        }
    }

    /// <summary>
    /// 判定
    /// </summary>
    private IEnumerator JudgeCoroutine()
    {
        canJudge = false;

        gaugeController.Stop();

        JudgeResult result = judgeController.Judge();

        switch (result)
        {
            case JudgeResult.Success:
                successCount++;
                resultText.color = Color.yellow;
                resultText.text = "成功！";
                break;

            case JudgeResult.Great:
                greatCount++;
                resultText.color = Color.red;
                resultText.text = "大成功！";
                break;

            default:
                resultText.color = Color.white;
                resultText.text = "失敗";
                break;
        }

        yield return new WaitForSeconds(resultDisplayTime);

        resultText.color = Color.black;
        resultText.text = "";

        successAreaController.CreateArea();

        gaugeController.StartMove();

        canJudge = true;
    }

    /// <summary>
    /// 鍛冶終了
    /// </summary>
    private IEnumerator FinishForging()
    {
        isPlaying = false;
        canJudge = false;

        D_Eq_StatusData weapon = CreateWeapon();

        finishedProductText.text = weapon.Name;

        miniGameUI.SetActive(false);

        yield return new WaitForSeconds(3f);

        //選択中の素材を削除する
        StartCoroutine(blacksmithManager.AfterForging());

        SystemBanner.SetActive(true);
        finishedProductText.text = "";
    }

    /// <summary>
    /// スコアに応じた武器の称号
    /// </summary>
    private string GetWeaponPrefix(int score)
    {
        if (score >= 45)
            return "伝説の";

        if (score >= 35)
            return "名工の";

        if (score >= 25)
            return "熟練の";

        if (score >= 20)
            return "良質な";

        if (score >= 10)
            return "普通の";

        return "粗悪な";
    }

    /// <summary>
    /// 武器生成
    /// </summary>
    private D_Eq_StatusData CreateWeapon()
    {
        RemoveNullEquipment();

        D_Eq_StatusData weapon = ScriptableObject.CreateInstance<D_Eq_StatusData>();

        int score = successCount * 2 + greatCount * 3;

        string prefix = GetWeaponPrefix(score);

        weapon.Name = $"{prefix}剣";

        //ID設定
        weapon.Id = GetNewEquipmentID();

        // ステータス
        SetStatus(weapon, material1, material2, score);

        // レア度
        weapon.SeeRarity = CalculateRarity(
            material1.SeeRarity,
            material2.SeeRarity,
            score);

        // 属性
        weapon.SeeAttribute =
            (D_Eq_StatusData.Attribute)(Random.value < 0.5f ?
            material1.SeeAttribute :
            material2.SeeAttribute);

        // アイコン
        weapon.DataIcon = GetRandomIcon(weapon.SeeRarity);

        db_PossessedEq.ItemList.Add(weapon);

        Debug.Log($"武器生成：{weapon.Name}");

        Debug.Log($"成功:{successCount}");
        Debug.Log($"大成功:{greatCount}");
        Debug.Log($"スコア:{score}");

        return weapon;
    }
    /// <summary>
    /// ItemList内のnullを削除
    /// </summary>
    private void RemoveNullEquipment()
    {
        db_PossessedEq.ItemList.RemoveAll(item => item == null);
    }

    /// <summary>
    /// 使用されていないIDを取得
    /// 将来的に独立させいろんなどこから参照出来るようにするかも
    /// </summary>
    private int GetNewEquipmentID()
    {
        int id = 0;

        while (true)
        {
            bool exists = false;

            foreach (var equipment in db_PossessedEq.ItemList)
            {
                if (equipment.Id == id)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
                return id;

            id++;
        }
    }

    /// <summary>
    /// 武器ステータスの設定
    /// </summary>
    private void SetStatus(D_Eq_StatusData weapon,D_It_StatusData material1, D_It_StatusData material2,int score)
    {
        //スコア10→10％上昇
        float rate = 1f + score * 0.01f;

        weapon.MaxHp =
            Mathf.RoundToInt((material1.Equipment.MaxHpup + material2.Equipment.MaxHpup) * rate);

        weapon.MaxMp =
            Mathf.RoundToInt((material1.Equipment.MaxMpup + material2.Equipment.MaxMpup) * rate);

        weapon.Attack =
            Mathf.RoundToInt((material1.Equipment.Attackup + material2.Equipment.Attackup) * rate);

        weapon.Magic =
            Mathf.RoundToInt((material1.Equipment.Magicup + material2.Equipment.Magicup) * rate);

        weapon.Defense =
            Mathf.RoundToInt((material1.Equipment.Defenseup + material2.Equipment.Defenseup) * rate);

        weapon.MagicDefense =
            Mathf.RoundToInt((material1.Equipment.MagicDefenseup + material2.Equipment.MagicDefenseup) * rate);

        weapon.Speed =
            Mathf.RoundToInt((material1.Equipment.Speedup + material2.Equipment.Speedup) * rate);

        weapon.CriticalRate =
            Mathf.RoundToInt((material1.Equipment.Criticalup + material2.Equipment.Criticalup) * rate);
    }

    /// <summary>
    /// レア度の設定
    /// </summary>
    private Rarity CalculateRarity(Rarity r1,Rarity r2,int score)
    {
        int a = (int)r1;
        int b = (int)r2;

        if (a == b)
            return (Rarity)a;

        int min = Mathf.Min(a, b);
        int max = Mathf.Max(a, b);

        float t = Mathf.Clamp01(score / 30f);

        int rarity =
            Mathf.RoundToInt(Mathf.Lerp(min, max, t));

        return (Rarity)rarity;
    }

    private Sprite GetRandomIcon(Rarity rarity)
    {
        string path = $"Im_Equipment/{rarity}";

        Sprite[] icons =
            Resources.LoadAll<Sprite>(path);

        if (icons.Length == 0)
        {
            Debug.LogWarning(path + " に画像がありません。");
            return null;
        }

        return icons[Random.Range(0, icons.Length)];
    }
}
