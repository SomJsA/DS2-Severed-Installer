# 3.17 标点左侧留白 / Punctuation left padding

根据3.16实机反馈，对人名中点`·`及常用句读标点增加少量左侧间距：`·，。、；：？！…,.!?;:`。只处理三份本地化字体FFN中实际存在的字符，不添加缺失字符。

每个目标字符的xOffset和xAdvance同步加1个字库单位（构建尺度40，屏幕实际距离随字号变化）。符号图像向右移一点，后续文字也同量后移，因此符号右侧原有留白不变。不是给整段文字再加空格，也不改变符号大小。

3.16的中文本体、英文数字及ASCII空格3的度量逐字节保留。图集、UV、纵向度量、字偶数据、字体分工和所有非目标字形记录不变。仅改变DAT0内4564F3D7字体流，不增加ASI。

`tools/patch_punctuation_left.py`要求精确3.16输入哈希，只允许目标字符xAdvance/xOffset字节变化。源码包不包含私有字体、游戏数据、图集和第三方二进制。

Selected punctuation receives one extra build unit on its left. Both xOffset and xAdvance increase by one, retaining right-side padding. All 3.16 Chinese, Latin, digit, space and unrelated punctuation records, glyph atlases, font roles and pair data remain exact. Screen-space distances depend on the engine text size and transforms. Full-playthrough certification is outside this sampled change.
