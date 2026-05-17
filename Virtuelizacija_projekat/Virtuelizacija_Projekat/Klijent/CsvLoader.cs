using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Globalization;
using System.IO;
using Common;


namespace Klijent
{
    public class CsvLoader
    {
        private static readonly CultureInfo culture = CultureInfo.InvariantCulture;

        private readonly string csvPath;
        private readonly string logPath;

        public CsvLoader()
        {
            csvPath = ConfigurationManager.AppSettings["csvPath"];
            logPath = ConfigurationManager.AppSettings["logPath"];
        }

 
        public List<SensorSample> LoadFirst100Samples()
        {
            List<SensorSample> samples = new List<SensorSample>();

            if (string.IsNullOrWhiteSpace(csvPath))
            {
                Console.WriteLine("[CsvLoader] GRESKA: Putanja do CSV fajla nije konfigurisana u App.config");
                return samples;
            }

            if (!File.Exists(csvPath))
            {
                Console.WriteLine("[CsvLoader] GRESKA: CSV fajl nije pronadjen na putanji: " + csvPath);
                return samples;
            }

            Console.WriteLine("[CsvLoader] Ucitavanje CSV fajla: " + csvPath);

            using (StreamReader reader = new StreamReader(csvPath))
            {
                using (StreamWriter logWriter = new StreamWriter(logPath, append: false))
                {
                    logWriter.WriteLine("=== LOG NEVALIDNIH REDOVA ===");
                    logWriter.WriteLine("Datum: " + DateTime.Now.ToString(culture));
                    logWriter.WriteLine("CSV fajl: " + csvPath);
                    logWriter.WriteLine(new string('-', 50));

                    // preskoci header red
                    string headerLine = reader.ReadLine();
                    if (headerLine == null)
                    {
                        Console.WriteLine("[CsvLoader] GRESKA: CSV fajl je prazan.");
                        return samples;
                    }

                    Console.WriteLine("[CsvLoader] Header: " + headerLine);

                    int redovUkupno = 0;      // (bez headera)
                    int validnih = 0;          
                    int nevalidnih = 0;        

                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        redovUkupno++;  

                        if (validnih >= 100)
                        {
                            logWriter.WriteLine($"[Red {redovUkupno + 1}] preskocen...");
                            continue;
                        }

                        SensorSample sample;
                        string greskaOpis;

                        if (TryParseRow(line, redovUkupno, out sample, out greskaOpis))
                        {
                            samples.Add(sample);
                            validnih++;
                        }
                        else
                        {
                            // upis nevalidnog reda u log
                            nevalidnih++;
                            logWriter.WriteLine($"[Red {redovUkupno + 1}] nevalidan - {greskaOpis}");
                            logWriter.WriteLine($"           Sadrzaj: {line}");
                            Console.WriteLine($"[CsvLoader] Nevalidan red {redovUkupno + 1}: {greskaOpis}");
                        }
                    }

                    logWriter.WriteLine(new string('-', 50));
                    logWriter.WriteLine($"Ukupno redova: {redovUkupno}");
                    logWriter.WriteLine($"Validnih: {validnih}");
                    logWriter.WriteLine($"Nevalidnih/preskocenih: {nevalidnih}");

                    Console.WriteLine($"[CsvLoader] Ucitavanje zavrseno. Validnih: {validnih}, Nevalidnih: {nevalidnih}");
                    Console.WriteLine("[CsvLoader] Log nevalidnih redova sacuvan: " + logPath);
                }
            }

            return samples;
        }


        private bool TryParseRow(string line, int rowIndex, out SensorSample sample, out string greskaOpis)
        {
            sample = null;
            greskaOpis = string.Empty;

            if (string.IsNullOrWhiteSpace(line))
            {
                greskaOpis = "Prazan red.";
                return false;
            }

            string[] parts = line.Split(',');

            if (parts.Length != 10)
            {
                greskaOpis = $"Pogresan broj kolona: ocekivano 10, pronadjeno {parts.Length}.";
                return false;
            }

            DateTime dateTime;
            double volume, tDht, pressure, tBmp;

            if (!DateTime.TryParse(parts[0].Trim(), culture, DateTimeStyles.None, out dateTime))
            {
                greskaOpis = $"Nevalidan DateTime: '{parts[0].Trim()}'.";
                return false;
            }

            if (!double.TryParse(parts[1].Trim(), NumberStyles.Float, culture, out volume))
            {
                greskaOpis = $"Nevalidan Volume: '{parts[1].Trim()}'.";
                return false;
            }

            if (!double.TryParse(parts[3].Trim(), NumberStyles.Float, culture, out tDht))
            {
                greskaOpis = $"Nevalidan T_DHT: '{parts[3].Trim()}'.";
                return false;
            }

            if (!double.TryParse(parts[4].Trim(), NumberStyles.Float, culture, out pressure))
            {
                greskaOpis = $"Nevalidan Pressure: '{parts[4].Trim()}'.";
                return false;
            }

            if (!double.TryParse(parts[5].Trim(), NumberStyles.Float, culture, out tBmp))
            {
                greskaOpis = $"Nevalidan T_BMP: '{parts[5].Trim()}'.";
                return false;
            }

            sample = new SensorSample
            {
                Volume = volume,
                T_DHT = tDht,
                T_BMP = tBmp,
                Pressure = pressure,
                DateTime = dateTime
            };

            return true;
        }
    }
}
