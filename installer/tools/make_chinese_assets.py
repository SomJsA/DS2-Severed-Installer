"""Extract the two known XLSX-localized BIGH members and build the optional Chinese assets.
The final EXE embeds this small archive; marker.DLC always remains the English base.
"""
import argparse,hashlib,struct,subprocess,json
from pathlib import Path

def digest(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  while b:=f.read(8*1024*1024):h.update(b)
 return h.hexdigest()

def main():
 a=argparse.ArgumentParser();a.add_argument('--chinese-dat2',type=Path,required=True);a.add_argument('--sevenzip',required=True);a.add_argument('--output',type=Path);args=a.parse_args();root=Path(__file__).resolve().parent.parent;folder=root/'assets/chinese';target=args.output or root/'assets/Chinese.assets.R8.7z'
 if target.exists():raise SystemExit('Output exists; choose a fresh output path.')
 if digest(args.chinese_dat2)!='964725c2a6f93f6e46d5047ae4a27e99f685e7dd4c7660de4c59c65e69669a5b':raise SystemExit('Expected the validated 218-row XLSX-localized DAT2 with subtitle windows.')
 with args.chinese_dat2.open('rb') as f:
  head=f.read(16)
  if head[:4]!=b'BIGH':raise SystemExit('Not BIGH')
  count=struct.unpack_from('>I',head,8)[0];index={h:(o,n) for o,n,h in struct.iter_unpack('>III',f.read(count*12))}
  for key,name,want in [(0x4F21D71F,'DLC.4F21D71F.str','a6b9f412502e12bc996cbe0290e4839189c2a59924097c39c4897b166ca9d15e'),(0x4F9AE556,'DLC.4F9AE556.toc','4771a3b58d03a6216dc5e802ca5e04676ee638ae8e1f0f49485f9f88abb6bd35')]:
   off,n=index[key];f.seek(off);data=f.read(n)
   if hashlib.sha256(data).hexdigest()!=want:raise SystemExit('Chinese member mismatch: '+name)
   (folder/name).write_bytes(data)
 target=target.resolve();target.parent.mkdir(parents=True,exist_ok=True)
 for name in ['Frontend.title.str','Frontend.title.toc']:
  if not (folder/name).is_file():raise SystemExit('Build the verified title-font overlay first: '+name)
 subprocess.run([args.sevenzip,'a','-t7z','-m0=lzma2:d64m','-mx=9','-mmt=4',str(target),'4564F3D7.str','D22D5B5A.str','DLC.4F21D71F.str','DLC.4F9AE556.toc','Frontend.title.str','Frontend.title.toc'],cwd=folder,check=True)
 print(json.dumps({'path':str(target),'bytes':target.stat().st_size,'sha256':digest(target)},indent=2))
if __name__=='__main__':main()
