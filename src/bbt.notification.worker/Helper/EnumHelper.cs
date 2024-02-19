using System.ComponentModel;

namespace bbt.notification.worker.Helper
{
    public static class EnumHelper
    {
        public static string GetDescription<TEnum>(this TEnum enumValue)
        {
            var fi = enumValue.GetType().GetField(enumValue.ToString());

            if (fi != null)
            {
                var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attributes.Length > 0)
                {
                    return attributes[0].Description;
                }

            }
            return enumValue.ToString();
        }

        public static int[] ToIntArray(this System.Enum o)
        {
            return o.ToString()
                .Split(new string[] { ", " }, StringSplitOptions.None)
                .Select(i => (int)System.Enum.Parse(o.GetType(), i))
                .ToArray();
        }

        public static object[] ToEnumArray(this System.Enum o)
        {
            return o.ToString()
                .Split(new string[] { ", " }, StringSplitOptions.None)
                .Select(i => System.Enum.Parse(o.GetType(), i))
                .ToArray();
        }
    }
}
