---
version: [3.11, 4.1]
domain: both
topic: migration
source: curated
---

# 3.11 -> 4.1 Bundle 迁移指南

> 状态：已提炼（2026-08-05）| 依据：`docs/bundle-difference-audit.md` 实机审计结果

---

## 1. 地面事实（实测）

| 项 | 3.11 | 4.1 |
|----|------|-----|
| 游戏引擎 | Unity 2022.3.43f1 | Unity 2022.3.43f1 |
| 引擎序列化格式 | 一致 | 一致 |
| bundle 分发协议 | bundles.json + /singleplayer/bundles + CRC32 | 相同 |

**注意：引擎相同不等于 bundle 直接兼容** -- 引擎层无差异，但 shader 和脚本绑定层有风险（见下）。

## 2. Bundle 构建版本分布（Life_in_Norvinsk 实测 2622 个）

| 构建版本 | 占比 | 说明 |
|----------|------|------|
| Unity 2019.4.39f1 | 85.4% | mod 作者用旧版 Unity 打包 |
| Unity 2022.3.43f1 | 11.3% | 匹配游戏引擎 |
| 其他 | 3.2% | 2019.4.40 / 2018.4 等 |

2019.4 bundle 在 3.11（2022.3 引擎）里靠**向前兼容**（TypeTree 序列化）运行。4.1 引擎相同，兼容机制同样适用。

## 3. 真实风险点（必须逐 mod 验证）

### 3.1 Shader（最常见破坏源）

Unity 官方承认 AssetBundle 向后兼容"not guaranteed in all situations"，**shader 是最常见问题**（官方 issue CASE IN-4831）。

EFT 0.16.1 (3.11) -> 0.16.9 (4.1) 是 patch 级差异，shader bundle 可能微调。2019.4 构建的 bundle 里的 EFT 自定义 shader（Bumped Specular SMap 等）依赖向前兼容。

**症状**：紫色模型、材质参数错误、透明/法线丢失。

### 3.2 MonoBehaviour 脚本绑定（本包无风险，其他包需验证）

4.1 客户端反混淆改了 ~9240 个类名。bundle 里的 prefab 若引用**混淆类名**（GClassXXX），绑定断裂。

**Life_in_Norvinsk 实测**：2622 个 bundle **0 个引用混淆类名**。引用的脚本类（PreviewPivot、EFT.Visual.LoddedSkin、HotObject）在两版游戏中名称相同。

**但其他整合包不保证** -- 若 mod 作者在 prefab 上挂了 EFT 内部混淆组件，迁移后会断。

### 3.3 bundles.json 元数据

3.11 `IModMetadata.IsBundleMod` 字段在 4.1 删除，改为检测 `bundles.json` 存在性。服务端 manifest 层面。

## 4. 迁移流程（每 mod）

```
1. 复制 bundle 文件 + bundles.json 到新 mod 目录
2. 更新 mod 元数据（删 IsBundleMod 相关）
3. 实机启动，检查服务端日志 bundle 加载（CRC32 通过）
4. 进游戏逐项验证：
   - 模型显示正常？（无紫色 = shader OK）
   - 材质正确？（无纯白/黑 = 材质参数 OK）
   - 附件挂点正确？（组件 OK）
   - PreviewPivot 预览正常？（SDK 脚本绑定 OK）
5. 异常处理：
   - 紫色模型 -> shader 不兼容 -> Unity 2022.3 + EFT SDK 重建
   - 组件报错 -> 脚本绑定断 -> 重建 prefab
   - 其他 -> 查 bundles.json 路径/CRC
```

## 5. 自动化验证工具

| 工具 | 用途 | 自动化程度 |
|------|------|------------|
| UnityPy（Python） | 批量读 bundle 头/资产类型/shader 依赖 | 可脚本化 |
| AssetRipper / AssetStudio | 提取 bundle 内容（重建前置） | 半自动 |
| Unity batch mode | `-executeMethod BuildAssetBundles` 无头重建 | 可脚本化 |
| SPT 服务端日志 | bundle 加载确认（CRC32） | 自动 |
| 游戏内验证 | 模型/shader/组件显示 | **人工必需** |

## 6. 结论

1. **引擎层无差异** -- 序列化格式一致，bundle 文件二进制可复制
2. **shader 是主要风险** -- 低概率但必须实机验证
3. **脚本绑定对本包无风险**（0/2622 引用混淆类）-- 其他包需逐一验证
4. **bundle 迁移不是瓶颈** -- 真正的瓶颈是 DLL（服务端 TS->C# 重写 + 客户端重编译）
5. **"复制即用"不能完事** -- 必须加严为逐 mod 实机验证
