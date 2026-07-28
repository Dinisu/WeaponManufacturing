using App.BaseSystem.DataStores.ScriptableObjects.Status;
using NUnit.Framework.Interfaces;
using UnityEngine;
using System.Collections;
using static MenuManager;

public class BlacksmithManager : MonoBehaviour
{
    [SerializeField] private GameObject SelectObj1;
    [SerializeField] private GameObject SelectObj2;
    [SerializeField] private GameObject ForgingButton;
    [SerializeField] private Transform SelectMaterial1;
    [SerializeField] private Transform SelectMaterial2;
    [SerializeField, Header("選択素材")]
    public GameObject Material1;
    public GameObject Material2;

    [SerializeField] MenuManager menuManager;
    [SerializeField] BlacksmithGameManager blacksmithGameManager;

    /// <summary>
    /// 鍛冶を開始する
    /// </summary>
    public void StartForging()
    {
        if (Material1 == null || Material2 == null) return;

        SelectObj1.SetActive(false);
        SelectObj2.SetActive(false);
        ForgingButton.SetActive(false);

        //選択中の素材データを渡して鍛冶を開始する
        blacksmithGameManager.Blacksmith(Material1.GetComponent<ItemUI>().ItemData, Material2.GetComponent<ItemUI>().ItemData);
    }

    /// <summary>
    /// 鍛冶に使用するアイテムの選択処理
    /// </summary>
    public void SelectMaterial(D_It_StatusData itemData, GameObject uiObj , ItemUI itemUI)
    {
        if (Material1 == null)
        {
            //UI生成
            Material1 = Instantiate(uiObj, SelectMaterial1);

            // 選択欄側のUI
            ItemUI materialItemUI = Material1.GetComponent<ItemUI>();

            // 所持数を減らす
            itemData.Number--;

            // 一覧UIの個数を更新
            itemUI.SetNumber(itemData.Number);

            // 選択欄UIを設定
            materialItemUI.Initialize(itemData, this, null, itemUI, () =>
            {
                materialItemUI.MateriallItem(itemData, Material1);
            });
            materialItemUI.HideNumber();   // ← 個数を非表示
        }
        else if (Material2 == null)
        {
            Material2 = Instantiate(uiObj, SelectMaterial2);

            // 選択欄側のUI
            ItemUI materialItemUI = Material2.GetComponent<ItemUI>();

            // 所持数を減らす
            itemData.Number--;

            // 一覧UIの個数を更新
            itemUI.SetNumber(itemData.Number);

            // 選択欄UIを設定
            materialItemUI.Initialize(itemData, this, null, itemUI, () =>
            {
                materialItemUI.MateriallItem(itemData, Material2);
            });
            materialItemUI.HideNumber();   // ← 個数を非表示
        }
        Debug.Log(itemData.Name);
    }
    public void CancelMaterial(D_It_StatusData itemData, GameObject materialObj, ItemUI sourceItemUI)
    {
        // 所持数を戻す
        itemData.Number++;

        // 一覧UIの個数を更新
        sourceItemUI.SetNumber(itemData.Number);

        Destroy(materialObj);
    }

    public IEnumerator AfterForging()
    {
        ItemUI itemUI1 = Material1.GetComponent<ItemUI>();
        ItemUI itemUI2 = Material2.GetComponent<ItemUI>();
        // 一覧UIの個数を更新
        itemUI1.SetNumber(itemUI1.ItemData.Number);
        itemUI2.SetNumber(itemUI2.ItemData.Number);

        Destroy(Material1);
        Destroy(Material2);

        SelectObj1.SetActive(true);
        SelectObj2.SetActive(true);
        ForgingButton.SetActive(true);

        yield return null;
        //UI更新
        menuManager.UIGeneration(BackgroundType.Blacksmith);
    }
}
