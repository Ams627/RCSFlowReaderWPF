using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace RCSFlowReaderWPF
{
    class RCSViewModel : DependencyObject
    {
        public int FElementCount
        {
            get { return (int)GetValue(FElementCountProperty); }
            set { SetValue(FElementCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FElementCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FElementCountProperty =
            DependencyProperty.Register("FElementCount", typeof(int), typeof(RCSViewModel), new PropertyMetadata(0));

        enum RCSFlowStates { Init, RCSFlowData, Header, FlowData, F, T, FF, };
        private async Task<int> ReadRCSFlowInfo()
        {
            using (var streamreader = new StreamReader("s:/RCS_R_F_170108_00555.xml"))
            {
                var xr = XmlTextReader.Create(streamreader, new XmlReaderSettings { Async = true });
                xr.MoveToContent();

                int fm1Count = 0;

                while (await xr.ReadAsync())
                {
                    if (xr.NodeType == XmlNodeType.Element)
                    {
                        if (xr.Name == "F")
                        {
                            fm1Count = 0;
                        }
                        else if (xr.Name == "FF")
                        {
                            if (xr.HasAttributes)
                            {
                                var fmatt = xr.GetAttribute("fm");
                                if (fmatt != null && fmatt == "00001")
                                {
                                    fm1Count++;
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
                                FElementCount++;
                            }
                        }
                    }
                }
            }
            return FElementCount;
        }

        public RCSViewModel()
        {
            ReadRCSFlowInfo();
        }
    }
}
