using System;

namespace APlaceAmongLords.Data
{
    [Serializable]
    public sealed class ResourceSet
    {
        private ResourceSet() { }

        public int Influence { get; private set; }
        public int Treasury { get; private set; }
        public int Supplies { get; private set; }
        public int Reputation { get; private set; }
        public int Troops { get; private set; }

        public int Get(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Influence: return Influence;
                case ResourceType.Treasury: return Treasury;
                case ResourceType.Supplies: return Supplies;
                case ResourceType.Reputation: return Reputation;
                case ResourceType.Troops: return Troops;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public void Change(ResourceType type, int amount)
        {
            switch (type)
            {
                case ResourceType.Influence: Influence = Math.Max(0, Influence + amount); break;
                case ResourceType.Treasury: Treasury = Math.Max(0, Treasury + amount); break;
                case ResourceType.Supplies: Supplies = Math.Max(0, Supplies + amount); break;
                case ResourceType.Reputation: Reputation = Math.Max(0, Reputation + amount); break;
                case ResourceType.Troops: Troops = Math.Max(0, Troops + amount); break;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static ResourceSet FromValues(int influence, int treasury, int supplies, int reputation, int troops)
        {
            return new ResourceSet
            {
                Influence = Math.Max(0, influence),
                Treasury = Math.Max(0, treasury),
                Supplies = Math.Max(0, supplies),
                Reputation = Math.Max(0, reputation),
                Troops = Math.Max(0, troops)
            };
        }
    }
}
