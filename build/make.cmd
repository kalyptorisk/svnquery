@echo off

set msbuild=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe

%msbuild% build.proj /t:Rebuild;RunTests;Publish /p:Configuration=Release

pause
