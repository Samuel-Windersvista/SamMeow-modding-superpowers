# THIRD-PARTY NOTICES

`SamMeow.DebugToolkit`（SPT 4.1.6 版）整合并移植了以下三个 MIT 许可的上游项目。以下为各上游的
许可证全文、版权行与来源。本工程分发时保留本文件与全部署名。

---

## 1. DrakiaXYZ-BotDebug (SPT-BotDebug)

- 版权行：`Copyright (c) 2025-2026 DrakiaXYZ`
- 版本：v1.8.0（源码对应 EFT 40743）
- 来源：https://github.com/DrakiaXYZ/SPT-BotDebug
- 归档：`knowledge/spt-kb/archive/forge/mods/DrakiaXYZ-BotDebug_918_source`
- 用途：Bot 调试覆盖层模块（8 档监视模式、头顶覆盖层、模式循环）的上游实现。

```
MIT License

Copyright (c) 2025-2026 DrakiaXYZ

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

---

## 2. DebugTooltip

- 版权行：`Copyright (c) 2024 Tyfon`
- 版本：v5.0.0（源码锚 SPT 4.1）
- 来源：https://github.com/tyfon7/DebugTooltip
- 归档：`knowledge/spt-kb/archive/forge/mods/DebugTooltip_1542_source`
- 用途：Tooltip 调试信息模块（物品/槽位/任务/商人 ID 注入与三档剪贴板复制）的上游实现。

```
MIT License

Copyright (c) 2024 Tyfon

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

---

## 3. Dad Gamer Mode (DadGamerMode)

- 版权行：上游 `LICENSE.txt` 为**未填写的占位模板**（原文即 `Copyright (c) [year] [fullname]`）。
  上游未声明具体版权人；本工程依据项目作者信息按 **Props / dvize** 署名。
- 版本：v1.9.3
- 来源：https://github.com/dvize/DadGamerMode
- 归档：`knowledge/spt-kb/archive/forge/mods/DadGamerMode_686_source`
- 用途：无敌 / 部分无敌模块（GodMode、Keep1Health、伤害上限、无坠落伤害、CODMode）与便利组
  QOL 全组的上游实现。

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

> 注：上游 DadGamerMode 的 `LICENSE.txt` 未填写年份与版权人（原文即
> `Copyright (c) [year] [fullname]`）。本工程不主张对上游代码的额外权利，
> 仅按 MIT 要求保留其许可证全文与来源。

---

## 依赖（非本插件代码，运行期依赖）

### BigBrain 1.5.0（`xyz.drakia.bigbrain`）

- 本插件对 BigBrain 声明**硬依赖**（`[BepInDependency("xyz.drakia.bigbrain", "1.5.0")]`）；
  缺失时 BepInEx 不加载本插件。
- 本仓库**无 BigBrain 源码**（仅 obj 残件）；编译引用与运行期依赖目标为本机 Norvinsk_Forever
  覆盖层内的 DLL：
  `mods\[6]AI拟人MOD的前置框架-BigBrain\BepInEx\plugins\DrakiaXYZ-BigBrain.dll`。
- 本工程**不分发** BigBrain 二进制；BigBrain 的许可证与署名以其自身分发为准。

---

## 本工程（SamMeow.DebugToolkit）

- 版权行：`Copyright (c) 2026 SamMeow`
- 许可：MIT（全文见仓库根 [LICENSE](LICENSE)）
- 本工程为上述三个上游项目的 SPT 4.1.6 整合与移植产物；分发时同时保留本文件与仓库根 `LICENSE`。
