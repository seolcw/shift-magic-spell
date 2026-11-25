namespace ShiftMagicSpell.Core
{
    /// <summary>공격 슬롯에 담긴 공격 블록. Hp = 손의 슬롯 깨기를 버틸 수 있는 횟수.</summary>
    public class AttackBlock
    {
        public string Word;
        public int Hp;
        public float Age;
        public float HitT;

        // 손에게 맞아 하얗게 번쩍이는 남은 시간(타격감)
        public float FlashT;
    }
}
