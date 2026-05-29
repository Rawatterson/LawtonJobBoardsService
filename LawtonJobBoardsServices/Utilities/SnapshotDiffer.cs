using LawtonJobBoardsServices.Models.Dto;
using LawtonJobBoardsServices.Models.Ordant;
using System.Reflection;

namespace LawtonJobBoardsServices.Utilities;

public static class SnapshotDiffer
{
    private static readonly PropertyInfo[] Properties =
        typeof(OrdantJobsSnapShot).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    public static List<PropertyDiff> Diff(OrdantJobsSnapShot cached, OrdantJobsSnapShot fresh)
    {
        var diffs = new List<PropertyDiff>();
        foreach (var prop in Properties)
        {
            var oldVal = prop.GetValue(cached);
            var newVal = prop.GetValue(fresh);
            if (!Equals(oldVal, newVal))
                diffs.Add(new PropertyDiff(prop.Name, oldVal?.ToString() ?? "(null)", newVal?.ToString() ?? "(null)"));
        }
        return diffs;
    }
}
