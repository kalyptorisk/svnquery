@echo off

set msbuild=%SystemRoot%\Microsoft.NET\Framework\v3.5\msbuild.exe

%msbuild% build.proj /t:Rebuild;RunTests;Publish /p:Configuration=Release

pause
