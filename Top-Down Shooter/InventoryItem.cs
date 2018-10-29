namespace Top_Down_Shooter
{
    public class InventoryItem
    {
        public ushort TotalAmmo { get; internal set; }
        public ushort AmmoInClip { get; internal set; }
        public double LastVisitTotalElapsedTime { get; internal set; }
        public double FireTimer { get; internal set; }
        public double AngleSpread { get; internal set; }

        public Types Type { get; private set; }
        public GunStats GunStats { get; private set; }

        public enum Types { Empty, Gun, Item, Grenade }

        public InventoryItem(Types type, GunStats gunStats)
        {
            Type = type;
            GunStats = gunStats;
            Reset();
        }

        public void Reset()
        {
            if (Type == Types.Gun)
            {
                TotalAmmo = GunStats.MaxTotalAmmo;
                AmmoInClip = GunStats.MaxAmmoInClip;
                FireTimer = 0;
                AngleSpread = GunStats.AngleSpreadInitial;
            }
        }
    }
}