namespace ShiftMagicSpell.Audio
{
    /// <summary>소리를 내는 게임 이벤트. SoundLibrary에서 이벤트마다 클립·볼륨·피치를 정한다.</summary>
    public enum SoundKey
    {
        Start,
        Move,
        Rotate,
        Deny,
        LetterMatch,
        WordComplete,
        Store,
        SpellFire,
        SpellBolt,
        SpellBeam,
        SpellMagic,
        SpellShift,
        SpellImpact,
        Combo,
        PlayerHurt,
        HandWarn,
        HandClick,
        HandSlam,
        BlockBreak,
        Warp,
        PhaseClear,
        Win,
        Death,

        // 엔딩(고장 난 커서): 금 → 펑(모양이 바뀔 때마다 높아짐) → 달아남 → 자막 글자
        EndingCrack,
        EndingCrackBig,
        EndingPop1,
        EndingPop2,
        EndingPop3,
        EndingZip,
        EndingType,
    }
}
