namespace AExpense.Data.Storage
{
    using System;

    [Serializable]
    public class StorageKey
    {
        private string invertedTicks;

        public StorageKey(string revertedTicks)
        {
            this.InvertedTicks = revertedTicks;
        }

        public static StorageKey Now
        {
            get
            {
                return new StorageKey(string.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks));
            }
        }

        public string InvertedTicks
        {
            get
            {
                return this.invertedTicks;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("InvertedTicks cannot be null or empty.");    
                }

                if (value.Length != 19)
                {
                    throw new ArgumentException("The reverted ticks have to be a string of 19 characters. Get it using StorageKey.Now.");
                }

                this.invertedTicks = value;
            }
        }

        public static bool operator ==(StorageKey left, StorageKey right)
        {
            if (((object)left == null) && ((object)right == null))
            {
                return true;
            }

            if (((object)left) == null)
            {
                return false;
            }

            if (((object)right) == null)
            {
                return false;
            }

            return left.invertedTicks == right.invertedTicks;
        }

        public static bool operator !=(StorageKey left, StorageKey right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return this.invertedTicks;
        }

        public override bool Equals(object obj)
        {
            StorageKey otherStorageKey = obj as StorageKey;

            if (otherStorageKey == null)
            {
                return base.Equals(obj);
            }

            return this.InvertedTicks.Equals(otherStorageKey.InvertedTicks, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return this.invertedTicks.GetHashCode();
        }
    }
}