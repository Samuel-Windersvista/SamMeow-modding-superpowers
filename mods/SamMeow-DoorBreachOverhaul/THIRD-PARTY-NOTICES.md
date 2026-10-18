# THIRD-PARTY NOTICES

`Door Breach Overhaul`（SPT 4.1.6 版）以 **Backdoor Bandit NG（BB-NG）** 为合并基座，并规划并入
**Sage's Door Overhaul（DO）**。以下为各上游的许可证全文、版权行与来源。本工程分发时保留本文件与全部署名。

---

## 1. Backdoor Bandit NG (BackdoorBandit)

- 版权行：上游 `LICENSE.txt` 为**未填写的占位模板**（原文即 `Copyright (c) [year] [fullname]`）。
  上游未声明具体版权人；本工程依据项目谱系按 **dvize / v-engelowski / kobethuy** 署名。
- 版本：master `451c6cb`（2026-10-13，含 4.1 迁移未发版）
- 来源：https://github.com/thuynguyentrungdang/backdoorbandit
- 用途：破门能力域（锁血池 / 武器白名单 / 容器与后备箱 / C4 交互菜单）与
  Fika 无依赖事件桥（`FikaBridge`）的上游实现。

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

> 注：上游 Backdoor Bandit NG 的 `LICENSE.txt` 未填写年份与版权人（原文即
> `Copyright (c) [year] [fullname]`）。本工程不主张对上游代码的额外权利，
> 仅按 MIT 要求保留其许可证全文与来源。

---

## 2. Sage's Door Overhaul (Door-Overhaul)

- 版权行：`Copyright (c) 2026, Sage`
- 版本：v1.0.0（源码锚 SPT 4.1.6）
- 来源：https://github.com/SageSPT/Door-Overhaul
- 用途：门体破坏模拟（弹孔 / 碎片 / 铰链 / 门闩 / 门板脱落）与调试面板的上游实现；门类交互能力（Peek / Open Further / Open Quietly / 推门 / 踢门持枪）在后续工单并入。
- 本工程对上游代码的修改（BSD-3 允许，特此说明）：命中分派改为单一观察点（`BallisticCollider.ApplyHit` postfix），取代上游 `collider.OnHitAction` 订阅；移除上锁门豁免（`locked: untouchable`）；日志与配置接入本工程（`DoorBreachPlugin.Logger` / 本工程 F12 配置面）。修改后的源码以本仓库为准。

```
BSD 3-Clause License

Copyright (c) 2026, Sage

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
```

---

## 本工程（Door Breach Overhaul）

- 版权行：`Copyright (c) 2026 SamMeow`
- 许可：MIT（全文见仓库根 [LICENSE](LICENSE)）
- 本工程为上述上游项目的 SPT 4.1.6 合并与移植产物；分发时同时保留本文件与仓库根 `LICENSE`。
