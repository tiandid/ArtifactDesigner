# 仙器设计师 — 版本迭代计划（0.0.1 → 0.1.0）

> 本文档按当前代码实际状态整理，已完成功能与待办项均与源码保持一致。

---

## 0.0.1 — 核心框架与占位场景 ✅ 已完成

**目标**：搭建可运行的 Unity 场景骨架，落地炼器与战斗的纯逻辑层。

### 已完成功能
| 模块 | 内容 |
|------|------|
| **场景搭建** | GameManager 占位 Primitive 场景：地面、水晶（防守目标）、玩家 Cube、摄像机、UI 管理器。 |
| **器坯系统** | ArtifactBlank：格子挂载/卸下/查询、重量公式、自定义 Inspector（ArtifactBlankEditor）。 |
| **部件系统** | ArtifactPart 七大类（AddSlot / Durability / Attack / Speed / ActiveSkill / PassiveSkill / SpiritCore）；DemoParts 工厂；SkillDefinition 数据与深拷贝。 |
| **能量线路** | EnergyNetwork：节点+无向线路、共享电网、供电分配（充足全生效/不足按 ID 优先）、动态增删断电。 |
| **战斗结算（纯逻辑）** | CombatSystem：回合制模拟（速度先手、60 回合上限）、击杀掉落 / 失败不掉落；DropTable / DropEntry 独立掷骰。 |
| **伤害公式** | FormulaHelper：攻击×波动×100/(100+防御)，最小 1；玩家/小怪属性换算（体→攻防血速）。 |
| **经验与境界** | ExpSystem：10 境界（凡人→渡劫）、升级公式 `560×等级×境界常数`、境界查询。 |
| **波次框架** | WaveManager：10 波计划 `{1,3,5,10,15,30,30,30,30,30}`，但 `maxWave = 1`（仅开放第 1 波）。 |
| **实体基类** | BattleEntity（血/攻/防/攻速/移速/攻击范围）；PlayerEntity、EnemyEntity、CrystalEntity 继承。 |
| **投射物占位** | Projectile：直线直射 P-01，命中实体结算伤害，生命周期 4 秒。 |
| **敌人 AI 基础** | EnemyAI：状态机（行进→攻击），优先追玩家、玩家过远/死亡则打水晶；SimplePathfinder 避障。 |
| **测试与编译** | LogicTests（20 项 PASS）、UnityCheck（桩编译验证）。 |

### 已知限制
- 掉落表在 WaveEnemyFactory 中为 `null`，战斗掉落未实际接入波次敌人。
- 波次仅开放第 1 波，通关即结束。
- 投射物仅为直线直射，无技能系统接入。

---

## 0.0.2 — 局内系统与任务链基础 ✅ 已完成

**目标**：引入玩家成长档案、任务引导、事件总线，打通局内局外数据流。

### 已完成功能
| 模块 | 内容 |
|------|------|
| **玩家档案** | PlayerProfile：等级/经验/灵石、三维属性（体/气/魂）、局内实时 HP/蓝量、死亡统计、局外属性成长公式（凡人境每级+1，练气境每级+2）。 |
| **任务系统** | QuestSystem：Q001~Q005 基础任务链 + B003 突破任务；波次自动接取（Q001→波1、Q004→波3、Q003→波4）；Q005 完美首关静默接取。 |
| **气泡引导** | BubbleSystem：屏幕底部中央气泡（White/Red/Gold/Blue），最大 2 条，点击 dismiss，OnGUI 实现。 |
| **全局事件** | GameEvents：实体生命周期、波次、任务、经验/等级、输入占位等静态事件总线。 |
| **胜负判定** | GameManager：水晶被毁→Defeat；最终波清波→Victory；Q005 按死亡数结算。 |
| **实体完善** | PlayerEntity：右击寻路+WASD、自动普攻、死亡复活（3 秒）、锁定目标、等级升级实时刷新属性；EnemyEntity：纯逻辑模型接入、死亡广播。 |
| **UI 管理器** | UIManager：单例挂载占位。 |
| **控制器** | PlayerController、ArtifactBlankController。 |

### 已知限制
- Q001~Q005 经验奖励为静态配置值（尚未接入动态公式）。
- B003 突破任务未绑定实际突破流程。

---

## 0.0.3 — 突破系统与任务链扩展 ✅ 已完成

**目标**：落地双轨突破管理器，扩展新手任务链至完整闭环，引入动态经验公式。

### 已完成功能
| 模块 | 内容 |
|------|------|
| **双轨突破管理器** | BreakthroughManager：资源轨（先蓝后血，蓝空扣血保留 1 点）+ 时间轨（倒计时=魂属性）；九品→一品 9 条进度条 + 超品阈值 7649；固定充能 15 灵气/秒；蓝条自然回复 5+聚灵阵加成。 |
| **突破状态** | PlayerProfile：IsPendingBreakthrough、CurrentBreakthroughQuestId、LastBreakthroughRank、CompleteBreakthrough() 应用永久属性奖励。 |
| **永久属性加成** | PermanentStats：突破奖励系数（护体/真气/凝实度/物理攻防/属性攻防/精神攻防），按品级累加。 |
| **任务链扩展** | QuestSystem 新增：Q006（初次炼器）→ Q009（器胚装配）→ Q010（推进器装配）→ Q007（熔炼技能结构）→ Q012（最终试炼）；Q008（设计成型）→ Q013（首件仙器）；B003（结丹突破）。 |
| **触发接口** | OnFirstCraftCompleted()、OnCoreEmbeded()、OnThrusterLinkCompleted()、OnFinalBossDefeated()、OnArtifactAssemblyConfirmed() 等（由炼器/战斗系统调用）。 |
| **动态经验公式** | Q 系列任务运行时计算：`Max(1, 升级所需经验×0.1 + 25×关卡数×(聚灵阵值+1))`。B003 仍读配置表。 |
| **聚灵阵占位** | PlayerProfile.CultivationArrayValue（默认 0，等聚灵阵系统注入）；GameManager.CurrentLevel（默认 1）。 |
| **波次绑定调整** | Q003 移至第 4 波，避免与 Q006 炼器教学冲突。 |

### 已知限制
- 聚灵阵值为 0（未接入真实系统），公式中通过 `+1` 保证最低 1 倍增幅。
- 炼器触发接口（OnFirstCraftCompleted 等）由外部系统调用，但炼器 UI 尚未闭环对接。
- 第 10 波 Boss 未配置（WavePlan 仍为 10 波小怪，无 Boss 数据）。
- maxWave 仍为 1，Q012 最终试炼需打到第 10 波，当前无法完整体验。

---

## 0.0.4 — 炼器 UI 闭环 ✅ 已完成

**目标**：将任务触发接口与实际炼器操作对接，完成新手教学可交互闭环。

### 已完成功能
| 模块 | 内容 |
|------|------|
| **炼器系统核心** | `ArtifactCraftingSystem.cs`：单例 MonoBehaviour，管理材料选择（五行）、炼制倒计时、灵核/推进器/技能结构产出、供能线连接（EnergyNetwork 接入）、契合度计算。 |
| **最简炼器 UI** | `SimpleCraftUI.cs`：OnGUI 实现，C 键开关，按当前进行中的炼器任务自动切换面板（Q006→Q009→Q010→Q007→Q002→Q013→Q008），按钮直接调用 ArtifactCraftingSystem 方法。 |
| **触发接口闭环** | QuestSystem 7 个触发接口（`OnFirstCraftCompleted` / `OnCoreEmbeded` / `OnThrusterLinkCompleted` / `OnArtifactAssemblyConfirmed` / `OnArtifactAssembled` / `OnArtifactCrafted` / `OnFinalBossDefeated`）均已由 ArtifactCraftingSystem / GameManager 正确调用。 |
| **GameManager 初始化** | `InitializeSubsystems()` 在 Awake 中自动挂载 ArtifactCraftingSystem + SimpleCraftUI。 |
| **Q003 波次修正** | `GameManager.NotifyEnemyKilled` 中 Q003 的 waveIndex 判定从 2 修正为 4，与 WaveAcceptMap 一致。 |

### 任务链调用验证
```
Q006(初次炼器) → SelectMaterial+ConfirmCraft → OnFirstCraftCompleted → 完成 → 自动接 Q009
Q009(器胚装配) → EmbedCore → OnCoreEmbeded → 完成 → 自动接 Q010
Q010(推进器装配) → InstallThrusters+ConnectEnergyLine → OnThrusterLinkCompleted → 完成 → 自动接 Q007+Q002
Q007(熔炼技能结构) → CraftSkillStructure → AddProgress(Q007) → 完成 → 自动接 Q012
Q002(确认装配) → ConfirmAssembly → OnArtifactAssemblyConfirmed → 完成
Q008(设计成型) → OnArtifactAssembled → 完成 → 自动接 Q013
Q013(首件仙器) → CraftArtifact → OnArtifactCrafted → 完成
```

### 已知限制
- `CraftSkillStructure` 中 Q007 的完成仅校验熔炼动作，"第三波存活"条件尚未接入（需 0.0.5 波次开放后补充）。
- Q007 气泡文案提到"白升绿"，但 `GameEnums.Rarity` 仍为白/蓝/紫/金/橙/红，绿色档位待决策。
- 完整美术 UI 可后续迭代替换当前的 OnGUI 占位实现。

---

## 0.0.5 — 灵石经济、契合度评级与波次开放 ✅ 已完成

**目标**：开放全部10波，建立灵石收支闭环与法宝契合度品质进阶体系。

### 已完成功能
| 模块 | 内容 |
|------|------|
| **波次全开放** | `WaveManager.maxWave` 从 `1` 改为 `10`，10波计划 `{1,3,5,10,15,30,30,30,30,30}` 全部可用。 |
| **灵石经济系统** | `SpiritEconomySystem.cs`：收入（击杀掉落随波次/品质递增、任务奖励、通关奖励按评分档位、出售部件/仙器按价值×30%回收）；支出（购买五行材料按品质计价、修复耐久、聚灵阵扩展格位成本递增、境界突破消耗）。所有收支通过PlayerProfile统一记账，附带OnBalanceChanged/OnIncome/OnExpense事件。 |
| **契合度评级体系** | `ArtifactCompatibilitySystem.cs`：6档品质进阶（白→蓝→紫→金→橙→红），每档对器胚大小、灵核品质、输出结构数量/类型/品质、供能线覆盖度有递进要求；高级兼容低级（蓝可用白灵核/结构）；10种法宝类型各有理想结构配置（剑1攻1主/钟2被/环1攻2主等）；结构匹配度评分+超额惩罚；最终契合度百分比=器胚20%+灵核25%+结构35%+供能20%。 |
| **品质进阶触发** | `ArtifactCraftingSystem.CraftArtifact()` 与 `ConfirmAssembly()` 中接入 `ArtifactCompatibilitySystem.TryUpgradeQuality()`，炼制/确认时自动判定并提升器胚 `rarity`。 |
| **GameManager 初始化** | `InitializeSubsystems()` 中挂载 `SpiritEconomySystem` + `ArtifactCompatibilitySystem`。 |

### 契合度档位配置（供策划调参）
| 档位 | 器胚最小格 | 灵核最低品质 | 输出结构数 | 需供能线 | 供能覆盖 |
|------|-----------|-------------|-----------|---------|---------|
| 白(Common) | 1 | 白(0) | 0 | 否 | 0% |
| 蓝(Uncommon) | 3 | 蓝(1) | 1 | 否 | 0% |
| 紫(Rare) | 5 | 紫(2) | 2 | 是 | ≥50% |
| 金(Epic) | 7 | 金(3) | 3 | 是 | ≥70% |
| 橙(Legendary) | 9 | 橙(4) | 4 | 是 | ≥85% |
| 红(Mythic) | 12 | 红(5) | 5 | 是 | 100% |

### 已知限制
- 第10波Boss数据、评分系统、通关评分仍待后续版本实现。
- 准备时间/波间隔仍为占位值（3秒/5秒），待调参。
- `GameEnums.Rarity` 仍为白/蓝/紫/金/橙/红，Q007"白升绿"气泡文案中绿色档位未纳入。

---

## 0.0.6 — Boss 配置、评分系统与局内计分板 ✅ 已完成

**目标**：加入第10波Boss，落地评分体系与局内计分板，打通第一关完整可玩流程。

### 已完成功能
| 模块 | 内容 |
|------|------|
| **Boss 配置** | `BossConfig.cs`：第10波Boss数据（血量=小怪×3、攻击×2.5），`WaveManager` 第10波特殊生成逻辑（最后一只替换为Boss）。 |
| **Q012 结算** | Boss 死亡时 `QuestSystem.OnFinalBossDefeated()` 自动完成最终试炼。 |
| **敌人特性扩展** | `EnemyEntity.cs` + `EnemyAI.cs` 接入“分身”与“真·分身1-10”特性：开战/受击消耗10%生命造幻影分身；真·分身1-10级具备本体10%~100%攻击力。`GameEnums.cs` 新增 `EnemyTrait` 枚举（含分身/真分身/体双倍/气双倍/神双倍/攻速三倍/范围三倍/元素免伤/体攻免伤/元攻免伤/魂体/入魔/执念/锁水晶）。 |
| **契合度调整** | `ArtifactCompatibilitySystem` 五行相生契合度设定为100%（同属性100%/相生100%/相克50%/无关60%）；不加耐久度与灵气浓度项。 |
| **局内计分板** | `ScoreSystem` + `ScoreBoardUI`：实时统计伤害/承伤/治疗/击杀/死亡/参与击杀；两列布局、按总分排序、自身行高亮；数值超万/亿/兆中文单位折算。 |
| **伤害分解悬停** | 悬停自身伤害总值弹出技能/普攻分解柱状图；最高项100%，其余按最高项比例定长。 |
| **通关评分** | `ScoreSystem.CalculateFinalScore`：F~SSS 共9档，满分10000；7项加权（参与击杀0-2000 / 对敌伤害0-2000 / 承伤0-2000 / 治疗0-1000 / 支线0-1000 / 复活-1000~+1000 / 水晶血量-1000~+1000）。 |
| **结算整合** | `SettlementManager` 委托 `ScoreSystem` 计算最终评分并生成奖励（经验/灵石/装备，按评分倍率与关卡锁品质）。 |
| **准备时间调整** | `WaveManager.prepareSeconds=30f`（首波准备），`WaveManager.interWaveSeconds=60f`（波间隔1分钟）。 |
| **事件扩展** | `EventBus` 新增 `HealingEvent`；`EntityDamageEvent` 扩展 `SkillId` / `IsAutoAttack`，普攻自动标记。 |

### 评分细则（供调参与参考）
| 项目 | 权重 | 公式 |
|------|------|------|
| 参与击杀 | 0~2000 | 个人参与击杀数 / 全队参与击杀总数 × 2000 |
| 对敌伤害 | 0~2000 | 个人伤害 / 全队总伤害 × 2000 |
| 承伤 | 0~2000 | 个人承伤 / 全队总承伤 × 2000 |
| 治疗 | 0~1000 | 单人模式默认1000；多人按占比 |
| 支线 | 0~1000 | 每个支线+200，上限5个（1000） |
| 复活 | -1000~+1000 | 1000 - 死亡次数×200，下限-1000 |
| 水晶血量 | -1000~+1000 | (血量%-50%)×2×1000；满血=1000，50%=0，0血=-1000 |

### 已知限制
- 投射物凝实度、空中对碰、墙体/护盾系统仍为占位，待 0.0.9 实现。
- 多人模式下计分板仅展示单人数据（当前架构为单玩家）。

---

## 0.0.7 — 聚灵阵系统 ✅ 已完成

**目标**：完整实现局外聚灵阵，影响局内灵力回复与突破收益。

### 已完成功能
| 模块 | 内容 |
|------|------|
| **阵盘界面设计** | 局外新增聚灵阵按键+界面；阵盘随境界扩大（初始半径3/7×7阵眼居中）。 |
| **灵气计算** | `ConcentrationCalculator`：范围+距离衰减叠加出阵眼聚灵浓度；相邻相生×1.05、相克×0.95、同属性共鸣×1.02；软上限与等效值换算。 |
| **灵物数据** | `SpiritItem` + `ArraySlot`：支持灵石/聚灵珠/阵旗/五行材料摆放；基础聚灵值×堆叠数。 |
| **阵盘管理** | `SpiritGatheringArray`：格位增删改查/交换/移动、存档读写、相邻预览、统计信息；炼丹灵池与灵气结晶产出。 |
| **材料聚灵值** | 白±1、蓝±2±1、紫±3±2、金±4±3、橙±5±4、红±6±5；品质值=五行属性强度。 |
| **修炼加速** | 等效值×100%修炼速度加成、×20%突破成功率加成、×50%局内灵力回复加成。 |
| **数据注入** | 真实聚灵阵等效值已可写入 `PlayerProfile.CultivationArrayValue`，替代占位0。 |
| **结构系统** | `StructureDefinition`/`StructureType`/`StructureSize`/`ElementColorMap`/`StructureFactory`：六类结构（灵核/输出/技能/推进器/核心/辅助）程序化生成，尺寸随境界递进，五行颜色映射。 |
| **辅助结构** | `AuxiliaryEffect`：10种辅助模块（投射增幅/耐久核心/器灵/灵气回收/护盾/暴击/吸血/冷却缩减/凝实强化/追踪），含灵智上限与同类取最高机制。 |
| **设计文档** | 敌人设计30个（体/气/魂各10）、结构设计344行（六类规格）、辅助结构185行（10种功能）。 |
| **美术资源** | 敌人34张、灵核120张、推进器20张、输出结构180张、技能结构180张、核心100张、辅助结构10张，共644张结构/敌人概念图。 |

### 已知限制
- 聚灵阵 UI 界面仍为占位，需后续替换为正式美术 UI。
- 法宝温养、炼丹炉供灵/灵气结晶的具体消耗接口待 0.0.8 接入。

---

## 0.0.8 — 经验丹与支线经验 ✅ 已完成

**目标**：完善经验产出体系，经验丹与支线任务经验落地。

### 已完成功能
| 模块 | 内容 |
|------|------|
| **经验丹公式** | `GameData.GetPillExp`：增加值 = 境界第 9 级 × 560 × 聚灵阵线性衰减等效值 × 品质系数；10 境界 × 8 档品质 = 80 种经验丹。 |
| **8 档品质系数** | `PillQuality` 枚举 + `GetQualityCoefficient`：下品 0.25 / 中品 0.55 / 上品 0.85 / 一纹 1.00 / 二纹 1.10 / 三纹 1.20 / 六纹 1.50 / 九纹 1.80。 |
| **支线任务经验** | `GameData.GetSubQuestExp`：单个 = 10%×当前升级所需经验 + 25×关卡数×聚灵阵等效值(0~1)；`QuestManager` 每关自动生成 4 个支线，完成时动态重算；验证等效值 0.3~0.5 时总量约占同级升级经验 40%~50%。 |
| **ExpPill 接入** | `ExpPill` 继承 `Item`：`OnUse` 调用 `Player.GainExp(ExpSource.Pill)`；`CreateRealmSet` / `CreateFullPillLibrary` 批量创建；`CalculateTradeValue` 占位接口；`QualityNameToGeneralQuality` 映射通用品质色。 |

### 已知限制
- 经验丹合成材料表仍为占位（`CraftingMaterials` 空字典），待炼制系统细化后填充。
- 交易价值公式为占位（经验值 × 系数 × 0.01），后续需接经济系统统一计价。
- 支线任务完成时机目前为“每完成一波自动完成一个支线”，后续可扩展为按实际目标判定（清剿/速战/守护/搜刮）。

---

## 0.0.9 — 投射物、碰撞与地形 ✅ 已完成

**目标**：实现完整的实时战斗投射物体系与地形交互。

### 已完成功能
| 模块 | 内容 |
|------|------|
| **投射物凝实度** | `Skill.cs` + `Projectile`：飞行中碰撞体、永不阻挡移动；敌我投射物空中对碰按凝实度 1:1 互扣；命中伤害 = (当前凝实度/总凝实度) × 100% 基础伤害。 |
| **碰撞体积** | `CollisionSystem`：全注册实体圆柱碰撞检测（圆-圆距离）；同阵营弹性 0.5 推挤无伤害；异阵营对称双伤（攻击×0.2）+ 推开；魂体 `IsGhostBody` 跳过碰撞。 |
| **墙/护盾系统** | `TerrainObject`：墙（血量 HP）与护盾（耐久 Durability）双类型；`TerrainManager`：每来源（玩家/法宝）各≤3，独立槽位独立 CD（5 秒），多来源可叠加；超限时回收最早创建地形。 |
| **护盾耐久制** | `TerrainType.Shield`：无凝实度池，恒定 100% 直至 `CurrentDurability ≤ 0` 或 `CurrentDuration ≤ 0`。 |
| **群体技能** | `Skill.IsAreaSkill`：非投射物，对鼠标位置 `AreaRadius` 范围内敌人直接结算伤害，不受凝实度/空中对碰/碰撞消散影响。 |
| **穿透型接口** | `IsPenetrating` 保留在 `Skill` 与 `Projectile` 中，行为未实现，接口已就位。 |
| **敌方地形决策** | `CollisionSystem.GetBlockingTerrain`：点到线段最短距离检测；AI 调用路径查询判定“挡路则击碎/绕路”。 |
| **地形收取** | `TerrainObject.Collect` + `TerrainManager.CollectTerrain`：强 A + 点击己方地形 → 主动破碎进 CD。 |
| **碰撞系统注册** | `CollisionSystem.RegisterEntity` / `RegisterTerrain` / `Unregister`；`FixedUpdate` 中 `ProcessEntityCollisions` + `ProcessEntityTerrainCollisions`。 |

### 已知限制
- 穿透型技能仅保留接口，命中实体后穿透飞行并衰减的行为待 0.1.0+ 实现。
- 抛物线/追踪/螺旋等轨迹类型仍为占位，`TrajectoryType` 枚举已定义但行为未展开。
- 敌方地形绕路/击碎的智能决策 AI 需后续接入 `EnemyAI` 寻路逻辑。

---

## 0.1.0 — 第一关完整可玩版本 ✅ 已完成

**目标**：整合 0.0.1~0.0.9 全部系统，第一关（凡人境 1-10 关）可完整通关并评分。

### 已完成功能
| 模块 | 内容 |
|------|------|
| **全系统联调** | `GameManager.InitializeSubsystems()` 挂载 `ArtifactCraftingSystem`、`SpiritEconomySystem`、`ArtifactCompatibilitySystem`、`ScoreSystem`、`SettlementManager`、`QuestManager`、`BubbleUI`、`WaveManager`、`TerrainManager`、`CollisionSystem`。 |
| **局外→局内数据流** | `PlayerProfile`（等级/经验/灵石/三维/突破状态）→ 局内 `PlayerEntity` 初始化 → 战斗结算 `SettlementManager` → 经验/灵石/掉落回写档案。 |
| **评分与结算** | `ScoreSystem` + `SettlementManager`：7 项评分（参与击杀/伤害/承伤/治疗/支线/复活/水晶）+ 奖励倍率与品质锁；`ScoreBoardUI` 局内计分板实时展示。 |
| **支线经验闭环** | `QuestManager.GenerateSubQuestsForStage` 每关生成 4 个支线，`CompleteQuest` 中按当前等级/关卡/聚灵阵等效值动态重算经验。 |
| **经验丹体系** | `ExpPill` + `GameData.GetPillExp`：10 境界 × 8 档品质 = 80 种经验丹，支持批量创建与使用。 |
| **突破闭环** | `BreakthroughManager`：双轨突破（资源轨 + 时间轨），九品→一品 9 条进度条，突破成功写入 `PermanentStats` 永久加成。 |
| **聚灵阵闭环** | `SpiritGatheringArray` + `ConcentrationCalculator`：阵盘格位摆放灵气材料，等效值注入 `PlayerProfile.CultivationArrayValue`，影响突破/经验/局内回复。 |
| **波次全开放** | `WaveManager.maxWave = 10`，波次计划 `{1,3,5,10,15,30,30,30,30,30}` 全部可用，第 10 波 Boss 替换最后一只小怪。 |
| **性能保障** | 敌人峰值 30 + Boss，对象池化在后续版本进一步优化；当前帧率由 `FixedUpdate` 轮询碰撞 + `Update` 投射物分离保证。 |
| **花括号与接口验证** | 47 个 `.cs` 文件全部通过花括号平衡检查；`Skill`、`CollisionSystem`、`TerrainObject`、`TerrainManager`、`QuestManager` 新增文件接口与现有系统一致。 |

### 已知限制
- 支线任务完成时机目前为“每完成一波自动完成一个支线”——此为占位实现，待支线任务系统（含清剿/守护/技能/搜刮等具体目标追踪与进度判定）实现后，必须替换为按实际目标判定完成。详见 `QuestManager.cs:278` TODO 注释与 MEMORY.md。
- 多人模式下计分板仅展示单人数据（当前架构为单玩家）。
- 抛物线/追踪/螺旋等投射物轨迹未展开。
- 穿透型技能行为未实现。

---

## 附录：当前任务链一览（Q001~Q013 + B003）

| 编号 | 名称 | 接取方式 | 完成后解锁 | 备注 |
|------|------|----------|-----------|------|
| Q001 | 遭遇 | 波1自动 | Q006 | 击败第1波敌人 |
| Q002 | 确认装配 | Q007完成后自动 | — | 契合度检查 |
| Q003 | 小试牛刀 | 波4自动 | — | 使用法宝击败第4波 |
| Q004 | 抵御攻击 | 波3自动 | — | 抵御第3波进攻 |
| Q005 | 完美首关 | 波1静默 | — | 0死亡通关全部10波 |
| Q006 | 初次炼器 | Q001完成后自动 | Q009 | 选择材料炼制灵核 |
| Q007 | 熔炼技能结构 | Q010完成后自动 | Q012 | 熔炼输出/技能结构，白升绿 |
| Q008 | 设计成型 | 通关后自动 | Q013 | 用掉落组装完整器胚 |
| Q009 | 器胚装配 | Q006完成后自动 | Q010 | 灵核入胚 |
| Q010 | 推进器装配 | Q009完成后自动 | Q007, Q002 | 3推进器+供能线 |
| Q012 | 最终试炼 | Q007完成后自动 | — | 抵御至第10波并击败Boss |
| Q013 | 首件仙器 | Q008完成后自动 | — | 完成炼制并获得仙器 |
| B003 | 结丹突破 | 玩家主动触发 | — | 筑基满级后双轨突破 |

---

*文档版本：2026-10-17*
*对应代码状态：QuestSystem.cs / GameManager.cs / WaveManager.cs / PlayerProfile.cs / BreakthroughManager.cs / ExpSystem.cs 等已落地*
