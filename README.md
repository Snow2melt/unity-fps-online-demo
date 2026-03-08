# Unity FPS Multiplayer Demo

一个基于 **Unity + Netcode for GameObjects + Unity Transport** 实现的 FPS 联机射击 Demo。  
项目面向 **Unity 客户端校招 / 春招面试展示**，重点体现了 **服务器权威射击、武器数据化、爆头伤害、计分板、多人重生同步** 等能力。

---

## 项目简介

本项目是一个多人联机 FPS Demo，支持 Host / Client 联机对战。  
项目核心目标不是单纯实现“能开枪”，而是围绕联机射击场景，完成一套更接近真实项目的核心链路，包括：

- 服务器权威的命中与伤害结算
- ScriptableObject 武器配置数据化
- 爆头伤害系统
- K/D 计分板
- 死亡 / 复活 / 随机重生点同步
- 本地 HUD 与角色头顶信息显示

---

## 技术栈

- **Engine**: Unity
- **Language**: C#
- **Networking**: Netcode for GameObjects
- **Transport**: Unity Transport (UTP)
- **UI**: UGUI + TextMeshPro
- **Data Driven**: ScriptableObject

---

## 核心功能

### 1. 联机角色与同步

- 支持 Host / Client 联机
- 玩家生成后，远端对象自动禁用本地输入与本地相机
- 使用 `ClientNetworkTransform` 实现客户端权威移动同步

### 2. 服务器权威射击与伤害结算

- 客户端只上报射击意图：`origin / direction / weaponSlot`
- 服务器负责：
  - 校验射击参数
  - 获取武器配置
  - 执行 Raycast 命中检测
  - 结算伤害
- 避免客户端直接上传命中结果或任意伤害值

### 3. 武器 ScriptableObject 数据化

- 使用 `WeaponConfig` 存储武器静态配置：
  - weaponName
  - damage
  - range
  - shootRate
  - shootCoolDownTime
  - recoilForce
  - maxBullets
  - reloadTime
  - headshotMultiplier
  - graphics prefab
- 使用 `WeaponState` 存储运行时状态：
  - currentBullets
  - isReloading
- 通过 `WeaponManager` 实现主副武器管理、切枪、换弹

### 4. 爆头伤害系统

- 通过 `HeadHitbox + Tag=Head` 区分头部命中
- 服务器命中判定后，根据 `headshotMultiplier` 计算爆头倍伤
- 解决了头部碰撞体与身体碰撞体遮挡导致的命中问题

### 5. 命中特效同步

- 服务器命中后广播命中点信息
- 各客户端本地生成命中特效
- 支持不同材质命中表现

### 6. 计分板（Scoreboard）

- 按住 `Tab` 显示，松开隐藏
- 显示所有玩家 `Name / K / D`
- 排序规则：
  - 击杀数 K 降序
  - 死亡数 D 升序
- 高亮本地玩家 `[You]`
- 修复了重复击杀导致的双计分问题

### 7. 死亡与复活系统

- 死亡后进入禁用控制、播放死亡表现状态
- 服务器负责恢复真实生命状态
- 所有客户端恢复角色复活表现
- owner 客户端执行实际位置传送，适配客户端权威移动模型
- 支持多个 `RespawnPoint` 随机重生

### 8. HUD 与角色信息显示

- 本地 HUD：
  - 血量
  - 子弹数
  - 换弹提示
- 角色头顶信息：
  - 玩家名
  - 血条朝向摄像机显示

---

## 关键脚本职责

### `Player.cs`

负责角色生命值、死亡、复活、组件启停、计分相关状态。

### `PlayerShooting.cs`

负责射击输入触发、射击请求上报、服务器权威命中检测、伤害结算、命中特效同步。

### `WeaponManager.cs`

负责武器实例化、主副武器切换、换弹、获取当前武器配置与运行时状态。

### `WeaponConfig.cs`

ScriptableObject 武器静态配置。

### `WeaponState.cs`

武器运行时状态数据。

### `PlayerController.cs`

负责角色移动、旋转、跳跃、后坐力反馈等控制逻辑。

### `PlayerInput.cs`

负责采集玩家输入。

### `PlayerUI.cs`

负责本地 HUD 显示。

### `PlayerInfo.cs`

负责玩家头顶名称与血条显示。

### `PlayerSetup.cs`

负责网络对象生成后的本地/远端初始化。

### `ScoreboardUI.cs`

负责计分板显示、刷新、排序、高亮本地玩家。

### `GameManager.cs`

负责玩家注册与查询管理。

---

## 服务器权威射击链路

1. 客户端检测开火输入，播放本地表现（枪口火焰 / 音效 / 子弹消耗）
2. 客户端调用 `ShootRequestServerRpc(...)`
3. 服务器校验射击请求是否合法
4. 服务器从 `WeaponManager` 获取武器配置，不信任客户端上传的伤害值
5. 服务器执行 Raycast 命中检测
6. 若命中头部，按爆头倍率计算伤害
7. 服务器调用 `TakeDamage()` 进行扣血与死亡判定
8. 若死亡，只在 `alive -> dead` 跃迁点结算一次 K/D
9. 服务器广播命中信息，各客户端本地生成命中特效

---

## 解决过的关键问题

### 1. 双计分问题

问题：目标死亡后继续命中，可能重复加击杀分。  
解决：将计分触发点收敛到 `TakeDamage()` 内部的 `alive -> dead` 跃迁点，只在首次死亡时结算一次。

### 2. 原地复活问题

问题：角色明明设置了重生坐标，但看起来仍像原地复活。  
原因：项目使用客户端权威移动，服务器直接改位置会被 owner 客户端的位置同步覆盖。  
解决：

- 服务器恢复生命状态
- 所有客户端恢复复活表现
- owner 客户端执行实际传送  
  从而与当前移动权威模型保持一致。

### 3. HUD 血条不更新

问题：本地 HUD 血条表现异常。  
解决：排查并修复 UI 前景/背景配置错误。

### 4. 散弹枪特效 / 音效丢失

问题：武器表现资源丢失。  
解决：修复 prefab 序列化引用缺失。

### 5. 爆头判定不稳定

问题：头部命中偶发被身体碰撞体遮挡。  
解决：优化头部 Hitbox 与命中检测逻辑。

---

## 项目亮点

- 实现了 **服务器权威射击与伤害结算**
- 将武器系统做成 **ScriptableObject 数据驱动**
- 支持 **爆头倍伤**
- 完成 **K/D 计分板** 与排序显示
- 修复了 **双计分** 与 **原地复活** 等联机常见问题
- 能清晰解释 **服务器规则权威 / 客户端表现同步 / owner 位置权威** 的边界

---

## 运行方式

### 本地联机测试

1. 使用 Unity Editor 启动 Host
2. 再启动一个 Build 版本作为 Client
3. 进入房间后进行联机测试

### 场景准备

请确保场景中已正确配置：

- 玩家预制体
- 武器配置资源
- `RespawnPoint` 标签的重生点
- HUD 与 Scoreboard UI

---

## 后续可扩展方向

- Bot 靶场
- 更完整的武器差异化设计
- Dedicated Server 流程进一步整理
- 命中反馈与枪械手感优化
- 回合制 / 胜负结算
- 断线重连与异常处理

---

## 项目用途说明

该项目主要用于：

- Unity 客户端岗位面试展示
- 联机射击核心逻辑练习
- 服务器权威 / 客户端权威边界理解
- 数据驱动武器系统实践

---

## Author

Fu Weichen
