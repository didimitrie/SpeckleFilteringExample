using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Speckle.Core.Api;
using Speckle.Core.Models;

namespace FlattenFilterSpeckle
{
  class Program
  {
    static void Main(string[] args)
    {
      // Note, you will need an account with speckle.xyz for this to work.
      var data = Helpers.Receive("https://speckle.xyz/streams/0d3cb7cb52/commits/681cdd572c").Result;
      var flatData = data.Flatten().ToList();

      var windows = flatData.FindAll(obj => (string)obj["category"] == "Windows");
      var timberWalls = flatData.FindAll(obj => obj is Objects.BuiltElements.Revit.RevitWall wall && wall.type == "Wall - Timber Clad");
      var rooms = flatData.FindAll(obj => obj is Objects.BuiltElements.Room);
      var levels = flatData.FindAll(obj => obj is Objects.BuiltElements.Level).Cast<Objects.BuiltElements.Level>().GroupBy(level => level.name).Select(g => g.First()).ToList();

      Console.WriteLine($"Found {windows.Count} windows.");
      Console.WriteLine($"Found {timberWalls.Count} timber walls.");
      Console.WriteLine($"Found {rooms.Count} rooms.");
      Console.WriteLine($"Found {levels.Count} levels.");
      
      var elementsByLevel = flatData.FindAll(obj => obj["level"] != null).GroupBy(obj => ((Base)obj["level"])["name"]);
      foreach(var grouping in elementsByLevel) {
        Console.WriteLine($"On level {grouping.Key} there are {grouping.Count()} elements.");
      }
    }
  }

  public static class Extensions
  {
    // Flattens a base object into all its constituent parts.
    public static IEnumerable<Base> Flatten(this Base obj)
    {
      yield return obj;

      var props = obj.GetDynamicMemberNames();
      foreach (var prop in props)
      {
        var value = obj[prop];
        if (value == null) continue;

        if (value is Base b)
        {
          var nested = b.Flatten();
          foreach (var child in nested) yield return child;
        }

        if (value is IDictionary dict)
        {
          foreach (var dictValue in dict.Values)
          {
            if (dictValue is Base lb)
            {
              foreach (var lbChild in lb.Flatten()) yield return lbChild;
            }
          }
        }

        if (value is IEnumerable enumerable)
        {
          foreach (var listValue in enumerable)
          {
            if (listValue is Base lb)
            {
              foreach (var lbChild in lb.Flatten()) yield return lbChild;
            }
          }
        }
      }
    }

    // https://stackoverflow.com/a/1300116 🤘 (not needed, used to get unique levels only as an example)
    // Note, this should be baked in .NET 6.
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
    {
      HashSet<TKey> knownKeys = new HashSet<TKey>();
      foreach (TSource element in source)
      {
        if (knownKeys.Add(keySelector(element)))
        {
          yield return element;
        }
      }
    }
  }
}
