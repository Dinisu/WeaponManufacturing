using App.BaseSystem.DataStores.ScriptableObjects.Status;
using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class MenuManager : MonoBehaviour
{
    [SerializeField, Header("表示背景")]
    private List<BackgroundData> backgrounds;
    [SerializeField, Header("アイテム表示プレハブ")]
    private GameObject data_UI;

    private Db_It_StatusDataBase db_PlayerItem;//所持アイテムデータベース
    private Db_Eq_StatusDataBase db_PossessedEq;//所持装備データベース
    private Db_Eq_StatusDataBase db_EquippedWeapon;
    private Dss_It_StatusDataStores dss_It_StatusDataStores;
    private Dss_Eq_StatusDataStores dss_Eq_StatusDataStores;

    [SerializeField] private BlacksmithManager blacksmithManager;
    [SerializeField] private WarehouseManager warehouseManager;
    [SerializeField] private BattleManager battleManager;

    private void Awake()
    {
        dss_It_StatusDataStores = FindAnyObjectByType<Dss_It_StatusDataStores>();
        dss_Eq_StatusDataStores = FindAnyObjectByType<Dss_Eq_StatusDataStores>();

        // FindDatabaseWithName を使用して Player_Item データベースを取得
        db_PlayerItem = dss_It_StatusDataStores.FindDatabaseWithName("Possessed_Item");
        db_PossessedEq = dss_Eq_StatusDataStores.FindDatabaseWithName("Possessed Equipment");
        db_EquippedWeapon = dss_Eq_StatusDataStores?.FindDatabaseWithName("Equipped weapon");
    }
    /// <summary>
    /// 表示背景の種類
    /// </summary>
    public enum BackgroundType
    {
        Battle,
        Blacksmith,
        Enchantment,
        BuyAndSell,
        Warehouse
    }

    [System.Serializable]

    public class BackgroundData//表示背景の管理クラス
    {
        public BackgroundType type;       //種類
        public GameObject background;     //背景
        public Transform displayFields;  //アイテムなどのUIを表示する場所
        public List<GameObject> itemList = new List<GameObject>(); //displayFieldsに表示しているアイテムリスト
    }

    /// <summary>
    /// 戦闘画面に移行
    /// </summary>
    public void Next_Battle()
    {
        foreach (var data in backgrounds)
        {
            if (data.type == BackgroundType.Battle)
            {
                data.background.SetActive(true);

                UIGeneration(BackgroundType.Battle);
            }
            else
            {
                data.background.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 鍛冶画面に移行
    /// </summary>
    public void Next_Blacksmit()
    {
        foreach (var data in backgrounds)
        {
            if (data.type == BackgroundType.Blacksmith)
            {
                data.background.SetActive(true);

                UIGeneration(BackgroundType.Blacksmith);
            }
            else
            {
                data.background.SetActive(false);
            }
        }
    }

    /// <summary>
    /// エンチャント画面に移行
    /// </summary>
    public void Next_Enchantment()
    {
        foreach (var data in backgrounds)
        {
            if (data.type == BackgroundType.Enchantment)
            {
                data.background.SetActive(true);

                UIGeneration(BackgroundType.Enchantment);
            }
            else
            {
                data.background.SetActive(false);
            }
        }
    }

    public void Next_Warehouse()
    {
        foreach (var data in backgrounds)
        {
            if (data.type == BackgroundType.Warehouse)
            {
                data.background.SetActive(true);

                warehouseManager?.ClearEquippedSlots();
                UIGeneration(BackgroundType.Warehouse);
            }
            else
            {
                data.background.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 画面に対応する所持品UI生成
    /// </summary>
    public void UIGeneration(BackgroundType blacksmith)
    {

        foreach (var data in backgrounds)
        {
            //表示中の画面のUI更新
            if (data.type == blacksmith)
            {
                // 古いUI削除
                foreach (var item in data.itemList)
                {
                    Destroy(item);
                }

                data.itemList.Clear();

                // 新しいUI生成,生成する所持品の種類分岐
                switch (data.type)
                {
                    case BackgroundType.Battle:
                        SelectMaterialClear();
                        battleManager?.RefreshEquippedWeapons();
                        break;

                    case BackgroundType.Blacksmith:
                        SelectMaterialClear();

                        GenerateMaterialItemUI(data);
                        break;

                    case BackgroundType.Enchantment:
                        SelectMaterialClear();

                        GenerateEquipmentUI(data);          
                        break;
                    case BackgroundType.Warehouse:
                        SelectMaterialClear();

                        GenerateMaterialItemUI(data);
                        GenerateEquipmentUI(data);
                        break ;
                }

                break;
            }
        }
    }

    /// <summary>
    /// 選択中の素材アイテムがあれば元に戻す
    /// </summary>
    private void SelectMaterialClear()
    {
        if (blacksmithManager.Material1 != null)
        {
            ItemUI itemUI = blacksmithManager.Material1.GetComponent<ItemUI>();

            if (itemUI != null)
            {
                itemUI.MateriallItem(itemUI.ItemData, blacksmithManager.Material1);
            }
        }

        if (blacksmithManager.Material2 != null)
        {
            ItemUI itemUI = blacksmithManager.Material2.GetComponent<ItemUI>();

            if (itemUI != null)
            {
                itemUI.MateriallItem(itemUI.ItemData, blacksmithManager.Material2);
            }
        }
    }

    /// <summary>
    /// 素材アイテムUI生成
    /// </summary>
    private void GenerateMaterialItemUI(BackgroundData data)
    {
        foreach (var itemData in db_PlayerItem.ItemList)
        {
            if (itemData.Number <= 0)
                continue;
            //アイテムが素材でなければ表示しない
            if (itemData.SeeKinds != D_It_StatusData.Kinds.Material)
                continue;

            //data_UIプレハブを生成して、displayFieldsの子オブジェクトとして配置する
            GameObject uiObj = Instantiate(data_UI, data.displayFields);

            // アイテムの画像を表示する子オブジェクトを取得
            ItemUI itemUI = uiObj.GetComponent<ItemUI>();

            itemUI.Initialize(itemData, blacksmithManager, warehouseManager, itemUI, () =>
            {
                if (data.type == BackgroundType.Warehouse)
                {
                    itemUI.SelectWarehouseItem(itemData, uiObj);
                }
                else
                {
                    itemUI.SelectItem(itemData, data_UI);
                }
            });
            itemUI.SetNumber(itemData.Number);


            data.itemList.Add(uiObj);
        }
    }
    /// <summary>
    /// 装備UI生成
    /// </summary>
    private void GenerateEquipmentUI(BackgroundData data)
    {
        foreach (var eqData in db_PossessedEq.ItemList)
        {
            //data_UIプレハブを生成して、displayFieldsの子オブジェクトとして配置する
            GameObject uiObj = Instantiate(data_UI, data.displayFields);

            // アイテムの画面を表示する子オブジェクトを取得
            ItemUI itemUI = uiObj.GetComponent<ItemUI>();

            itemUI.Initialize(eqData, blacksmithManager, warehouseManager, itemUI, () =>
            {
                if (data.type == BackgroundType.Warehouse)
                {
                    itemUI.SelectWarehouseItem(eqData, uiObj);
                }
                else
                {
                    itemUI.SelectItem(eqData);
                }
            });

            data.itemList.Add(uiObj);

            if (data.type == BackgroundType.Warehouse && db_EquippedWeapon != null && db_EquippedWeapon.ItemList.Contains(eqData))
            {
                warehouseManager.CreateEquippedSlotUI(eqData, uiObj);
            }
        }
    }
}
