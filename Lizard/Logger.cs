using System.IO;

namespace Lizard
{
	public class Logger
	{
		private static Logger Instance = null;
		private StreamWriter LogWriter = null;

		private Logger()
		{
			FileStream logStream = new FileStream("Lizard/Lizard.log", FileMode.Create);
			LogWriter = new StreamWriter(logStream);
		}

		~Logger()
		{
			LogWriter.Close();
		}

		public static void Init()
		{
			if (Instance == null)
			{
				Instance = new Logger();
				Log("Logger initialised\n");
			}
		}

		internal static void Log(string text)
		{
			Instance.LogWriter.WriteLine(text);
			Instance.LogWriter.Flush();
		}

		internal static void Log(object obj)
		{
			Log(obj == null ? "(null)" : obj.ToString());
		}
	}
}
