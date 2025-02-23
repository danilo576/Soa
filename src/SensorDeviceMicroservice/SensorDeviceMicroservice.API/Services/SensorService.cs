﻿ using CsvHelper;
using CsvHelper.Configuration;
using SensorDeviceMicroservice.API.Model;
using System.Globalization;
using System.Timers;

namespace SensorDeviceMicroservice.API.Services
{
    public class SensorService
    {
        public const decimal DEFAULT_THRESHOLD = 0.1M;
        public decimal Threshold { get; set; }
        public double Timeout { get; set; }
        public decimal lastValue = 0;
        public bool IsThresholdSet { get; set; } //cemu ovo sluzi
 

        public Data DataToProceed { get; set; }


        public string SensorType { get; set; }

        private readonly System.Timers.Timer _timer;

        private StreamReader _streamReader;

        private CsvReader _csv;

        public string _filePath;

        private static bool _shouldTimerWork = false;

        public SensorService(string sensorType)
        {
            IsThresholdSet = false;
            SensorType = sensorType;
            DataToProceed = new Data();
            DataToProceed.SensorType = sensorType;
            Threshold = DEFAULT_THRESHOLD;
            Timeout = 3000;
            _timer = new System.Timers.Timer(Timeout);
            _timer.Elapsed += OnTimerEventAsync;
            //_filePath = "./Resources/air_pol_delhi.csv";
            _filePath = "/tmp/air_pol_delhi.csv";
            _streamReader = new StreamReader(_filePath);
            CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture);
            _csv = new CsvReader(_streamReader, config);
            _csv.Read();
            _csv.ReadHeader();
        }


        public void SensorStop()
        {
            _shouldTimerWork = false;
            _timer.Enabled = false;
        }

        public void SensorStart()
        {
            _shouldTimerWork = true;
            _timer.Enabled = true;
        }

        private async void OnTimerEventAsync(object sender, ElapsedEventArgs args)
        {
            if (_shouldTimerWork)
            {
                ReadValue();
                
                Console.WriteLine(DataToProceed.SensorType);
                Console.WriteLine("Prethodna vrednost: " + lastValue + ", a nova vrednost: " + DataToProceed.Value);

                if( (DataToProceed.Value > (lastValue + lastValue*Threshold)) || (DataToProceed.Value < (lastValue - lastValue * Threshold)) )
                { //ako se nova vrednost promenila za threshold u odnosu na prethodnu vrednost onda se salje nova vrednost
                    Console.WriteLine("Saljem novu vrednost: " + DataToProceed.Value);
                    HttpClient httpClient = new HttpClient();
                    var responseMessage = await httpClient.PostAsJsonAsync("http://datamicroservice.api:80/api/Data/AddData", DataToProceed);
                    //Console.WriteLine(responseMessage);
                }

                lastValue = DataToProceed.Value;
            }

        }

        public void SetTimeout(double interval)
        {
            _timer.Stop();
            this.Timeout = interval;
            _timer.Interval = interval;
            _timer.Start();
        }


        private void ReadValue()
        {
            try
            {
                string sensor_value;
                //string id;
                string city;
                string site;
                string toDate;
                string fromDate;

                if (_csv.Read())
                {
                    sensor_value = _csv.GetField<string>(DataToProceed.SensorType);
                   // id = _csv.GetField<string>("id");
                    city = _csv.GetField<string>("city");
                    site = _csv.GetField<string>("site");
                    toDate = _csv.GetField<string>("to_date");
                    fromDate = _csv.GetField<string>("from_date");
                }

                else
                {
                    _streamReader.DiscardBufferedData();
                    using (_csv) { }
                    _streamReader = new StreamReader(_filePath);
                    CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture);
                    _csv = new CsvReader(_streamReader, config);
                    _csv.Read();
                    _csv.ReadHeader();
                    sensor_value = _csv.GetField<string>(DataToProceed.SensorType);
                   // id = _csv.GetField<string>("id");
                    city = _csv.GetField<string>("city");
                    site = _csv.GetField<string>("site");
                    toDate = _csv.GetField<string>("to_date");
                    fromDate = _csv.GetField<string>("from_date");
                }
                DataToProceed.Id = "";
                DataToProceed.City = city;
                DataToProceed.Site = site;
                DataToProceed.ToDate = toDate;
                DataToProceed.FromDate = fromDate;
                DataToProceed.Value = decimal.Parse(sensor_value, CultureInfo.InvariantCulture);
            }
            catch (IOException e)
            {
                Console.WriteLine("Something went wrong,this file can not be read: ");
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
