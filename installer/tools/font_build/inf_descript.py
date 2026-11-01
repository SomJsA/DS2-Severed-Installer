import re
from turtle import width

def _int(value):
    _ = hex(value)[2:]
    assert len(_) <= 4
    _ = _.rjust(4,'0')
    return _[2:]+_[:2]

_int(320)
class ds3letter:
    def __init__(self, _hex) -> None:
        if isinstance(_hex,bytes):
            _ = re.findall('.{2}', _hex.decode())
            _hex = " ".join(_)
        self._hex = ''.join(_hex.split(' '))

        self.load()

    def load(self):
        _hex = self._hex
        _ = _hex[:4]
        letter_ascii = int((_[2:4]+_[:2]), 16)
        self.char = chr(letter_ascii)
        self.width = int(int(_hex[4:6], 16))
        self.height = int(int(_hex[8:10], 16))
        self.startX = int(int(_hex[26:28]+_hex[24:26], 16)/16)
        self.startY = int(int(_hex[30:32]+_hex[28:30], 16)/16)
        self.endX = int(int(_hex[34:36]+_hex[32:34], 16)/16)
        self.endY = int(int(_hex[38:40]+_hex[36:38], 16)/16)

    def dump(self):
        inf = ['00000000']
        inf.append(_int(ord(self.char)))
        inf.append(_int(self.width))
        inf.append(_int(self.height))
        inf.append('210000000E00')
        inf.append(_int((self.startX)*16))
        inf.append(_int((self.startY)*16))
        inf.append(_int((self.startX+self.width)*16))
        inf.append(_int((self.startY+self.height)*16))

        return ''.join(inf)
