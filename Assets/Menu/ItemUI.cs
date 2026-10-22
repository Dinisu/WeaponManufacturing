using App.BaseSystem.DataStores.ScriptableObjects.Status;
using NUnit.Framework.Interfaces;
using System.Runtime.InteropServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class ItemUI : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private Image rarityColor;
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private Button button;
    private ItemUI sourceItemUI;

    [Header("素材データ")]
    public D_It_StatusData ItemData;

    private BlacksmithManager blacksmithManager;
    private WarehouseManager warehouseManager;
    // Enchantment画面で選択操作を受け渡すマネージャー。
    private EnchantmentManager enchantmentManager;


    /// <summary>
    /// アイテムUIの初期化処理
    /// エンチャント画面では、選択操作の受け渡し先も設定する。
    /// </summary>
    public void Initialize(D_It_StatusData itemData, BlacksmithManager manager, WarehouseManager warehouse, ItemUI sourceItemUI, UnityAction action, EnchantmentManager enchantment = null)
    {
        blacksmithManager = manager;                  //鍛冶マネージャーを取得
        warehouseManager = warehouse;
        enchantmentManager = enchantment;
        this.sourceItemUI = sourceItemUI;

        ItemData = itemData;                          //鍛造などで参照出来るように。

        itemImage.type = Image.Type.Simple;
        itemImage.preserveAspect = true;
        itemImage.sprite = itemData.DataIcon;         // UIに画面を入れる
        SetRarityColor(itemData.SeeRarity);           // UIのレア度ごとの色設定

        //このボタンの処理設定
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    /// <summary>
    /// 装備UIの初期化処理
    /// エンチャント画面では、選択操作の受け渡し先も設定する。
    /// </summary>
    public void Initialize(D_Eq_StatusData itemData, BlacksmithManager manager, WarehouseManager warehouse, ItemUI sourceItemUI, UnityAction action, EnchantmentManager enchantment = null)
    {
        blacksmithManager = manager;                  //鍛冶マネージャーを取得
        warehouseManager = warehouse;
        enchantmentManager = enchantment;
        this.sourceItemUI = sourceItemUI;

        itemImage.type = Image.Type.Simple;
        itemImage.preserveAspect = true;
        itemImage.sprite = itemData.DataIcon;         // UIに画面を入れる
        SetRarityColor(itemData.SeeRarity);           // UIのレア度ごとの色設定

        //このボタンの処理設定
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    /// <summary>
    /// 所持数表示
    /// </summary>
    public void SetNumber(int number)
    {
        numberText.text = number.ToString();
    }

    /// <summary>
    /// 所持数を表示しない
    /// </summary>
    public void HideNumber()
    {
        numberText.text = "";
    }

    /// <summary>
    /// 鍛冶に使用するアイテム選択処理の呼び出し
    /// </summary>
    public void SelectItem(D_It_StatusData itemData, GameObject uiObj)
    {
        if (itemData.Number > 0)
        {
            // エンチャント用UIなら、素材選択を優先して処理する。
            if (enchantmentManager != null)
            {
                enchantmentManager.SelectMaterial(itemData, uiObj, this);
                return;
            }

            if (blacksmithManager != null)
            {
                blacksmithManager.SelectMaterial(itemData, uiObj, this);
            }
        }

        Debug.Log(itemData.Name);
    }

    public void SelectWarehouseItem(D_It_StatusData itemData, GameObject uiObj)
    {
        if (itemData.Number > 0)
        {
            if (warehouseManager != null)
            {
                warehouseManager.SelectWarehouseItem(itemData, uiObj, this);
                return;
            }
        }

        Debug.Log($"Warehouse select: {itemData.Name}");
    }

    /// <summary>
    /// 素材選択のキャンセル処理の呼び出し
    /// </summary>
    public void MateriallItem(D_It_StatusData itemData, GameObject Material)
    {
        if (blacksmithManager != null)
        {
            blacksmithManager.CancelMaterial(itemData, Material, sourceItemUI);
        }
    }
    public void SelectWarehouseItem(D_Eq_StatusData itemData, GameObject uiObj)
    {
        if (itemData != null)
        {
            if (warehouseManager != null)
            {
                warehouseManager.SelectWarehouseEquipment(itemData, uiObj, this);
                return;
            }

            Debug.Log($"Warehouse select equipment: {itemData.Name}");
        }
    }

    public void SelectItem(D_Eq_StatusData itemData)
    {
        // エンチャント画面でのみ、装備を対象武器として選択する。
        if (enchantmentManager != null)
        {
            enchantmentManager.SelectWeapon(itemData, gameObject);
            return;
        }

        Debug.Log(itemData.Name);
    }

    /// <summary>
    /// アイテム画像の表示場所
    /// </summary>
    /*public void SetIcon(Sprite sprite)
    {
        itemImage.sprite = sprite;
    }

    /// <summary>
    /// アイテムの所持数表示
    /// </summary>
    public void ItemNumber(int number) 
    { 
        numberText.text = number.ToString();
    }

    /// <summary>
    /// レア度ごとの色設定（呼び出し用）
    /// </summary>
    public void SetItem(Sprite sprite, Rarity rarity)
    {
        itemImage.sprite = sprite;
        SetRarityColor(rarity);
    }*/

    /// <summary>
    /// レア度ごとの色設定（処理部分）
    /// </summary>
    private void SetRarityColor(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:
                rarityColor.color = Color.white;
                break;

            case Rarity.Uncommon:
                rarityColor.color = Color.green;
                break;

            case Rarity.Rare:
                rarityColor.color = Color.blue;
                break;

            case Rarity.Epic:
                rarityColor.color = new Color(0.7f, 0f, 1f); // 紫
                break;

            case Rarity.Legendary:
                rarityColor.color = new Color(1f, 0.6f, 0f); // オレンジ
                break;
        }
    }
}
