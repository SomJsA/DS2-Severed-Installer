"""Reproduce 21 short Chinese action-label changes from private LH2 input."""
from pathlib import Path
import argparse,hashlib,json,struct,sys
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--source',type=Path,required=True)
p.add_argument('--output',type=Path,required=True)
p.add_argument('--research-dir',type=Path,required=True)
a=p.parse_args();sys.path.insert(0,str(a.research_dir))
from ds2 import streamfile
u=lambda b,p:struct.unpack_from('<I',b,p)[0]
source=a.source.read_bytes();assets=streamfile.read(source)[1]
languages=[v for v in assets if v.type=='LH2'];assert len(languages)==1
old=languages[0].payload();new=bytearray(old)
rows=json.loads(Path(__file__).with_name('short_action_labels.json').read_text(encoding='utf-8'))
n=u(old,16);offsets=[u(old,32+4*n+4*i) for i in range(n)]
for row in rows:
 i=next(i for i in range(n) if u(old,32+4*i)==int(row['id'],16));at=offsets[i];end=old.index(0,at)
 assert old[at:end].decode('utf-8')==row['chinese_before']
 assert not any(at<x<end for x in offsets)
 text=row['chinese_trial'].encode('utf-8');assert len(text)<=end-at
 new[at:end+1]=text+bytes(end+1-at-len(text))
changed=[]
for i,at in enumerate(offsets):
 if old[at:old.index(0,at)]!=new[at:new.index(0,at)]:changed.append('%08X'%u(old,32+4*i))
assert set(changed)=={x['id'] for x in rows} and len(changed)==21
out=bytearray(source);position=12;current=None;consumed=0
while position+12<=len(source):
 size=u(source,position+4)
 if not size:break
 tag=source[position:position+4][::-1];sub=source[position+8:position+12][::-1]
 if tag==b'SHOC' and sub==b'SHDR':current=streamfile.Asset(position,source[position:position+size],False)
 elif tag==b'SHOC' and current and current.type=='LH2':
  assert sub==b'SDAT';take=min(size-12,len(new)-consumed);out[position+12:position+12+take]=new[consumed:consumed+take];consumed+=take
 position+=size
assert consumed==len(new) and len(out)==len(source)
for before,after in zip(assets,streamfile.read(out)[1]):assert after.payload()==(bytes(new) if before.type=='LH2' else before.payload())
assert hashlib.sha256(out).hexdigest()=='06bd09bf7304dc2336379a089a045baaa6ee4cc3cf5ea56cb043e5fbc1161121'
with a.output.open('xb') as f:f.write(out)
print('Verified 21 exact labels; all other strings and asset payloads preserved')
