using UnityEngine;

namespace Game.Items
{
    public class TestItem : Item { }

    public abstract class Item
    {
        private int _repeats;
        public int Repeats
        {
            get
            {
                return _repeats;
            }
            set
            {
                int change = value - _repeats;
                if (change > 0)
                {
                    OnAddRepeat(change);
                }
                if (change < 0)
                {
                    OnRemoveRepeat(change);
                }
                _repeats = value;
            }
        }

        public virtual void OnEquip()
        {
            OnAddRepeat(Repeats);
        }

        public virtual void OnUnequip()
        {
            OnRemoveRepeat(Repeats);
        }

        protected virtual void OnAddRepeat(int count = 1)
        {
            Debug.Log($"Added {count} stacks of {GetType().Name}");
        }

        protected virtual void OnRemoveRepeat(int count = 1)
        {
            OnAddRepeat(-count);
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
