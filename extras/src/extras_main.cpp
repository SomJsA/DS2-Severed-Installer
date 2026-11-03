#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <wincrypt.h>
#include <cstdio>
#include <cstdint>
#include <cstring>
#include <mutex>
#include "safetyhook/safetyhook.hpp"
#include "runtime_config.hpp"
extern "C" void ds2fix_install(int);
extern "C" void ds2fix_campaign(int);
extern "C" void ds2save_start();
static HANDLE g_log=INVALID_HANDLE_VALUE;
static std::mutex g_log_lock;
static safetyhook::MidHook g_level;
static const char kLevel[]="53 55 8B E9 C7 45 18 07 00 00 00 A1 ?? ?? ?? ?? 8B 40 38 D1 E8 24 01 88 44 24 0A 8A 45 1C D0 E8 24 01 83 7D 4C 00 56 8D 75 28 88 44 24 0F 8B 06 0F 95 44 24 10 57 85 C0 75 05 B8 ?? ?? ?? ?? 68 ?? ?? ?? ?? 50 E8 ?? ?? ?? ?? 8B 0D ?? ?? ?? ??";
static const char kSave[]="B8 94 10 00 00 E8 ?? ?? ?? ?? 8B 84 24 A4 10 00 00 55 56 57 8B BC 24 A4 10 00 00 89 07 8B 84 24 AC 10 00 00 8D 34 80 03 F6 03 F6 8B E9 8B 8D F8 02 00 00";
static bool readable(const void* p,size_t size) {
 MEMORY_BASIC_INFORMATION m{};if(!p||!VirtualQuery(p,&m,sizeof(m))||m.State!=MEM_COMMIT||(m.Protect&(PAGE_GUARD|PAGE_NOACCESS)))return false;
 uintptr_t a=reinterpret_cast<uintptr_t>(p),end=reinterpret_cast<uintptr_t>(m.BaseAddress)+m.RegionSize;return a<=end&&size<=end-a;
}
extern "C" void ds2save_log(const char* text) {
 if(g_log==INVALID_HANDLE_VALUE)return;std::lock_guard lock(g_log_lock);char line[640];int n=_snprintf_s(line,sizeof(line),_TRUNCATE,"%10lu [%lu] %s\r\n",GetTickCount(),GetCurrentThreadId(),text);if(n>0){DWORD done;WriteFile(g_log,line,n,&done,nullptr);}
}
static BYTE* scan_bytes(BYTE* base,size_t size,const char* signature) {
 BYTE bytes[256]{},mask[256]{};unsigned length=0;
 for(const char* p=signature;*p;){if(*p==' '){++p;continue;}if(length>=256||!p[1])return nullptr;if(p[0]=='?'&&p[1]=='?'){length++;p+=2;continue;}unsigned v=0;if(sscanf_s(p,"%2x",&v)!=1)return nullptr;bytes[length]=BYTE(v);mask[length++]=1;p+=2;}
 if(size<length)return nullptr;BYTE* hit=nullptr;
 for(size_t at=0;at<=size-length;++at){if(base[at]!=bytes[0])continue;unsigned n=1;for(;n<length&&(!mask[n]||base[at+n]==bytes[n]);++n){}if(n==length){if(hit)return nullptr;hit=base+at;}}
 return hit;
}
static BYTE* scan_game(const char* signature) {
 BYTE* base=reinterpret_cast<BYTE*>(GetModuleHandleW(nullptr));if(!readable(base,sizeof(IMAGE_DOS_HEADER)))return nullptr;
 auto* dos=reinterpret_cast<IMAGE_DOS_HEADER*>(base);if(dos->e_magic!=IMAGE_DOS_SIGNATURE||dos->e_lfanew<=0||dos->e_lfanew>1024*1024)return nullptr;
 auto* nt=reinterpret_cast<IMAGE_NT_HEADERS*>(base+dos->e_lfanew);if(!readable(nt,sizeof(*nt))||nt->Signature!=IMAGE_NT_SIGNATURE||nt->FileHeader.Machine!=IMAGE_FILE_MACHINE_I386||!nt->FileHeader.NumberOfSections)return nullptr;
 auto* section=IMAGE_FIRST_SECTION(nt);if(!readable(section,sizeof(*section)))return nullptr;size_t count=section->Misc.VirtualSize;
 if(section->VirtualAddress>=nt->OptionalHeader.SizeOfImage||count>nt->OptionalHeader.SizeOfImage-section->VirtualAddress||!readable(base+section->VirtualAddress,count))return nullptr;
 return scan_bytes(base+section->VirtualAddress,count,signature);
}
extern "C" void* ds2save_find_builder(){return scan_game(kSave);}
static bool severed_request(const void* request) {
 if(!readable(request,0x2c))return false;const char* folder=nullptr;memcpy(&folder,static_cast<const BYTE*>(request)+0x28,4);if(!folder)return false;
 char text[128]{};size_t n=0;for(;n<sizeof(text);++n){if(!readable(folder+n,1))return false;text[n]=folder[n];if(!text[n])break;}if(n==sizeof(text))return false;
 while(n&&(text[n-1]=='\\'||text[n-1]=='/'))text[--n]=0;
 return _stricmp(text,"pdlc1_ch01_mng")==0;
}
static void observe_level(safetyhook::Context& ctx){ds2fix_campaign(severed_request(reinterpret_cast<void*>(ctx.ebp))?1:0);}
static BYTE* level_site(BYTE* anchor){static const BYTE expected[]={0x83,0xc4,0x08,0x85,0xc0,0x0f,0x95,0xc3};BYTE* site=anchor?anchor+80:nullptr;return readable(site,sizeof(expected))&&!memcmp(site,expected,sizeof(expected))?site:nullptr;}
static bool combined_core_loaded() {
 HMODULE module=GetModuleHandleW(L"SeveredPC.asi");if(!module)return false;wchar_t path[MAX_PATH]{};if(!GetModuleFileNameW(module,path,MAX_PATH))return false;
 HANDLE file=CreateFileW(path,GENERIC_READ,FILE_SHARE_READ|FILE_SHARE_WRITE|FILE_SHARE_DELETE,nullptr,OPEN_EXISTING,0,nullptr);if(file==INVALID_HANDLE_VALUE)return false;
 HCRYPTPROV provider=0;HCRYPTHASH hash=0;BYTE buffer[8192],digest[32];DWORD count=0,bytes=32;bool ok=CryptAcquireContextW(&provider,nullptr,nullptr,PROV_RSA_AES,CRYPT_VERIFYCONTEXT)&&CryptCreateHash(provider,CALG_SHA_256,0,0,&hash);size_t total=0;
 while(ok){if(!ReadFile(file,buffer,sizeof(buffer),&count,nullptr)){ok=false;break;}if(!count)break;total+=count;if(total>4*1024*1024){ok=false;break;}ok=CryptHashData(hash,buffer,count,0)!=0;}
 if(ok)ok=CryptGetHashParam(hash,HP_HASHVAL,digest,&bytes,0)!=0;if(hash)CryptDestroyHash(hash);if(provider)CryptReleaseContext(provider,0);CloseHandle(file);if(!ok)return false;
 char hex[65]{};for(unsigned i=0;i<32;i++)sprintf_s(hex+i*2,3,"%02x",digest[i]);
 const char* old[]={"fec3972b90bc48a41f854fcc783b9f18e580ff6c332f32776d6e77c07ab52fd5","bee028e9b49fb9bd0a73042f99c97d4ea4b8222be32bfdcba388a5fbbaa301a5","68a6e822ba54da3b248fb882ce121d712143644ed396f63b14bffe46f0aaad8a","02ca50558e5f2cbe6fd1c7c6fcc153869ad8b4efd261a9771eff746698569603","b0738f7c3fcb299991f96dacb04625c9185a76abd483d97c9783d6b011a3ed21","44825f30ded51c224e79371d04807432e5896315e372e6da35fbf613f5f2b716","cbc5d2a6a0eb28353e2af8c833469cd238de74a3535906501e77f235eafea475","62e35b4d9187b5e4a96ee1290db2f79d50c39998a2563eb2b5bab0080ac1a66c"};for(const char* value:old)if(!strcmp(hex,value))return true;return false;
}
static DWORD WINAPI start(void*) {
 wchar_t ini[MAX_PATH]{};if(!severed_menu_ini_path(ini,MAX_PATH))return 0;wchar_t* slash=wcsrchr(ini,L'\\');if(!slash)return 0;wcscpy_s(slash+1,MAX_PATH-size_t(slash+1-ini),L"SeveredExtras.log");
 g_log=CreateFileW(ini,GENERIC_WRITE,FILE_SHARE_READ,nullptr,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,nullptr);ds2save_log("SeveredExtras standalone-1; MarkerPatch loader; no save writes or Chinese data patching");
 if(combined_core_loaded()){ds2save_log("DISABLED: old combined SeveredPC detected; use the matching split core or upstream original to avoid duplicate hooks");return 0;}
 ds2save_start();
 for(unsigned n=0;n<300;++n){BYTE* anchor=scan_game(kLevel);if(anchor){BYTE* site=level_site(anchor);if(!site){ds2save_log("LEVEL observer layout unsupported; node fix stays inactive");return 0;}auto hook=safetyhook::MidHook::create(site,observe_level);if(!hook){ds2save_log("LEVEL observer could not attach; node fix stays inactive");return 0;}g_level=std::move(*hook);ds2save_log("LEVEL observer attached outside DLC-core entry signature; no dependency on SeveredPC");ds2fix_install(1);return 0;}Sleep(200);}
 ds2save_log("LEVEL observer unavailable; node fix stays inactive");return 0;
}
#ifndef DS2EXTRAS_TEST
BOOL WINAPI DllMain(HINSTANCE own,DWORD why,LPVOID){if(why==DLL_PROCESS_ATTACH){DisableThreadLibraryCalls(own);wchar_t exe[MAX_PATH]{};GetModuleFileNameW(nullptr,exe,MAX_PATH);const wchar_t* p=wcsrchr(exe,L'\\');if(!_wcsicmp(p?p+1:exe,L"deadspace2.exe")){HANDLE thread=CreateThread(nullptr,0,start,nullptr,0,nullptr);if(thread)CloseHandle(thread);}}return TRUE;}
#else
extern "C" bool extras_request_test(const void* p){return severed_request(p);}
extern "C" void* extras_scan_level_test(BYTE* bytes,size_t count){return level_site(scan_bytes(bytes,count,kLevel));}
extern "C" bool extras_attach_level_test(void* site){auto h=safetyhook::MidHook::create(site,observe_level);if(!h)return false;g_level=std::move(*h);return true;}
extern "C" void extras_reset_level_test(){g_level.reset();}
#endif
