# 许可与归属 / License and Attribution

本目录（`mods/SPT5-AccurateCircularRadar/`）是对上游 mod 的 **SPT 5.0 移植副本**。

## 上游作品

- **名称**：Accurate Circular Radar（项目名 `Tyrian-Radar`）
- **作者**：Leonana69
- **来源**：<https://github.com/Leonana69/Tyrian-Radar-Standalone>
- **版本**：1.3.4
- **许可证**：Creative Commons Attribution 3.0（CC BY 3.0）

## 许可条款

上游作品按 **Creative Commons Attribution 3.0** 许可发布。许可证全文见：

<https://creativecommons.org/licenses/by/3.0/legalcode>

该许可证允许复制、分发、改编与商业使用，条件是**保留署名**（Attribution）。

## 本移植的署名与改动说明

本移植副本：

- **移植作者：SamMeow**（上游作者 Leonana69 未参与本移植）；移植版本号 `1.3.4-spt5.1`；
- 保留上游代码的 `namespace Radar`、类型名、配置键与 GUID（`com.leonana69.radar`）；
- 保留上游 `bundle/` 内的 AssetBundle 与贴图（未重新构建）；
- 为 SPT 5.0 / EFT 1.1.5（IL2CPP / BepInEx 6 / net6.0）做了 API 适配与若干降级，
  逐条列在 `README.md` 的「移植偏差与降级记录」。

依据 CC BY 3.0 的署名要求：本移植不是上游官方发布，上游作者 Leonana69 未参与本移植，
也不对本移植的改动负责。若再分发，请同时保留本文件与 `README.md` 中的署名段落。

## 第三方组件

本工程**不分发**任何游戏或 SPT/BepInEx 程序集：所有运行时程序集均以
`<HintPath>` 指向使用者本机的 SPT 安装目录，且 `<Private>false</Private>`。
