# 3.18 剧情中点与句读微调 / Complete dialogue middle dot

用户反馈剧情人名“·”仍不够清楚，明确要求点本身更饱满并保留一点左距；句号不用增加间距，逗号左距再增加一点。

- 中英文句号`。.`恢复3.16的xAdvance/xOffset，逐字节相同。
- 中英文逗号`，,`左侧比3.16累计增加2个字库单位，xAdvance同步增加，右侧留白保持。
- 剧情RussellSquare人名中点原UV取样区域(546,0)–(553,6)只采到完整圆点的上部，真实DDS图集的完整圆点位于约(545,4)–(550,9)。改为带边缘留白的(544,3)–(551,10)，同时把5×5quad改为10×10，保留居中和少量左侧留白。新步进22、xOffset6、yOffset19。使用原图集完整圆点，不重画整套字体、不重新压缩纹理。
- 标题字体的中点不放大。其他3.17标点微调保留；所有中文、英数、空格3、其他字形、图集、纵向全局度量、字偶及字体引用不变。只修改三份FFN中13个记录（剧情5、两份标题各4），其余资源逐字节一致。

`tools/patch_middle_dot.py` requires the exact 3.17 input hash and permits only the period/comma metric fields and one dialogue middle-dot record to change. The complete circle already exists in the DDS atlas; all texture bytes remain exact. The local comparison is an offline DDS/FFN simulation, not in-game acceptance. Full-playthrough certification is not claimed.

2026-11-04: final installer passed 249 checks and was installed to Steam. Independent DDS sampling verifies that the new UV includes the entire original dot; periods match 3.16 exactly and comma metrics add two units. Steam requested switching away from another session playing CS2; the user explicitly declined, so 3.18 in-game display/wrapping remains pending. No session-switch confirmation was sent. The virtual controller was stopped and both saves/settings were rechecked.
