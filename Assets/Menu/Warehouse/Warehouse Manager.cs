using App.BaseSystem.DataStores.ScriptableObjects.Status;
using TMPro;
using UnityEngine;

public class WarehouseManager : MonoBehaviour
{
    // 装備スロットの親Transform（UI配置先）
    [SerializeField] private Transform EquippedWeapon1;
    [SerializeField] private Transform EquippedWeapon2;
    [SerializeField] private Transform EquippedWeapon3;
    [SerializeField] private Transform EquippedWeapon4;

    // 各スロットの装備情報表示用テキスト
    [SerializeField] private TextMeshProUGUI Equipped1Text;
    [SerializeField] private TextMeshProUGUI Equipped2Text;
    [SerializeField] private TextMeshProUGUI Equipped3Text;
    [SerializeField] private TextMeshProUGUI Equipped4Text;

    private Dss_Eq_StatusDataStores dss_Eq_StatusDataStores;
    private Db_Eq_StatusDataBase db_EquippedWeapon;

    // スロットに配置した装備UIを保持する配列
    private GameObject[] equippedWeaponUIs;

    private void Awake()
    {
        // DB ストアを検索して、各データベースを取得
        dss_Eq_StatusDataStores = FindAnyObjectByType<Dss_Eq_StatusDataStores>();
        db_EquippedWeapon = dss_Eq_StatusDataStores?.FindDatabaseWithName("Equipped weapon");

        // 装備スロットは4つ分
        equippedWeaponUIs = new GameObject[4];
    }

    /// <summary>
    /// 倉庫内のアイテムを選択したときの処理
    /// </summary>
    public void SelectWarehouseItem(D_It_StatusData itemData, GameObject uiObj, ItemUI itemUI)
    {
        if (itemData != null)
        {
            Debug.Log($"Warehouse select: {itemData.Name}");
            // TODO: 倉庫用の選択処理をここに実装
        }
    }

    /// <summary>
    /// 倉庫内の装備を選択し、装備スロットへ追加する処理
    /// </summary>
    public void SelectWarehouseEquipment(D_Eq_StatusData itemData, GameObject uiObj, ItemUI itemUI)
    {
        if (itemData == null)
            return;

        if (db_EquippedWeapon == null)
        {
            Debug.LogWarning("WarehouseManager: Equipped weapon database not found.");
            return;
        }

        // すでに装備済みなら追加しない
        if (db_EquippedWeapon.ItemList.Contains(itemData))
        {
            Debug.Log($"Already equipped: {itemData.Name}");
            return;
        }

        // 装備スロットは最大4つまで
        if (db_EquippedWeapon.ItemList.Count >= 4)
        {
            Debug.Log("Equipped weapon slots are full.");
            return;
        }

        // DB に装備を追加
        db_EquippedWeapon.ItemList.Add(itemData);

        int slotIndex = CreateEquippedSlotUI(itemData, uiObj);
        if (slotIndex >= 0)
        {
            Debug.Log($"Equipped {itemData.Name} in slot {slotIndex + 1}.");
        }
    }

    /// <summary>
    /// 既に装備中の装備を装備スロットに表示する
    /// </summary>
    public int CreateEquippedSlotUI(D_Eq_StatusData itemData, GameObject uiObj)
    {
        if (itemData == null || uiObj == null)
            return -1;

        int slotIndex = GetNextEmptySlotIndex();
        if (slotIndex < 0)
        {
            Debug.LogWarning("No empty equip slot available.");
            return -1;
        }

        Transform slotParent = GetSlotTransform(slotIndex) ?? EquippedWeapon1;
        if (slotParent == null)
        {
            Debug.LogWarning($"Equip slot {slotIndex + 1} is not assigned.");
            return -1;
        }

        GameObject equippedUI = Instantiate(uiObj, slotParent);
        equippedWeaponUIs[slotIndex] = equippedUI;
        equippedUI.transform.localPosition = Vector3.zero;
        equippedUI.transform.localScale = Vector3.one;

        ItemUI equippedItemUI = equippedUI.GetComponent<ItemUI>();
        if (equippedItemUI != null)
        {
            equippedItemUI.Initialize(itemData, null, this, null, () => CancelEquipped(itemData, slotIndex));
            equippedItemUI.HideNumber();
        }

        SetEquippedText(slotIndex, itemData);
        return slotIndex;
    }

    /// <summary>
    /// 空き装備スロットのインデックスを取得
    /// </summary>
    private int GetNextEmptySlotIndex()
    {
        for (int i = 0; i < equippedWeaponUIs.Length; i++)
        {
            if (equippedWeaponUIs[i] == null)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// スロット番号から対応する親Transformを取得
    /// </summary>
    private Transform GetSlotTransform(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return EquippedWeapon1;
            case 1: return EquippedWeapon2;
            case 2: return EquippedWeapon3;
            case 3: return EquippedWeapon4;
            default: return null;
        }
    }

    /// <summary>
    /// スロット番号から対応するテキスト表示を取得
    /// </summary>
    private TextMeshProUGUI GetSlotText(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return Equipped1Text;
            case 1: return Equipped2Text;
            case 2: return Equipped3Text;
            case 3: return Equipped4Text;
            default: return null;
        }
    }

    /// <summary>
    /// 装備スロットの表示テキストを更新
    /// </summary>
    private void SetEquippedText(int slotIndex, D_Eq_StatusData itemData)
    {
        TextMeshProUGUI slotText = GetSlotText(slotIndex);
        if (slotText == null)
            return;

        slotText.text = $"{itemData.Name}\nATK: {itemData.Attack}  DEF: {itemData.Defense}  SPD: {itemData.Speed}";
    }

    /// <summary>
    /// 装備スロットの表示テキストをクリア
    /// </summary>
    private void ClearEquippedText(int slotIndex)
    {
        TextMeshProUGUI slotText = GetSlotText(slotIndex);
        if (slotText != null)
            slotText.text = string.Empty;
    }

    /// <summary>
    /// 装備スロットの UI を全てクリアする
    /// </summary>
    public void ClearEquippedSlots()
    {
        for (int i = 0; i < equippedWeaponUIs.Length; i++)
        {
            if (equippedWeaponUIs[i] != null)
            {
                Destroy(equippedWeaponUIs[i]);
                equippedWeaponUIs[i] = null;
            }
            ClearEquippedText(i);
        }
    }

    /// <summary>
    /// 装備の解除処理
    /// </summary>
    private void CancelEquipped(D_Eq_StatusData itemData, int slotIndex)
    {
        if (db_EquippedWeapon != null)
        {
            db_EquippedWeapon.ItemList.Remove(itemData);
        }

        if (slotIndex >= 0 && slotIndex < equippedWeaponUIs.Length)
        {
            if (equippedWeaponUIs[slotIndex] != null)
            {
                Destroy(equippedWeaponUIs[slotIndex]);
                equippedWeaponUIs[slotIndex] = null;
            }
        }

        ClearEquippedText(slotIndex);
        Debug.Log($"Unequipped {itemData.Name} from slot {slotIndex + 1}.");
    }
}
