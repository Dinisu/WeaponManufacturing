using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using UnityEngine;


namespace App.BaseSystem.DataStores.ScriptableObjects.Status
{
    [CreateAssetMenu(menuName = "ScriptableObject/Data/EquipmentStatus")]
    public class D_Eq_StatusData : BaseData
    {
        public Rarity SeeRarity
        {
            get => rarity;
            set => rarity = value;
        }
        [SerializeField, Header("レア度")]
        private Rarity rarity;

        public enum Attribute
        {
            //上から無、火、水、木、金、土
            None,
            Fire,
            Water,
            Tree,
            Metal,
            Soil,
        }
        public Attribute SeeAttribute//参照時はこれを呼ぶ
        {
            get => attribute;
            set => attribute = value;
        }
        [SerializeField, Header("属性")]
        private Attribute attribute;
        public int MaxHp
        {
            get => maxhp;
            set => maxhp = value;
        }
        [SerializeField, Header("最大耐久度")]
        private int maxhp;

        public int Hp
        {
            get => hp;
            set => hp = Mathf.Clamp(value, 0, maxhp); // 0 <= hp <= maxhp
        }
        //[HideInInspector]
        [SerializeField]
        private int hp;
        public int MaxMp
        {
            get => maxmp;
            set => maxmp = value;
        }
        [SerializeField, Header("最大MP")]
        private int maxmp;

        public int Mp
        {
            get => mp;
            set => mp = Mathf.Clamp(value, 0, maxmp); // 0 <= hp <= maxmp
        }
        [SerializeField]
        private int mp;

        public int Attack
        {
            get => attack;
            set => attack = value;
        }
        [SerializeField, Header("攻撃力")]
        private int attack;

        public int Magic
        {
            get => magic;
            set => magic = value;
        }
        [SerializeField, Header("魔力")]
        private int magic;

        public int Defense
        {
            get => defense;
            set => defense = value;
        }
        [SerializeField, Header("防御力")]
        private int defense;

        public int MagicDefense
        {
            get => magicDefense;
            set => magicDefense = value;
        }
        [SerializeField, Header("魔法防御力")]
        private int magicDefense;

        public int Speed
        {
            get => speed;
            set => speed = value;
        }
        [SerializeField, Header("速度")]
        private int speed;

        public float CriticalRate
        {
            get => criticalRate;
            set => criticalRate = value;
        }
        [SerializeField, Header("クリティカル率")]
        private float criticalRate;

        [SerializeField, Header("アクセサリー1")]
        public D_It_StatusData Accessories1;
        [SerializeField, Header("アクセサリー2")]
        public D_It_StatusData Accessories2;


        [SerializeField, Header("付与スキルリスト(エンチャント)")]
        public List<D_Sk_StatusData> SkillList = new List<D_Sk_StatusData>();

        [SerializeField, Header("受けているスキルバフ")]
        public List<ActiveBuff<D_Sk_StatusData>> ActiveBuffs = new();//スキルバフのデータ管理
        
        [SerializeField, Header("受けているアイテムバフ")]
        public List<ActiveBuff<D_It_StatusData>> ActiveBuffs_It = new();//アイテムバフのデータ管理

        public int Price
        {
            get => price;
            set => price = value;
        }
        [SerializeField, Header("値段")]
        private int price;

        public enum Abnormalstatus
        {
            None
        }

        [SerializeField, Header("データアイコン画像")]
        public Sprite DataIcon;

        [SerializeField, Header("外見")]
        public GameObject Character;
        public GameObject B_Icon;
        // エンチャント解除時に元の値へ戻せるよう、武器作成時の能力値を一度だけ保存する。
        [SerializeField, HideInInspector] private bool hasOriginalStats;
        [SerializeField, HideInInspector] private int originalMaxHp;
        [SerializeField, HideInInspector] private int originalHp;
        [SerializeField, HideInInspector] private int originalMaxMp;
        [SerializeField, HideInInspector] private int originalMp;
        [SerializeField, HideInInspector] private int originalAttack;
        [SerializeField, HideInInspector] private int originalMagic;
        [SerializeField, HideInInspector] private int originalDefense;
        [SerializeField, HideInInspector] private int originalMagicDefense;
        [SerializeField, HideInInspector] private int originalSpeed;
        [SerializeField, HideInInspector] private float originalCriticalRate;

        /// <summary>武器作成時の能力値を保存する。既存値は上書きしない。</summary>
        public void SaveOriginalStats()
        {
            if (hasOriginalStats) return;

            originalMaxHp = MaxHp;
            originalHp = Hp;
            originalMaxMp = MaxMp;
            originalMp = Mp;
            originalAttack = Attack;
            originalMagic = Magic;
            originalDefense = Defense;
            originalMagicDefense = MagicDefense;
            originalSpeed = Speed;
            originalCriticalRate = CriticalRate;
            hasOriginalStats = true;
        }

        /// <summary>保存した能力値へ、スキルリスト内のバフ倍率を適用し直す。</summary>
        public void RecalculateEnchantedStats()
        {
            //SaveOriginalStats();

            float attackMultiplier = 1f;
            float magicMultiplier = 1f;
            float defenseMultiplier = 1f;
            float magicDefenseMultiplier = 1f;
            float speedMultiplier = 1f;
            float criticalMultiplier = 1f;

            if (SkillList != null)
            {
                foreach (D_Sk_StatusData skill in SkillList)
                {
                    if (skill == null || skill.SeeKinds != D_Sk_StatusData.Kinds.Buff) continue;

                    switch (skill.SeeBuff_DeBuff_Kinds)
                    {
                        case D_Sk_StatusData.Buff_DeBuff_Kinds.Attack: attackMultiplier *= skill.Efficacy1; break;
                        case D_Sk_StatusData.Buff_DeBuff_Kinds.Magic: magicMultiplier *= skill.Efficacy1; break;
                        case D_Sk_StatusData.Buff_DeBuff_Kinds.Defense: defenseMultiplier *= skill.Efficacy1; break;
                        case D_Sk_StatusData.Buff_DeBuff_Kinds.MagicDefense: magicDefenseMultiplier *= skill.Efficacy1; break;
                        case D_Sk_StatusData.Buff_DeBuff_Kinds.Speed: speedMultiplier *= skill.Efficacy1; break;
                        case D_Sk_StatusData.Buff_DeBuff_Kinds.Critical: criticalMultiplier *= skill.Efficacy1; break;
                    }
                }
            }

            Attack = Mathf.RoundToInt(originalAttack * attackMultiplier);
            Magic = Mathf.RoundToInt(originalMagic * magicMultiplier);
            Defense = Mathf.RoundToInt(originalDefense * defenseMultiplier);
            MagicDefense = Mathf.RoundToInt(originalMagicDefense * magicDefenseMultiplier);
            Speed = Mathf.RoundToInt(originalSpeed * speedMultiplier);
            CriticalRate = originalCriticalRate * criticalMultiplier;
        }
    }
}