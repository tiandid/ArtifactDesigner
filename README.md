# 仙器设计师（Unity 电脑游戏）

仙器设计师游戏工程（当前阶段：炼器子系统）。
策划需求：电脑游戏 · Unity · 仙器设计师。
成长体系：人物境界获得属性、炼丹、炼器、技能。本阶段先落地 **炼器** 完整闭环。

---

## 一、当前已实现内容

### 1. 器坯系统（ArtifactBlank）
- 器坯拥有多个**格子**，部件挂载在格子上。
- 核心规则：**格子越多，重量越大**。
  总重量 = 器坯基础重量 + 当前总格子数 × 每格重量 + 各部件自身重量之和。
- 挂载/卸下/查询接口完整，支持按索引挂载或自动找空位挂载。
- 配套自定义 Inspector（`ArtifactBlankEditor`）：场景中显示格子数、重量、每个槽位状态，并支持一键生成演示部件挂载调试。

### 2. 部件系统（ArtifactPart，七大类）
| 部件类型 | 效果 |
| --- | --- |
| 增加格子（AddSlot） | 增加器坯格子数 |
| 增加耐久（Durability） | 提升耐久上限 |
| 增加攻击（Attack） | 提升攻击 |
| 增加速度（Speed） | 提升速度 |
| 增加主动技能（ActiveSkill） | 携带一个主动技能，需能量供电才生效 |
| 增加被动技能（PassiveSkill） | 携带一个被动技能，需能量供电才生效 |
| 能量核心（EnergyCore） | 输出能量，用线路连接技能部件 |

- 示例部件工厂 `DemoParts` 提供每一类的演示数据（烈焰斩、灵盾、玄晶核心等）。
- 技能数据由 `SkillDefinition` 描述（类型、能耗、威力、描述），支持深拷贝克隆。

### 3. 能量线路系统（EnergyNetwork）
- 模型：节点（能量核心 / 技能部件）+ 无向线路构成电网。
- 规则：**技能部件必须通过线路连通到能量核心才生效**。
- 供电分配采用“共享电网”模型：
  - 一个连通分量内，总供给 = 所有活跃能量核心输出之和，总需求 = 所有活跃技能部件需求之和；
  - 供给充足 → 全部技能生效；供给不足 → 按节点 ID 顺序优先分配，靠后的技能断电；
  - 多核心可并联组成同一电网共同供电。
- 线路可动态增删，移除线路后对应技能立即断电。

### 4. 打怪掉落系统（CombatSystem）
- 敌人模型 `Enemy`：血量、攻击、速度、经验，可配置掉落表。
- 掉落表 `DropTable`：多条 `DropEntry` 独立判定（部件类型、稀有度、概率、数量范围）。
- 战斗流程：回合制（速度高者先手）→ 击杀成功 → 按掉落表掷骰产出部件；失败一律不掉落。
- 防护：超过最大回合（60）判防守失败，防止战斗卡死。
- 部件的主要获取方式即“打怪掉落”，掉落产出直接接入部件/器坯系统。

### 5. 预留：境界成长（RealmSystem）
- 凡人 → 练气 → 筑基 → 金丹 → 元婴 → 化神 → 炼虚 → 合体 → 大乘 → 渡劫，共 10 境。
- 每境提供基础属性表（攻击/防御/速度/耐久/能量），为“人物境界获得属性”预留。

---

## 二、工程结构

```
仙器设计师/
├── Assets/
│   └── Scripts/
│       ├── Core/        # 枚举、属性集合、境界表
│       │   ├── GameEnums.cs
│       │   ├── AttributeSet.cs
│       │   └── RealmSystem.cs
│       ├── Artifact/    # 炼器：器坯、部件、技能、属性合并
│       │   ├── ArtifactBlank.cs
│       │   ├── ArtifactPart.cs
│       │   ├── ArtifactStats.cs
│       │   ├── SkillDefinition.cs
│       │   └── DemoParts.cs
│       ├── Energy/      # 能量节点、线路、网络
│       │   ├── EnergyNode.cs
│       │   ├── EnergyLink.cs
│       │   └── EnergyNetwork.cs
│       ├── Combat/      # 打怪掉落：敌人、掉落表、战斗
│       │   ├── Enemy.cs
│       │   ├── DropTable.cs
│       │   └── CombatSystem.cs
│       ├── Game/        # Unity 运行时控制器
│       │   └── ArtifactBlankController.cs
│       └── Editor/      # 编辑器扩展
│           └── ArtifactBlankEditor.cs
├── LogicTests/          # 纯逻辑测试（无需 Unity 环境，.NET 8）
└── UnityCheck/          # Unity API 桩编译检查（验证脚本可被 Unity 编译）
```

---

## 三、在 Unity 中使用

1. 在 Unity Hub 新建一个 **2D/3D 项目**（推荐 2021.3 LTS 或更高，C# 9+）。
2. 把 `Assets/Scripts/` 整个文件夹复制到你的 Unity 项目 `Assets/` 下。
3. 等待编译完成后：
   - 场景中新建空物体，挂载 `ArtifactBlankController`；
   - 运行前调用 `Init("blank_demo", "演示剑胚", 2, 10, 5)`（或在 Inspector 的“重置为 2 格器坯”点击）；
   - 用 Inspector 顶部的“生成并挂载演示部件”逐类测试，观察格子数、重量、属性面板变化；
   - 能量部分可在代码中构造 `EnergyNetwork`：`AddNode` 核心与技能节点 → `AddLink` 连接 → `IsPowered("技能节点")` 判断是否生效。
4. 打怪掉落示例（纯逻辑层可直接调用）：
   ```csharp
   var enemy = new Enemy("imp1", "山精", 2, maxHp: 30, attack: 5, speed: 1.0f, exp: 20);
   enemy.dropTable = new DropTable { tableId = "t_imp" };
   enemy.dropTable.entries.Add(new DropEntry(PartType.Attack, Rarity.Uncommon, 1f, 1, 1));
   var result = CombatSystem.SimulateBattle(100, new AttributeSet { attack = 100, speed = 2f }, enemy, new Random());
   // result.victory == true 时 result.drops 为击杀掉落的部件
   ```

> 提醒：`LogicTests` 与 `UnityCheck` 是辅助验证工程，仅用于无 Unity 环境下做静态检查与逻辑回归，不需要放进 Unity 项目。

---

## 四、运行逻辑测试（可选，无需 Unity）

环境：.NET 8 SDK。

```bash
dotnet build 仙器设计师/LogicTests/LogicTests.csproj -v q --nologo
dotnet run --project 仙器设计师/LogicTests/LogicTests.csproj --no-build
```

预期输出：全部测试 PASS，`==== 通过 20 / 20 ====`。

覆盖项：境界属性表、器坯重量公式、扩容部件、挂载/卸下边界、七类部件生成、技能数据与克隆、能量连线/断电/过载分配/多核心并联、打怪击杀掉落/失败不掉落/回合上限。

---

## 五、后续开发方向（规划）

1. **守家战斗**：敌人沿三路涌向水晶，玩家操控角色与法宝拦截防守；把器坯炼制的法宝搭载入战。
2. **境界成长**：击杀经验 → 突破 → 境界属性面板，境界影响可炼制器坯档次。
3. **炼丹**：炼丹炉、丹方、材料合成（材料可复用打怪掉落）。
4. **技能战斗**：把已供电的主动/被动技能接入战斗结算（伤害、护盾、DEBUFF）。
5. **UI**：锻造界面（器坯格子可视化 + 拖拽部件）、电网编辑（连线绘制）、背包/掉落界面。