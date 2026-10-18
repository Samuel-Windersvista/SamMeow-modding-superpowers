# THIRD-PARTY NOTICES

`Door Breach Overhaul Server（破门大修服务端）`（SPT 4.1.6 版）的 C4 物品模板、工作台配方与 C4 bundle 资源
来自 **Backdoor Bandit NG（BB-NG）** 的上游发行物 / 仓库快照。分发时保留本文件与仓库根 `LICENSE`。

---

## 1. Backdoor Bandit NG (BackdoorBandit)

- 版权行：上游 `LICENSE.txt` 为**未填写的占位模板**（原文即 `Copyright (c) [year] [fullname]`）。
  上游未声明具体版权人；本工程依据项目谱系按 **dvize / v-engelowski / kobethuy** 署名。
- 版本：master `451c6cb`（2026-10-13）
- 来源：https://github.com/thuynguyentrungdang/backdoorbandit
- 用途：本工程 C4 物品体系（`C4Explosive` 模板、`bundles/C4Explosive.bundle`、工作台配方）的数据与资源来源。
  数据取自仓库快照 `external/references/backdoorbandit-ng/server files/user/mods/DoorBreacher/`
  （与 BB-NG 2.0.4 发行包同尺寸同源）：
  - `database/templates/items.jsonc` 的 `C4Explosive` 对象 -> `db/customitems/c4explosive.jsonc`
  - `database/templates/craftingItem.jsonc` -> `db/hideout/c4explosive_recipe.jsonc`
  - `bundles/C4Explosive.bundle` -> `bundles/C4Explosive.bundle`
    （2,147,196 B，SHA256 `471bfb5ad220a517d5f474af68ef7a2bc7af50e27ef00587ec055156f1f19415`）

```
MIT License

Copyright (c) [year] [fullname]

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

> 注：上游 Backdoor Bandit NG 的 `LICENSE.txt` 未填写年份与版权人。本工程不主张对上游数据/资源的额外权利，
> 仅按 MIT 要求保留其许可证全文与来源。

---

## 2. 本工程（Door Breach Overhaul Server）

- 版权行：`Copyright (c) 2026 SamMeow`
- 许可：MIT（全文见仓库根 [LICENSE](LICENSE)）
- 本工程为上述上游项目在 SPT 4.1.6 上的服务端重建产物；分发时同时保留本文件与仓库根 `LICENSE`。
