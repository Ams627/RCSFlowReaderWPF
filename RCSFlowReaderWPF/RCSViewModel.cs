﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
        public DateTime QuoteDate { get; set; }

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

        // Using a DependencyProperty as the backing store for FElementRelevantCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FElementRelevantCountProperty =
            DependencyProperty.Register("FElementRelevantCount", typeof(int), typeof(RCSViewModel), new PropertyMetadata(0));

        public int TElementCount
        {
            get { return (int)GetValue(TElementCountProperty); }
            set { SetValue(TElementCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TElementCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TElementCountProperty =
            DependencyProperty.Register("TElementCount", typeof(int), typeof(RCSViewModel), new PropertyMetadata(0));

        enum RCSFlowStates { Init, RCSFlowData, Header, FlowData, F, T, FF, };
        private async Task<int> ReadRCSFlowInfo()
        {
            FElementTotalCount = FElementRelevantCount = TElementCount = 0;
            using (var streamreader = new StreamReader("s:/RCS_R_F_170108_00555.xml"))
            {
                var xr = XmlTextReader.Create(streamreader, new XmlReaderSettings { Async = true });
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
                            ticketType = xr.GetAttribute("t");
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

                                    DateTime endDate, startDate, quoteDate;

                                    var rawdate = "20" + xr.GetAttribute("u");
                                    if (DateTime.TryParseExact(rawdate, "yyyymmdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
                                    {
                                        if (endDate.Date >= DateTime.Now.Date)
                                        {
                                            rawdate = "20" + xr.GetAttribute("f");
                                            if (DateTime.TryParseExact(rawdate, "yyyymmdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
                                            {
                                                var pattribute = xr.GetAttribute("p");
                                                if (string.IsNullOrWhiteSpace(pattribute))
                                                {
                                                    quoteDate = DateTime.Now;
                                                }
                                                else
                                                {
                                                    rawdate = "20" + pattribute;
                                                    if (!DateTime.TryParseExact(rawdate, "yyyymmdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out quoteDate))
                                                    {
                                                        quoteDate = DateTime.Now;
                                                    }
                                                }
                                                var season = xr.GetAttribute("s");
                                                var ff = new FF
                                                {
                                                    EndDate = endDate,
                                                    StartDate = startDate,
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
            await ReadRCSFlowInfo();
            FlowDone = true;
        }

        public RCSViewModel()
        {
            StartRead();
        }
    }
}
