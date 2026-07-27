using App.BaseSystem.DataStores.ScriptableObjects.Status;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    [Header("プレイヤーデータ")]
    [SerializeField] public D_Ch_StatusData statusData; // プレイヤーの基本ステータスデータ

    [Header("魔法関連")]
    [SerializeField] private D_Sk_StatusData magicMissile; // 使用する魔法データ
    [SerializeField] private Transform magicSpawnPoint; // 魔法生成位置

    [Header("生成プレハブ")]
    [SerializeField] private GameObject magicPrefab; // 魔法のプレハブ
    [SerializeField] private GameObject weaponPrefab; // 武器アイコン用プレハブ

    [Header("武器の生成位置")]
    [SerializeField] private Transform GenerationPosition1;
    [SerializeField] private Transform GenerationPosition2;
    [SerializeField] private Transform GenerationPosition3;
    [SerializeField] private Transform GenerationPosition4;

    [Header("背景スクロール")]
    [SerializeField] private Transform[] backgroundPanels; // スクロールする背景パネル
    [SerializeField] private float backgroundScrollSpeed = 2f; // 背景スクロール速度
    [SerializeField] private float backgroundLoopWidth = 40f; // パネルループ幅

    [Header("敵出現")]
    [SerializeField] private float enemySpawnInterval = 3f; // 敵生成間隔
    [SerializeField] private float enemySpawnX = 12f; // 敵出現X座標（Canvas ルートのローカル座標）
    [SerializeField] private float enemySpawnY = 0f; // 敵出現Y座標（Canvas ルートのローカル座標）
    [SerializeField] private RectTransform enemyCanvasRoot; // 敵を配置する Canvas 内の親

    [Header("進捗表示")]
    [SerializeField] private TextMeshProUGUI progressText; // 進捗表示用テキスト
    [SerializeField] private float progressPerSecond = 1f; // 1秒あたりの進捗量

    private Dss_It_StatusDataStores dss_It_StatusDataStores;
    private Dss_Eq_StatusDataStores dss_Eq_StatusDataStores;
    private Dss_Ch_StatusDataStores dss_Ch_StatusDataStores;

    private Db_It_StatusDataBase db_PlayerItem; // 所持アイテムDB
    private Db_Eq_StatusDataBase db_EquippedWeapon; // 所持装備DB
    private Db_Ch_StatusDataBase db_EnemyList; // 敵リストDB

    private float spawnTimer;
    private float progressDistance;
    public bool isPlaying = true; // 戦闘中フラグ
    private bool stopScroll;

    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
    private readonly List<GameObject> spawnedWeapons = new List<GameObject>();

    private RuntimePlayerData runtimePlayer;

    private void Awake()
    {
        // DB ストアを検索して、各データベースを取得
        dss_It_StatusDataStores = FindAnyObjectByType<Dss_It_StatusDataStores>();
        dss_Eq_StatusDataStores = FindAnyObjectByType<Dss_Eq_StatusDataStores>();
        dss_Ch_StatusDataStores = FindAnyObjectByType<Dss_Ch_StatusDataStores>();

        db_PlayerItem = dss_It_StatusDataStores?.FindDatabaseWithName("Possessed_Item");
        db_EquippedWeapon = dss_Eq_StatusDataStores?.FindDatabaseWithName("Equipped weapon");
        db_EnemyList = dss_Ch_StatusDataStores?.FindDatabaseWithName("Enemy List");

        Debug.Log($"weaponPrefab={(weaponPrefab == null ? "null" : "ok")}, db_EquippedWeapon={(db_EquippedWeapon == null ? "null" : "ok")}, items={db_EquippedWeapon?.ItemList?.Count}");

        if (statusData == null)
        {
            Debug.LogError("[BattleManager] statusData が設定されていません。");
            return;
        }

        // magicMissile が設定されていない場合は、プレイヤーのスキルリストから初期値を取得
        if (magicMissile == null)
        {
            magicMissile = GetDefaultMagic();
        }

        runtimePlayer = CreateRuntimePlayer();

        // 装備中の武器を画面に生成
        SpawnEquippedWeapons();
        UpdateProgressText();
    }

    private void Update()
    {
        if (!isPlaying || runtimePlayer == null)
            return;

        // 背景と進捗はスクロール中のみ更新
        if (!stopScroll)
        {
            ScrollBackground();
            progressDistance += progressPerSecond * Time.deltaTime;
            UpdateProgressText();
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= enemySpawnInterval)
        {
            spawnTimer = 0f;
            SpawnEnemy();
        }

        CheckEnemyPositions();

        // 左クリックで魔法発射
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            CastMagic();
        }

        // プレイヤー HP が 0 なら戦闘をリセット
        if (runtimePlayer.Hp <= 0)
        {
            StartCoroutine(ResetBattle());
        }
    }

    /// <summary>
    /// プレイヤーのスキルリストから魔法データを取り出す
    /// </summary>
    private D_Sk_StatusData GetDefaultMagic()
    {
        if (statusData.SkillList != null && statusData.SkillList.ItemList != null && statusData.SkillList.ItemList.Count > 0)
        {
            return statusData.SkillList.ItemList[0];
        }
        return null;
    }

    /// <summary>
    /// 実行時プレイヤーデータを生成し、装備補正を反映する
    /// </summary>
    private RuntimePlayerData CreateRuntimePlayer()
    {
        var player = new RuntimePlayerData
        {
            MaxHp = statusData.MaxHp,
            Hp = statusData.Hp > 0 ? statusData.Hp : statusData.MaxHp,
            MaxMp = statusData.MaxMp,
            Mp = statusData.Mp > 0 ? statusData.Mp : statusData.MaxMp,
            Attack = statusData.Attack,
            Magic = statusData.Magic,
            Defense = statusData.Defense,
            MagicDefense = statusData.MagicDefense,
            Speed = statusData.Speed,
            CriticalRate = statusData.CriticalRate
        };

        ApplyEquipmentBonuses(player);
        player.Hp = Mathf.Clamp(player.Hp, 0, player.MaxHp);
        player.Mp = Mathf.Clamp(player.Mp, 0, player.MaxMp);

        return player;
    }

    /// <summary>
    /// 所持装備のステータスをプレイヤーに加算する
    /// </summary>
    private void ApplyEquipmentBonuses(RuntimePlayerData player)
    {
        if (db_EquippedWeapon == null || db_EquippedWeapon.ItemList == null)
            return;

        foreach (var equipment in db_EquippedWeapon.ItemList)
        {
            if (equipment == null)
                continue;

            player.MaxHp += equipment.MaxHp;
            player.MaxMp += equipment.MaxMp;
            player.Attack += equipment.Attack;
            player.Magic += equipment.Magic;
            player.Defense += equipment.Defense;
            player.MagicDefense += equipment.MagicDefense;
            player.Speed += equipment.Speed;
            player.CriticalRate += equipment.CriticalRate;
        }
    }

    /// <summary>
    /// 武器プレハブを4つまで生成し、装備データを BattleUI にセットする
    /// </summary>
    private void SpawnEquippedWeapons()
    {
        if (weaponPrefab == null || db_EquippedWeapon == null || db_EquippedWeapon.ItemList == null)
            return;

        var spawnPoints = new[] { GenerationPosition1, GenerationPosition2, GenerationPosition3, GenerationPosition4 };
        int index = 0;

        foreach (var equipment in db_EquippedWeapon.ItemList)
        {
            if (equipment == null)
                continue;

            while (index < spawnPoints.Length && spawnPoints[index] == null)
            {
                Debug.LogWarning($"[BattleManager] GenerationPosition{index + 1} が未設定です。次の位置を探します。");
                index++;
            }

            if (index >= spawnPoints.Length)
            {
                Debug.LogWarning("[BattleManager] 空きの武器生成位置がありません。生成を中止します。");
                break;
            }

            var spawnPoint = spawnPoints[index];
            var weapon = Instantiate(weaponPrefab, spawnPoint);
            if (weapon != null)
            {
                weapon.transform.localPosition = Vector3.zero;
            }

            var battleUI = weapon.GetComponent<BattleUI>();
            if (battleUI != null)
            {
                battleUI.WeaponUsed = equipment;
            }

            SetWeaponVisual(weapon, equipment.DataIcon);

            Debug.Log($"{equipment.Name}を生成しました。");
            spawnedWeapons.Add(weapon);
            index++;
        }
    }

    /// <summary>
    /// 背景を左へスクロールし、画面外になったパネルをループさせる
    /// </summary>
    private void ScrollBackground()
    {
        if (backgroundPanels == null || backgroundPanels.Length == 0)
            return;

        foreach (var panel in backgroundPanels)
        {
            if (panel == null)
                continue;

            panel.position += Vector3.left * backgroundScrollSpeed * Time.deltaTime;

            if (panel.position.x < -backgroundLoopWidth)
            {
                panel.position += Vector3.right * backgroundLoopWidth * backgroundPanels.Length;
            }
        }
    }

    /// <summary>
    /// 進捗テキストを更新
    /// </summary>
    private void UpdateProgressText()
    {
        if (progressText != null)
        {
            progressText.text = $"進捗: {progressDistance:F1}m";
        }
    }

    /// <summary>
    /// 生成済みの敵を画面外判定で削除する
    /// </summary>
    private void CheckEnemyPositions()
    {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            var enemy = spawnedEnemies[i];
            if (enemy == null)
            {
                spawnedEnemies.RemoveAt(i);
                continue;
            }

            if (enemy.transform.position.x < -enemySpawnX)
            {
                Destroy(enemy);
                spawnedEnemies.RemoveAt(i);
            }
        }
    }


    /// <summary>
    /// 敵をランダムに生成する
    /// </summary>
    private void SpawnEnemy()
    {
        if (db_EnemyList == null || db_EnemyList.ItemList == null || db_EnemyList.ItemList.Count == 0)
            return;

        var enemyTemplate = GetRandomEnemyData();
        if (enemyTemplate == null || enemyTemplate.Character == null)
            return;

        int level = Mathf.Max(1, Mathf.FloorToInt(progressDistance / 10f) + 1);
        var runtimeEnemy = EnemyStatSettings(enemyTemplate, level);

        var spawnPosition = new Vector3(enemySpawnX, enemySpawnY, 0f);
        GameObject enemyObject;

        if (enemyCanvasRoot != null)
        {
            enemyObject = Instantiate(enemyTemplate.Character, enemyCanvasRoot);
            enemyObject.name = $"{enemyTemplate.Name}_Lv{level}";

            var rectTransform = enemyObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localPosition = spawnPosition;
            }
            else
            {
                enemyObject.transform.localPosition = spawnPosition;
            }
        }
        else
        {
            Debug.LogWarning("[BattleManager] enemyCanvasRoot が設定されていません。Canvas ルートを割り当ててください。");
            enemyObject = Instantiate(enemyTemplate.Character, spawnPosition, Quaternion.identity);
            enemyObject.name = $"{enemyTemplate.Name}_Lv{level}";
        }

        var behavior = enemyObject.GetComponent<EnemyBehavior>();
        if (behavior != null)
        {
            behavior.EnemyData = runtimeEnemy;
        }

        SetEnemyVisual(enemyObject, enemyTemplate.DataIcon);
        spawnedEnemies.Add(enemyObject);
    }

    /// <summary>
    /// 敵リストからランダムで1体取得する
    /// </summary>
    private D_Ch_StatusData GetRandomEnemyData()
    {
        if (db_EnemyList.ItemList == null || db_EnemyList.ItemList.Count == 0)
            return null;

        int index = Random.Range(0, db_EnemyList.ItemList.Count);
        for (int i = 0; i < db_EnemyList.ItemList.Count; i++)
        {
            var enemy = db_EnemyList.ItemList[(index + i) % db_EnemyList.ItemList.Count];
            if (enemy != null)
                return enemy;
        }

        return null;
    }

    /// <summary>
    /// 敵の見た目をアイコンで設定できる場合にセットする
    /// </summary>
    private void SetEnemyVisual(GameObject enemyObject, Sprite icon)
    {
        if (enemyObject == null || icon == null)
            return;

        var spriteRenderer = enemyObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = icon;
            return;
        }

        var image = enemyObject.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = icon;
        }
    }

    /// <summary>
    /// 武器の見た目をアイコンで設定する
    /// </summary>
    private void SetWeaponVisual(GameObject weaponObject, Sprite icon)
    {
        if (weaponObject == null || icon == null)
            return;

        var spriteRenderer = weaponObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = icon;
            return;
        }

        var image = weaponObject.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = icon;
        }
    }

    /// <summary>
    /// 左クリックで魔法を生成して飛ばす
    /// </summary>
    private void CastMagic()
    {
        if (magicPrefab == null || magicMissile == null || magicSpawnPoint == null)
            return;

        var magicObject = Instantiate(magicPrefab, magicSpawnPoint.position, magicSpawnPoint.rotation);
        var magicUI = magicObject.GetComponent<MagicUI>();
        if (magicUI != null)
        {
            magicUI.ActivationSpell = magicMissile;
        }

        Destroy(magicObject, 5f);
    }

    /// <summary>
    /// プレイヤーが倒れたときに敵と武器を一掃してリセットする
    /// </summary>
    private IEnumerator ResetBattle()
    {
        isPlaying = false;
        stopScroll = true;

        ClearSpawnedEnemies();
        ClearSpawnedWeapons();

        progressDistance = 0f;
        UpdateProgressText();

        yield return new WaitForSeconds(1f);

        runtimePlayer = CreateRuntimePlayer();
        SpawnEquippedWeapons();

        stopScroll = false;
        isPlaying = true;
    }

    /// <summary>
    /// 生成済みの敵をすべて削除する
    /// </summary>
    private void ClearSpawnedEnemies()
    {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] != null)
            {
                Destroy(spawnedEnemies[i]);
            }
        }
        spawnedEnemies.Clear();
    }

    /// <summary>
    /// 生成済みの武器をすべて削除する
    /// </summary>
    private void ClearSpawnedWeapons()
    {
        for (int i = spawnedWeapons.Count - 1; i >= 0; i--)
        {
            if (spawnedWeapons[i] != null)
            {
                Destroy(spawnedWeapons[i]);
            }
        }
        spawnedWeapons.Clear();
    }

    /// <summary>
    /// 敵ステータスをテンプレートから生成し、レベルに応じて強化する
    /// </summary>
    private D_Ch_StatusData EnemyStatSettings(D_Ch_StatusData enemyData, int level)
    {
        var enemy = ScriptableObject.CreateInstance<D_Ch_StatusData>();
        enemy.Name = enemyData.Name;
        enemy.Id = enemyData.Id;
        enemy.Level = level;
        enemy.SeeAttribute = enemyData.SeeAttribute;
        enemy.DataIcon = enemyData.DataIcon;
        enemy.Event = enemyData.Event;
        enemy.Character = enemyData.Character;

        enemy.LevelStatus = new D_Ch_StatusData.LevelTable();
        if (enemyData.LevelStatus != null)
        {
            enemy.LevelStatus.MaxHpup = enemyData.LevelStatus.MaxHpup;
            enemy.LevelStatus.MaxMpup = enemyData.LevelStatus.MaxMpup;
            enemy.LevelStatus.Attackup = enemyData.LevelStatus.Attackup;
            enemy.LevelStatus.Magicup = enemyData.LevelStatus.Magicup;
            enemy.LevelStatus.Defenceup = enemyData.LevelStatus.Defenceup;
            enemy.LevelStatus.MagicDefenseup = enemyData.LevelStatus.MagicDefenseup;
            enemy.LevelStatus.Speedup = enemyData.LevelStatus.Speedup;
        }

        var baseHp = enemyData.MaxHp;
        var baseMp = enemyData.MaxMp;
        var baseAttack = enemyData.Attack;
        var baseMagic = enemyData.Magic;
        var baseDefense = enemyData.Defense;
        var baseMagicDefense = enemyData.MagicDefense;
        var baseSpeed = enemyData.Speed;

        var levelBonus = Mathf.Max(0, level - 1);
        enemy.MaxHp = baseHp + enemy.LevelStatus.MaxHpup * levelBonus;
        enemy.Hp = enemy.MaxHp;
        enemy.MaxMp = baseMp + enemy.LevelStatus.MaxMpup * levelBonus;
        enemy.Mp = enemy.MaxMp;
        enemy.Attack = baseAttack + enemy.LevelStatus.Attackup * levelBonus;
        enemy.Magic = baseMagic + enemy.LevelStatus.Magicup * levelBonus;
        enemy.Defense = baseDefense + enemy.LevelStatus.Defenceup * levelBonus;
        enemy.MagicDefense = baseMagicDefense + enemy.LevelStatus.MagicDefenseup * levelBonus;
        enemy.Speed = baseSpeed + enemy.LevelStatus.Speedup * levelBonus;

        enemy.CriticalRate = enemyData.CriticalRate;
        enemy.Accessories1 = enemyData.Accessories1;
        enemy.Accessories2 = enemyData.Accessories2;
        enemy.SkillList = enemyData.SkillList;
        enemy.ActiveBuffs = new List<ActiveBuff<D_Sk_StatusData>>(enemyData.ActiveBuffs ?? new List<ActiveBuff<D_Sk_StatusData>>());
        enemy.ActiveBuffs_It = new List<ActiveBuff<D_It_StatusData>>(enemyData.ActiveBuffs_It ?? new List<ActiveBuff<D_It_StatusData>>());
        enemy.DroppedItems = new List<D_Ch_StatusData.Droppedable>(enemyData.DroppedItems ?? new List<D_Ch_StatusData.Droppedable>());

        return enemy;
    }

    private class RuntimePlayerData
    {
        public int MaxHp;
        public int Hp;
        public int MaxMp;
        public int Mp;
        public int Attack;
        public int Magic;
        public int Defense;
        public int MagicDefense;
        public int Speed;
        public float CriticalRate;
    }

    /// <summary>
    /// 敵を倒した際などのランダムアイテムドロップ
    /// </summary>
    public bool Random_drop(D_It_StatusData dropItem, int rate)
    {
        if (dropItem == null) return false;
        if (db_PlayerItem == null) return false;

        int clampedRate = Mathf.Clamp(rate, 0, 100);
        bool isDropped = Random.Range(0, 100) < clampedRate;
        if (!isDropped) return false;

        D_It_StatusData existingItem = null;
        foreach (var owned in db_PlayerItem.ItemList)
        {
            if (owned == null) continue;
            if (owned == dropItem || owned.Id == dropItem.Id || owned.Name == dropItem.Name)
            {
                existingItem = owned;
                break;
            }
        }

        if (existingItem != null)
        {
            existingItem.Number += 1;
            Debug.Log($"アイテムドロップ: {dropItem.Name} を追加 ({dropItem.Number}個目)");
        }
        else
        {
            if (dropItem.Number <= 0)
            {
                dropItem.Number = 1;
            }
            else
            {
                dropItem.Number += 1;
            }
            db_PlayerItem.ItemList.Add(dropItem);
            Debug.Log($"アイテムドロップ: {dropItem.Name} を新規入手 ({dropItem.Number}個)");
        }

        return true;
    }

    // プレイヤーにダメージを与える。EnemyBehavior から呼ばれる。
    public void ApplyPlayerDamage(int amount, GameObject damagedObject = null)
    {
        if (runtimePlayer == null)
            return;

        runtimePlayer.Hp = Mathf.Max(0, runtimePlayer.Hp - amount);

        if (damagedObject != null)
        {
            var popup = damagedObject.GetComponent<DamagePopup>();
            if (popup != null)
            {
                popup.ShowDamage(amount);
            }
        }
    }

    // 敵にダメージを与える。BattleUI から呼ばれる。
    public void ApplyEnemyDamage(int amount, GameObject damagedObject = null)
    {
        if (damagedObject == null)
            return;

        var enemyBehavior = damagedObject.GetComponent<EnemyBehavior>();
        if (enemyBehavior == null || enemyBehavior.EnemyData == null)
            return;

        enemyBehavior.EnemyData.Hp = Mathf.Max(0, enemyBehavior.EnemyData.Hp - amount);

        var popup = damagedObject.GetComponent<DamagePopup>();
        if (popup != null)
        {
            popup.ShowDamage(amount);
        }
    }
}
