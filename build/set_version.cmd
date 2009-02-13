@echo off

set msbuild=%SystemRoot%\Microsoft.NET\Framework\v3.5\msbuild.exe

%msbuild% build.proj /t:SetVersion 

pause
