---
version: [通用]
domain: both
topic: operations
source: curated
---
# 破坏性操作护栏（失误教训）

> 状态：沉淀（2026-08-05，Forge release zips 误删事故）
> 适用：任何对知识库/归档目录的批量清理、迁移、重命名操作

## 事故记录

**事故**：清理 `knowledge/spt-kb/archive/forge/mods/` 源码仓库的二进制时，
清理脚本的删除名单包含 `.zip`，误删了 **95 个 `<id>_release/` 发布 zip**（mod 发布产物）。

**根因**：
1. 删除名单设计错误——`.zip` 不该出现在"清理二进制"名单。release zips 是 Forge 归档的**正规组成**，不是待清理杂质。
2. 缺少破坏性操作护栏——脚本执行前没有 dry-run 预览、没有目录白名单保护、没有删除审计。

**后果**：95 个发布 zip 不可恢复（目录未 git 跟踪、无备份）。

## 铁律（今后所有破坏性操作必须遵守）

### 1. Dry-run 先行
批量删除/移动前，先跑 dry-run（只列出将受影响的对象），人工确认后再执行。
```javascript
// 示例：先收集再删除
const toDelete = collect();       // 第一步：只收集
console.log(toDelete);            // 第二步：人工审查
toDelete.forEach(delete);         // 第三步：确认后执行
```

### 2. 归档结构白名单保护
`_release` / `_source` 等**正式归档目录**永不进删除名单。
删除名单只针对"源码树内的杂质"（`obj/` `bin/` `node_modules/` `*.dll` 构建产物等），
不针对归档自身的组成文件。

### 3. 删除审计
删除后立即核对：
- 预期保留的对象是否完好（如 `_source` 目录数、release 目录数）
- 删除前后目录计数对比，异常立即上报

### 4. 破坏性操作前后快照
对大规模清理，操作前记录目录/文件计数基线：
```powershell
# 基线
(Get-ChildItem -Recurse -File | Measure-Object).Count
# 操作后对比
```
发现计数异常骤降（如 release 目录 95→0）立即停止。

## 检查清单（写清理脚本时过一遍）

- [ ] 删除名单是否误包含归档组成格式（zip/release 产物）？
- [ ] 有 dry-run 模式吗？
- [ ] 归档目录（_release/_source 等）在白名单保护里吗？
- [ ] 删除后有审计核对吗？
- [ ] 目标目录是 git 跟踪的吗？（未跟踪目录 = 无恢复手段，更要谨慎）

## 关联

- `maintaining-modding-environments` / `maintaining-spt-modding-environment` skill 的清理流程
- `knowledge/spt-kb/archive/forge/` 归档结构（`<id>_release/` 发布 zip + `<id>_source/` 源码）
