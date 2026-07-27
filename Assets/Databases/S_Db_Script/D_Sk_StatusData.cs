using NUnit.Framework.Interfaces;
using System.Collections.Generic;
using UnityEngine;


namespace App.BaseSystem.DataStores.ScriptableObjects.Status
{
    [CreateAssetMenu(menuName = "ScriptableObject/Data/SkillStatus")]
    public class D_Sk_StatusData : BaseData
    {
        [TextArea, Header("効果説明")]
        public string EfficacyItemDescription;

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

        public enum SkillRange
        {
            Himself,      // 自分
            Single_enemy, // 敵単体
            All_enemies   // 敵全体 
        }
        public SkillRange SeeSkillRange//参照時はこれを呼ぶ
        {
            get => skillRange;
            set => skillRange = value;
        }
        [SerializeField, Header("スキル範囲")]
        private SkillRange skillRange;

        public enum Kinds
        {
            //アタック、ディフェンス、ファスト、スロー、クイック、回復、バフ、デバフ、アビリティ
            Attack,
            Defense,
            Fast,
            slow,
            Quick,
            Recovery,
            Buff,
            DeBuff,
            Abilities
        }
        public Kinds SeeKinds//参照時はこれを呼ぶ
        {
            get => kind;
            set => kind = value;
        }
        [SerializeField, Header("種類")]
        private Kinds kind;

        public enum Buff_DeBuff_Kinds// バフ・デバフ対象ステータス
        {
            None,
            Attack,
            Magic,
            Defense,
            MagicDefense,
            Speed,
            Critical
        }
        public Buff_DeBuff_Kinds SeeBuff_DeBuff_Kinds//参照時はこれを呼ぶ
        {
            get => buff_DeBuff_kind;
            set => buff_DeBuff_kind = value;
        }
        [SerializeField, Header("バフ対象ステータス")]
        private Buff_DeBuff_Kinds buff_DeBuff_kind;

        public float Efficacy1
        {
            get => efficacy1;
            set => efficacy1 = value;
        }
        [SerializeField, Header("威力、効力１")]
        private float efficacy1;
        public float Efficacy2
        {
            get => efficacy2;
            set => efficacy2 = value;
        }
        [SerializeField, Header("威力、効力２")]//効果が複数ある場合
        private float efficacy2;


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

        public int Duration
        {
            get => duration;
            set => duration = value;
        }
        [SerializeField, Header("効果時間")]
        private int duration;

        public int MpConsumption
        {
            get => mpConsumption;
            set => mpConsumption = value;
        }
        [SerializeField, Header("MP消費量")]
        private int mpConsumption;


        [SerializeField, Header("アイコン")]
        public GameObject Icon;

        [SerializeField, Header("データアイコン画像")]
        public Sprite DataIcon;

        [SerializeField, Header("バトル用アイコン")]
        public GameObject B_Icon;

        [SerializeField, Header("エフェクト")]
        public GameObject Effect;

        [SerializeField, Header("効果音")]
        public AudioClip SoundEffects;
    }
}
