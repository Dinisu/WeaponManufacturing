using App.BaseSystem.DataStores.ScriptableObjects.Status;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ホバー中の素材または装備に合わせて、詳細パネルを表示する。
/// </summary>
public class DetailsPanelManager : MonoBehaviour
{
    [Header("詳細パネル")]
    [SerializeField] private RectTransform panelRoot; // 詳細パネルを生成する全画面領域
    [SerializeField] private GameObject itemDetailsPanelPrefab; // 素材詳細パネルのプレハブ
    [SerializeField] private GameObject weaponDetailsPanelPrefab; // 武器詳細パネルのプレハブ
    [SerializeField] private float panelScreenPadding = 24f; // パネルが画面端に接しないための余白

    private GameObject itemDetailsPanel; // 生成済みの素材詳細パネル
    private GameObject weaponDetailsPanel; // 生成済みの武器詳細パネル
    private ItemUI hoveredItemUI; // 現在詳細を表示している元UI

    /// <summary>ホバー中の素材データを素材詳細パネルに表示する。</summary>
    public void ShowItemDetails(D_It_StatusData itemData, ItemUI sourceUI)
    {
        if (itemData == null || sourceUI == null || !EnsurePanel(ref itemDetailsPanel, itemDetailsPanelPrefab))
            return;

        hoveredItemUI = sourceUI;
        HidePanel(weaponDetailsPanel);

        SetText(itemDetailsPanel.transform, "ItemName", itemData.Name);
        SetText(itemDetailsPanel.transform, "ItemDescription", itemData.ItemDescription);
        SetText(itemDetailsPanel.transform, "EffectDescription", itemData.EfficacyItemDescription);
        SetImage(itemDetailsPanel.transform, "ItemIcon", itemData.DataIcon);
        PositionPanel(itemDetailsPanel.GetComponent<RectTransform>(), sourceUI.transform as RectTransform);
        itemDetailsPanel.SetActive(true);
    }

    /// <summary>ホバー中の武器データと付与スキルを武器詳細パネルに表示する。</summary>
    public void ShowWeaponDetails(D_Eq_StatusData weaponData, ItemUI sourceUI)
    {
        if (weaponData == null || sourceUI == null || !EnsurePanel(ref weaponDetailsPanel, weaponDetailsPanelPrefab))
            return;

        hoveredItemUI = sourceUI;
        HidePanel(itemDetailsPanel);

        SetText(weaponDetailsPanel.transform, "WeaponName", weaponData.Name);
        SetText(weaponDetailsPanel.transform, "Attribute", $"属性: {GetAttributeName(weaponData.SeeAttribute)}");
        SetText(weaponDetailsPanel.transform, "MaxHp", $"最大HP: {weaponData.MaxHp}");
        SetText(weaponDetailsPanel.transform, "MaxMp", $"最大MP: {weaponData.MaxMp}");
        SetText(weaponDetailsPanel.transform, "Attack", $"攻撃力: {weaponData.Attack}");
        SetText(weaponDetailsPanel.transform, "Magic", $"魔力: {weaponData.Magic}");
        SetText(weaponDetailsPanel.transform, "Defense", $"防御力: {weaponData.Defense}");
        SetText(weaponDetailsPanel.transform, "MagicDefense", $"魔法防御: {weaponData.MagicDefense}");
        SetText(weaponDetailsPanel.transform, "Speed", $"速度: {weaponData.Speed}");
        SetText(weaponDetailsPanel.transform, "CriticalRate", $"クリティカル率: {weaponData.CriticalRate:0.##}");
        SetImage(weaponDetailsPanel.transform, "WeaponIcon", weaponData.DataIcon);
        SetEnchantments(weaponDetailsPanel.transform, weaponData);
        PositionPanel(weaponDetailsPanel.GetComponent<RectTransform>(), sourceUI.transform as RectTransform);
        weaponDetailsPanel.SetActive(true);
    }

    /// <summary>指定したUIが現在のホバー元なら詳細を閉じる。</summary>
    public void HideDetails(ItemUI sourceUI)
    {
        if (sourceUI != null && hoveredItemUI != sourceUI)
            return;

        HideDetails();
    }

    /// <summary>表示中の詳細パネルを閉じ、画面切り替えに備える。</summary>
    public void HideDetails()
    {
        hoveredItemUI = null;
        HidePanel(itemDetailsPanel);
        HidePanel(weaponDetailsPanel);
    }

    /// <summary>詳細パネルを必要なときだけ生成して返す。</summary>
    private bool EnsurePanel(ref GameObject panelInstance, GameObject panelPrefab)
    {
        if (panelInstance != null)
            return true;
        if (panelRoot == null || panelPrefab == null)
            return false;

        panelInstance = Instantiate(panelPrefab, panelRoot, false);
        panelInstance.SetActive(false);
        return true;
    }

    /// <summary>画面上のホバー元位置を基準に、反対側の半分へパネルを配置する。</summary>
    private void PositionPanel(RectTransform panelRect, RectTransform sourceRect)
    {
        if (panelRoot == null || panelRect == null || sourceRect == null)
            return;

        Canvas canvas = panelRoot.GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = sourceRect.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.rootCanvas.worldCamera
            : null;

        Vector3 sourceScreenPosition = RectTransformUtility.WorldToScreenPoint(eventCamera, sourceRect.position);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                panelRoot, sourceScreenPosition, eventCamera, out Vector2 sourceLocalPosition))
            return;

        Rect rootRect = panelRoot.rect;
        Rect panelBounds = panelRect.rect;
        float halfPanelWidth = panelBounds.width * 0.5f;
        float halfPanelHeight = panelBounds.height * 0.5f;
        bool sourceIsOnRight = sourceLocalPosition.x >= rootRect.center.x;

        // 右側のUIには左半分、左側のUIには右半分を割り当てる。
        float targetX = sourceIsOnRight
            ? rootRect.xMin + rootRect.width * 0.25f
            : rootRect.xMin + rootRect.width * 0.75f;
        targetX = ClampPanelPosition(targetX, rootRect.xMin, rootRect.xMax, halfPanelWidth);

        // 縦方向はホバー元に近づけつつ、パネル全体が画面内に入るよう制限する。
        float targetY = ClampPanelPosition(sourceLocalPosition.y, rootRect.yMin, rootRect.yMax, halfPanelHeight);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(targetX - rootRect.center.x, targetY - rootRect.center.y);
    }

    /// <summary>パネルの半分の大きさと余白を考慮して座標を画面内に収める。</summary>
    private float ClampPanelPosition(float position, float min, float max, float halfSize)
    {
        float minPosition = min + halfSize + panelScreenPadding;
        float maxPosition = max - halfSize - panelScreenPadding;
        if (minPosition > maxPosition)
            return (min + max) * 0.5f;

        return Mathf.Clamp(position, minPosition, maxPosition);
    }

    /// <summary>武器に付与されているスキルを最大3件までパネルへ反映する。</summary>
    private void SetEnchantments(Transform panel, D_Eq_StatusData weaponData)
    {
        for (int i = 0; i < 3; i++)
        {
            D_Sk_StatusData skill = weaponData.SkillList != null && i < weaponData.SkillList.Count
                ? weaponData.SkillList[i]
                : null;
            string nameField = $"Enchant{i + 1}Name";
            string descriptionField = $"Enchant{i + 1}Description";
            SetText(panel, nameField, skill != null ? skill.Name : string.Empty);
            SetText(panel, descriptionField, skill != null ? skill.EfficacyItemDescription : string.Empty);
            SetObjectActive(panel, nameField, skill != null);
            SetObjectActive(panel, descriptionField, skill != null);
        }
    }

    /// <summary>属性enumをパネルに表示する日本語名へ変換する。</summary>
    private string GetAttributeName(D_Eq_StatusData.Attribute attribute)
    {
        switch (attribute)
        {
            case D_Eq_StatusData.Attribute.Fire: return "火属性";
            case D_Eq_StatusData.Attribute.Water: return "水属性";
            case D_Eq_StatusData.Attribute.Tree: return "木属性";
            case D_Eq_StatusData.Attribute.Metal: return "金属性";
            case D_Eq_StatusData.Attribute.Soil: return "土属性";
            default: return "無属性";
        }
    }

    /// <summary>名前で取得したTMPテキストがある場合に表示内容を更新する。</summary>
    private void SetText(Transform panel, string objectName, string value)
    {
        Transform target = panel.Find(objectName);
        TMP_Text text = target != null ? target.GetComponent<TMP_Text>() : null;
        if (text != null)
            text.text = value ?? string.Empty;
    }

    /// <summary>名前で取得したアイコン画像がある場合にスプライトを更新する。</summary>
    private void SetImage(Transform panel, string objectName, Sprite sprite)
    {
        Transform target = panel.Find(objectName);
        Image image = target != null ? target.GetComponent<Image>() : null;
        if (image == null)
            return;

        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.sprite = sprite;
    }

    /// <summary>名前で取得した詳細行のGameObjectがある場合に表示状態を更新する。</summary>
    private void SetObjectActive(Transform panel, string objectName, bool isActive)
    {
        Transform target = panel.Find(objectName);
        if (target != null)
            target.gameObject.SetActive(isActive);
    }

    /// <summary>パネルが未生成または破棄済みでも安全に非表示にする。</summary>
    private void HidePanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(false);
    }
}
