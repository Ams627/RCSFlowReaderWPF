using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml;

namespace RCSFlowReaderWPF
{
    class Flow
    {
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string Route { get; set; }
    }

    class FF
    {
        /// <summary>
        /// f attribute
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// u attribute
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// p attribute
        /// </summary>
        public DateTime? QuoteDate { get; set; }

        /// <summary>
        /// k attribute
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// s attribute
        /// </summary>
        public string SeasonDetails { get; set; }
    }

    class RCSViewModel : DependencyObject
    {
        private readonly string _allowedTicketTypes = "7DS 7DF PSS PSF SDS SDR FDS FDR CDS CDR SVR OPB SOR FOR PBD PB7 FFX FSX F1L FSL VA1";
        //private readonly string _allowedTicketTypes = "SDS SDR FDS FDR CDS CDR SVR OPB SOR FOR PBD PB7 FFX FSL";
        //  MCA MCM MCQ MCW


        Dictionary<Flow, Dictionary<string, List<FF>>> flowdic = new Dictionary<Flow, Dictionary<string, List<FF>>>();
        public bool FlowDone
        {
            get { return (bool)GetValue(FlowDoneProperty); }
            set { SetValue(FlowDoneProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FlowDone.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FlowDoneProperty =
            DependencyProperty.Register("FlowDone", typeof(bool), typeof(RCSViewModel), new PropertyMetadata(false));

        public int FElementTotalCount
        {
            get { return (int)GetValue(FElementTotalCountProperty); }
            set { SetValue(FElementTotalCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FElementTotalCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FElementTotalCountProperty =
            DependencyProperty.Register("FElementTotalCount", typeof(int), typeof(RCSViewModel), new PropertyMetadata(0));
        
        public int FElementRelevantCount
        {
            get { return (int)GetValue(FElementRelevantCountProperty); }
            set { SetValue(FElementRelevantCountProperty, value); }
        }

        public static readonly DependencyProperty FElementRelevantCountProperty =
            DependencyProperty.Register("FElementRelevantCount", typeof(int), typeof(RCSViewModel), new PropertyMetadata(0));

        public int TElementCount
        {
            get { return (int)GetValue(TElementCountProperty); }
            set { SetValue(TElementCountProperty, value); }
        }

        public static readonly DependencyProperty TElementCountProperty =
            DependencyProperty.Register("TElementCount", typeof(int), typeof(RCSViewModel), new PropertyMetadata(0));

        public long MemoryUsage
        {
            get { return (long)GetValue(MemoryUsageProperty); }
            set { SetValue(MemoryUsageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MemoryUsage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MemoryUsageProperty =
            DependencyProperty.Register("MemoryUsage", typeof(long), typeof(RCSViewModel), new PropertyMetadata(0L));


        DateTime? GetDate(string rcsDate)
        {
            DateTime? result = null;
            var parseResult = DateTime.TryParseExact("20" + rcsDate, "yyyymmdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d);
            if (parseResult)
            {
                result = d;
            }
            return result;
        }


        private DateTime _startTime;



        public int Seconds
        {
            get { return (int)GetValue(SecondsProperty); }
            set { SetValue(SecondsProperty, value); }
        }

        public static readonly DependencyProperty SecondsProperty =
            DependencyProperty.Register("Seconds", typeof(int), typeof(RCSViewModel), new PropertyMetadata(0));

        enum RCSFlowStates { Init, RCSFlowData, Header, FlowData, F, T, FF, };
        private async Task<int> ReadRCSFlowInfo()
        {
            var ticketTypes = new HashSet<string>(_allowedTicketTypes.Split());
            FElementTotalCount = FElementRelevantCount = TElementCount = 0;
            using (var streamreader = new StreamReader("s:/RCS_R_F_170528_00575.xml"))
            {
                var xr = XmlReader.Create(streamreader, new XmlReaderSettings { Async = true });
                xr.MoveToContent();

                int fm1Count = 0;

                var flow = new Flow { Origin = "****", Destination = "****", Route = "*****" };
                string ticketType = "";

                while (await xr.ReadAsync())
                {
                    if (xr.NodeType == XmlNodeType.Element)
                    {
                        if (xr.Name == "F")
                        {
                            FElementTotalCount++;
                            fm1Count = 0;
                            flow = new Flow
                            {
                                Origin = xr.GetAttribute("o"),
                                Destination = xr.GetAttribute("d"),
                                Route = xr.GetAttribute("r")
                            };
                        }
                        else if (xr.Name == "T")
                        {
                            TElementCount++;
                            // skip all child elements if ticket type is not in the allowed list:
                            ticketType = xr.GetAttribute("t");
                            if (!ticketTypes.Contains(ticketType))
                            {
                                xr.Skip();
                            }
                        }
                        else if (xr.Name == "FF")
                        {
                            if (xr.HasAttributes)
                            {
                                var fmatt = xr.GetAttribute("fm");
                                var key = xr.GetAttribute("k");
                                if (fmatt != null && key != null && fmatt == "00001")
                                {
                                    fm1Count++;
                                    var endDate = GetDate(xr.GetAttribute("u"));
                                    var startDate = GetDate(xr.GetAttribute("f"));
                                    var quoteDate = GetDate(xr.GetAttribute("p"));
                                    if (endDate == null)
                                    {
                                        var endDate2 = GetDate(xr.GetAttribute("u"));
                                        IXmlLineInfo xli = (IXmlLineInfo)xr;
                                        throw new Exception($"Syntax error in file - end date must be specified (line {xli.LineNumber})");
                                    }
                                    if (startDate== null)
                                    {
                                        IXmlLineInfo xli = (IXmlLineInfo)xr;
                                        throw new Exception($"Syntax error in file - start date must be specified (line {xli.LineNumber})");
                                    }

                                    var season = xr.GetAttribute("s");
                                    var ff = new FF
                                    {
                                        EndDate = endDate ?? DateTime.MinValue,
                                        StartDate = startDate ?? DateTime.MinValue,
                                        QuoteDate = quoteDate,
                                        SeasonDetails = season,
                                        Key = key
                                    };
                                    if (!flowdic.ContainsKey(flow))
                                    {
                                        flowdic[flow] = new Dictionary<string, List<FF>>();
                                    }
                                    if (!flowdic[flow].ContainsKey(ticketType))
                                    {
                                        flowdic[flow][ticketType] = new List<FF>();
                                    }
                                    flowdic[flow][ticketType].Add(ff);
                                }
                            }
                        }
                    }
                    else if (xr.NodeType == XmlNodeType.EndElement)
                    {
                        if (xr.Name == "F")
                        {
                            if (fm1Count > 0)
                            {
                                fm1Count = 0;
                                FElementRelevantCount++;
                            }
                        }
                    }
                }
            }
            return FElementRelevantCount;
        }
        private async void StartRead()
        {
            _startTime = DateTime.Now;
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100),
                IsEnabled = true
            };
            timer.Tick += (s, e) => {
                Seconds = (int)(DateTime.Now - _startTime).TotalSeconds;
            };
            timer.Start();

            await ReadRCSFlowInfo();
            timer.Stop();

            using (var stream = new StreamWriter("s:/output2.xml"))
            {
                foreach (var flow in flowdic)
                {
                    stream.WriteLine($"<F i=\"I\" r=\"{flow.Key.Route}\" o=\"{flow.Key.Origin}\" d=\"{flow.Key.Destination}\">");
                    foreach (var ticket in flow.Value)
                    {
                        stream.WriteLine($"    <T t=\"{ticket.Key}\">");
                        foreach (var ff in ticket.Value)
                        {
                            var quoteDate = ff.QuoteDate == null ? "" : $"p=\"{ff.QuoteDate:yymmdd}\" ";
                            var key = ff.Key == null ? "" : $"k=\"{ff.Key}\" ";
                            var season = ff.SeasonDetails == null ? "" : $"s=\"{ff.SeasonDetails}\" ";
                            // u f s p k fm
                            stream.WriteLine($"        <FF u=\"{ff.EndDate:yymmdd}\" f=\"{ff.StartDate:yymmdd}\" {season}{quoteDate}{key}fm=\"00001\"/>");
                        }
                        stream.WriteLine("    </T>");
                    }
                    stream.WriteLine("</F>");
                }
            }
            timer.Stop();
            FlowDone = true;
        }

        public RCSViewModel()
        {
            // start a timer to update memory usage property once per second:
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 1)
            };
            timer.Tick += (s, e) =>
            {
                // update memory usage dependency property with number of megabytes used:
                MemoryUsage = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;
            };
            timer.Start();
            StartRead();
        }
    }
}
