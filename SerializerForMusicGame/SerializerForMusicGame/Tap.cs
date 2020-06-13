namespace SerializerForMusicGame
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;

  /// <summary>
  /// タップを表す構造体です。
  /// </summary>
  public struct Tap
  {
    /// <summary>
    /// タップのタイミングを取得または設定します。
    /// </summary>
    public double Time { get; set; }

    /// <summary>
    /// タップの位置番号を取得または設定します。
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Touch (True) または Release (False) を取得または設定します。
    /// </summary>
    public bool Touch { get; set; }
  }
}
