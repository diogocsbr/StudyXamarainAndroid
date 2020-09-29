using Android.App;
using Android.OS;

using Android.Support.V7.App;
using Android.Support.V4.App;

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

        static readonly int NOTIFICATION_ID = 1000;
        static readonly string CHANNEL_ID = "location_notification";
        internal static readonly string COUNT_KEY = "count";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            //
            CreateNotificationChannel();

            //init
            Init();
        }

        async Task Init() {

            //see connectivity = not ocurring in the same time
            //Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;

            //checkBattery
            Battery.BatteryInfoChanged += Battery_BatteryInfoChanged;

            while (true)
            {
                //Detect Lat - Log - ok
                var pObj =  await CurrentPosition();

                //verificar se retornou algum erro.
                Console.WriteLine($"x :: Latitude: {pObj.Lat}, Longitude: {pObj.Log}, Altitude: {pObj.Alt}");

                //Conectivity - ok
                if (haveAcessInternet())
                {
                    //verify if has Positions on file

                    //1 - read Files
                    var Listpostions = GetPositionsFromPreferences();

                    //2 - send to Api
                    if (Listpostions.Count > 0)
                    {
                        SendToApi(Listpostions).Wait();

                        //3 -  Clean (limpar a lista)
                        ClearPositionsFile();
                    }

                    //TODO: Send to APi
                    await SendToApi(pObj);
                }
                else
                {
                    //localStorage - ok
                    SavePosition(pObj);
                }


                //next process - ok
                Thread.Sleep(_TimeDetections_seconds);
            }
        }

        private void Battery_BatteryInfoChanged(object sender, BatteryInfoChangedEventArgs e)
        {
            //Question: If the 
            //Battery.EnergySaverStatusChanged += OnEnergySaverStatusChanged;
            // var status = e.EnergySaverStatus; on?

            var level = e.ChargeLevel;
            var state = e.State;
            var source = e.PowerSource;
            Console.WriteLine($"Reading: Level: {level}, State: {state}, Source: {source}");

            if (level < 0.20)
            {
                CreateLocalNotification();
            }
        }

        void CreateLocalNotification() {

            int count = 1;

            // Pass the current button press count value to the next activity:
            var valuesForActivity = new Bundle();
            valuesForActivity.PutInt(COUNT_KEY, count);

            // When the user clicks the notification, MainActivity will start up.
            var resultIntent = new Intent(this, typeof(MainActivity));

            // Pass some values to MainActivity:
            //resultIntent.PutExtras(valuesForActivity);

            // Construct a back stack for cross-task navigation:
            var stackBuilder = Android.Support.V4.App.TaskStackBuilder.Create(this);
            stackBuilder.AddParentStack(Java.Lang.Class.FromType(typeof(MainActivity)));
            stackBuilder.AddNextIntent(resultIntent);

            //// Create the PendingIntent with the back stack:
            var resultPendingIntent = stackBuilder.GetPendingIntent(0, (int)PendingIntentFlags.UpdateCurrent);

            // Build the notification:
            var builder = new NotificationCompat.Builder(this, CHANNEL_ID)
                          .SetAutoCancel(true) // Dismiss the notification from the notification area when the user clicks on it
                          .SetContentIntent(resultPendingIntent) // Start up this activity when the user clicks the intent.
                          .SetContentTitle("JustDrive") // Set the title
                          .SetNumber(count) // Display the count in the Content Info
                          .SetSmallIcon(Resource.Drawable.notification_bg) // This is the icon to display
                          .SetContentText($"Battery is low, charge it"); // the message to display.

            // Finally, publish the notification:
            var notificationManager = NotificationManagerCompat.From(this);
            notificationManager.Notify(NOTIFICATION_ID, builder.Build());

            count++;
        }

        void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification
                // channel on older versions of Android.
                return;
            }

            var name = Resources.GetString(Resource.String.channel_name);
            var description = GetString(Resource.String.channel_description);
            var channel = new NotificationChannel(CHANNEL_ID, name, NotificationImportance.Default)
            {
                Description = description
            };

            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(channel);
        }

        /// <summary>
        /// save file preferences
        /// </summary>
        /// <param name="pos"></param>
        private void SavePosition(Position pos)
        {
            Console.WriteLine("=> saving positions to file");
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

        private void ClearPositionsFile()
        {
            Console.WriteLine("=> saving positions to file");
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
            _listPositions.Clear();

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
                //1 - read Files
                var Listpostions = GetPositionsFromPreferences();

                //2 - send to Api
                if (Listpostions.Count > 0)
                {
                    SendToApi(Listpostions).Wait();

                    //3 -  Clean (limpar a lista)
                    ClearPositionsFile();
                }
            }

        }


        /// <summary>
        /// send to Api
        /// </summary>
        /// <returns></returns>
        async Task SendToApi(Model.Position position) 
        {
            Console.WriteLine("=> Sending to api direct");
            Console.WriteLine($"=> lat: {position.Lat}, log: {position.Log}, place: {position.GeoCodeAddress.AdminArea}");
        }

        async Task SendToApi(List<Model.Position> positions)
        {
            Console.WriteLine("=> Sending to api from file ");

            foreach (var item in positions)
            {
                Console.WriteLine($"=> lat: {item.Lat}, log: {item.Log}, place: {item.GeoCodeAddress.AdminArea}");
            }
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