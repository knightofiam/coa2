using System;

namespace com.forerunnergames.coa2.core.settings;

public readonly struct GameSettings : IEquatable <GameSettings>
{
  public string PlaceholderSetting1 { get; init; }
  public bool Equals (GameSettings other) => PlaceholderSetting1 == other.PlaceholderSetting1;
  public override bool Equals (object? obj) => obj is GameSettings other && Equals (other);
  public override int GetHashCode() => PlaceholderSetting1.GetHashCode();
  public static bool operator == (GameSettings left, GameSettings right) => left.Equals (right);
  public static bool operator != (GameSettings left, GameSettings right) => !left.Equals (right);
  public override string ToString() => $"{nameof (GameSettings)} [Placeholder Setting 1 {PlaceholderSetting1}]";
}
