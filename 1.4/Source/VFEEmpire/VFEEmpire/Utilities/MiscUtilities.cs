using System.Collections.Generic;

namespace VFEEmpire;

public static class MiscUtilities
{
    public static bool Any<T>(this HashSet<T> source) => source.Count > 0;
}
