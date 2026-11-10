@echo off
setlocal
cd /d "%~dp0"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo Не найден компилятор C# ^(.NET Framework 4^).
  if not defined CI pause
  exit /b 1
)

echo Сборка ArgDump.exe ...
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 /out:..\ArgDump.exe ArgDump.cs
if errorlevel 1 goto fail

echo Сборка ZapretControl.exe ...
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 /win32icon:app.ico /out:..\ZapretControl.exe ^
  /resource:..\ArgDump.exe,ZapretControl.ArgDump.exe ^
  /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.ServiceProcess.dll ^
  /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll ^
  Program.cs Core.cs Controls.cs MainForm.cs AssemblyInfo.cs
if errorlevel 1 goto fail

echo.
echo Готово: ..\ZapretControl.exe
exit /b 0

:fail
echo.
echo СБОРКА НЕ УДАЛАСЬ.
if not defined CI pause
exit /b 1
