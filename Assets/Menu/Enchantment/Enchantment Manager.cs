using App.BaseSystem.DataStores.ScriptableObjects.Status;
using System.Collections;
using TMPro;
using UnityEngine;

public class EnchantmentManager : MonoBehaviour
{
    [Header("選択枠")]
    // 選択した武器と素材を表示する枠。画面UIはInspectorから割り当てる。
    [SerializeField] private Transform selectedWeaponSlot;
    [SerializeField] private Transform[] selectedMaterialSlots = new Transform[3];

    [Header("エンチャント結果")]
    // 成功メッセージ表示先はInspectorから割り当てる。
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private float resultDisplaySeconds = 3f;

    // 現在選択中のUIと、素材取消時に所持数を戻すための参照。
    private GameObject selectedWeapon;
    private D_Eq_StatusData selectedWeaponData;
    private readonly GameObject[] selectedMaterials = new GameObject[3];
    private readonly D_It_StatusData[] selectedMaterialData = new D_It_StatusData[3];
    private readonly ItemUI[] sourceMaterialUIs = new ItemUI[3];
    private Dss_Sk_StatusDataStores skillStores;
    private Db_Sk_StatusDataBase enchantSkillDatabase;
    private Coroutine resultMessageCoroutine;

    /// <summary>
    /// エンチャント対象の武器を1つ選択し、選択枠へ表示する。
    /// 選択中の武器UIをクリックすると選択を取り消す。
    /// </summary>
    public void SelectWeapon(D_Eq_StatusData weaponData, GameObject sourceUI)
    {
        if (weaponData == null || sourceUI == null || selectedWeapon != null || selectedWeaponSlot == null)
            return;

        // 一覧UIの画面座標を引き継がず、選択枠を基準に複製する。
        selectedWeapon = Instantiate(sourceUI, selectedWeaponSlot, false);
        selectedWeaponData = weaponData;
        RectTransform selectedWeaponRect = selectedWeapon.GetComponent<RectTransform>();
        if (selectedWeaponRect != null)
        {
            // 生成元の右下アンカーを引き継がず、選択枠の中央を基準にする。
            selectedWeaponRect.anchorMin = new Vector2(0.5f, 0.5f);
            selectedWeaponRect.anchorMax = new Vector2(0.5f, 0.5f);
            selectedWeaponRect.pivot = new Vector2(0.5f, 0.5f);
        }

        ItemUI selectedItemUI = selectedWeapon.GetComponent<ItemUI>();
        if (selectedItemUI == null)
        {
            Destroy(selectedWeapon);
            selectedWeapon = null;
            selectedWeaponData = null;
            return;
        }

        selectedItemUI.Initialize(weaponData, null, null, null, CancelWeapon, this);
    }

    /// <summary>
    /// Enchant種類の素材を最大3つ選択し、所持数を1つ減らす。
    /// </summary>
    public void SelectMaterial(D_It_StatusData materialData, GameObject itemPrefab, ItemUI sourceItemUI)
    {
        if (materialData == null || itemPrefab == null || sourceItemUI == null || materialData.Number <= 0)
            return;

        for (int i = 0; i < selectedMaterials.Length; i++)
        {
            if (selectedMaterials[i] != null || selectedMaterialSlots == null || i >= selectedMaterialSlots.Length || selectedMaterialSlots[i] == null)
                continue;

            GameObject selectedObject = Instantiate(itemPrefab, selectedMaterialSlots[i]);
            ItemUI selectedItemUI = selectedObject.GetComponent<ItemUI>();
            if (selectedItemUI == null)
            {
                Destroy(selectedObject);
                return;
            }

            selectedMaterials[i] = selectedObject;
            selectedMaterialData[i] = materialData;
            sourceMaterialUIs[i] = sourceItemUI;

            materialData.Number--;
            sourceItemUI.SetNumber(materialData.Number);

            int slotIndex = i;
            selectedItemUI.Initialize(materialData, null, null, sourceItemUI,
                () => CancelMaterial(slotIndex), this);
            selectedItemUI.HideNumber();
            return;
        }
    }

    /// <summary>
    /// 選択中の武器UIを破棄して選択を解除する。
    /// </summary>
    public void CancelWeapon()
    {
        if (selectedWeapon == null)
            return;

        Destroy(selectedWeapon);
        selectedWeapon = null;
        selectedWeaponData = null;
    }

    /// <summary>
    /// 指定枠の素材選択を解除し、所持数と一覧表示を元に戻す。
    /// </summary>
    public void CancelMaterial(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= selectedMaterials.Length || selectedMaterials[slotIndex] == null)
            return;

        D_It_StatusData materialData = selectedMaterialData[slotIndex];
        if (materialData != null)
        {
            materialData.Number++;
            if (sourceMaterialUIs[slotIndex] != null)
            {
                sourceMaterialUIs[slotIndex].SetNumber(materialData.Number);
            }
        }

        Destroy(selectedMaterials[slotIndex]);
        selectedMaterials[slotIndex] = null;
        selectedMaterialData[slotIndex] = null;
        sourceMaterialUIs[slotIndex] = null;
    }

    /// <summary>
    /// 画面切り替えや一覧更新の前に、武器と素材の選択をすべて解除する。
    /// </summary>
    public void ClearSelection()
    {
        CancelWeapon();
        for (int i = 0; i < selectedMaterials.Length; i++)
        {
            CancelMaterial(i);
        }
    }

    /// <summary>選択済みの武器と3つの素材からスキルを抽選して付与する。</summary>
    public void Enchant()
    {
        // 武器のSkillListに登録済みのエンチャントが3つなら、新しい素材を消費せず終了する。
        if (selectedWeaponData != null && selectedWeaponData.SkillList != null &&
            selectedWeaponData.SkillList.FindAll(skill => skill != null).Count >= 3)
        {
            ShowResultMessage("エンチャントは3つまでです。");
            return;
        }

        if (selectedWeaponData == null || selectedMaterials.Length != 3 ||
            selectedMaterialData.Length != 3 || System.Array.Exists(selectedMaterialData, data => data == null))
            return;

        if (enchantSkillDatabase == null)
        {
            skillStores = FindAnyObjectByType<Dss_Sk_StatusDataStores>();
            enchantSkillDatabase = skillStores != null ? skillStores.FindDatabaseWithName("Enchant_Sk_List") : null;
        }

        if (enchantSkillDatabase == null || enchantSkillDatabase.ItemList == null)
        {
            Debug.LogWarning("Enchant_Sk_Listを取得できません。");
            return;
        }

        // レア度とバフ種類を別々に抽選し、各素材の寄与を33ポイントずつ加算する。
        Rarity selectedRarity = (Rarity)DrawWeightedValue(
            (int)Rarity.Common,
            material => (int)material.SeeRarity);
        D_Sk_StatusData.Buff_DeBuff_Kinds selectedBuffKind =
            (D_Sk_StatusData.Buff_DeBuff_Kinds)DrawWeightedValue(
                (int)D_Sk_StatusData.Buff_DeBuff_Kinds.Attack,
                material => (int)material.SeeBuff_DeBuff_Kinds);

        System.Collections.Generic.List<D_Sk_StatusData> candidates = enchantSkillDatabase.ItemList.FindAll(
            skill => skill != null && skill.SeeRarity == selectedRarity &&
                     skill.SeeBuff_DeBuff_Kinds == selectedBuffKind);
        if (candidates.Count == 0)
        {
            Debug.LogWarning($"レア度{selectedRarity}、バフ{selectedBuffKind}のスキルデータがありません。");
            return;
        }

        D_Sk_StatusData skillData = candidates[Random.Range(0, candidates.Count)];
        selectedWeaponData.SaveOriginalStats();
        if (selectedWeaponData.SkillList == null)
            selectedWeaponData.SkillList = new System.Collections.Generic.List<D_Sk_StatusData>();
        selectedWeaponData.SkillList.Add(skillData);
        selectedWeaponData.RecalculateEnchantedStats();

        ShowResultMessage($"{skillData.Name}を付与しました。");
        ConsumeSelection();
    }

    /// <summary>CommonまたはAttackの1ポイントを含む100ポイントから値を抽選する。</summary>
    private int DrawWeightedValue(int fallbackValue, System.Func<D_It_StatusData, int> valueSelector)
    {
        int roll = Random.Range(0, 100);
        int cumulativeWeight = 1;
        if (roll < cumulativeWeight)
            return fallbackValue;

        for (int i = 0; i < selectedMaterialData.Length; i++)
        {
            int materialValue = valueSelector(selectedMaterialData[i]);
            cumulativeWeight += 33;
            if (roll < cumulativeWeight)
                return materialValue;
        }

        return fallbackValue;
    }

    /// <summary>エンチャント完了メッセージを一定時間だけ表示する。</summary>
    private void ShowResultMessage(string message)
    {
        if (resultText == null) return;

        if (resultMessageCoroutine != null)
            StopCoroutine(resultMessageCoroutine);
        resultMessageCoroutine = StartCoroutine(ShowResultMessageCoroutine(message));
    }

    private IEnumerator ShowResultMessageCoroutine(string message)
    {
        resultText.text = message;
        resultText.gameObject.SetActive(true);
        yield return new WaitForSeconds(resultDisplaySeconds);
        resultText.text = string.Empty;
        resultText.gameObject.SetActive(false);
        resultMessageCoroutine = null;
    }

    /// <summary>成功時に選択UIを片付け、選択済み素材を消費済みのまま確定する。</summary>
    private void ConsumeSelection()
    {
        if (selectedWeapon != null) Destroy(selectedWeapon);
        selectedWeapon = null;
        selectedWeaponData = null;

        for (int i = 0; i < selectedMaterials.Length; i++)
        {
            if (selectedMaterials[i] != null) Destroy(selectedMaterials[i]);
            selectedMaterials[i] = null;
            selectedMaterialData[i] = null;
            sourceMaterialUIs[i] = null;
        }
    }
}
