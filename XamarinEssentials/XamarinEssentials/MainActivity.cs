using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using System.Threading.Tasks;
using Xamarin.Essentials;
using System;
using System.Threading;
using System.Linq;
using XamarinEssentials.Model;
using System.Collections.Generic;
using Android.Content;
using Newtonsoft.Json;

namespace XamarinEssentials
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        //Seconds * minutes
        int _TimeDetections_seconds = 60000 * 1;

        List<Position> _listPositions;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            //init
            Init();
        }

        async Task Init() {

            //see connectivity
            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;


            while (true)
            {
                //Detect Lat - Log - ok
                var pObj =  await CurrentPosition();

                //verificar se retornou algum erro.
                Console.WriteLine($"x :: Latitude: {pObj.Lat}, Longitude: {pObj.Log}, Altitude: {pObj.Alt}");

                //Conectivity - ok
                if (haveAcessInternet())
                {
                    //TODO: Send to APi
                    await SendToApi(pObj);
                }
                else
                {
                    //localStorage - ok
                    SavePosition(pObj);
                }

                //TODO: detectando a bateria
                //TODO: LocalNotifications


                //next process - ok
                Thread.Sleep(_TimeDetections_seconds);
            }
        }

        /// <summary>
        /// save file preferences
        /// </summary>
        /// <param name="pos"></param>
        private void SavePosition(Position pos)
        {
            // get shared preferences
            ISharedPreferences pref = Application.Context.GetSharedPreferences("PREFERENCE_NAME", FileCreationMode.Private);

            // read exisiting value
            var customers = pref.GetString("PositionsCell", null);

            // if preferences return null, initialize listOfPositionsCell
            if (customers == null)
            {
                _listPositions = new List<Position>();
            }
            else
            {
                _listPositions = JsonConvert.DeserializeObject<List<Position>>(customers);
            }

            // if deserialization return null, initialize listOfPositionsCell
            if (_listPositions == null)
            {
                _listPositions = new List<Position>();
            }

            // add your object to list of PositionsCell
            _listPositions.Add(pos);

            // convert the list to json
            var listOfCustomersAsJson = JsonConvert.SerializeObject(_listPositions);

            ISharedPreferencesEditor editor = pref.Edit();

            // set the value to PositionsCell key
            editor.PutString("PositionsCell", listOfCustomersAsJson);

            // commit the changes
            editor.Commit();
        }

        /// <summary>
        /// read file preferences
        /// </summary>
        /// <returns></returns>
        private List<Position> GetPositionsFromPreferences()
        {
            // get shared preferences
            ISharedPreferences pref = Application.Context.GetSharedPreferences("PREFERENCE_NAME", FileCreationMode.Private);

            // read exisiting value
            var positions = pref.GetString("PositionsCell", null);

            // if preferences return null, initialize listOfCustomers
            if (positions == null)
                return new List<Position>();

            var listPostions = JsonConvert.DeserializeObject<List<Position>>(positions);

            if (listPostions == null)
                return new List<Position>();

            return listPostions;
        }

        /// <summary>
        /// detect connectivity
        /// </summary>
        /// <returns></returns>
        private bool haveAcessInternet()
        {
            var current = Connectivity.NetworkAccess;

            // Connection to internet is available ?
            return (current == NetworkAccess.Internet);
        }

        /// <summary>
        /// Event connectivity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            var access = e.NetworkAccess;
            var profiles = e.ConnectionProfiles;

            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                //TODO: 
                //verify if has positions to send throu api

                //1 - read Files

                //2 - send to Api
            }

        }


        /// <summary>
        /// send to Api
        /// </summary>
        /// <returns></returns>
        async Task SendToApi(Model.Position positions) 
        {
            Console.WriteLine("=> Sending from api");
        }

        async Task SendToApi(List<Model.Position> positions)
        {
            Console.WriteLine("=> Sending from api");
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }


        private async Task<Model.Position> CurrentPosition() {

            Model.Position pObj = new Model.Position();

            try
            {
                //basic
                //var location = await Geolocation.GetLastKnownLocationAsync();

                var request = new GeolocationRequest(GeolocationAccuracy.High);
                var location = await Geolocation.GetLocationAsync(request);

                if (location != null)
                {
                    Console.WriteLine($"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}");

                    pObj.Lat = location.Latitude;
                    pObj.Log = location.Longitude;
                    pObj.Alt = location.Altitude;

                    pObj.GeoCodeAddress = await CurrentPositionPlace(location.Latitude, location.Longitude);
                }
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Handle not supported on device exception
                pObj.MessageError = fnsEx.Message;
            }
            catch (FeatureNotEnabledException fneEx)
            {
                // Handle not enabled on device exception
                pObj.MessageError = fneEx.Message;
            }
            catch (PermissionException pEx)
            {
                // Handle permission exception
                pObj.MessageError = pEx.Message;
            }
            catch (Exception ex)
            {
                // Unable to get location
                pObj.MessageError = ex.Message;
            }

            return pObj;
        }

        private async Task<GeoCodeAddress> CurrentPositionPlace(double _lat, double _log)
        {
            GeoCodeAddress gObj = new GeoCodeAddress();

            try
            {
                var lat = _lat;
                var lon = _log;

                var placemarks = await Geocoding.GetPlacemarksAsync(lat, lon);

                var placemark = placemarks?.FirstOrDefault();
                if (placemark != null)
                {
                    gObj = new GeoCodeAddress()
                    {
                        AdminArea = placemark.AdminArea,
                        CountryCode = placemark.CountryCode,
                        CountryName = placemark.CountryName,
                        FeatureName = placemark.FeatureName,
                        Locality =       placemark.Locality,
                        PostalCode =      placemark.PostalCode,
                        SubAdminArea =    placemark.SubAdminArea,
                        SubLocality =     placemark.SubLocality,
                        SubThoroughfare = placemark.SubThoroughfare,
                        Thoroughfare =    placemark.Thoroughfare
                    };
                }
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Feature not supported on device
                gObj.MessageError = fnsEx.Message;
            }
            catch (Exception ex)
            {
                // Handle exception that may have occurred in geocoding
                gObj.MessageError = ex.Message;
            }

            return gObj;
        }


    }
}