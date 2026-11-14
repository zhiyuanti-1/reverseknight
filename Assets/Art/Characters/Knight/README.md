# 反转骑士 · 角色美术（非像素 2D）

风格：手绘卡通风、约 3 头身，不是像素图。光源统一来自左上，主色沿用策划案的夜色板
（背景 `#151725`、盔甲象牙白、披风铁锈红、面甲一道冷白发光的横缝）。

## 文件

```
Assets/Resources/Art/Characters/Knight/      ← 运行时可用 Resources.Load 加载
  knight_idle.png          侧视待机（默认站立）
  knight_idle_front.png    正视待机（菜单/结算/立绘用）
  knight_run_1..4.png      跑步循环
  knight_jump.png          上升
  knight_fall.png          下落
  knight_land.png          落地缓冲
  frames.json              画布 / 锚点 / PPU 参数
Assets/Art/Characters/Knight/Reference/
  knight_turnaround.png    三视图设定稿（风格基准）
  knight_actionsheet.png   原始动作表（8 格，未抠背）
  knight_preview.png       动作总览：上排正立、下排 180° 翻转后
```

## 规格

| 项 | 值 |
|---|---|
| 单帧画布 | 704 × 640 px |
| 角色高度 | 约 420 px（对应 1.4 世界格） |
| 脚底基线 | y = 592 px（画布自上而下） |
| 锚点 pivot | (352, 382) → 归一化 (0.5, 0.5969) |
| 像素密度 | 300 px / 世界单位 |
| 格式 | PNG，透明底，双线性过滤，不生成 mipmap |

所有帧共用同一画布与同一缩放，只做平移不做缩放，因此切换帧时角色不会抖。

锚点取的是碰撞盒（0.7 × 1.4）的中心，即脚底往上 0.7 格。这样把 `visualRoot`
原地旋转 180° 就能得到正确的倒挂姿态，不用额外偏移。

## 接入 Unity（尚未接入）

`Assets/Editor/KnightArtImporter.cs` 已经负责导入设置（Sprite / 双线性 / 锚点 / PPU），
不用手工调 Inspector。运行时可以这样用：

```csharp
Sprite Load(string name) => Resources.Load<Sprite>("Art/Characters/Knight/" + name);
// 挂到 PlayerFactory 生成的 Visual 下，localPosition = zero 即可，脚底会自动落在碰撞盒底部。
```

## 还缺的

- 跑步 4 帧略粗，正式版建议 6 帧。
- 反转瞬间的残影/冲击特效、死亡特效。
- 地形、机关、UI 素材。
