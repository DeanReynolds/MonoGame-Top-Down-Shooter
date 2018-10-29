namespace Top_Down_Shooter
{
    public struct TileInfo
    {
        public bool IsSolid { get; private set; }

        public TileInfo(bool isSolid)
        {
            IsSolid = isSolid;
        }
    }
}