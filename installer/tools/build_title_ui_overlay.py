"""Build the Chinese title-font overlay from the privately supplied save-icon DAT3.

Usage: python build_title_ui_overlay.py --source-dat3 PATH --research-dir PATH --output-dir PATH
The proprietary input/output resources are deliberately excluded from source packages.
"""
import argparse,sys,struct,hashlib,json
from pathlib import Path

u=lambda b,p:struct.unpack_from('<I',b,p)[0]
sha=lambda b:hashlib.sha256(b).hexdigest()

def patch_apx(data):
    chunks=[];o=8;changed=0
    while o<len(data):
        n=u(data,o+4)
        if n<8 or o+n>len(data):raise ValueError('Invalid APX chunk')
        tag=data[o:o+4]
        if tag!=b'gila':chunks.append([tag,data[o+8:o+n]])
        o+=n
    d=dict(chunks)
    if b'itpa' not in d or b'snoc' not in d:return data,0
    b=bytearray(d[b'itpa']);root=u(d[b'snoc'],20)
    if u(b,root)!=9:raise ValueError('Invalid APX root')
    count,table=struct.unpack_from('<II',b,root+20)
    if count>100000 or table+4*count>len(b):raise ValueError('Invalid APX table')
    for i in range(count):
        p=u(b,table+4*i)
        if not p or u(b,p)!=3:continue
        at=u(b,p+8)
        if b[at:at+14]!=b'RussellSquare\0':continue
        struct.pack_into('<I',b,p+8,len(b));b.extend(b'eurostileltstdbold32\0');changed+=1
    if not changed:return data,0
    for chunk in chunks:
        if chunk[0]==b'itpa':chunk[1]=bytes(b)
    out=bytearray(b'fxpa'+bytes(4))
    for tag,b in chunks:
        if (len(out)+8)%16:
            pad=(-(len(out)+8))%16
            if pad<8:pad+=16
            out+=b'gila'+struct.pack('<I',pad)+bytes(pad-8)
        out+=tag+struct.pack('<I',len(b)+8)+b
    struct.pack_into('<I',out,4,len(out));return bytes(out),changed

def main():
    p=argparse.ArgumentParser();p.add_argument('--source-dat3',type=Path,required=True);p.add_argument('--research-dir',type=Path,required=True);p.add_argument('--output-dir',type=Path,required=True);a=p.parse_args()
    if sha(a.source_dat3.read_bytes())!='3e237c3d9650c39920881b6717939e47c75f0cacc99f60b1390b3769399e6db8':raise ValueError('Expected validated save-icon DAT3')
    sys.path.insert(0,str(a.research_dir));from ds2 import bigarchive,streamfile,streamwrite,toc
    with a.source_dat3.open('rb') as f:
        idx,_=bigarchive.read_index(f)
        def read(h):o,n=idx[h];f.seek(o);return f.read(n)
        original=read(0x15c7731f);table=read(0x16408156)
    _,assets=streamfile.read(original);w=streamwrite.Writer();total=0;changed=[]
    for asset in assets:
        if asset.type=='APX':b,n=patch_apx(asset.payload())
        else:n=0
        if n:
            total+=len(b)-asset.usize;w.add_asset(asset,b);changed.append(asset.name)
        else:
            w._put(asset.header)
            for _,chunk in asset.chunks:w._put(chunk)
    stream=w.data();table=toc.set_size(table,'frontend\\frontend',sum(x.usize for x in assets)+total)
    if sha(stream)!='009e420c33771ff3694c22b0f0317a30c0f46472452b2adc50a9e9c87252442c' or sha(table)!='29a17fc868d38d198aecdaf7759bca847dc967b822a7a564491f633a77023db2':raise ValueError('Output differs from verified role overlay')
    _,back=streamfile.read(stream)
    for before,after in zip(assets,back):
        if before.name not in changed:assert before.payload()==after.payload()
    a.output_dir.mkdir(parents=True,exist_ok=True)
    (a.output_dir/'Frontend.title.str').write_bytes(stream);(a.output_dir/'Frontend.title.toc').write_bytes(table)
    (a.output_dir/'title-font-manifest.json').write_text(json.dumps({'changed_movies':changed,'font':'eurostileltstdbold32','save_artwork_preserved':True,'stream_sha256':sha(stream),'toc_sha256':sha(table)},indent=2),encoding='utf-8')
    print('Verified',len(changed),'title movies; all other payloads preserved')
if __name__=='__main__':main()
