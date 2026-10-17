using UnityEngine;
using ArtifactDesigner.Core;
using ArtifactDesigner.Entity;

namespace ArtifactDesigner
{
    /// <summary>
    /// 玩家输入控制器（旧 Input Manager）：
    /// - WASD / 方向键移动；
    /// - 鼠标右键点击地面移动（0.0.1 采用 Ray + y=0 平面求交，无需 Physics 碰撞体）；
    /// - Q/W/E/R 技能键（0.0.1 仅占位触发事件，技能系统后续版本接入）；
    /// - F1 选中自己（占位，连招/自我目标后续接入）。
    /// 输入系统切换（新 Input System）在后续版本支持。
    /// </summary>
    [RequireComponent(typeof(PlayerEntity))]
    public class PlayerController : MonoBehaviour
    {
        private PlayerEntity player;

        private void Awake()
        {
            player = GetComponent<PlayerEntity>();
        }

        private void Update()
        {
            if (player == null || !player.IsAlive) return;
            if (GameManager.Instance != null &&
                GameManager.Instance.State != GameState.WaveActive &&
                GameManager.Instance.State != GameState.Preparing)
            {
                // 结算/失败状态禁止操作
                return;
            }

            HandleMoveInput();
            HandleSkillInput();
            HandleTerrainCollectInput();
        }

        private void HandleMoveInput()
        {
            // WASD 直接移动（覆盖寻路目标）
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                Vector3 dir = new Vector3(h, 0f, v).normalized;
                player.transform.position += dir * (player.moveSpeed * Time.deltaTime);
                player.moveToTarget = false;
                return;
            }

            // 鼠标右键点击地面移动
            if (Input.GetMouseButton(1))
            {
                if (Camera.main != null)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    // 与 y=0 平面求交：t = -origin.y / direction.y
                    if (Mathf.Abs(ray.direction.y) > 0.0001f)
                    {
                        float t = -ray.origin.y / ray.direction.y;
                        if (t > 0f)
                        {
                            Vector3 hit = ray.origin + ray.direction * t;
                            player.moveTarget = hit;
                            player.moveToTarget = true;
                        }
                    }
                }
            }
        }

        private void HandleSkillInput()
        {
            // 0.0.1 仅占位：技能系统在 0.0.6 接入，这里保留统一入口
            if (Input.GetKeyDown(KeyCode.Q)) { GameEvents.RaiseSkillPressed(0); }
            if (Input.GetKeyDown(KeyCode.W)) { GameEvents.RaiseSkillPressed(1); }
            if (Input.GetKeyDown(KeyCode.E)) { GameEvents.RaiseSkillPressed(2); }
            if (Input.GetKeyDown(KeyCode.R)) { GameEvents.RaiseSkillPressed(3); }
            if (Input.GetKeyDown(KeyCode.F1)) { GameEvents.RaiseSelfSelected(); }
        }

        // ==================== 0.0.9 强 A 收取 ====================

        /// <summary>
        /// 强 A 收取逻辑：
        ///   - 按住 A（或 Attack 键）+ 鼠标左键点击己方地形 → 收取（破碎并进入冷却）；
        ///   - A + 空地 = 走 A（当前仅取消寻路，由 AutoAttack 接管）；
        ///   - A + 己方单位无伤害。
        /// 这里使用 LeftShift + 鼠标左键作为强 A 组合（避免与普攻冲突）。
        /// </summary>
        private void HandleTerrainCollectInput()
        {
            // 组合键：LeftShift + 鼠标左键
            if (!Input.GetKey(KeyCode.LeftShift)) return;
            if (!Input.GetMouseButtonDown(0)) return;

            if (Camera.main == null) return;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            // 与 y=0 平面求交
            if (Mathf.Abs(ray.direction.y) < 0.0001f) return;
            float t = -ray.origin.y / ray.direction.y;
            if (t <= 0f) return;
            Vector3 clickPos = ray.origin + ray.direction * t;

            // 优先检测是否点到了己方地形（射线检测替代方案：直接查最近地形）
            var tm = TerrainManager.Instance;
            if (tm == null) return;

            // 收取玩家自己创建的地形（如果存在）
            bool collected = tm.CollectNearestTerrain(player, clickPos, maxRange: 4f);
            if (collected)
            {
                // 收取成功：取消寻路，避免误移动
                player.moveToTarget = false;
            }
            else
            {
                // A + 空地 = 走 A：取消寻路，交由 AutoAttack 处理
                player.moveToTarget = false;
            }
        }
    }
}