@echo off
setlocal
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars32.bat" >nul
if errorlevel 1 exit /b 1
cd /d "%~dp0"
if not exist build mkdir build
cl /nologo /c /O2 /MT /TC /Fo:build\Zydis.obj vendor\safetyhook\Zydis.c
if errorlevel 1 exit /b 1
for %%T in (safetyhook) do cl /nologo /c /O2 /MT /EHsc /std:c++latest /Ivendor /Fo:build\%%T.obj vendor\safetyhook\%%T.cpp
if errorlevel 1 exit /b 1
for %%T in (extras_main shop_runtime save_titles) do (
 cl /nologo /c /O2 /MT /EHsc /std:c++latest /Ivendor /Fo:build\%%T.obj src\%%T.cpp
 if errorlevel 1 exit /b 1
)
link /nologo /DLL /MACHINE:X86 /OUT:build\SeveredExtras.asi build\extras_main.obj build\shop_runtime.obj build\save_titles.obj build\safetyhook.obj build\Zydis.obj user32.lib kernel32.lib advapi32.lib shell32.lib
if errorlevel 1 exit /b 1
cl /nologo /c /O2 /MT /EHsc /std:c++latest /DDS2FIX_TEST /Ivendor /Fo:build\shop_test.obj src\shop_runtime.cpp
if errorlevel 1 exit /b 1
cl /nologo /O2 /MT /EHsc /std:c++latest /Ivendor /Febuild\shop_policy_tests.exe /Fobuild\shop_policy_tests.obj tests\shop_policy_tests.cpp build\shop_test.obj build\safetyhook.obj build\Zydis.obj /link /MACHINE:X86
if errorlevel 1 exit /b 1
build\shop_policy_tests.exe
if errorlevel 1 exit /b 1
cl /nologo /c /O2 /MT /EHsc /std:c++latest /DDS2SAVE_TEST /Ivendor /Fo:build\save_test.obj src\save_titles.cpp
if errorlevel 1 exit /b 1
cl /nologo /O2 /MT /EHsc /std:c++latest /Ivendor /Febuild\save_title_tests.exe /Fobuild\save_title_tests.obj tests\save_title_tests.cpp build\save_test.obj build\safetyhook.obj build\Zydis.obj user32.lib kernel32.lib advapi32.lib shell32.lib /link /MACHINE:X86
if errorlevel 1 exit /b 1
build\save_title_tests.exe E:\DS2-Severed-Patch\work\split-save-check-%RANDOM%-%RANDOM% E:\DS2-Severed-Patch\maintenance\save-display-live-investigation-20261103\DS2DAT3.apx.expected.DAT
exit /b %errorlevel%
