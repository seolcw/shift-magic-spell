using UnityEngine;

namespace ShiftMagicSpell.Core
{
    /// <summary>웹 프로토타입 COL 색과 도트 팔레트에서 고른 색. 로직이 남기는 연출 기록에도 쓰여서 Core에 둔다.</summary>
    public static class PaletteUtils
    {
        public static readonly Color Background = Hex(0x14161c);
        public static readonly Color Text = Hex(0xe6e8ee);
        public static readonly Color Dim = Hex(0x8a90a0);
        public static readonly Color KeyEdge = Hex(0x252832);
        public static readonly Color Warn = Hex(0xff4d4d);
        public static readonly Color Glow = Hex(0xffd24a);
        public static readonly Color Shift = Hex(0x3d7bff);
        public static readonly Color ShiftHi = Hex(0x7fa8ff);
        public static readonly Color Ok = Hex(0x5fe08a);
        public static readonly Color Hand = Hex(0xd9dce6);
        public static readonly Color Bullet = Hex(0xffa23a);
        public static readonly Color Hp = Hex(0xff5d73);
        public static readonly Color Player = Hex(0xff7ab0);
        public static readonly Color FireCore = Hex(0xfff3c4);

        public static Color Hex(int rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f, alpha);
        }
    }
}
