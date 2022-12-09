using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    }
}
