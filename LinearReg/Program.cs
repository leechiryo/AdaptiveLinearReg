using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace LinearReg
{
  class Program
  {
    static int MaxDeviation(string barType)
    {
      switch (barType)
      {
        case "1M": return 5;
        case "5M": return 10;
        case "1H": return 15;
        case "4H": return 30;
        default: return 50;
      }
    }

    static void Main(string[] args)
    {
      try
      {
        if (args.Length < 2)
        {
          Console.WriteLine("How to use: LinearReg bar_file_path BAR_TYPE");
          Console.WriteLine("where BAR_TYPE = 1M | 5M | 1H | 4H | 1D");
          return;
        }

        if (!File.Exists(args[0]))
        {
          Console.WriteLine("Cannot find the minute file to process.");
          return;
        }

        if (args[1] != "1M" && args[1] != "5M" && args[1] != "1H" && args[1] != "4H" && args[1] != "1D")
        {
          Console.WriteLine("How to use: LinearReg bar_file_path BAR_TYPE");
          Console.WriteLine("where BAR_TYPE = 1M | 5M | 1H | 4H | 1D");
          return;
        }

        string barType = args[1];

        List<int> vals = new List<int>();
        List<string> dates = new List<string>();

        // load the data.
        using (FileStream fs = new FileStream(args[0], FileMode.Open, FileAccess.Read, FileShare.None))
        using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
        {
          while (sr.Peek() >= 0)
          {
            string line = sr.ReadLine();

            string[] fields = line.Split(',');

            if (fields.Length != 5)
            {
              Console.WriteLine("Cannot parse the minute data: {0}", line);
              return;
            }

            int val = 0;
            for (int i = 1; i <= 4; i++)
            {
              val += int.Parse(fields[i].Substring(0, 6).Replace(".", ""));
            }
            vals.Add(val / 4);
            dates.Add(fields[0]);
          }
        }

        var lines = AutoLeastSquare(vals, MaxDeviation(barType));

        foreach (var l in lines)
        {
          Console.WriteLine("{0}, {1}, {2:F15}, ({3} -> {4}), {5}, {6}", 
            dates[l.Item1], dates[l.Item2], l.Item3, vals[l.Item1], vals[l.Item2], l.Item2 - l.Item1, l.Item4);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
        return;
      }
    }

    private static List<Tuple<int, int, double, double>> AutoLeastSquare(List<int> vals, int maxDist)
    {
      List<Tuple<int, int, double, double>> retval = new List<Tuple<int, int, double, double>>();
      int startIdx = 0;
      double lastb1 = 0.0;
      double lastDist = 0.0;
      for (int i = 1; i < vals.Count; i++)
      {
        var result = FirstLeastSquare(vals, startIdx, i, maxDist);
        if (result.Item1)
        {
          lastb1 = result.Item2;
          lastDist = result.Item3;
        }
        else
        {
          retval.Add(new Tuple<int, int, double, double>(startIdx, i - 1, lastb1, lastDist));
          startIdx = i;
        }
      }

      retval.Add(new Tuple<int, int, double, double>(startIdx, vals.Count-1, lastb1, lastDist));

      return retval;
    }


    /// <summary>
    /// auto least square
    /// </summary>
    /// <param name="vals"></param>
    /// <returns></returns>
    private static Tuple<bool, double, double> FirstLeastSquare(List<int> vals, int startIdx, int endIdx, int maxDist)
    {
      // least square line's equation: y = b0 + b1x
      double xbar = (endIdx + startIdx) / 2.0;
      double ysum = 0.0;
      for (int i = startIdx; i <= endIdx; i++)
      {
        ysum += vals[i];
      }

      double ybar = ysum * 1.0 / (endIdx - startIdx + 1);

      double numerator = 0.0;
      double denominator = 0.0;
      for (int i = startIdx; i <= endIdx; i++)
      {
        numerator += (i - xbar) * (vals[i] - ybar);
        denominator += (i - xbar) * (i - xbar);
      }
      double b1 = numerator * 1.0 / denominator;
      double b0 = ybar - b1 * xbar;

      List<Tuple<int, int, double>> retval = new List<Tuple<int, int, double>>();

      double sumDist = 0.0;
      for (int i = startIdx; i <= endIdx; i++)
      {
        double x0 = i;
        double y0 = vals[i];
        double y = b0 + b1 * x0;

        sumDist += (y0 - y) * (y0 - y);
      }

      double avgDist = Math.Sqrt(sumDist / (endIdx - startIdx));

      if (avgDist > maxDist)
      {
        return new Tuple<bool, double, double>(false, 0.0, avgDist);
      }

      return new Tuple<bool, double, double>(true, b1, avgDist);
    }
  }
}
