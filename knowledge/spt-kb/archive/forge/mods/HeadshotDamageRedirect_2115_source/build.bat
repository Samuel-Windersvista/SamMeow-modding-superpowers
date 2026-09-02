@echo off
call "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsMSBuildCmd.bat"
msbuild HeadshotDamageRedirect.csproj -p:Configuration=Release -v:minimal
