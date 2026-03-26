using DynamicData;

namespace AlicatMFCRemastered.UI.Helpers;

internal static class SourceListExtensions
{
  public static void SyncWith<T>(this ISourceList<T> list, IEnumerable<T> incoming) where T : notnull
  {
    var incomingSet = new HashSet<T>(incoming);

    for(int i = list.Count - 1; i >= 0; i--)
    {
      if(!incomingSet.Contains(list.Items[i]))
      {
        list.RemoveAt(i);
      }
    }

    var existingSet = new HashSet<T>(list.Items);
    foreach(var item in incomingSet)
    {
      if(!existingSet.Contains(item))
      {
        list.Add(item);
      }
    }
  }
}
