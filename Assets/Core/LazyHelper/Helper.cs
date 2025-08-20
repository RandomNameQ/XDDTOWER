using System;
using UnityEngine;

namespace LazyHelper
{
    public static class Helper
    {
        public static bool HasAllFlags<TEnum>(this TEnum source, TEnum flags)
     where TEnum : Enum
        {
            ulong sourceValue = Convert.ToUInt64(source);
            ulong flagsValue = Convert.ToUInt64(flags);

            // Если оба None - возвращаем true
            if (sourceValue == 0 && flagsValue == 0)
                return true;

            return (sourceValue & flagsValue) == flagsValue;
        }

        // Проверка, что source содержит ХОТЯ БЫ ОДИН флаг из flags
        public static bool HasAnyFlag<TEnum>(this TEnum source, TEnum flags)
            where TEnum : Enum
        {
            ulong sourceValue = Convert.ToUInt64(source);
            ulong flagsValue = Convert.ToUInt64(flags);

            // Если оба None - возвращаем true
            if (sourceValue == 0 && flagsValue == 0)
                return true;

            return (sourceValue & flagsValue) != 0;
        }
    }
}

