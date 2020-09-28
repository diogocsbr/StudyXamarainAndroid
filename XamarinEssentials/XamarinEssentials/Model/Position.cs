using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace XamarinEssentials.Model
{
    public class Position
    {
        public Position()
        {
            DateOcurr = DateTime.UtcNow;
        }

        //date ocurrency
        public DateTime DateOcurr { get; private set; }

        public double Lat { get; set; }
        public double Log { get; set; }
        public double? Alt { get; set; }

        public GeoCodeAddress GeoCodeAddress { get; set; }
        public string MessageError { get; set; }
    }


    public class GeoCodeAddress
    {
        public string  AdminArea { get; set; }
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
        public string FeatureName { get; set; }
        public string Locality { get; set; }
        public string PostalCode { get; set; }
        public string SubAdminArea { get; set; }
        public string SubLocality { get; set; }
        public string SubThoroughfare { get; set; }
        public string Thoroughfare { get; set; }


        public string MessageError { get; set; }

    }
}