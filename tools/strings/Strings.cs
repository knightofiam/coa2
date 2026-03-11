using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace com.forerunnergames.coa2.tools;

public static partial class Strings
{
  // @formatter:off
  [GeneratedRegex ("[ ]{2,}")] private static partial Regex MatchMultipleSpaces();
  private static readonly ObjectPool <StringBuilder> SbPool = new(() => new StringBuilder());
  private static readonly ObjectPool <ConcurrentBag <string>> CbPool = new(() => []);
  [GeneratedRegex ("(?<!^)([A-Z])")] private static partial Regex ProperCaseToWordsRegex();
  public static string SplitToWords (string s) => ProperCaseToWordsRegex().Replace (s, " $1");
  public static string StripMultipleSpaces (string s) => MatchMultipleSpaces().Replace (s, " ");
  public static string PluralizeS (string s, int count, string adjective = "") => $"{count} {(adjective.Length > 0 ? $"{adjective} " : "")}{(count == 1 ? s : $"{s}s")}";
  public static string PluralizeY (string s, int count, string adjective = "") => $"{count} {(adjective.Length > 0 ? $"{adjective} " : "")}{(count == 1 ? s : s [..^1] + "ies")}";
  // ReSharper disable once InconsistentNaming
  public static string PluralizeES (string s, int count, string adjective = "") => $"{count} {(adjective.Length > 0 ? $"{adjective} " : "")}{(count == 1 ? s : $"{s}es")}";
  public static string PluralizeIsAre (int count) => count == 1 ? "is" : "are";
  public static string PluralizeVerbS (string s, int count) => count == 1 ? $"{s}s" : s; // Verbs are singular when describing plural objects, and vice versa.
  // Use for converting method-with-args calls to a string format, primarily for logging.
  public static string ToString (string method, object[] args) => args.Length == 0 ? $"{method}()" : $"{args.Aggregate (method + " (", (current, append) => $"{current} {ToString (append)},")})".Replace (",)", ")").Replace ("( ", "(");
  // @formatter:on

  // Custom ToString method for high-performance, high-volume logging.
  public static string ToString <T> (IEnumerable <T>? e, string sep = ", ", string prepend = "", string append = "", Func <T, string>? f = null)
  {
    if (e == null) return string.Empty;
    var array = e as T[] ?? e.ToArray();
    if (array.Length == 0) return string.Empty;
    f ??= s => prepend + s + append;
    var results = CbPool.Get();
    var sb = SbPool.Get();
    var first = true;

    // TODO If supporting iOS, use regular for loop - parallel execution will crash.
    Parallel.ForEach (array, x => results.Add (f (x)));

    // Don't use Parallel.ForEach here, StringBuilder is not thread safe and will randomly crash.
    foreach (var result in results)
    {
      if (first)
      {
        first = false;
        sb.Append (result);
      }
      else
      {
        sb.Append (sep).Append (result);
      }
    }

    // @formatter:off
    var finalResult = sb.ToString();
    sb.Clear();
    while (results.TryTake (out _)) { }
    SbPool.Return (sb);
    CbPool.Return (results);
    return finalResult;
    // @formatter:on
  }

  // Use for converting unknown-type, potentially-null objects safely to a string format, primarily for logging.
  public static string ToString (object? o) =>
    (o switch
    {
      null => "",
      _ => o as string ?? o switch
      {
        byte[] b => $"byte[{b.Length}]",
        IDictionary d => "\n" + ToString (Convert (d), sep: "\n", f: x => $"{x.Key}, {(x.Value is IEnumerable e ? ToString (e) : x.Value)}"),
        IEnumerable e => ToString (e.Cast <object>()),
        _ => o.ToString()
      }
    })!;

  // Workaround for InvalidCastException when using d.Cast <DictionaryEntry>().
  private static IEnumerable <DictionaryEntry> Convert (IDictionary d)
  {
    // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
    foreach (DictionaryEntry entry in d) yield return entry;
  }
}
