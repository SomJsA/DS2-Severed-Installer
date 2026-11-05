"""Add one build unit before selected separators and sentence punctuation.

The xAdvance increase equals xOffset increase, preserving right-side whitespace.
Only the three localized FFN resources change; atlases, other characters, pair
data and all remaining resource bytes are retained from the exact 3.16 input.
"""
from pathlib import Path
import argparse, hashlib, json, struct, sys

SOURCE_SHA='ad6071ed6eb6a6b9dfceeb438afd44cf210f92fc8b7f9d294eac436261894beb'
FONT_NAMES={'russellsquare32.ffn','eurostileltstdbold32.ffn','ishimura_sans_bold.ffn'}
PUNCTUATION='·，。、；：？！…,.!?;:'
def sha(b):return hashlib.sha256(b).hexdigest()

def patch(data):
    assert data[:4]==b'TNOF'
    count=struct.unpack_from('<H',data,10)[0];out=bytearray(data);rows=[];allowed=set()
    for i in range(count):
        at=1104+24*i;r=struct.unpack_from('<f3H3h4H',data,at);c=chr(r[1])
        if c not in PUNCTUATION:continue
        advance,left=r[4:6];assert 0<advance<32767 and left<32767
        struct.pack_into('<hh',out,at+10,advance+1,left+1)
        allowed.update(range(at+10,at+14))
        rows.append({'character':c,'codepoint':f'U+{ord(c):04X}','old_advance':advance,'new_advance':advance+1,'old_left':left,'new_left':left+1,'right_padding_exact':(advance-left-r[2])==((advance+1)-(left+1)-r[2])})
    assert {'·','，','。',',','.'}.issubset({v['character'] for v in rows})
    assert all(i in allowed for i,(a,b) in enumerate(zip(data,out)) if a!=b)
    assert out[:1104]==data[:1104] and out[1104+24*count:]==data[1104+24*count:]
    return bytes(out),{'glyph_count':count,'modified_glyphs':len(rows),'rows':rows,'all_other_glyphs_exact':True,'pair_data_and_padding_exact':True}

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ['input','output','report','research-tools']:p.add_argument('--'+name,type=Path,required=True)
    a=p.parse_args();assert not a.output.exists();source=a.input.read_bytes();assert sha(source)==SOURCE_SHA
    sys.path.insert(0,str(a.research_tools));from ds2 import streamfile,streamwrite
    be,assets=streamfile.read(source);assert not be;w=streamwrite.Writer();reports={};modified={}
    for item in assets:
        payload=None
        if item.type=='FFN' and item.name in FONT_NAMES:
            payload,report=patch(item.payload());modified[item.name]=payload;reports[item.name]=report
        w._put(item.header);at=0
        for kind,chunk in item.chunks:
            if payload is None:w._put(chunk);continue
            assert kind=='SDAT';n=min(item.usize-at,len(chunk)-12);c=bytearray(chunk);c[12:12+n]=payload[at:at+n];at+=n;w._put(c)
        if payload is not None:assert at==len(payload)
    result=w.data();assert len(result)==len(source) and len(reports)==3
    _,back=streamfile.read(result);assert len(assets)==len(back)
    for old,new in zip(assets,back):
        assert old.header==new.header and old.offset==new.offset
        if old.type=='FFN' and old.name in modified:assert new.payload()==modified[old.name]
        else:assert old.chunks==new.chunks and old.payload()==new.payload()
    a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_bytes(result)
    report={'status':'PASS_PUNCTUATION_LEFT_PADDING','source_sha256':SOURCE_SHA,'output_sha256':sha(result),'output_bytes':len(result),'left_padding_delta':1,'advance_delta':1,'selected_punctuation':PUNCTUATION,'all_atlas_chunks_exact':True,'all_other_assets_exact':True,'chinese_latin_digits_spaces_exact':True,'font_roles_unchanged':True,'fonts':reports}
    a.report.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in report.items() if k!='fonts'},ensure_ascii=True,indent=2))
if __name__=='__main__':main()
