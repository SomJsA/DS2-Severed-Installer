#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <cstdio>
#include <cstring>
#include <string>
extern "C" void* ds2fix_core_base_ctor_test(void*,void*,void*,void*,const wchar_t*);
extern "C" void* ds2fix_core_resolve_assign_test(BYTE*);
extern "C" void ds2fix_campaign(int){}
extern "C" void ds2fix_install(int){}
extern "C" void ds2fix_message(DWORD){}
struct S { const char* text;DWORD length,capacity;void* release; };
struct E { S name,description,folder,streams,extra,checkpoint;DWORD flags,unlock; };
struct V { E* entries;DWORD count,capacity; };
static_assert(sizeof(S)==16&&sizeof(E)==0x68,"x86 layout");
static BYTE frontend[0x980+sizeof(V)];static E entries[3];static const char wanted[]="\xE6\xAD\xBB\x20\xE4\xBA\xA1\x20\xE7\xA9\xBA\x20\xE9\x97\xB4\x20\x32\x20\xE6\x9C\xAC\x20\xE4\xBD\x93",dlcTitle[]="\xE6\xAD\xBB\x20\xE4\xBA\xA1\x20\xE7\xA9\xBA\x20\xE9\x97\xB4\x20\x32\xEF\xBC\x9A\x20\xE9\x98\xBB\x20\xE6\x96\xAD",dlcDescription[]="\xE7\x9B\x96\x20\xE4\xBC\xAF\xC2\xB7\xE7\xBB\xB4\x20\xE5\x8B\x92\x20\xE7\x9A\x84\x20\xE6\x95\x85\x20\xE4\xBA\x8B\xE3\x80\x82";
static unsigned order,assignments,expectedAssignments,checks;static bool good,expectedChinese;
static void check(bool ok,const char* text){printf("%s %s\n",ok?"PASS":"FAIL",text);if(!ok)ExitProcess(1);++checks;}
static S string(const char* s){return {s,DWORD(strlen(s)),DWORD(strlen(s)),reinterpret_cast<void*>(0x11223344)};}
static V* vector(){return reinterpret_cast<V*>(frontend+0x980);}
static void setup(unsigned baseIndex=0){memset(frontend,0,sizeof(frontend));memset(entries,0,sizeof(entries));for(unsigned i=0;i<3;++i){entries[i].name=string("original title with symbol");entries[i].description=string("keep description");entries[i].folder=string("other_episode");entries[i].streams=string("keep stream");entries[i].extra=string("keep extra");entries[i].checkpoint=string("keep checkpoint");entries[i].flags=0x1234+i;entries[i].unlock=0xAB00+i;}entries[baseIndex].folder=string("ds_2");*vector()={entries,3,3};}
__declspec(noinline) static void* __fastcall ctor(void* self,void*){good=good&&self==frontend&&order==0;order=1;return reinterpret_cast<void*>(0x76543210);}
__declspec(noinline) static void __fastcall assign(void* self,void*,const char* text){good=good&&order==1&&!strcmp(text,wanted);auto e=static_cast<E*>(self);good=good&&e>=entries&&e<entries+3&&!strcmp(e->folder.text,"ds_2");e->name=string(text);++assignments;}
__declspec(noinline) static void __fastcall add(void* self,void*,const char* name,const char* description,const char* folder,const char* streams,const char* checkpoint,int flags,int unlock,const char* extra){good=good&&order==1&&self==frontend&&assignments==expectedAssignments;good=good&&!strcmp(name,expectedChinese?dlcTitle:"Dead Space 2: Severed")&&!strcmp(description,expectedChinese?dlcDescription:"Gabe Weller's story")&&!strcmp(folder,"pdlc1_ch01_mng")&&!strcmp(streams,"pdlc1_ch01_mng.str")&&!strcmp(checkpoint,"XONTREALPR_CB22182_E50854D_v3")&&flags==0&&unlock==0&&!extra;order=2;}
static void invoke(const wchar_t* ini,bool chinese,unsigned expected,void* fn=reinterpret_cast<void*>(&assign)){order=assignments=0;good=true;expectedChinese=chinese;expectedAssignments=expected;void* result=ds2fix_core_base_ctor_test(frontend,reinterpret_cast<void*>(&ctor),reinterpret_cast<void*>(&add),fn,ini);check(good&&order==2&&assignments==expected&&result==reinterpret_cast<void*>(0x76543210),"constructor -> base rename -> DLC append ABI and return preserved");}
int wmain(){wchar_t path[MAX_PATH];GetModuleFileNameW(nullptr,path,MAX_PATH);*wcsrchr(path,wchar_t(92))=0;std::wstring yes=std::wstring(path)+L"/base-label-yes.ini",no=std::wstring(path)+L"/base-label-no.ini";WritePrivateProfileStringW(L"Localization",L"SimplifiedChinese",L"1",yes.c_str());WritePrivateProfileStringW(L"Localization",L"SimplifiedChinese",L"0",no.c_str());
 setup();E before[3];memcpy(before,entries,sizeof(entries));invoke(yes.c_str(),true,1);check(!strcmp(entries[0].name.text,wanted),"canonical base label becomes exact user text");check(!memcmp(&entries[0].description,&before[0].description,sizeof(E)-sizeof(S)),"base description, folder, streams, checkpoint and flags unchanged");check(!memcmp(entries+1,before+1,2*sizeof(E)),"other episode entries unchanged");
 setup();memcpy(before,entries,sizeof(entries));invoke(no.c_str(),false,0);check(!memcmp(entries,before,sizeof(entries)),"English mode leaves original labels intact");
 setup(2);invoke(yes.c_str(),true,1);check(!strcmp(entries[2].name.text,wanted)&&strcmp(entries[0].name.text,wanted),"base episode recognized by folder after reordering");
 setup();entries[0].folder=string("ds_20");invoke(yes.c_str(),true,0);check(assignments==0,"folder prefixes do not match base episode");
 setup();entries[0].folder.text=reinterpret_cast<const char*>(1);invoke(yes.c_str(),true,0);
 setup();entries[0].folder.length=3;invoke(yes.c_str(),true,0);
 setup();entries[0].folder.capacity=3;invoke(yes.c_str(),true,0);
 setup();vector()->count=4;invoke(yes.c_str(),true,0);
 setup();vector()->capacity=65;invoke(yes.c_str(),true,0);
 setup();vector()->entries=reinterpret_cast<E*>(1);invoke(yes.c_str(),true,0);
 setup();invoke(yes.c_str(),true,0,nullptr);
 BYTE code[26]={0x56,0x81,0xC1,0x80,0x09,0,0,0xE8};code[21]=0xE8;int delta=int(reinterpret_cast<BYTE*>(&assign)-(code+26));memcpy(code+22,&delta,4);check(ds2fix_core_resolve_assign_test(code)==reinterpret_cast<void*>(&assign),"assignment call target resolved from validated AddEpisode shape");code[21]=0x90;check(!ds2fix_core_resolve_assign_test(code),"unexpected assignment opcode refused");code[21]=0xE8;delta=int(code-(code+26));memcpy(code+22,&delta,4);check(!ds2fix_core_resolve_assign_test(code),"non-executable assignment target refused");
 printf("ALL BASE LABEL CHECKS PASSED %u\n",checks);return 0;}
