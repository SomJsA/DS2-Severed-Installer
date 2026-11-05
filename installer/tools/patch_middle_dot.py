"""Restore periods, give commas one more left unit, and reveal the complete dialogue middle dot.

The existing atlas already contains a complete round dot. Its old UV rectangle
samples only the top two visible rows. Only the middle-dot record is resized and
repositioned to sample the complete dot; every texture byte stays exact.
"""
from pathlib import Path
import argparse,hashlib,json,struct,sys
SOURCE_SHA='370ccc2120f315e7a350eb2de956537366b9e3b3057ab33dd997c095d7e6f147'
NAMES={'russellsquare32.ffn','eurostileltstdbold32.ffn','ishimura_sans_bold.ffn'}
def sha(b):return hashlib.sha256(b).hexdigest()
def patch(data,name):
    out=bytearray(data);n=struct.unpack_from('<H',data,10)[0];allowed=set();rows=[]
    for i in range(n):
        at=1104+24*i;v=list(struct.unpack_from('<f3H3h4H',data,at));old=v[:];code=v[1]
        if code in [0x3002,0x2e]:
            v[4]-=1;v[5]-=1;policy='periods_restore_3_16';allowed.update(range(at+10,at+14))
        elif code in [0xff0c,0x2c]:
            v[4]+=1;v[5]+=1;policy='commas_left_two_units_over_3_16';allowed.update(range(at+10,at+14))
        elif code==0xb7 and name=='russellsquare32.ffn':
            assert v[2:7]==[5,5,19,7,22] and v[7:11]==[8736,0,8848,96]
            v[2:7]=[10,10,22,6,19]
            v[7:11]=[544*16,3*16,551*16,10*16]
            policy='complete_round_dot_uv_and_fuller_quad';allowed.update(range(at+6,at+24))
        else:continue
        struct.pack_into('<f3H3h4H',out,at,*v)
        rows.append({'codepoint':f'U+{code:04X}','character':chr(code),'policy':policy,'old_record':old,'new_record':v})
    assert len(rows)==(5 if name=='russellsquare32.ffn' else 4)
    assert all(i in allowed for i,(a,b) in enumerate(zip(data,out)) if a!=b)
    assert out[:1104]==data[:1104] and out[1104+24*n:]==data[1104+24*n:]
    return bytes(out),{'glyph_count':n,'modified_records':len(rows),'rows':rows,'all_other_records_and_pair_data_exact':True}
def main():
    p=argparse.ArgumentParser(description=__doc__)
    for flag in ['input','output','report','research-tools']:p.add_argument('--'+flag,type=Path,required=True)
    a=p.parse_args();assert not a.output.exists();source=a.input.read_bytes();assert sha(source)==SOURCE_SHA
    sys.path.insert(0,str(a.research_tools));from ds2 import streamfile,streamwrite
    be,assets=streamfile.read(source);assert not be;w=streamwrite.Writer();reports={};modified={}
    for item in assets:
        payload=None
        if item.type=='FFN' and item.name in NAMES:payload,reports[item.name]=patch(item.payload(),item.name);modified[item.name]=payload
        w._put(item.header);at=0
        for kind,chunk in item.chunks:
            if payload is None:w._put(chunk);continue
            assert kind=='SDAT';n=min(item.usize-at,len(chunk)-12);c=bytearray(chunk);c[12:12+n]=payload[at:at+n];at+=n;w._put(c)
        if payload is not None:assert at==len(payload)
    result=w.data();assert len(result)==len(source) and len(reports)==3;_,back=streamfile.read(result);assert len(back)==len(assets)
    for old,new in zip(assets,back):
        assert old.header==new.header and old.offset==new.offset
        if old.type=='FFN' and old.name in modified:assert new.payload()==modified[old.name]
        else:assert old.payload()==new.payload() and old.chunks==new.chunks
    a.output.write_bytes(result)
    report={'status':'PASS_COMPLETE_MIDDLE_DOT_AND_REFINED_PUNCTUATION','source_sha256':SOURCE_SHA,'output_sha256':sha(result),'output_bytes':len(result),'periods_exact_to_3_16':True,'commas_left_delta_from_3_16':2,'other_3_17_punctuation_kept':True,'dialogue_dot':'complete existing circle sampled with 10x10 quad instead of clipped 5x5 quad','all_atlases_exact':True,'chinese_latin_digits_spaces_font_roles_exact':True,'fonts':reports}
    a.report.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');print(json.dumps({k:v for k,v in report.items() if k!='fonts'},indent=2))
if __name__=='__main__':main()
