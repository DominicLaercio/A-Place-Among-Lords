using System;

namespace APlaceAmongLords.Data
{
    [Serializable]
    public sealed class AttributeSet
    {
        private bool allocated;
        public bool IsAllocated { get { return allocated; } }
        public int Diplomacy { get; private set; }
        public int Warfare { get; private set; }
        public int Management { get; private set; }
        public int Combat { get; private set; }

        public int Get(AttributeType type)
        {
            switch (type)
            {
                case AttributeType.Diplomacy: return Diplomacy;
                case AttributeType.Warfare: return Warfare;
                case AttributeType.Management: return Management;
                case AttributeType.Combat: return Combat;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public bool TryAllocate(int diplomacy, int warfare, int management, int combat, int pointBudget)
        {
            if (allocated || pointBudget < 0 || diplomacy < 0 || warfare < 0 || management < 0 || combat < 0)
                return false;
            if (diplomacy + warfare + management + combat != pointBudget)
                return false;

            Diplomacy = diplomacy;
            Warfare = warfare;
            Management = management;
            Combat = combat;
            allocated = true;
            return true;
        }
    }
}


