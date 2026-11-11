@echo off
setlocal
set ROOT=%~dp0
cd /d "%ROOT%"

set FRAMEWORK=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319
if not exist "%FRAMEWORK%\csc.exe" set FRAMEWORK=%WINDIR%\Microsoft.NET\Framework\v4.0.30319
set WPF=%FRAMEWORK%\WPF
set CSC=%FRAMEWORK%\csc.exe

if not exist "%CSC%" (
  echo Could not find the .NET Framework 4.x C# compiler.
  echo Install .NET Framework 4.8 and try again.
  exit /b 1
)

if not exist dist mkdir dist

"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /debug- ^
  /out:dist\BestClip.exe ^
  /win32icon:assets\app.ico ^
  /win32manifest:app.manifest ^
  /r:"%WPF%\PresentationFramework.dll" ^
  /r:"%WPF%\PresentationCore.dll" ^
  /r:"%WPF%\WindowsBase.dll" ^
  /r:"%FRAMEWORK%\System.Xaml.dll" ^
  /r:"%FRAMEWORK%\System.dll" ^
  /r:"%FRAMEWORK%\System.Core.dll" ^
  /r:"%FRAMEWORK%\System.Xml.dll" ^
  /r:"%FRAMEWORK%\System.Drawing.dll" ^
  /r:"%FRAMEWORK%\System.Windows.Forms.dll" ^
  /r:"%FRAMEWORK%\System.Runtime.Serialization.dll" ^
  /resource:src\MainWindow.xaml,BestClip.MainWindow.xaml ^
  /resource:src\SettingsWindow.xaml,BestClip.SettingsWindow.xaml ^
  /resource:assets\app.ico,BestClip.app.ico ^
  src\*.cs

if errorlevel 1 (
  echo.
  echo Build failed.
  exit /b 1
)

copy /Y app.config dist\BestClip.exe.config >nul
echo.
echo Built dist\BestClip.exe
