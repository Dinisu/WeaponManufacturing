using App.BaseSystem.DataStores.ScriptableObjects.Status;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuyAndSellManager : MonoBehaviour
{
    private enum TradeMode { None, Buy, Sell } //売買画面で現在選択している取引方法。

    /// <summary>売買対象として選択中のアイテムまたは装備の情報。</summary>
    private sealed class SelectedTrade
    {
        public D_It_StatusData itemData; // 取引対象のアイテムデータ。装備の場合はnull。
        public D_Eq_StatusData equipmentData; // 取引対象の装備データ。アイテムの場合はnull。
        public GameObject selectedUI; // 選択中フィールドに生成した表示UI。
        public GameObject sourceUI; // 所持品一覧にある元の表示UI。
        public ItemUI sourceItemUI; // 売却選択時に所持数表示を更新する元UI。
        public int price; // 取引対象1つあたりの価格。
        public bool isPurchase; // 購入取引の場合はtrue。
    }

    [Header("画面")]
    [SerializeField]
    private Transform selectionPanel; // 売買方法を選ぶパネル。
    [SerializeField]
    private Transform buySellField; // 売買対象を選ぶ画面領域。
    [SerializeField]
    private Transform possessionsOrMerchandiseField; // 所持品または商品の一覧領域。
    [SerializeField]
    private Transform somethingToBuyOrSellField; // 購入・売却候補を置く一覧領域。

    [Header("ボタン")]
    [SerializeField]
    private Button buySelectionButton; // 購入方法を選ぶボタン。
    [SerializeField]
    private Button sellSelectionButton; // 売却方法を選ぶボタン。
    [SerializeField]
    private Button decisionButton; // 選択した取引を確定するボタン。

    [Header("テキスト")]
    [SerializeField]
    private TMP_Text decisionButtonText; // 確定ボタンに表示する購入または売却の文字。
    [SerializeField]
    private TMP_Text amountText; // 現在選択中の取引合計額を表示するテキスト。

    private GameObject buyAndSellDataUI; // 一覧および選択中の取引に使うUIプレハブ。
    private Db_It_StatusDataBase possessedItemDatabase; // プレイヤーが所持するアイテムのデータベース。
    private Db_It_StatusDataBase merchandiseDatabase; // 購入可能な商品のデータベース。
    private Db_Eq_StatusDataBase possessedEquipmentDatabase; // プレイヤーが所持する装備のデータベース。
    private Db_Eq_StatusDataBase equippedEquipmentDatabase; // 現在装備中の装備のデータベース。
    private readonly List<GameObject> sourceUIs = new List<GameObject>(); // 現在の一覧領域に生成したUI。画面更新時に破棄する。
    private readonly List<SelectedTrade> selectedTrades = new List<SelectedTrade>(); // 購入または売却候補として選ばれている取引。
    private TradeMode currentMode; // 現在の売買画面で選択されている取引方法。

    /// <summary>MenuManagerから売買データベースと表示プレハブを受け取る。</summary>
    public void Begin(GameObject itemPrefab, Db_It_StatusDataBase possessedItems,
        Db_It_StatusDataBase merchandise, Db_Eq_StatusDataBase possessedEquipment,
        Db_Eq_StatusDataBase equippedEquipment)
    {
        CancelPendingTransaction();
        ClearSourceUIs();
        buyAndSellDataUI = itemPrefab;
        possessedItemDatabase = possessedItems;
        merchandiseDatabase = merchandise;
        possessedEquipmentDatabase = possessedEquipment;
        equippedEquipmentDatabase = equippedEquipment;
        currentMode = TradeMode.None;

        UpdateAmount();
        if (selectionPanel != null) selectionPanel.gameObject.SetActive(true);
        if (buySellField != null) buySellField.gameObject.SetActive(false);
    }

    /// <summary>画面を離れる際に選択分を戻して売買画面のUIを片付ける。</summary>
    public void CancelPendingTransaction()
    {
        foreach (SelectedTrade trade in selectedTrades.ToArray())
        {
            if (!trade.isPurchase)
            {
                if (trade.itemData != null)
                {
                    trade.itemData.Number++;
                    if (trade.sourceItemUI != null)
                        trade.sourceItemUI.SetNumber(trade.itemData.Number);
                }
                if (trade.equipmentData != null && trade.sourceUI != null)
                    trade.sourceUI.SetActive(true);
            }
            if (trade.selectedUI != null)
                Destroy(trade.selectedUI);
        }

        selectedTrades.Clear();
        currentMode = TradeMode.None;
        UpdateAmount();
        ClearSourceUIs();
    }

    /// <summary>購入モードへ切り替え、購入可能な商品一覧を生成する。</summary>
    public void SelectBuyMode()
    {
        PrepareMode(TradeMode.Buy);
        if (merchandiseDatabase == null || merchandiseDatabase.ItemList == null)
            return;

        List<D_It_StatusData> merchandise = new List<D_It_StatusData>(merchandiseDatabase.ItemList);
        merchandise.Sort((left, right) =>
        {
            if (left == null) return right == null ? 0 : 1;
            if (right == null) return -1;
            return left.Id.CompareTo(right.Id);
        });
        foreach (D_It_StatusData itemData in merchandise)
        {
            // イベントが設定された商品は通常の購入一覧には含めない。
            if (itemData == null || itemData.Event != null)
                continue;
            CreateItemUI(itemData, possessionsOrMerchandiseField,
                () => AddPurchase(itemData), false);
        }
    }

    /// <summary>売却モードへ切り替え、所持アイテムと装備の一覧を生成する。</summary>
    public void SelectSellMode()
    {
        PrepareMode(TradeMode.Sell);
        if (possessedItemDatabase != null && possessedItemDatabase.ItemList != null)
        {
            foreach (D_It_StatusData itemData in possessedItemDatabase.ItemList)
            {
                if (itemData == null || itemData.Number <= 0)
                    continue;
                ItemUI itemUI = CreateItemUI(itemData, possessionsOrMerchandiseField, null, true);
                if (itemUI == null)
                    continue;

                GameObject sourceUI = itemUI.gameObject;
                itemUI.Initialize(itemData, null, null, null,
                    () => AddItemForSale(itemData, sourceUI, itemUI));
            }
        }

        if (possessedEquipmentDatabase != null && possessedEquipmentDatabase.ItemList != null)
        {
            possessedEquipmentDatabase.ItemList.RemoveAll(equipment => equipment == null);
            foreach (D_Eq_StatusData equipmentData in possessedEquipmentDatabase.ItemList)
            {
                ItemUI itemUI = CreateEquipmentUI(equipmentData, possessionsOrMerchandiseField, null);
                if (itemUI == null)
                    continue;

                GameObject sourceUI = itemUI.gameObject;
                itemUI.Initialize(equipmentData, null, null, null,
                    () => AddEquipmentForSale(equipmentData, sourceUI));
            }
        }
    }

    /// <summary>指定された売買モードに合わせて画面と合計額を更新する。</summary>
    private void PrepareMode(TradeMode mode)
    {
        ClearSourceUIs();
        currentMode = mode;
        if (selectionPanel != null) selectionPanel.gameObject.SetActive(false);
        if (buySellField != null) buySellField.gameObject.SetActive(true);
        if (decisionButtonText != null)
            decisionButtonText.text = mode == TradeMode.Buy ? "購入" : "売却";
        UpdateAmount();
    }

    /// <summary>指定された商品を購入候補に追加する。</summary>
    private void AddPurchase(D_It_StatusData itemData)
    {
        AddSelectedTrade(itemData, null, null, null, itemData.Price, true);
    }

    /// <summary>アイテムを売却候補に加え、所持数を一時的に減らす。</summary>
    private void AddItemForSale(D_It_StatusData itemData, GameObject sourceUI, ItemUI sourceItemUI)
    {
        if (itemData == null || itemData.Number <= 0)
            return;
        if (!AddSelectedTrade(itemData, null, sourceUI, sourceItemUI, itemData.Price, false))
            return;
        itemData.Number--;
        sourceItemUI.SetNumber(itemData.Number);
    }

    /// <summary>装備を売却候補に加え、所持品一覧で一時的に非表示にする。</summary>
    private void AddEquipmentForSale(D_Eq_StatusData equipmentData, GameObject sourceUI)
    {
        if (equipmentData == null ||
            !AddSelectedTrade(null, equipmentData, sourceUI, null, equipmentData.Price, false))
            return;
        sourceUI.SetActive(false);
    }

    /// <summary>選択中一覧に取引を追加し、表示と合計額を更新する。</summary>
    private bool AddSelectedTrade(D_It_StatusData itemData, D_Eq_StatusData equipmentData,
        GameObject sourceUI, ItemUI sourceItemUI, int price, bool isPurchase)
    {
        if (buyAndSellDataUI == null || somethingToBuyOrSellField == null)
            return false;

        SelectedTrade trade = new SelectedTrade
        {
            itemData = itemData,
            equipmentData = equipmentData,
            sourceUI = sourceUI,
            sourceItemUI = sourceItemUI,
            price = price,
            isPurchase = isPurchase
        };
        trade.selectedUI = Instantiate(buyAndSellDataUI, somethingToBuyOrSellField, false);
        ItemUI selectedItemUI = trade.selectedUI.GetComponent<ItemUI>();
        if (selectedItemUI == null)
        {
            Destroy(trade.selectedUI);
            return false;
        }

        if (itemData != null)
        {
            selectedItemUI.Initialize(itemData, null, null, null, () => RemoveSelectedTrade(trade));
            selectedItemUI.SetBuyAndSellDisplay(itemData.Name, price, false);
        }
        else
        {
            selectedItemUI.Initialize(equipmentData, null, null, null, () => RemoveSelectedTrade(trade));
            selectedItemUI.SetBuyAndSellDisplay(equipmentData.Name, price, false);
        }

        selectedTrades.Add(trade);
        UpdateAmount();
        return true;
    }

    /// <summary>選択中の取引を外し、売却品なら所持状態を元に戻す。</summary>
    private void RemoveSelectedTrade(SelectedTrade trade)
    {
        if (!selectedTrades.Remove(trade))
            return;
        if (!trade.isPurchase)
        {
            if (trade.itemData != null)
            {
                trade.itemData.Number++;
                if (trade.sourceItemUI != null)
                    trade.sourceItemUI.SetNumber(trade.itemData.Number);
            }
            if (trade.equipmentData != null && trade.sourceUI != null)
                trade.sourceUI.SetActive(true);
        }
        if (trade.selectedUI != null)
            Destroy(trade.selectedUI);
        UpdateAmount();
    }

    /// <summary>所持金を確認して取引を確定し、所持データを更新する。</summary>
    public void ConfirmTrade()
    {
        if (currentMode == TradeMode.None || selectedTrades.Count == 0 || GameManager.Instance == null)
            return;

        int total = CalculateTotal();
        if (currentMode == TradeMode.Buy)
        {
            if (possessedItemDatabase == null || GameManager.Instance.PlayerMoney < total)
                return;
            GameManager.Instance.PlayerMoney -= total;
            foreach (SelectedTrade trade in selectedTrades)
            {
                if (trade.itemData != null)
                {
                    // 商品専用データも所持品データベースへ加えて、購入後の一覧表示を保つ。
                    if (possessedItemDatabase != null && !possessedItemDatabase.ItemList.Contains(trade.itemData))
                        possessedItemDatabase.ItemList.Add(trade.itemData);
                    trade.itemData.Number++;
                }
            }
        }
        else
        {
            GameManager.Instance.PlayerMoney += total;
            foreach (SelectedTrade trade in selectedTrades)
            {
                if (trade.equipmentData != null && possessedEquipmentDatabase != null)
                    possessedEquipmentDatabase.ItemList.Remove(trade.equipmentData);
                if (trade.equipmentData != null && equippedEquipmentDatabase != null)
                    equippedEquipmentDatabase.ItemList.Remove(trade.equipmentData);
            }
        }

        // 決定後は所持数を戻さず、選択表示だけを消して一覧を更新する。
        foreach (SelectedTrade trade in selectedTrades)
        {
            if (trade.selectedUI != null)
                Destroy(trade.selectedUI);
        }
        selectedTrades.Clear();
        ClearSourceUIs();
        UpdateAmount();
        if (currentMode == TradeMode.Buy)
            SelectBuyMode();
        else
            SelectSellMode();
    }

    /// <summary>アイテム用の売買UIを生成し、必要な表示とクリック処理を設定する。</summary>
    private ItemUI CreateItemUI(D_It_StatusData itemData, Transform parent,
        UnityEngine.Events.UnityAction action, bool showNumber)
    {
        if (itemData == null || buyAndSellDataUI == null || parent == null)
            return null;
        GameObject uiObject = Instantiate(buyAndSellDataUI, parent, false);
        ItemUI itemUI = uiObject.GetComponent<ItemUI>();
        if (itemUI == null)
        {
            Destroy(uiObject);
            return null;
        }
        itemUI.Initialize(itemData, null, null, null, action ?? (() => { }));
        itemUI.SetBuyAndSellDisplay(itemData.Name, itemData.Price, showNumber);
        if (showNumber)
            itemUI.SetNumber(itemData.Number);
        sourceUIs.Add(uiObject);
        return itemUI;
    }

    /// <summary>装備用の売買UIを生成し、表示とクリック処理を設定する。</summary>
    private ItemUI CreateEquipmentUI(D_Eq_StatusData equipmentData, Transform parent,
        UnityEngine.Events.UnityAction action)
    {
        if (equipmentData == null || buyAndSellDataUI == null || parent == null)
            return null;
        GameObject uiObject = Instantiate(buyAndSellDataUI, parent, false);
        ItemUI itemUI = uiObject.GetComponent<ItemUI>();
        if (itemUI == null)
        {
            Destroy(uiObject);
            return null;
        }
        itemUI.Initialize(equipmentData, null, null, null, action ?? (() => { }));
        itemUI.SetBuyAndSellDisplay(equipmentData.Name, equipmentData.Price, false);
        sourceUIs.Add(uiObject);
        return itemUI;
    }

    /// <summary>一覧領域に生成したUIをすべて破棄する。</summary>
    private void ClearSourceUIs()
    {
        foreach (GameObject uiObject in sourceUIs)
        {
            if (uiObject != null)
                Destroy(uiObject);
        }
        sourceUIs.Clear();
    }

    /// <summary>選択中取引の合計額を画面に表示する。</summary>
    private void UpdateAmount()
    {
        if (amountText != null)
            amountText.text = $"{CalculateTotal()}$";
    }

    /// <summary>選択中取引の価格を合算して返す。</summary>
    private int CalculateTotal()
    {
        int total = 0;
        foreach (SelectedTrade trade in selectedTrades)
            total += trade.price;
        return total;
    }

}
