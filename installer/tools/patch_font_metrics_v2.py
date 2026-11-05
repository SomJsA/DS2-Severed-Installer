"""Candidate 3 spacing patch: native CJK advance, adjusted Latin and punctuation."""
from pathlib import Path
import argparse,hashlib,json,struct,sys
BASE_SHA='9e2332d439cf845908071c818ae09db4d73e5eb88833c3de8d988e2dfee71506'
FONTS={'russellsquare32.ffn','eurostileltstdbold32.ffn','ishimura_sans_bold.ffn'}
OPEN=set('（【《〈「『〔［｛');CLOSE=set('）】》〉」』〕］｝');MARKS=set('，。！？、')
def sha(data):return hashlib.sha256(data).hexdigest()
def adjust(code,width,height,advance,left):
    c=chr(code)
    if not width or not height or advance<=0:return advance,left,None
    if c==' ':return max(advance,5),left,'word_space'
    if 65<=code<=90 or 97<=code<=122 or 48<=code<=57:return advance+1,left,'latin_digit'
    if 0x3400<=code<=0x4DBF or 0x4E00<=code<=0x9FFF or 0xF900<=code<=0xFAFF:return advance,left,None
    if c in '：；':
        a=max(advance,width+8,32);return a,(a-width)//2,'colon_semicolon'
    if c in OPEN:
        a=max(advance,width+10,26);return a,a-width-4,'opening_bracket'
    if c in CLOSE:
        a=max(advance,width+10,26);return a,4,'closing_bracket'
    if c in MARKS:return max(advance,width+8,30),3,'cjk_mark'
    if c in '“‘':
        a=max(advance+4,width+10,20);return a,a-width-3,'opening_quote'
    if c in '”’':return max(advance+4,width+10,20),3,'closing_quote'
    if c=='·':
        a=max(advance+4,width+8,18);return a,(a-width)//2,'middle_dot'
    if c in ':;':return advance+4,left+2,'ascii_colon_semicolon'
    if c in '()[]{}':return advance+4,left+2,'ascii_bracket'
    if c in ',.!?':return advance+3,left+1,'ascii_mark'
    if c in '-/\\|_+=%$#@&*' or c in '—–…':return advance+3,left+1,'other_punctuation'
    return advance,left,None

def patch_font(data):
    assert data[:4]==b'TNOF';count=struct.unpack_from('<H',data,10)[0];assert 1104+24*count<=len(data)
    out=bytearray(data);changes=[];allowed=set();kinds={}
    for i in range(count):
        at=1104+24*i;row=struct.unpack_from('<f3H3h4H',data,at);code,width,height,advance,left=row[1:6]
        a,x,kind=adjust(code,width,height,advance,left)
        assert -32768<=x<=32767 and -32768<=a<=32767
        if (a,x)!=(advance,left):
            assert kind is not None
            struct.pack_into('<hh',out,at+10,a,x);allowed.update(range(at+10,at+14));kinds[kind]=kinds.get(kind,0)+1
            changes.append({'index':i,'codepoint':code,'character':chr(code),'width':width,'old_advance':advance,'new_advance':a,'old_left':left,'new_left':x,'policy':kind})
    assert all(i in allowed for i,(a,b) in enumerate(zip(data,out)) if a!=b)
    assert len(out)==len(data) and out[:1104]==data[:1104] and out[1104+24*count:]==data[1104+24*count:]
    return bytes(out),{'glyph_count':count,'changed_glyphs':len(changes),'kinds':kinds,'changes':changes,'ascii_images_exact_metrics_deliberately_adjusted':True,'uv_width_height_vertical_and_font_header_exact':True}

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--input',type=Path,required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--research-tools',type=Path,required=True);p.add_argument('--report',type=Path,required=True);a=p.parse_args()
    assert not a.output.exists();source=a.input.read_bytes();assert sha(source)==BASE_SHA
    sys.path.insert(0,str(a.research_tools));from ds2 import streamfile,streamwrite
    be,assets=streamfile.read(source);assert not be and source[:12]==streamwrite.FILE_HEADER
    writer=streamwrite.Writer();reports={};modified={}
    for asset in assets:
        payload=None
        if asset.type=='FFN' and asset.name in FONTS:
            payload,report=patch_font(asset.payload());reports[asset.name]=report;modified[asset.name]=payload
        writer._put(asset.header);at=0
        for kind,chunk in asset.chunks:
            if payload is not None:
                assert kind=='SDAT','Known baseline FFN chunks must be raw'
                n=min(asset.usize-at,len(chunk)-12);assert n>=0;part=bytearray(chunk);part[12:12+n]=payload[at:at+n];at+=n;writer._put(bytes(part))
            else:writer._put(chunk)
        if payload is not None:assert at==len(payload)
    output=writer.data();assert set(reports)==FONTS and len(output)==len(source)
    _,actual=streamfile.read(output);assert len(actual)==len(assets)
    for before,after in zip(assets,actual):
        assert before.header==after.header and before.offset==after.offset
        if before.name in modified and before.type=='FFN':assert after.payload()==modified[before.name]
        else:assert before.chunks==after.chunks and before.payload()==after.payload()
    a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_bytes(output)
    report={'status':'OFFLINE_FONT_METRICS_CANDIDATE3','source_sha256':BASE_SHA,'output_sha256':sha(output),'output_bytes':len(output),'assets':len(assets),'fonts':reports,'face_roles':{'russellsquare32.ffn':'title/MStiffHei','eurostileltstdbold32.ffn':'story/SweiShearSansCJKsc-Regular','ishimura_sans_bold.ffn':'story/SweiShearSansCJKsc-Regular'},'all_texture_chunks_exact':True,'all_other_assets_exact':True,'font_count_unchanged':True,'line_height_unchanged':True,'cjk_advance_delta':0,'latin_digit_advance_delta':1,'word_space_min':5,'no_game_or_save_writes':True}
    a.report.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps({'status':report['status'],'font_stream_sha256':report['output_sha256'],'bytes':len(output),'fonts':{k:{x:v[x] for x in ['glyph_count','changed_glyphs','kinds']} for k,v in reports.items()},'textures_unchanged':True},ensure_ascii=False,indent=2))
if __name__=='__main__':main()
