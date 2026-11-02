@echo off
setlocal
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars32.bat" >nul
if errorlevel 1 exit /b 1
cd /d "%~dp0"
if not exist build mkdir build
cl /nologo /c /O2 /MT /TP /EHsc /std:c++latest /DSEVERED_ASI /Fo:build\probe_core.obj src\probe_core.c
if errorlevel 1 exit /b 1
cl /nologo /c /O2 /MT /TC /Fo:build\Zydis.obj vendor\safetyhook\Zydis.c
if errorlevel 1 exit /b 1
cl /nologo /c /O2 /MT /EHsc /std:c++latest /Ivendor /Fo:build\safetyhook.obj vendor\safetyhook\safetyhook.cpp
if errorlevel 1 exit /b 1
cl /nologo /c /O2 /MT /EHsc /std:c++latest /Ivendor /Fo:build\shop_runtime.obj src\shop_runtime.cpp
if errorlevel 1 exit /b 1
link /nologo /DLL /MACHINE:X86 /OUT:build\SeveredPC.asi build\probe_core.obj build\shop_runtime.obj build\safetyhook.obj build\Zydis.obj user32.lib kernel32.lib
if errorlevel 1 exit /b 1
cl /nologo /c /O2 /MT /EHsc /std:c++latest /DDS2FIX_TEST /Ivendor /Fo:build\shop_runtime_test.obj src\shop_runtime.cpp
if errorlevel 1 exit /b 1
cl /nologo /O2 /MT /EHsc /std:c++latest /Ivendor /Febuild\shop_policy_tests.exe /Fobuild\shop_policy_tests.obj tests\shop_policy_tests.cpp build\shop_runtime_test.obj build\safetyhook.obj build\Zydis.obj /link /MACHINE:X86
if errorlevel 1 exit /b 1
build\shop_policy_tests.exe
if errorlevel 1 exit /b 1
cl /nologo /c /O2 /MT /TP /EHsc /std:c++latest /DSEVERED_ASI /DDS2FIX_CORE_TEST /Fo:build\probe_core_test.obj src\probe_core.c
if errorlevel 1 exit /b 1
cl /nologo /O2 /MT /EHsc /std:c++latest /Febuild\core_abi_tests.exe /Fobuild\core_abi_tests.obj tests\core_abi_tests.cpp build\probe_core_test.obj user32.lib kernel32.lib /link /MACHINE:X86
if errorlevel 1 exit /b 1
build\core_abi_tests.exe
exit /b %errorlevel%
