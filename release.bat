@echo off
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe /property:GenerateArchive=True;Configuration=Release Argopt.Sln
pause