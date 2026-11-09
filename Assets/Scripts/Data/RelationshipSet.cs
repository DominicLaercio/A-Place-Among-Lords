using System;

namespace APlaceAmongLords.Data
{
    [Serializable]
    public sealed class RelationshipSet
    {
        private readonly int[] values = new int[Enum.GetValues(typeof(RelationshipTarget)).Length];

        public int Get(RelationshipTarget target)
        {
            return values[(int)target];
        }

        public void Change(RelationshipTarget target, int amount)
        {
            int index = (int)target;
            values[index] = Math.Max(-5, Math.Min(5, values[index] + amount));
        }
    }
}
