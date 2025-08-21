using System;
using UnityEngine;

namespace LazyHelper
{
    public static class Helper
    {
        // Проверка, что source содержит ВСЕ флаги из flags
        public static bool ContainsAllFlags<TEnum>(this TEnum source, TEnum flags)
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
        // public static bool ContainsAnyFlag<TEnum>(this TEnum source, TEnum flags)
        //     where TEnum : Enum
        // {
        //     ulong sourceValue = Convert.ToUInt64(source);
        //     ulong flagsValue = Convert.ToUInt64(flags);

        //     // Если оба None - возвращаем true
        //     if (sourceValue == 0 && flagsValue == 0)
        //         return true;

        //     return (sourceValue & flagsValue) != 0;
        // }


        // Проверка, что source содержит конкретный флаг
        public static bool ContainsFlag<TEnum>(this TEnum source, TEnum flag) where TEnum : Enum
        {
            ulong sourceValue = Convert.ToUInt64(source);
            ulong flagValue = Convert.ToUInt64(flag);

            // Если flag = 0, то проверяем что source тоже 0
            if (flagValue == 0)
                return sourceValue == 0;

            return (sourceValue & flagValue) == flagValue;
        }

        // Подсчет количества активных флагов
        public static int GetActiveFlagsCount<TEnum>(this TEnum flags) where TEnum : Enum
        {
            int count = 0;
            ulong value = Convert.ToUInt64(flags);

            while (value != 0)
            {
                count += (int)(value & 1);
                value >>= 1;
            }

            return count;
        }

        
    }
}

