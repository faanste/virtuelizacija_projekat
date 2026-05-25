using Common;
using System;
using System.Globalization;
using System.IO;

namespace Server
{
    public class MeasurementFileWriter : IDisposable
    {
        private readonly StreamWriter measurementsWriter;
        private readonly StreamWriter rejectsWriter;
        private bool disposed = false;

        private static readonly CultureInfo culture = CultureInfo.InvariantCulture;

        public MeasurementFileWriter(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                folderPath = "data";
            }

            Directory.CreateDirectory(folderPath);

            string measurementsPath = Path.Combine(folderPath, "measurements_session.csv");
            string rejectsPath = Path.Combine(folderPath, "rejects.csv");

            measurementsWriter = new StreamWriter(
                new FileStream(measurementsPath, FileMode.Create, FileAccess.Write, FileShare.Read));

            rejectsWriter = new StreamWriter(
                new FileStream(rejectsPath, FileMode.Create, FileAccess.Write, FileShare.Read));

            measurementsWriter.AutoFlush = true;
            rejectsWriter.AutoFlush = true;

            measurementsWriter.WriteLine("Volume,T_DHT,T_BMP,Pressure,DateTime");
            rejectsWriter.WriteLine("Volume,T_DHT,T_BMP,Pressure,DateTime,Reason");

            Console.WriteLine("[FileWriter] Kreiran fajl: " + Path.GetFullPath(measurementsPath));
            Console.WriteLine("[FileWriter] Kreiran fajl: " + Path.GetFullPath(rejectsPath));
        }

        public void WriteMeasurement(SensorSample sample)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(MeasurementFileWriter));
            }

            if (sample == null)
            {
                return;
            }

            measurementsWriter.WriteLine(BuildSampleRow(sample));
        }

        public void WriteReject(SensorSample sample, string reason)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(MeasurementFileWriter));
            }

            string row;

            if (sample == null)
            {
                row = ",,,,";
            }
            else
            {
                row = BuildSampleRow(sample);
            }

            rejectsWriter.WriteLine(row + "," + EscapeCsv(reason));
        }

        private static string BuildSampleRow(SensorSample sample)
        {
            return string.Join(",",
                sample.Volume.ToString(culture),
                sample.T_DHT.ToString(culture),
                sample.T_BMP.ToString(culture),
                sample.Pressure.ToString(culture),
                sample.DateTime.ToString("o", culture));
        }

        private static string EscapeCsv(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            value = value.Replace("\"", "\"\"");

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return "\"" + value + "\"";
            }

            return value;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MeasurementFileWriter()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (measurementsWriter != null)
                    {
                        measurementsWriter.Dispose();
                    }

                    if (rejectsWriter != null)
                    {
                        rejectsWriter.Dispose();
                    }

                    Console.WriteLine("[FileWriter] Fajlovi su zatvoreni.");
                }

                disposed = true;
            }
        }
    }
}