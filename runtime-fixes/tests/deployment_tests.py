import pathlib,json,shutil,subprocess,hashlib,uuid
R=pathlib.Path(r'E:\DS2-Severed-Patch');P=pathlib.Path(__file__).resolve().parents[1]
D=R/'dist/experimental/Severed-Shop-Test1-20261102'
T=R/'work/dlc-runtime-deployment-tests-20261102'/('run-'+uuid.uuid4().hex[:8]);T.mkdir(parents=True)
M=R/'maintenance/dlc-runtime-fixes-20261102'
PS=pathlib.Path(r'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe')
manifest=json.loads((D/'manifest.json').read_text())
def h(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def fixture(name,config=True,logs=True):
    p=T/name
    assert not p.exists(),'Do not overwrite previous validation fixture'
    (p/'game/plugins').mkdir(parents=True)
    g=p/'game'
    (g/'deadspace2.exe').write_bytes(b'Non-executable deployment fixture')
    (g/'DS2DAT2.DAT').write_bytes(b'Game data sentinel -- never modify')
    deps=R/'workspace/DAS2-Windows/analysis/ds2_oneclick_patch_v5/build-deps'
    shutil.copy2(deps/'SeveredPC.asi',g/'plugins/SeveredPC.asi')
    shutil.copy2(deps/'dinput8.dll',g/'dinput8.dll')
    (g/'MarkerPatch.ini').write_text('[DLC]\nEnableHackerDLC=1\n[General]\nLoadASIPlugins=1\n',encoding='ascii')
    if config:(g/'plugins/SeveredRuntimeFixes.ini').write_bytes(b'original user configuration\r\n')
    if logs:
        (g/'severed_probe.log').write_bytes(b'original core log\r\n')
        (g/'plugins/SeveredRuntimeFixes.log').write_bytes(b'original trial log\r\n')
    original={str(q.relative_to(g)):q.read_bytes() for q in g.rglob('*') if q.is_file()}
    return p,g,original
def ps(script,*args,success=True):
    run=subprocess.run([str(PS),'-NoProfile','-File',str(D/script),*map(str,args)],capture_output=True)
    if (run.returncode==0)!=success:
        raise RuntimeError(run.stdout.decode(errors='replace')+'\n'+run.stderr.decode(errors='replace'))
    return run
def apply(p,g,profile=1):
    b=p/'backups'
    ps('Apply-Test.ps1','-GameDir',g,'-Profile',profile,'-BackupRoot',b)
    leaves=list(b.iterdir());assert len(leaves)==1
    return leaves[0]
results=[]
p,g,original=fixture('normal-existing-config')
b=apply(p,g);rec=json.loads((b/'test-install.json').read_text(encoding='utf-8-sig'))
assert rec['status']=='INSTALLED' and rec['profile']==1
assert h(g/'plugins/SeveredPC.asi')==manifest['test_asi_sha256']
assert h(b/'SeveredPC.asi.original')==manifest['original_asi_sha256']
assert (g/'DS2DAT2.DAT').read_bytes()==original['DS2DAT2.DAT']
(g/'plugins/SeveredRuntimeFixes.ini').write_text('[Shop]\nProfile=2\n',encoding='ascii')
(g/'plugins/SeveredRuntimeFixes.log').write_bytes(b'trial output that must be retained\r\n')
(g/'severed_probe.log').write_bytes(b'new core log\r\n')
ps('Restore-Test.ps1','-BackupDir',b)
assert all((g/n).read_bytes()==v for n,v in original.items())
assert any(q.read_bytes()==b'trial output that must be retained\r\n' for q in (b/'after-test').iterdir())
results.append({'case':'apply and restore with existing settings/logs and edited test settings','result':'PASS','save_backup_files':len(rec['save_files'])})
p,g,original=fixture('normal-no-config-or-logs',False,False);b=apply(p,g,2)
(g/'plugins/SeveredRuntimeFixes.log').write_bytes(b'owned trial log')
(g/'severed_probe.log').write_bytes(b'owned core log')
ps('Restore-Test.ps1','-BackupDir',b)
assert all((g/n).read_bytes()==v for n,v in original.items())
assert not (g/'plugins/SeveredRuntimeFixes.ini').exists() and not (g/'plugins/SeveredRuntimeFixes.log').exists() and not (g/'severed_probe.log').exists()
results.append({'case':'restore removes only newly-created settings/logs after preserving them','result':'PASS'})
p,g,original=fixture('unknown-plugin-refused');(g/'plugins/SeveredPC.asi').write_bytes(b'unknown user plugin')
before={str(q.relative_to(g)):h(q) for q in g.rglob('*') if q.is_file()}
ps('Apply-Test.ps1','-GameDir',g,'-BackupRoot',p/'backups',success=False)
assert all(h(g/n)==v for n,v in before.items()) and not (p/'backups').exists()
results.append({'case':'unknown existing plugin refused without changes','result':'PASS'})
p,g,original=fixture('corrupt-backup-refused');b=apply(p,g);(b/'SeveredPC.asi.original').write_bytes(b'corrupt fixture backup')
before={str(q.relative_to(g)):h(q) for q in g.rglob('*') if q.is_file()}
ps('Restore-Test.ps1','-BackupDir',b,success=False)
assert all(h(g/n)==v for n,v in before.items())
results.append({'case':'corrupt backup refused without changes to current install','result':'PASS'})
p,g,original=fixture('modified-current-plugin-refused');b=apply(p,g);(g/'plugins/SeveredPC.asi').write_bytes(b'another user update')
before={str(q.relative_to(g)):h(q) for q in g.rglob('*') if q.is_file()}
ps('Restore-Test.ps1','-BackupDir',b,success=False)
assert all(h(g/n)==v for n,v in before.items())
results.append({'case':'plugin changed after trial refused on restore','result':'PASS'})
p,g,original=fixture('collect-logs');(g/'plugins/SeveredRuntimeFixes.log').write_text('10 [1] MSG hash=A53EE17B occurrence=1\n20 [1] SHOP guid=37C7B444-8607BF9D-4E59414D-36445241 profile=1 bypass=1 old=0 restored=0 result=10\n',encoding='ascii')
run=ps('Collect-TestLogs.ps1','-GameDir',g)
folders=sorted((M/'test-runs').iterdir(),key=lambda q:q.stat().st_mtime_ns);collected=folders[-1]
summary=json.loads((collected/'summary.json').read_text(encoding='utf-8-sig'))
assert summary['message_lines']==1 and summary['shop_lines']==1 and not summary['gameplay_fix_verified']
(collected/'FIXTURE_ONLY.txt').write_text('Synthetic deployment/collector validation, not game playback.',encoding='utf-8')
results.append({'case':'collector decodes structured event/store output without declaring gameplay success','result':'PASS'})
(M/'deployment-tests.json').write_text(json.dumps({'status':'PASS','engine':'Windows PowerShell 5.1','cases':results,'live_game_plugin_modified':False,'fixture_root':str(T)},ensure_ascii=True,indent=2))
print(json.dumps({'status':'PASS','cases':len(results),'details':results}))
