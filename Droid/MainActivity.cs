using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V4.App;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Support.V4.Content;
using Android;
using Android.Content.PM;
using Android.Locations;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Android.Views;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using System;
using System.Threading.Tasks;
using Android.Preferences;
using Android.Content;
using System.Linq;
using Android.Graphics;
using OMAPGMap.Models;
using Android.Views.InputMethods;
using System.Collections.Generic;
using MoreLinq;
using System.Threading;
using static Android.Gms.Maps.GoogleMap;

namespace OMAPGMap.Droid
{
    [Activity(Label = "OMA PGMap", MainLauncher = true, Icon = "@mipmap/ic_launcher", WindowSoftInputMode = SoftInput.AdjustResize)]
    public class MainActivity : AppCompatActivity, IOnMapReadyCallback, IInfoWindowAdapter
    {
        MapFragment _mapFragment = null;
        GoogleMap map = null;

        private LatLng mDefaultLocation = new LatLng(41.2524, -95.9980); //default location middle of omaha
        private int defaultZoom = 14;
        private int request_fine_location = 1;
        private bool mLocationPermissionGranted;
        private Location lastKnownLocation;
        private bool centeredMap = false;
        private EditText username;
        private EditText password;
        private CardView loginHolder;
        private  ISharedPreferences prefs;
        private CardView settingsHolder;
        private TextView loginMessage;

        public static int NumPokes = 378;
        private int[] pokeResourceMap = new int[NumPokes];
        private List<Pokemon> PokesOnMap = new List<Pokemon>();
        private List<Pokemon> PokesVisible = new List<Pokemon>();

        private ListView settingsListview;

        private Timer secondTimer;
        private Timer minuteTimer;
        private UserSettings settings => ServiceLayer.SharedInstance.Settings;

        private Location userLocation;

        readonly string[] PermissionsLocation =
        {
            Manifest.Permission.AccessCoarseLocation,
            Manifest.Permission.AccessFineLocation
        };

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Window.RequestFeature(WindowFeatures.NoTitle);
            AppCenter.Start("7ac229ee-9940-46b8-becc-d2611c48b2ad", typeof(Analytics), typeof(Crashes));

            prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            await ServiceLayer.SharedInstance.InitalizeSettings();

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            for (int i = 0; i < NumPokes; i++)
            {
                try
                {
                    pokeResourceMap[i] = (int)typeof(Resource.Mipmap).GetField($"p{i.ToString("d3")}").GetValue(null);
                }catch(Exception e)
                {
                    Console.WriteLine($"poke {i} not found");
                }
            }
            _mapFragment = FragmentManager.FindFragmentByTag("map") as MapFragment;
            if (_mapFragment == null)
            {
                GoogleMapOptions mapOptions = new GoogleMapOptions()
                    .InvokeMapType(GoogleMap.MapTypeNormal)
                    .InvokeZoomControlsEnabled(false)
                    .InvokeCompassEnabled(true);    

                Android.App.FragmentTransaction fragTx = FragmentManager.BeginTransaction();
                _mapFragment = MapFragment.NewInstance(mapOptions);
                fragTx.Add(Resource.Id.map, _mapFragment, "map");
                fragTx.Commit();
            }

            loginHolder = FindViewById(Resource.Id.loginHolder) as CardView;
            username = FindViewById(Resource.Id.username) as EditText;
            if(settings.LoggedIn)
            {
                loginHolder.Visibility = ViewStates.Gone;
                _mapFragment.GetMapAsync(this);
                var imm = GetSystemService(Context.InputMethodService) as InputMethodManager;
                imm.HideSoftInputFromWindow(username.WindowToken, 0);
                await LoadData();
                secondTimer = new Timer(HandleTimerCallback, null, 5000, 5000);
                minuteTimer = new Timer(refreshMap, null, 60000, 60000);
            } else 
            {
                
                password = FindViewById(Resource.Id.password) as EditText;
                username.RequestFocus();
                _mapFragment.GetMapAsync(this);
            }

            var loginButton = FindViewById(Resource.Id.signInButton) as Button;
            loginButton.Click += LoginButton_Click;
            var settingButton = FindViewById(Resource.Id.settingsButton);
            settingButton.Click += SettingButton_Click;
            var layerButton = FindViewById(Resource.Id.layerssButton);
            layerButton.Click += LayerButton_Click;
            settingsHolder = FindViewById(Resource.Id.settingsHolder) as CardView;
            var settingsDone = settingsHolder.FindViewById(Resource.Id.settingsDoneButton);
            settingsDone.Click += SettingsDone_Click;

            settingsListview = FindViewById(Resource.Id.settingsListView) as ListView;
            settingsListview.Adapter = new SettingsAdaptor(this, pokeResourceMap);
        }

        private void refreshMap(object state)
        {
            RunOnUiThread(async () =>
            {
                await ServiceLayer.SharedInstance.LoadData();
                UpdateMapPokemon(false);
            });
        }

        private void HandleTimerCallback(object state)
        {
            RunOnUiThread(() =>
            {
                UpdateMapPokemon(false);
            });

        }

        private async Task LoadData()
        {
            var progress = new ProgressDialog(this);  
            progress.Indeterminate = true;  
            progress.SetProgressStyle(ProgressDialogStyle.Spinner);  
            progress.SetMessage("Loading...");  
            progress.SetCancelable(false);  
            progress.Show();

            await ServiceLayer.SharedInstance.LoadData();
            progress.Dismiss();

            var toAdd = ServiceLayer.SharedInstance.Pokemon.Where(p => !settings.PokemonTrash.Contains(p.pokemon_id));
            var bounds = map.Projection.VisibleRegion.LatLngBounds;
            var visible = toAdd.Where(p => bounds.Contains(p.Location));
            foreach(var p in visible)
            {
                AddPokemonMarker(p);
            }
            PokesVisible.AddRange(toAdd.Except(visible));
        }

        private async void RefreshData()
        {
            try
            {
                await ServiceLayer.SharedInstance.LoadData();
            }
            catch (Exception)
            {
                //swallow exception because it tastes good
            }
            UpdateMapPokemon(false);
        }

        private void UpdateMapPokemon(bool reload)
        {
            var bounds = map.Projection.VisibleRegion.LatLngBounds;
            var toRemove = PokesOnMap.Where(p => !bounds.Contains(p.Location) || p.ExpiresDate < DateTime.UtcNow).ToList();
            if(reload)
            {
                toRemove = PokesOnMap;
            }
            foreach (var p in toRemove)
            {
                p.PokeMarker.Remove();
                p.PokeMarker = null;
                PokesOnMap.Remove(p);
            }
            IEnumerable<Pokemon> toAdd;
            if (settings.NinetyOnlyEnabled)
            {
                toAdd = ServiceLayer.SharedInstance.Pokemon.Where(p => p.iv > 0.9).Except(PokesOnMap);
            }
            else
            {
                toAdd = ServiceLayer.SharedInstance.Pokemon.Where(p => !settings.PokemonTrash.Contains(p.pokemon_id)).Except(PokesOnMap);
            }

            var visible = toAdd.Where(p => bounds.Contains(p.Location));
            foreach (var p in visible)
            {
                AddPokemonMarker(p);
            }
            PokesVisible.AddRange(toAdd.Except(visible));
            Console.WriteLine($"Adding {visible.Count()} mons to the map");
            var now = DateTime.UtcNow;
            foreach(var p in PokesOnMap)
            {
                var diff = p.ExpiresDate - now;
                if (diff.Minutes < 1)
                {
                    p.PokeMarker.Alpha = diff.Seconds / 60.0f;
                }
            }
        }

        private void AddPokemonMarker(Pokemon p)
        {
            var mOps = new MarkerOptions();
            mOps.SetPosition(p.Location);

            mOps.SetIcon(BitmapDescriptorFactory.FromBitmap(GetPokemonMarker(p)));
            mOps.Anchor(0.5f, 0.5f);
            var marker = map.AddMarker(mOps);
            marker.Tag = $"poke:{p.id}";
            p.PokeMarker = marker;
            PokesOnMap.Add(p);
        }

        public void OnMapReady(GoogleMap mapp)
        {
            map = mapp;
            var camUpdate = CameraUpdateFactory.NewLatLngZoom(mDefaultLocation, defaultZoom);
            map.MoveCamera(camUpdate);
            DisplayUserLocation();

            map.MyLocationChange += (sender, e) => 
            {
                if(!centeredMap)
                {
                    centeredMap = true;
                    var cam = CameraUpdateFactory.NewLatLngZoom(new LatLng(e.Location.Latitude, e.Location.Longitude), defaultZoom);
                    map.MoveCamera(cam);
                }
                userLocation = e.Location;
            };
            map.SetInfoWindowAdapter(this);
            map.CameraIdle += Map_CameraIdle;
        }

        void Map_CameraIdle(object sender, EventArgs e)
        {
            UpdateMapPokemon(false);
        }

        private void RequestLocation()
        {
            if(ContextCompat.CheckSelfPermission(ApplicationContext, Android.Manifest.Permission.AccessFineLocation) == Permission.Granted)
            {
                mLocationPermissionGranted = true;
                DisplayUserLocation();
            } else 
            {
                RequestPermissions(PermissionsLocation, 0);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == 0 && grantResults.Length > 0 && grantResults[0] == Permission.Granted)
            {
                mLocationPermissionGranted = true;
                DisplayUserLocation();
            }
        }

        public void DisplayUserLocation()
        {
            if(map == null)
            {
                return;
            }
            if(mLocationPermissionGranted)
            {
                map.MyLocationEnabled = true;
                map.UiSettings.MyLocationButtonEnabled = true;

            } else
            {
                map.MyLocationEnabled = false;
                map.UiSettings.MyLocationButtonEnabled = true;

                RequestLocation();
            }

        }

        Bitmap GetPokemonMarker(Pokemon poke)
        {
            var mapMarker = ((LayoutInflater)GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.map_marker, null);
            var img = mapMarker.FindViewById(Resource.Id.marker_img) as ImageView;
            try
            {
                img.SetImageResource(pokeResourceMap[poke.pokemon_id]);
            }catch(Exception){}
            var imgTitle = mapMarker.FindViewById(Resource.Id.marker_text) as TextView;
            imgTitle.Text = poke.ExpiresDate.AddHours(-6.0).ToShortTimeString();
            mapMarker.Measure(0, 0);
            mapMarker.Layout(0, 0, mapMarker.MeasuredWidth, mapMarker.MeasuredHeight);
            mapMarker.BuildDrawingCache();
            var rval = Bitmap.CreateBitmap(mapMarker.MeasuredWidth, mapMarker.MeasuredHeight, Bitmap.Config.Argb8888);
            var canvas = new Canvas(rval);
            canvas.DrawColor(Color.White, PorterDuff.Mode.SrcIn);
            mapMarker.Draw(canvas);
            return rval;
        }

        async void LoginButton_Click(object sender, EventArgs e)
        {
            settings.Username = username.Text;
            settings.Password = password.Text;
            var loggedIn = await ServiceLayer.SharedInstance.VerifyCredentials();
            if(loggedIn)
            {
                loginHolder.Visibility = ViewStates.Gone;
                await LoadData();
            } else 
            {
                Android.Support.V7.App.AlertDialog.Builder alert = new Android.Support.V7.App.AlertDialog.Builder(this);
                alert.SetTitle("Error Logging In");
                alert.SetMessage("Username or password incorrect");
                alert.SetPositiveButton("Ok", (senderAlert, args) => { });

                Dialog dialog = alert.Create();
                dialog.Show();
            }
        }

        public View GetInfoContents(Marker marker)
        {
            var markerInfo = ((LayoutInflater)GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.pokemon_info, null);
            var title = markerInfo.FindViewById(Resource.Id.info_title) as TextView;
            var distLabel = markerInfo.FindViewById(Resource.Id.info_distance) as TextView;
            var markerTag = marker.Tag.ToString().Split(':');
            var t = markerTag[0];
            var id = markerTag[1];
            if (t == "poke") 
            {
                var poke = ServiceLayer.SharedInstance.Pokemon.Where(p => p.id == id).FirstOrDefault();
                var infoTitle = $"{poke.name} ({poke.gender}) - #{poke.pokemon_id}";
                if (!string.IsNullOrEmpty(poke.move1))
                {
                    t = $"{t} - {poke.iv.ToString("F1")}%";
                }
                title.Text = infoTitle;
                if (userLocation != null)
                {
                    var l = new Location("");
                    l.Latitude = marker.Position.Latitude;
                    l.Longitude = marker.Position.Longitude;
                    var dist = userLocation.DistanceTo(l) * 0.000621371;
                    distLabel.Text = $"{dist.ToString("F1")} miles away";
                } else {
                    distLabel.Text = "Dist unknown";
                }
            }

            return markerInfo;
        }

        public View GetInfoWindow(Marker marker)
        {
            return null;
        }

        void SettingButton_Click(object sender, EventArgs e)
        {
            settingsHolder.Visibility = ViewStates.Visible;
        }

        void LayerButton_Click(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            var layersMenu = ((LayoutInflater)GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.map_layers, null);
            var popup = new PopupWindow(layersMenu);
            popup.ShowAsDropDown(button, 40, 40, GravityFlags.Bottom);
        }

        void SettingsDone_Click(object sender, EventArgs e)
        {
            settingsHolder.Visibility = ViewStates.Gone;
            UpdateMapPokemon(true);
        }
    }
}

