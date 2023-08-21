using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.Win32.Foundation;
using Windows.Win32.System.Power;
using Windows.Win32.System.Registry;

namespace ShortDev.Windows.ShellEnhance.UI;

public static class PowerApi
{
    public static unsafe Guid ActiveSchema
    {
        get
        {
            Guid* guid = default;
            var result = PowerGetActiveScheme(HKEY.Null, &guid);
            if (result != 0)
                throw new Win32Exception((int)result);

            return *guid;
        }
        set
        {
            var guid = value;
            var result = PowerSetActiveScheme(HKEY.Null, &guid);
            if (result != 0)
                throw new Win32Exception((int)result);
        }
    }

    public static unsafe string GetFriendlyName(Guid guid)
    {
        uint bufferSize = 0;

        var result = PowerReadFriendlyName(HKEY.Null, &guid, (Guid*)0, (Guid*)0, (byte*)0, &bufferSize);
        if (result != 0)
            throw new Win32Exception((int)result);

        if (bufferSize <= 0)
            return string.Empty;

        byte* pName = stackalloc byte[(int)bufferSize];

        result = PowerReadFriendlyName(HKEY.Null, &guid, (Guid*)0, (Guid*)0, pName, &bufferSize);
        if (result != 0)
            throw new Win32Exception((int)result);

        return new string((char*)pName);
    }

    public static unsafe IReadOnlyList<Guid> AllPowerSchemas
    {
        get
        {
            List<Guid> schemas = new();

            Guid value = default;
            uint bufferSize = (uint)sizeof(Guid);

            WIN32_ERROR result = 0;
            for (uint index = 0; result == 0; index++)
            {
                result = PowerEnumerate(HKEY.Null, (Guid*)0, (Guid*)0, POWER_DATA_ACCESSOR.ACCESS_SCHEME, index, (byte*)&value, &bufferSize);

                if (result == 0)
                    schemas.Add(value);
            }
            if (result != WIN32_ERROR.ERROR_NO_MORE_ITEMS)
                throw new Win32Exception((int)result);

            return schemas;
        }
    }
}