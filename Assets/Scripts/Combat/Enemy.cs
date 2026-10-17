using System;
using ArtifactDesigner.Core;

namespace ArtifactDesigner.Combat
{
    /// <summary>
    /// 敌人基础模型（纯逻辑层）。
    /// 打怪是部件的主要获取途径：击杀敌人后按 DropTable 掉落炼器部件。
    /// </summary>
    [Serializable]
    public class Enemy
    {
        public string enemyId;      // 唯一 ID
        public string enemyName;    // 名称（如：山精、妖狐、魔修）
        public int level;           // 等级/层数，影响数值与掉落档次
        public int maxHp;           // 生命上限
        public int attack;          // 攻击力
        public float speed;         // 速度（决定先手）
        public int expReward;       // 击杀经验奖励（预留，配合境界成长）
        public DropTable dropTable; // 击杀掉落表；为空则不掉落部件

        public Enemy() { }

        public Enemy(string id, string name, int level, int maxHp, int attack, float speed, int exp)
        {
            enemyId = id;
            enemyName = name;
            this.level = level;
            this.maxHp = maxHp;
            this.attack = attack;
            this.speed = speed;
            expReward = exp;
        }

        /// <summary>是否配置了掉落表。</summary>
        public bool HasDrops => dropTable != null && dropTable.entries.Count > 0;
    }
}