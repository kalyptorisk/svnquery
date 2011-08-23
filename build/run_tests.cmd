@echo off

set msbuild=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe

%msbuild% build.proj /t:RunTests /p:Configuration=Release

pause
