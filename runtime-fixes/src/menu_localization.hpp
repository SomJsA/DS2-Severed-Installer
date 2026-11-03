#pragma once
#include <windows.h>
#include <wchar.h>
struct SeveredMenuText { const char* title; const char* description; bool chinese; };
static const SeveredMenuText kSeveredEnglish = { "Dead Space 2: Severed", "Gabe Weller's story", false };
static const SeveredMenuText kSeveredChinese = { "\xE6\xAD\xBB\x20\xE4\xBA\xA1\x20\xE7\xA9\xBA\x20\xE9\x97\xB4\x20\x32\xEF\xBC\x9A\x20\xE9\x98\xBB\x20\xE6\x96\xAD", "\xE7\x9B\x96\x20\xE4\xBC\xAF\xC2\xB7\xE7\xBB\xB4\x20\xE5\x8B\x92\x20\xE7\x9A\x84\x20\xE6\x95\x85\x20\xE4\xBA\x8B\xE3\x80\x82", true };
static const SeveredMenuText& severed_menu_from_ini(const wchar_t* ini) {
    return ini && GetPrivateProfileIntW(L"Localization",L"SimplifiedChinese",0,ini)==1 ? kSeveredChinese : kSeveredEnglish;
}
#ifdef DS2FIX_CORE_TEST
static const wchar_t* g_menu_test_ini = nullptr;
#endif
static bool severed_menu_ini_path(wchar_t* ini, DWORD capacity) {
    HMODULE own=nullptr;
    if(!ini || capacity==0 || !GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,reinterpret_cast<LPCWSTR>(&severed_menu_from_ini),&own)) return false;
    DWORD n=GetModuleFileNameW(own,ini,capacity);
    if(n==0 || n>=capacity) return false;
    wchar_t* slash=wcsrchr(ini,wchar_t(92));
    static const wchar_t filename[]=L"SeveredRuntimeFixes.ini";
    if(!slash || size_t(slash-ini+1)+sizeof(filename)/sizeof(filename[0])>capacity) return false;
    return wcscpy_s(slash+1,capacity-size_t(slash-ini+1),filename)==0;
}
static const SeveredMenuText& severed_menu_for_game() {
#ifdef DS2FIX_CORE_TEST
    if(g_menu_test_ini) return severed_menu_from_ini(g_menu_test_ini);
#endif
    wchar_t ini[MAX_PATH];
    return severed_menu_ini_path(ini,MAX_PATH) ? severed_menu_from_ini(ini) : kSeveredEnglish;
}
