namespace AExpense.Data.Storage
{
    using System;

    public abstract class KindUnion : KindEntity
    {
        public TIKindEntity ToKind<TIKindEntity>() where TIKindEntity : IEntity
        {
            if (!typeof(TIKindEntity).IsInterface)
            {
                throw new InvalidOperationException("TIKindEntity must be an interface type");
            }
            string interfaceName = "I" + this.Kind + "Entity";
            if (typeof(TIKindEntity).Name != interfaceName)
            {
                throw new InvalidCastException("This " + this.Kind + " entity must be cast to an " + interfaceName);
            }
            return (TIKindEntity)(Object)this;
        }
    }
}