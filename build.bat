@echo off
cd /d "%~dp0"
set CSC="%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set REFS=/r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Xml.dll

rem 1) temporary build to generate the cursor-shaped icon
%CSC% /nologo /codepage:65001 /target:winexe /out:_tmp.exe %REFS% MyCursor.cs || goto fail
start "" /wait "%~dp0_tmp.exe" --make-icon "%~dp0app.ico"
del _tmp.exe

rem 2) final build with icon
%CSC% /nologo /codepage:65001 /target:winexe /optimize+ /win32icon:app.ico /out:MyCursor.exe %REFS% MyCursor.cs || goto fail
echo Build OK: MyCursor.exe
exit /b 0
:fail
echo Build FAILED
pause
exit /b 1
