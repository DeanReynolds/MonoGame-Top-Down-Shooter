using Microsoft.Xna.Framework.Audio;
using System;

namespace Top_Down_Shooter
{
    public struct GunStats
    {
        public const int SpreadSeedMaxBits = 10;

        public static readonly int SpreadSeedMaxValues = (int)Math.Pow(2, SpreadSeedMaxBits);

        public byte InventorySlot;
        public byte BulletsPerShot;
        public double RoundsPerSecond;
        public float AngleSpreadInitial;
        public float AngleSpreadWidenMax;
        public byte AngleSpreadWidenRateInShots;
        public float AngleSpreadWidenRateIncreasePerShot;
        public float AngleSpreadWidenCooldownInSeconds;
        public float AngleSpreadWidenCooldownDecreasePerSecond;
        public float MaxDamagePerBullet;
        public float RangePerBullet;
        public float DamageKnockoffPerMeter;
        public float DamageKnockoffPerPlayerHeadHit;
        public float DamageKnockoffPerPlayerShoulderHit;
        public ushort MaxTotalAmmo;
        public ushort MaxAmmoInClip;
        public float KickVisualInitial;
        public float KickVisualLead;
        public float KickVisualMax;
        public float KickVisualRecoverRate;
        public float KickPhysicalInitial;
        public float KickPhysicalLead;
        public SoundEffect SoundFire;
    }
}