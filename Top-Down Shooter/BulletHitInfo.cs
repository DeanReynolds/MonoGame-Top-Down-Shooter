using System;

namespace Top_Down_Shooter
{
    public class BulletHitInfo
    {
        public static readonly int HitTypesCountIndex = (Enum.GetValues(typeof(HitTypes)).Length - 1);

        public Player Victim { get; private set; }
        public HitTypes HitType { get; private set; }

        public enum HitTypes { Head, Shoulder }

        public BulletHitInfo(Player victim, HitTypes hitType)
        {
            Victim = victim;
            HitType = hitType;
        }
    }
}