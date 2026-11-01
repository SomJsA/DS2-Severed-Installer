from math import ceil
import numpy
import cv2

from os import remove
from PIL import Image, ImageDraw, ImageFont
from fontTools.ttLib import TTFont


class fonts_manager:
    def __init__(self) -> None:
        self.fonts = {}

    def load(self, font_name, textSize=40):
        if not self.fonts.get(font_name):
            ttf = TTFont(f'data/{font_name}.ttf')
            cmap = ttf.getBestCmap()
            self.fonts[font_name] = {
                'ttf': ttf,
                'cmap': cmap
            }
        self.fonts[font_name][textSize] = ImageFont.truetype(
            f'data/{font_name}.ttf', textSize, encoding='utf-8')

    def get_font(self, font_name, textSize):
        try:
            return self.fonts[font_name][textSize]
        except Exception as e:
            self.load(font_name, textSize)
            return self.get_font(font_name, textSize)

    def get_letter_info(self,letter,font_name,textSize):
        try:
            ttf = self.fonts[font_name]['ttf']
            cmap = self.fonts[font_name]['cmap']
        except KeyError as e:
            self.load(font_name)
            return self.get_letter_info(letter,font_name,textSize)

        try:
            unitsPerEm = ttf['head'].unitsPerEm

            letter_c = cmap[ord(letter)]

            try:
                glyf = ttf['glyf'][letter_c]
                hmtx = ttf['hmtx'][letter_c] if 'hmtx' in ttf.keys() else (0,0)
                if 'vmtx' in ttf.keys():
                    vmtx = ttf['vmtx'][letter_c]
                else:
                    vmtx = (0,0)

                width = (glyf.xMax-glyf.xMin)*textSize/unitsPerEm
                height = (glyf.yMax - glyf.yMin)*textSize/unitsPerEm
            except Exception as e:
                hmtx = vmtx = (0,0)
                width = 1
                height = 1

            return {
                'width':width,
                'height':height,
                'xoffset':hmtx[1]*textSize/unitsPerEm,
                'yoffset':vmtx[1]*textSize/unitsPerEm
            }
        except KeyError as e:
            pass

fm = fonts_manager()

def generate_tg4d(png_path):
    tg4d_path = "_".join(png_path.replace('.png', '').split('_')[1:])
    dds_path = png_path.replace('.png', '.dds')
    payload = bytearray()

    with Image.open(png_path) as source:
        image = source.convert("RGBA")
        while True:
            image.save(dds_path, format="DDS", pixel_format="DXT5")
            with open(dds_path, 'rb') as dds_f:
                payload.extend(dds_f.read()[128:])
            if image.width == 1 and image.height == 1:
                break
            image = image.resize(
                (max(1, image.width // 2), max(1, image.height // 2)),
                Image.Resampling.LANCZOS,
            )

    _ = str(int(png_path[:4])+1).rjust(4, '0')
    with open(f'ds2output/tg4d/{_}_{tg4d_path}.tg4d', 'wb+') as tg4d:
        tg4d.write(payload)

    remove(dds_path)

def load_txt(filename):
    # def weight(letter):
    #     try:
    #         _ = ttf_cmap[ord(letter)]
    #         glyf = ttf['glyf'][_]
    #         width = (glyf.xMax-glyf.xMin)
    #         height = (glyf.yMax - glyf.yMin)
    #         return width*height
    #     except Exception:
    #         return 9999*9999

    # def weight(letter):
    #     uni = letter.encode('unicode-escape').decode()
    #     if uni.startswith('\\u'):
    #         return int(uni[2:], 16)
    #     return 0

    def weight(letter):
        if letter in '？“”[]。！，&*：·%；;':
            return 0
        return ord(letter)

    with open(filename, 'r', encoding='utf8') as f:
        text = f.read()

    # 去重排序
    texts = list(set(list(text)))
    texts.sort(key=weight, reverse=False)

    return ''.join(texts)

def cv2pil(_image):
    return Image.fromarray(cv2.cvtColor(_image, cv2.COLOR_BGRA2RGBA))


def pil2cv(_image):
    return cv2.cvtColor(numpy.asarray(_image), cv2.COLOR_RGBA2BGRA)


def letter_render(_image, content, position, textSize, font_name=''):
    position = list(position)
    if content == '_':
        return _image
    if font_name == 'mstiffhei':
        position[1] -= 3
    elif font_name == '微软雅黑':
        position[1] -= 5
    elif font_name == 'font':
        position[0] += 1

    font = fm.get_font(font_name,textSize)
    l_info = fm.get_letter_info(content,font_name,textSize)
    position[0] -= ceil(l_info['xoffset'])
    position[1] -= ceil(l_info['yoffset'])

    if(isinstance(_image, numpy.ndarray)):
        _image = Image.fromarray(cv2.cvtColor(_image, cv2.COLOR_BGRA2RGBA))
    draw = ImageDraw.Draw(_image)
    draw.text(position, content, (255, 255, 255), font=font)
    return _image
