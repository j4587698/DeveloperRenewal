namespace GraphLib.Entity
{
    public class TokenCacheEntity: BaseEntity
    {
        public string Name { get; set; }

        public byte[] Token { get; set; }
    }
}