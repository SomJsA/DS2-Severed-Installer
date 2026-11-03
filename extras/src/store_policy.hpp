#pragma once
#include <cstdint>
#include <cstddef>
namespace ds2fix {
struct Guid { uint32_t words[4]; };
inline constexpr Guid power_node = {{0x37C7B444,0x8607BF9D,0x4E59414D,0x36445241}};
inline constexpr Guid standard_items[] = {
    {{0x36C8BC4F,0x67BB5F38,0x44455257,0x34303245}}, // Pulse Rifle
    {{0x36C8BC48,0xED477CBA,0x44455257,0x34303245}}, // Force Gun
    {{0x36C8BC4F,0x6F9078E2,0x44455257,0x34303245}}, // Ripper
    {{0x36C8BC3F,0xF55615C8,0x44455257,0x34303245}}, // Contact Beam
    {{0x58CA30C7,0x78E54CEB,0x3045454C,0x58373634}}, // Mine Gun
    {{0x4FCB1321,0x0471623E,0x4952414D,0x4C4C454E}}, // Rivet Gun Ammo
    {{0x4BC78CEC,0xDFF02CCF,0x544E4543,0x53574F4B}}, // Line Gun Rack
    {{0x4BC78C36,0x9D67757C,0x544E4543,0x53574F4B}}, // Pulse Rifle Bullet
    {{0x30C8692D,0x6DBBF822,0x4E484F4A,0x324E4F53}}, // Flamethrower Fuel
    {{0x30C87D90,0xE7570C3E,0x4C454D50,0x4B534544}}, // Force Gun Ammo
    {{0x30C8692D,0x79E101DC,0x4E484F4A,0x324E4F53}}, // Ripper Ammo
    {{0x30C87377,0xE3BC55EA,0x4C454D50,0x4B534544}}, // Contact Beam Ammo
    {{0x4BC78C36,0x9D5DEC14,0x544E4543,0x53574F4B}}, // Medium Health Kit
    {{0x4BC78C36,0x9D604E6E,0x544E4543,0x53574F4B}}, // Large Health Kit
    {{0x30C889E7,0x7019FF4E,0x4E484F4A,0x324E4F53}}, // Seeker Ammo
    {{0x4BC806EA,0x40D9D6F7,0x544E4543,0x53574F4B}}, // Stasis Energy Small
    {{0x37C7B444,0x8607BF9D,0x4E59414D,0x36445241}}, // Power Node
    {{0x35C97DB8,0x53ABA0A9,0x44455257,0x38343145}}, // Javelin Ammo
    {{0x58CA30C7,0xA9ADE7CB,0x3045454C,0x58373634}}, // Detonator Ammo
    {{0x45CB0685,0xDD95DCDA,0x43534147,0x4E47494F}}, // Seeker Rifle
};
inline bool equal(const Guid& a,const Guid& b) { for(int i=0;i<4;++i)if(a.words[i]!=b.words[i])return false;return true; }
inline bool permitted(const Guid& guid,int profile,bool severed) { if(!severed || profile<1 || profile>2)return false; if(profile==1)return equal(guid,power_node); for(const auto& g:standard_items)if(equal(guid,g))return true;return false;}
}
