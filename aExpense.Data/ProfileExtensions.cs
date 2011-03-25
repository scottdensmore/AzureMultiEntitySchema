namespace AExpense.Data
{
    using System.Web.Profile;

    public static class ProfileExtensions
    {
        public static T GetProperty<T>(this ProfileBase profile, string property)
        {
            object value = profile.GetPropertyValue(property);

            if (value == null)
            {
                return default(T);
            }

            return (T)value;
        }
    }
}