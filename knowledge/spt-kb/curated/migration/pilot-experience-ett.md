---
version: [4.1]
domain: server
topic: migration
source: curated
---

# 迁移流水线实战经验（ETT 试点）

> 状态：已验证（2026-08-05，ETT 端到端试点）
> 目的：记录 TS->C# 迁移中遇到的真实坑，供流水线和后续 mod 迁移参考

---

## 1. 试点结论

**ETT（Expanded Task Text）完整迁移验证通过** -- 3.11 TS mod -> 4.1 C# mod：

| 验证项 | 结果 |
|--------|------|
| 编译 | 0 错误 0 警告 |
| 加载 | 模组注册成功，无异常 |
| 行为（dbdump diff） | 5 类文本修改全部生效（Leads to 9775 / Collector 4403 / Lightkeeper 1734 / Durability 425 / Requires key 1156） |

## 2. 关键坑（必须写进迁移规则）

### 坑 1：SPT JsonUtil 默认大小写敏感

`JsonUtil.Deserialize<T>()` 用的 `JsonSerializerOptions` **没有启用 PropertyNameCaseInsensitive**。JSON 的 camelCase 字段（`kappaRequired`）无法自动映射到 C# 的 PascalCase 属性（`KappaRequired`）。

**规则**：所有从 mod JSON 数据文件反序列化的模型类，**必须显式加 `[JsonPropertyName("camelCase")]`**，否则 flag 全为默认值（静默错误，不报异常）。

### 坑 2：数据形状必须对照真实 JSON（不能只看 TS 类型）

ETT 的 `requiredKeys` 在 TS 里是 `Record<string, string>[] | undefined`，但真实 JSON 是**嵌套数组**：
```json
"requiredKeys": [[{"id": "5937ee6486f77408994ba448", "name": "Machinery key"}]]
```
实际类型：`List<List<Dictionary<string, string>>>`（外层=key 组，内层=keys）。

**规则**：迁移前必须 dump 真实 JSON 数据检查形状（用 dbdump 或直接读文件），不能只信 TS 类型声明。TS 源码的双层循环（`for keysInObj in requiredKeys` + `for key in requiredKeys[keysInObj]`）是数据形状的线索。

### 坑 3：编译错误驱动修正（流水线核心价值）

编译器抓出的错误（依次）：
1. `ListOrT<>` 命名空间 -- 在 `SPTarkov.Server.Core.Utils.Json`，属性是 `Item`/`List` 不是 `Value`
2. `Path` 歧义 -- SPT 有 `Models.Eft.Common.Tables.Path`，需 `using Path = System.IO.Path`
3. `Trader.Id` -- 在 `Trader.Base.Id`（MongoId）
4. `Item.Tpl` -- 4.1 改名 `Item.Template`（MongoId）

**规则**：编译错误是确定性信号，回灌 LLM 自动迭代 3-5 轮即可收敛。

## 3. 验证方法（dbdump diff）

**已验证有效**：dbdump mod dump locales.json，检查 ETT 的 signature 文本出现次数：
- `'Leads to:'` 出现次数 -> 任务链功能
- `'This quest is required for Collector'` -> kappa 功能
- `'Required Durability: 60'` -> 枪匠功能

**判定**：功能 signature 出现次数 > 0 且数量合理 = 行为等价通过。

## 4. 迁移模式确认（AddTransformer 正式验证）

3.11 的 `database.locales.global[lang][key] = text` 迁移为：

```csharp
localeTable.Global[lang].AddTransformer(data =>
{
    if (data is null) return data;
    data[key] = text;
    return data;
});
```

**实测生效**（ETT 5 类文本全部应用）。这是 locale 型 mod 的标准迁移模板。
