using App.BaseSystem.DataStores.ScriptableObjects.Status;
using UnityEngine;

public class EnchantmentManager : MonoBehaviour
{
    [Header("選択枠")]
    // 選択した武器と素材を表示する枠。画面UIはInspectorから割り当てる。
    [SerializeField] private Transform selectedWeaponSlot;
    [SerializeField] private Transform[] selectedMaterialSlots = new Transform[3];

    // 現在選択中のUIと、素材取消時に所持数を戻すための参照。
    private GameObject selectedWeapon;
    private readonly GameObject[] selectedMaterials = new GameObject[3];
    private readonly D_It_StatusData[] selectedMaterialData = new D_It_StatusData[3];
    private readonly ItemUI[] sourceMaterialUIs = new ItemUI[3];

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
}
