@echo off
setlocal
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars32.bat" >nul
if errorlevel 1 exit /b 1
cd /d "%~dp0"
if not exist build mkdir build
cl /nologo /c /O2 /MT /TP /EHsc /std:c++latest /DSEVERED_ASI /Fo:build\core.obj src\probe_core.c
if errorlevel 1 exit /b 1
link /nologo /DLL /MACHINE:X86 /OUT:build\SeveredPC.asi build\core.obj user32.lib kernel32.lib
if errorlevel 1 exit /b 1
cl /nologo /c /O2 /MT /TP /EHsc /std:c++latest /DSEVERED_ASI /DDS2FIX_CORE_TEST /Fo:build\core_test.obj src\probe_core.c
if errorlevel 1 exit /b 1
for %%T in (core_abi_tests menu_abi_tests base_label_tests) do (
 cl /nologo /O2 /MT /EHsc /std:c++latest /Febuild\%%T.exe /Fobuild\%%T.obj tests\%%T.cpp build\core_test.obj user32.lib kernel32.lib /link /MACHINE:X86
 if errorlevel 1 exit /b 1
 build\%%T.exe
 if errorlevel 1 exit /b 1
)
