namespace SerializerForMusicGame
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.IO;
  using System.IO.Ports;
  using System.Linq;
  using System.Text;
  using System.Text.RegularExpressions;
  using System.Threading.Tasks;

  /// <summary>
  /// Serializerです。
  /// </summary>
  public class Serializer
  {
    /// <summary>
    /// タップ時間を取得または設定します。
    /// </summary>
    public static double TapHoldTime { get; set; } = 0.08;

    /// <summary>
    /// 元のフォーマット形式からデシリアライズします。
    /// </summary>
    /// <param name="reader"> 元のフォーマット形式の文字列を読み込む <see cref="StreamReader"/>。 </param>
    /// <param name="outputsConsole"> <see cref="StreamReader"/> の内容をコンソールに出力するかどうか。 </param>
    /// <returns> タップ一覧。 </returns>
    public static List<Tap> Deserialize(StreamReader reader, bool outputsConsole)
    {
      var tapPairs = new Dictionary<char, int>();
      var holdonPairs = new Dictionary<char, int>();
      var holdoffPairs = new Dictionary<char, int>();
      var blankChars = new List<char>();

      var bpmRegex = new Regex(
        "^#bpm\\s*?(?<bpm>\\d+(.\\d*)?)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);
      var beatsRegex = new Regex(
        "^#beats\\s*?(?<beats>\\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);
      var defineRegex = new Regex(
        "^#define\\s+?(?<char>[^\\s#%\\[\\]|])\\s*?(?<number>\\d+)\\s*?(?<type>TAP|(HOLD_?)?(ON|OFF)|$)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);
      var defineRegex0 = new Regex(
        "^#define\\s+?(?<char>[^\\s#%\\[\\]|])\\s*(BLANK.*?|$)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);
      var delayRegex = new Regex(
        "^#delay\\s*?(?<milliseconds>\\d+(.\\d*)?)",
      RegexOptions.IgnoreCase | RegexOptions.Singleline);

      var numberAndTimePairs = new Dictionary<int, double>();
      var results = new List<Tap>();

      double bpm = 0;
      int beats = 1;
      int bar = 0;
      double time = 0;

      if (outputsConsole)
      {
        Console.WriteLine("ファイル内容:");
        Console.WriteLine();
      }

      while (!reader.EndOfStream)
      {
        string line = reader.ReadLine();

        if (outputsConsole) { Console.WriteLine(line); }

        if (string.IsNullOrWhiteSpace(line)) { continue; }
        int topIndex = 0;
        while (char.IsWhiteSpace(line[topIndex])) { topIndex++; }
        if (line[topIndex] == '%') { continue; }
        if (line[topIndex] == '#')
        {
          var match = bpmRegex.Match(line);

          if (match.Success)
          {
            bpm = double.Parse(match.Groups["bpm"].Value);
            continue;
          }

          match = beatsRegex.Match(line);

          if (match.Success)
          {
            beats = int.Parse(match.Groups["beats"].Value);
            continue;
          }

          match = defineRegex.Match(line);

          if (match.Success)
          {
            if (match.Groups["type"].Value.IndexOf("ON", StringComparison.OrdinalIgnoreCase) >= 0)
            {
              holdonPairs.Add(Convert.ToChar(match.Groups["char"].Value), int.Parse(match.Groups["number"].Value));
            }
            else if (match.Groups["type"].Value.IndexOf("OFF", StringComparison.OrdinalIgnoreCase) >= 0)
            {
              holdoffPairs.Add(Convert.ToChar(match.Groups["char"].Value), int.Parse(match.Groups["number"].Value));
            }
            else
            {
              tapPairs.Add(Convert.ToChar(match.Groups["char"].Value), int.Parse(match.Groups["number"].Value));
            }

            continue;
          }

          match = defineRegex0.Match(line);

          if (match.Success)
          {
            blankChars.Add(Convert.ToChar(match.Groups["char"].Value));
            continue;
          }

          match = delayRegex.Match(line);

          if (match.Success)
          {
            time += double.Parse(match.Groups["milliseconds"].Value) / 1000;
            continue;
          }

          continue;
        }

        var countAndChars = new List<CountAndChar>();
        int count = -1;
        bool isMulti = false;
        char lastChar = '\0';

        foreach (var c in line)
        {
          if (isMulti)
          {
            if (c == ']') { isMulti = false; }
            else { countAndChars.Add(new CountAndChar() { Count = count, Character = c }); }
          }
          else
          {
            if (char.IsWhiteSpace(c)) { continue; }
            if (c == '%') { break; }

            count++;

            if (c == '[') { isMulti = true; }
            else if (c == '|')
            {
              if (lastChar != '\0')
              {
                bar++;
                results.AddRange(CreateTaps(bpm, beats, time, count, countAndChars, tapPairs, holdonPairs, holdoffPairs, blankChars));
                countAndChars.Clear();
                time += 60 * beats / bpm;
              }

              count = -1;
            }
            else { countAndChars.Add(new CountAndChar() { Count = count, Character = c }); }
          }

          lastChar = c;
        }

        if (lastChar != '|' && lastChar != '\0')
        {
          bar++;
          if (count != -1)
          {
            results.AddRange(CreateTaps(bpm, beats, time, count + 1, countAndChars, tapPairs, holdonPairs, holdoffPairs, blankChars));
          }

          time += 60 * beats / bpm;
        }
      }

      if (outputsConsole) { Console.WriteLine(); }

      return results.OrderBy(t => t.Time).ToList();
    }

    /// <summary>
    /// Arduino で使用するためのフォーマット形式にシリアライズします。
    /// </summary>
    /// <param name="taps"> タップ一覧。 </param>
    /// <returns> Arduino で使用するためのテキスト。</returns>
    public static IEnumerable<char> SerializeForArduino(IEnumerable<Tap> taps)
    {
      foreach (var t in taps)
      {
        double time = Math.Round(t.Time * 1000);
        string row = $"{time:F0}, {t.Number}, {(t.Touch ? "1" : "0")}";
        foreach (var c in row) { yield return c; }
        yield return '\n';
      }
    }

    /// <summary>
    /// Arduino に文字列を送信します。
    /// </summary>
    /// <param name="portName"> ポート名。 </param>
    /// <param name="text"> 送信する文字列。 </param>
    public static void SendToArduino(string portName, IEnumerable<char> text)
    {
      try
      {
        using (SerialPort port = new SerialPort(portName, 9600))
        {
          port.Open();

          foreach (var c in text)
          {
            port.Write(c.ToString());
            while (port.ReadChar() != c) { Task.Delay(1).Wait(); }
            Console.Write(c);
          }

          port.Write("\0");
          port.ReadChar();
          char lastChar;

          do
          {
            lastChar = (char)port.ReadChar();
            Console.Write(lastChar);
          } while (lastChar != '\0');

          port.Close();
        }
      }
      catch
      {
        Console.WriteLine("シリアル通信に失敗しました。");
      }
    }

    /// <summary>
    /// 各情報からタップ情報のリストを生成します。
    /// </summary>
    /// <param name="bpm"> BPM。 </param>
    /// <param name="beats"> 1小節あたりの拍数。 </param>
    /// <param name="time"> 現在の時間。 </param>
    /// <param name="counts"> 小節内の総カウント数。 </param>
    /// <param name="countAndChars"> カウントと文字のペア。 </param>
    /// <param name="tapPairs"> タップを表す文字と位置番号のペア。 </param>
    /// <param name="holdonPairs"> ホールドの始まりを表す文字と位置番号のペア。 </param>
    /// <param name="holdoffPairs"> ホールドの終わりを表す文字と位置番号のペア。 </param>
    /// <param name="blankChars"> 何もしないことを表す文字のリスト。 </param>
    /// <returns> タップ情報のリスト。 </returns>
    protected static IEnumerable<Tap> CreateTaps(
      double bpm,
      int beats,
      double time,
      int counts,
      IEnumerable<CountAndChar> countAndChars,
      Dictionary<char, int> tapPairs,
      Dictionary<char, int> holdonPairs,
      Dictionary<char, int> holdoffPairs,
      IEnumerable<char> blankChars)
    {
      foreach (var c in countAndChars)
      {
        double tapTime = time + (60 * beats / bpm * c.Count / counts);

        if (tapPairs.ContainsKey(c.Character))
        {
          yield return new Tap() { Time = tapTime, Number = tapPairs[c.Character], Touch = true };
          yield return new Tap() { Time = tapTime + TapHoldTime, Number = tapPairs[c.Character], Touch = false };
        }
        else if (holdonPairs.ContainsKey(c.Character))
        {
          yield return new Tap() { Time = tapTime, Number = holdonPairs[c.Character], Touch = true };
        }
        else if (holdoffPairs.ContainsKey(c.Character))
        {
          yield return new Tap() { Time = tapTime, Number = holdoffPairs[c.Character], Touch = false };
        }
      }
    }
  }
}
