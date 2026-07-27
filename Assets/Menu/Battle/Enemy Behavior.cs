using App.BaseSystem.DataStores.ScriptableObjects.Status;
using UnityEngine;

public class EnemyBehavior : MonoBehaviour
{
    [Header("敵データ")]
    [SerializeField] public D_Ch_StatusData EnemyData;

    [Header("プレイヤー接触判定")]
    //[SerializeField] private string playerTag = "Player"; // プレイヤーに設定するタグ
    [SerializeField] private float damageInterval = 1f; // ダメージ判定の間隔

    private BattleManager battleManager;
    private bool isTouchingPlayer;
    private float damageTimer;
    private RectTransform myRectTransform;
    private RectTransform playerRectTransform;
    private GameObject playerGameObject;
    private bool useUIOverlap = false; // true when both this and player are UI RectTransforms

    private void Awake()
    {
        battleManager = FindAnyObjectByType<BattleManager>();

        myRectTransform = GetComponent<RectTransform>();
        playerGameObject = GameObject.FindWithTag("Player");
        if (playerGameObject != null)
        {
            playerRectTransform = playerGameObject.GetComponent<RectTransform>();
        }

        useUIOverlap = (myRectTransform != null && playerRectTransform != null);

        if (EnemyData != null)
        {
            // 生成直後にHPを最大値に揃える
            EnemyData.Hp = Mathf.Clamp(EnemyData.Hp, 0, EnemyData.MaxHp);
            if (EnemyData.Hp == 0)
            {
                EnemyData.Hp = EnemyData.MaxHp;
            }
        }
    }

    private void Update()
    {
        if (EnemyData == null)
            return;

        if (EnemyData.Hp <= 0)
        {
            Die();
            return;
        }

        // UI の場合は矩形重なり判定で接触を判断
        if (useUIOverlap)
        {
            bool overlap = RectTransformOverlaps(myRectTransform, playerRectTransform);
            if (overlap)
            {
                if (!isTouchingPlayer)
                {
                    isTouchingPlayer = true;
                    damageTimer = 0f;
                }
            }
            else
            {
                isTouchingPlayer = false;
            }
        }

        // プレイヤーと接触中は停止し、定期的にダメージを与える
        if (isTouchingPlayer)
        {
            damageTimer -= Time.deltaTime;
            if (damageTimer <= 0f)
            {
                AttackPlayer();
                damageTimer = damageInterval;
            }
        }
        else
        {
            MoveForward();
        }
    }

    // 敵を左に移動させる
    private void MoveForward()
    {
        transform.position += Vector3.left * EnemyData.Speed * Time.deltaTime;
    }

    // プレイヤーにダメージを与える
    private void AttackPlayer()
    {
        if (battleManager == null)
            return;

        int damage = Mathf.Max(1, EnemyData.Attack);
        battleManager.ApplyPlayerDamage(damage, playerGameObject);
    }

    // 敵の HP が 0 になったときの処理
    private void Die()
    {
        if (battleManager != null && EnemyData?.DroppedItems != null)
        {
            foreach (var drop in EnemyData.DroppedItems)
            {
                if (drop == null || drop.DroppedItem == null)
                    continue;

                battleManager.Random_drop(drop.DroppedItem, drop.Droprate);
            }
        }

        Destroy(gameObject);
    }

    // プレイヤーに触れたら停止してダメージ判定を開始する
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            // 物理判定のフォールバック
            playerGameObject = other.gameObject;
            isTouchingPlayer = true;
            damageTimer = 0f;
        }
    }

    // プレイヤーから離れたら再び前進する
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            isTouchingPlayer = false;
        }
    }

    // RectTransform のワールドコーナーからスクリーン領域の Rect を作成して重なり判定
    private bool RectTransformOverlaps(RectTransform a, RectTransform b)
    {
        if (a == null || b == null) return false;

        Vector3[] aCorners = new Vector3[4];
        Vector3[] bCorners = new Vector3[4];
        a.GetWorldCorners(aCorners);
        b.GetWorldCorners(bCorners);

        var aMin = aCorners[0];
        var aMax = aCorners[2];
        var bMin = bCorners[0];
        var bMax = bCorners[2];

        Rect rectA = new Rect(aMin.x, aMin.y, aMax.x - aMin.x, aMax.y - aMin.y);
        Rect rectB = new Rect(bMin.x, bMin.y, bMax.x - bMin.x, bMax.y - bMin.y);

        return rectA.Overlaps(rectB);
    }
}
