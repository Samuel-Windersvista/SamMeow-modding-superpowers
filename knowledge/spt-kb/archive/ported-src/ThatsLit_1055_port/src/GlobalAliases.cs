// 3.11 -> 4.1 port: disambiguate System vs UnityEngine members used bare in the
// decompiled source (original 3.11 project had these aliases implicitly via
// the obfuscated-name resolver; the 4.1 deobfuscated assemblies expose both).
global using Object = UnityEngine.Object;
global using Random = UnityEngine.Random;
