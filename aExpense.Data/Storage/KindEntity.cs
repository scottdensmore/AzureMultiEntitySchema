namespace AExpense.Data.Storage
{
    using System;

    public abstract class KindEntity : Entity
    {
        protected KindEntity()
        {
        }

        protected KindEntity(string kind) : this(null, null, kind)
        {
        }

        protected KindEntity(string partitionKey, string rowKey, string kind) : base(partitionKey, rowKey)
        {
            this.Kind = kind;
        }

        public string Kind { get; set; }

        public TEnum ToEnum<TEnum>() where TEnum : struct
        {
            return (TEnum)Enum.Parse(typeof(TEnum), this.Kind);
        }
    }
}