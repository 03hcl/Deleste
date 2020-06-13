namespace SerializerForMusicGame
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;

  /// <summary>
  /// プログラムを格納するクラスです。
  /// </summary>
  public class Program
  {
    /// <summary>
    /// メイン エントリです。
    /// </summary>
    /// <param name="args"> 引数。 </param>
    public static void Main(string[] args)
    {
      if (!args.Any()) { args = new string[] { "1", "COM4", ".\\test-arduino0.txt" }; }

      char mode;
      string portName = string.Empty;

      if (args[0].Length == 1)
      {
        mode = args[0][0];
        args[0] = string.Empty;

        if (mode == '1')
        {
          portName = args[1];
          args[1] = string.Empty;
        }
      }
      else
      {
        Console.WriteLine("実行モードを選択してください。");
        Console.WriteLine("シリアライズ実行: 0, Arduinoへ送信: 1");
        mode = Console.ReadKey().KeyChar;
        Console.WriteLine();

        if (mode == '1')
        {
          Console.WriteLine("ポート名を入力してください。");
          portName = Console.ReadLine();
        }
      }

      if (mode == '0')
      {
        foreach (var f in args)
        {
          if (string.IsNullOrWhiteSpace(f)) { continue; }

          Console.WriteLine("入力ファイルパス: " + f);
          Console.WriteLine();
          List<Tap> data;

          using (var reader = new StreamReader(f, Encoding.UTF8))
          {
            data = Serializer.Deserialize(reader, true);
          }

          var outfilename = Path.GetFileNameWithoutExtension(f) + "-arduino.txt";
          Console.WriteLine("出力ファイル名: " + outfilename);
          Console.WriteLine();
          Console.WriteLine("デシリアライズ -> Arduinoシリアライズ結果:");
          Console.WriteLine();

          using (var writer = new StreamWriter(outfilename, false, Encoding.UTF8))
          {
            foreach (var c in Serializer.SerializeForArduino(data))
            {
              Console.Write(c);
              writer.Write(c);
            }
          }
        }
      }
      else if (mode == '1')
      {
        foreach (var f in args)
        {
          if (string.IsNullOrWhiteSpace(f)) { continue; }

          Console.WriteLine("入力ファイルパス: " + f);
          Console.WriteLine();

          string data;
          using (var reader = new StreamReader(f, Encoding.UTF8)) { data = reader.ReadToEnd(); }
          Serializer.SendToArduino(portName, data);
        }
      }

      Console.WriteLine();
      Console.WriteLine("いずれかのキーを押すと終了します...");
      Console.ReadKey();
    }
  }
}
