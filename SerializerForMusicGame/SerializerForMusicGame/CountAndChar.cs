namespace SerializerForMusicGame
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;

  /// <summary>
  /// カウントと文字のペアを表すクラスです。
  /// </summary>
  public struct CountAndChar
  {
    /// <summary>
    /// カウントを取得または設定します。
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// 文字を取得または設定します。
    /// </summary>
    public char Character { get; set; }
  }
}
