using Godot;

namespace com.forerunnergames.coa2.tools;

public static class StringExtensions
{
  public static string LStrip (this string instance, string chars)
  {
    var num = 0;
    var length = instance.Length;
    while (num < length && chars.Find (instance[num]) != -1) ++num;
    return num == 0 ? instance : instance.Substr (num, length - num);
  }
}
