"""Correct Chinese separators and restore Latin/digit TTF advance and bearings.

Only xAdvance and xOffset are changed. Atlas data, glyph bounds/UVs, vertical
metrics, punctuation, Chinese character metrics and existing pair data stay exact.
"""
from pathlib import Path
from fontTools.ttLib import TTFont
import argparse,hashlib,json,struct,sys,math

SOURCE_SHA='b17e968af9eb78beb3f3f6261cc36c5965f774997a8a2be9cdcc171d1ca9e3f0'
FONT_SOURCES={'russellsquare32.ffn':('story','df140c929d10307ca270de1161134d20bf1cc3b12aaee7b6df50ebfb4b8ceb2d'),
 'eurostileltstdbold32.ffn':('title','c14c692b526830a48bfa7d946afb3beb350f078abad3cd67d245877fed52b751'),
 'ishimura_sans_bold.ffn':('title','c14c692b526830a48bfa7d946afb3beb350f078abad3cd67d245877fed52b751')}
def sha(b):return hashlib.sha256(b).hexdigest()
def rounded(v):return math.floor(v+.5)

def patch(data,font):
    assert data[:4]==b'TNOF';n=struct.unpack_from('<H',data,10)[0]
    cmap=font.getBestCmap();units=font['head'].unitsPerEm;out=bytearray(data);rows=[];allowed=set();latin=0
    for i in range(n):
        at=1104+24*i;r=struct.unpack_from('<f3H3h4H',data,at);code=r[1];old_advance,old_left=r[4:6]
        if code==32:
            assert old_advance==5;advance,left=3,old_left;policy='word_and_chinese_separator'
        elif 65<=code<=90 or 97<=code<=122 or 48<=code<=57:
            a,l=font['hmtx'][cmap[code]];advance,left=rounded(a*40/units),rounded(l*40/units);policy='source_latin_metrics';latin+=1
        else:continue
        assert -32768<=left<=32767 and 0<advance<=32767
        struct.pack_into('<hh',out,at+10,advance,left);allowed.update(range(at+10,at+14))
        rows.append({'character':chr(code),'old_advance':old_advance,'new_advance':advance,'old_left':old_left,'new_left':left,'policy':policy})
    assert latin==62 and len(rows)==63
    assert all(i in allowed for i,(a,b) in enumerate(zip(data,out)) if a!=b)
    assert out[:1104]==data[:1104] and out[1104+24*n:]==data[1104+24*n:]
    return bytes(out),{'glyph_count':n,'latin_digit_glyphs':latin,'space_advance':3,'rows':rows,'pair_data_and_padding_exact':True}

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for flag in ['input','output','report','research-tools','story-font','title-font']:p.add_argument('--'+flag,type=Path,required=True)
    a=p.parse_args();assert not a.output.exists();source=a.input.read_bytes();assert sha(source)==SOURCE_SHA
    sys.path.insert(0,str(a.research_tools));from ds2 import streamfile,streamwrite
    be,assets=streamfile.read(source);assert not be;w=streamwrite.Writer();modified={};reports={}
    for item in assets:
        payload=None
        if item.type=='FFN' and item.name in FONT_SOURCES:
            role,want=FONT_SOURCES[item.name];path=getattr(a,role+'_font');font_bytes=path.read_bytes();assert sha(font_bytes)==want
            font=TTFont(path);payload,report=patch(item.payload(),font);font.close();report.update(source_ttf_sha256=want,role=role);reports[item.name]=report;modified[item.name]=payload
        w._put(item.header);at=0
        for kind,chunk in item.chunks:
            if payload is None:w._put(chunk);continue
            assert kind=='SDAT';n=min(item.usize-at,len(chunk)-12);c=bytearray(chunk);c[12:12+n]=payload[at:at+n];at+=n;w._put(c)
        if payload is not None:assert at==len(payload)
    result=w.data();assert len(result)==len(source);_,back=streamfile.read(result)
    assert len(assets)==len(back) and len(reports)==3
    for old,new in zip(assets,back):
        assert old.header==new.header and old.offset==new.offset
        if old.type=='FFN' and old.name in modified:assert new.payload()==modified[old.name]
        else:assert old.chunks==new.chunks and old.payload()==new.payload()
    a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_bytes(result)
    report={'status':'PASS_READABLE_SPACING_METADATA','source_sha256':SOURCE_SHA,'output_sha256':sha(result),'output_bytes':len(result),'space_advance':3,'latin_policy':'rounded source TTF hmtx advance and left bearing at 40 units','all_atlas_chunks_exact':True,'all_other_assets_exact':True,'cjk_and_punctuation_metrics_exact':True,'font_roles_unchanged':True,'fonts':reports}
    a.report.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:v for k,v in report.items() if k!='fonts'},indent=2))
if __name__=='__main__':main()
