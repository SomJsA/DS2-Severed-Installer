#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <cstdio>
#include <cstring>
#include <string>
extern "C" void* ds2fix_core_ctor_config_test(void*,void*,void*,const wchar_t*);
extern "C" bool ds2fix_core_menu_ini_path_test(wchar_t*,DWORD);
extern "C" void ds2fix_campaign(int){}
extern "C" void ds2fix_install(int){}
extern "C" void ds2fix_message(DWORD){}
static unsigned order=0,checks=0; static bool valid=true,expectedChinese=false;
static const char expectedTitle[]="\xE6\xAD\xBB\x20\xE4\xBA\xA1\x20\xE7\xA9\xBA\x20\xE9\x97\xB4\x20\x32\xEF\xBC\x9A\x20\xE9\x98\xBB\x20\xE6\x96\xAD";
static const char expectedDescription[]="\xE7\x9B\x96\x20\xE4\xBC\xAF\xC2\xB7\xE7\xBB\xB4\x20\xE5\x8B\x92\x20\xE7\x9A\x84\x20\xE6\x95\x85\x20\xE4\xBA\x8B\xE3\x80\x82";
__declspec(noinline) static void* __fastcall fake_ctor(void* self,void*) {
    valid=valid && self==reinterpret_cast<void*>(0x12345678) && order==0; order=1; return reinterpret_cast<void*>(0x76543210);
}
__declspec(noinline) static void __fastcall fake_add(void* self,void*,const char* name,const char* desc,const char* folder,const char* streams,const char* checkpoint,int flags,int unlock,const char* extra) {
    valid=valid && self==reinterpret_cast<void*>(0x12345678) && order==1;
    valid=valid && !strcmp(name,expectedChinese?expectedTitle:"Dead Space 2: Severed") && !strcmp(desc,expectedChinese?expectedDescription:"Gabe Weller's story");
    wchar_t converted[128];valid=valid && MultiByteToWideChar(CP_UTF8,MB_ERR_INVALID_CHARS,name,-1,converted,128)>0 && MultiByteToWideChar(CP_UTF8,MB_ERR_INVALID_CHARS,desc,-1,converted,128)>0;
    valid=valid && !strcmp(folder,"pdlc1_ch01_mng") && !strcmp(streams,"pdlc1_ch01_mng.str") && !strcmp(checkpoint,"XONTREALPR_CB22182_E50854D_v3") && flags==0 && unlock==0 && extra==nullptr; order=2;
}
static bool check(const wchar_t* path,bool chinese,const char* label) {
    valid=true;order=0;expectedChinese=chinese;
    auto result=ds2fix_core_ctor_config_test(reinterpret_cast<void*>(0x12345678),reinterpret_cast<void*>(&fake_ctor),reinterpret_cast<void*>(&fake_add),path);
    bool ok=valid&&order==2&&result==reinterpret_cast<void*>(0x76543210);printf("%s %s\n",ok?"PASS":"FAIL",label);if(ok)++checks;return ok;
}
static bool write_bytes(const wchar_t* path,const void* bytes,DWORD count) { HANDLE f=CreateFileW(path,GENERIC_WRITE,0,nullptr,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,nullptr);if(f==INVALID_HANDLE_VALUE)return false;DWORD written=0;bool ok=WriteFile(f,bytes,count,&written,nullptr)&&written==count;CloseHandle(f);return ok; }
int wmain(int argc,wchar_t** argv) {
    if(argc==3) return check(argv[1],!wcscmp(argv[2],L"1"),"installer config -> real x86 constructor/AddEpisode")?0:1;
    wchar_t module[MAX_PATH];GetModuleFileNameW(nullptr,module,MAX_PATH);*wcsrchr(module,wchar_t(92))=0;std::wstring base=std::wstring(module)+L"/menu-tests-\u6D4B\u8BD5";CreateDirectoryW(base.c_str(),nullptr);
    wchar_t located[MAX_PATH];std::wstring expected=std::wstring(module)+wchar_t(92)+L"SeveredRuntimeFixes.ini";
    if(!ds2fix_core_menu_ini_path_test(located,MAX_PATH)||wcscmp(located,expected.c_str())){puts("FAIL config path follows owning plugin module");return 1;}++checks;puts("PASS config path follows owning plugin module");
    if(ds2fix_core_menu_ini_path_test(located,1)){puts("FAIL truncated config path must be refused");return 1;}++checks;puts("PASS truncated config path refused");
    if(!check((base+L"/missing.ini").c_str(),false,"missing config defaults English"))return 1;
    const char* values[]={"0","1","2","-1","true","garbage",""};
    for(unsigned i=0;i<7;++i){std::wstring path=base+L"/case"+std::to_wstring(i)+L".ini";std::string body="[Shop]\r\nProfile=0\r\n[Localization]\r\nSimplifiedChinese="+std::string(values[i])+"\r\n";if(!write_bytes(path.c_str(),body.data(),DWORD(body.size()))||!check(path.c_str(),i==1,"explicit localization flag and unchanged episode ABI"))return 1;}
    std::wstring path=base+L"/utf16.ini",body=L"\xFEFF[Localization]\r\nSimplifiedChinese=1\r\n";if(!write_bytes(path.c_str(),body.data(),DWORD(body.size()*sizeof(wchar_t)))||!check(path.c_str(),true,"Unicode path and UTF-16 INI"))return 1;
    path=base+L"/utf8bom.ini";std::string ascii="\xEF\xBB\xBF; managed settings\r\n[Localization]\r\nSimplifiedChinese=1\r\n";if(!write_bytes(path.c_str(),ascii.data(),DWORD(ascii.size()))||!check(path.c_str(),true,"UTF-8 BOM before comment"))return 1;
    printf("ALL MENU ABI CHECKS PASSED %u\n",checks);return 0;
}
