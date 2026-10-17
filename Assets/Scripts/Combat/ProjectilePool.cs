using System.Collections.Generic;
using UnityEngine;
using ArtifactDesigner.Entity;

namespace ArtifactDesigner.Combat
{
    /// <summary>
    /// 投射物对象池（0.0.9）。
    /// 管理投射物 GameObject 的复用，避免战斗中频繁 Instantiate/Destroy。
    /// 按来源实体分组维护，回池时重置所有运行时状态。
    /// </summary>
    public class ProjectilePool : MonoBehaviour
    {
        public static ProjectilePool Instance { get; private set; }

        [Header("预制体")]
        public GameObject projectilePrefab; // 若为空则回退到 Instantiate Primitive

        private readonly Queue<Projectile> pool = new Queue<Projectile>();
        private int _spawnedCount = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 从池中取出一个投射物，若无则新建。
        /// 调用者负责 Setup。
        /// </summary>
        public Projectile Get(Vector3 position, Quaternion rotation)
        {
            Projectile proj = null;
            while (pool.Count > 0)
            {
                proj = pool.Dequeue();
                if (proj != null && proj.gameObject != null) break;
                proj = null;
            }

            if (proj == null)
            {
                GameObject go = CreateNewProjectileGo(position, rotation);
                proj = go.GetComponent<Projectile>();
                if (proj == null) proj = go.AddComponent<Projectile>();
                _spawnedCount++;
            }
            else
            {
                proj.transform.position = position;
                proj.transform.rotation = rotation;
                proj.gameObject.SetActive(true);
            }

            proj.ResetState();
            return proj;
        }

        /// <summary>将投射物回收到池中。自动重置状态并隐藏。</summary>
        public void Return(Projectile proj)
        {
            if (proj == null || proj.gameObject == null) return;
            proj.ResetState();
            proj.gameObject.SetActive(false);
            pool.Enqueue(proj);
        }

        /// <summary>清空池子（关卡切换等场景）。</summary>
        public void Clear()
        {
            while (pool.Count > 0)
            {
                var p = pool.Dequeue();
                if (p != null && p.gameObject != null) Destroy(p.gameObject);
            }
            pool.Clear();
            _spawnedCount = 0;
        }

        private GameObject CreateNewProjectileGo(Vector3 position, Quaternion rotation)
        {
            if (projectilePrefab != null)
            {
                return Instantiate(projectilePrefab, position, rotation);
            }
            // 回退：创建最小可见占位体
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile_" + _spawnedCount;
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = Vector3.one * 0.3f;
            Destroy(go.GetComponent<Collider>()); // 碰撞由 CollisionManager 管理
            return go;
        }
    }
}
