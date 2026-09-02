# C �?Forge 下载归档 MANIFEST

> 来源：Forge 官方下载�?026-08-08），C 桶第二优�?第一优先
> 说明：旧版本�?.11/4.0 时代）mod，黑�?DLL 无源码；zip 归档 + 元数据留�?

| forgeId | slug | version | SPTarkov 目标 | 备注 | zip 归档 | 大小 |
|---------|------|---------|--------------|------|---------|------|
| 1023 | wtt-artem | 3.0.1 | SPTarkov 4.0.3 | 服务端黑盒DLL | �?>500MB) | 1549 MB |
| 1025 | painter | 2.0.0 | SPTarkov 4.0.4 | 服务端黑盒DLL | �?| 21 MB |
| 1125 | tactical-gear-component | 2.0.0 | SPTarkov 4.0.4 | 服务端黑盒DLL | �?>500MB) | 626 MB |
| 1140 | quest-tracker | 1.7.0 | 客户端BepInEx | 客户端DLL+bundle | �?| 0 MB |
| 1358 | brightlasers | 2.1.0 | 服务端C# | 服务�?laser.bundle | �?| 0 MB |
| 1538 | ref-spt-friendly-quests | 2.1.0 | 服务端C# | 服务�?quests.json | �?| 0 MB |
| 1539 | bosses-have-lega-medals | 2.1.0 | 服务端C# | 服务�?| �?| 0 MB |
| 1575 | increase-climb-height | 2.0.1 | 服务端C# | 服务�?| �?| 0 MB |
| 1688 | more-energy-drinks | 1.2.0 | 服务端C# | 服务�?87 bundles | �?| 111 MB |
| 1896 | tarkov-hd-item-rework-project | 0.43.5 | 服务端C# | 服务�?47 bundles | �?| 192 MB |
| 1971 | croupier-loadout-generator-flea-quicksell | 2.0.5 | 服务端C# | 服务�?6 bundles | �?| 20 MB |
| 2003 | hollywoodfx | 2.0.0 | 客户端BepInEx | 客户�?1GB资源 | �?| 258 MB |

## 反编译留档（2026-08-08�?

黑盒 DLL 已用 ilspycmd 反编译源码并归档�?`decompiled/<forgeId>/`�?
- 1023 Artem: WTTArtem 主类（商人注册）+ Helper（ILSpy �?record ModMetadata �?bug，已手写补全�?
- 1025 Painter: Painter 主类 + EpicTraderHelper
- 1125 TGC: TGC 主类�?6KB�? 6 �?Model �?

依赖：Artem/Painter 引用 WTTServerCommonLib（WTT 服务端公共库，外部依赖）�?
重编译路线（如需）：SPTarkov.Server.Core 4.1.2 NuGet + WTTServerCommonLib + Refs-410�?

## 重编译进度（2026-08-08�?

| forgeId | 状�?| 说明 |
|---------|------|------|
| 1023 Artem | **已重编译 4.1.2** | 反编�?�?表模型注�?�?部署替换 4.0 黑盒；源码留�?
ecompiled/1023-Artem/；知识见 server-mod-311-to-41.md �?7 �?|
| 1025 Painter | **已重编译 4.1.2** | �?Artem 模式；部署替换；源码留档 recompiled/1025-Painter/ |
| 1125 TacticalGear | **已重编译 4.1.2** | 表模�?+ 精简版（WTT CreateCustomItems + 商人 assort + locale）；部署替换；源码留�?recompiled/1125-TGC/ |
| 1348 Scorpion | **���ر��� 4.1.2** | ��Ы���ˣ�ILSpy �����루record ModMetadata ��д��ȫ������ģ��ע�루TradersTable/LocaleTable/HideoutTable/TemplateTable��+ TraderConfig/RagfairConfig ע�룻RouteAction 4.1 ǩ����OnLoadAsync�������滻 + ��ȫ data �غɣ�Դ����� recompiled/1348-Scorpion/ |
| 1688 HoodsEnergyDrinks | **���ر��� 4.1.2** | �����루record ModMetadata ��д��ȫΪ IModMetadata class��+ ��ģ��ע�루TemplateTable/LocationTable/GlobalTable/TradersTable��+ RagfairConfig ֱ��ע�룻OnLoadAsync��MongoId ��ʽת�������NewItemName ������� overlay [2]�¾�ˮ-HoodsEnergyDrinks + ���� mod ���ݣ�Դ����� recompiled/1688-HoodsEnergyDrinks/ |
| 2676 PitFireTeam | **已重编译 4.1.2** | 友方AI系统；ILSpy 反编译（record ModMetadata 手写补全为 IModMetadata）；表模型注入（TradersTable/LocaleTable/GlobalTable）+ LostOnDeathConfig/PmcConfig 注入；OnLoadAsync；Router=Transient(400000)/Callbacks=Transient/Service=Singleton（Scoped 触发 HttpServer DI 崩溃）；WsGroupMatchInvite* 移除改用 WsNotificationEvent+ExtensionData；FriendlyProfileController 删除（4.1.2 非 virtual + 无 TypeOverride，功能由 SocialRouter 链式路由覆盖）；运行时验证通过（SPT_410）；源码留档 recompiled/2676-PitFireTeam/ |

## SptVersion 补丁批处理（2026-08-08�?2 �?mod�?

�?Mono.Cecil 直接�?Metadata/ModMetadata 构造函数里�?ldstr SptVersion（~4.0.x �?~4.1.0），
免重编译。判断标准：�?DatabaseService/ConfigServer/DatabaseServer/LocaleService 引用�?
方法�?server-mod-311-to-41.md 7.7 节�?

1594 progressivebotsystem / 2060 SecureMapbook / 2097 botplacementsystem /
2195 ConsortiumOfThings / 2614 CustomProfiles / 2628 tarkovcraft / 874 AES
（补丁前 DLL 备份�?.bak�?
