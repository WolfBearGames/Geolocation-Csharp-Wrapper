using System.Linq;
using System.Text;
using Godot;
using System;
using System.Threading.Tasks;

// C# wrapper for Geolocation Android and iOS Plugin
public class Geolocation : Node
{
    public bool Supported { get; private set; } = false;

    public GeolocationErrorCodes LastError;
    public Platforms Platform;

    public enum Platforms
    {
        iOS,
        Android,
        other
    }

    public enum GeolocationAuthorizationStatus
    {
        PERMISSION_STATUS_UNKNOWN = 1 << 0,
        PERMISSION_STATUS_DENIED = 1 << 1,
        PERMISSION_STATUS_ALLOWED = 1 << 2,
    }

    public enum GeolocationDesiredAccuracyConstants
    {
        ACCURACY_BEST_FOR_NAVIGATION = 1 << 0,
        ACCURACY_BEST = 1 << 1,
        ACCURACY_NEAREST_TEN_METERS = 1 << 2,
        ACCURACY_HUNDRED_METERS = 1 << 3,
        ACCURACY_KILOMETER = 1 << 4,
        ACCURACY_THREE_KILOMETER = 1 << 5,
        ACCURACY_REDUCED = 1 << 6,
    }

    public enum GeolocationErrorCodes
    {
        ERROR_DENIED = 1 << 0,
        ERROR_NETWORK = 1 << 1,
        ERROR_HEADING_FAILURE = 1 << 2,
        ERROR_LOCATION_UNKNOWN = 1 << 3,
        ERROR_TIMEOUT = 1 << 4,
        ERROR_UNSUPPORTED = 1 << 5,
        ERROR_LOCATION_DISABLED = 1 << 6,
        ERROR_UNKNOWN = 1 << 7
    }

    public Godot.Object GeolocationPlugin = null!;

    #region event handlers

    public event EventHandler<string>? Log;
    public event EventHandler<GeolocationErrorCodes>? Error;
    public event EventHandler<GeolocationAuthorizationStatus>? AuthorizationChanged;
    public event EventHandler<Location>? LocationUpdate;
    public event EventHandler<Heading>? HeadingUpdate;
    public event EventHandler<bool>? LocationCapabilityResult;

    #endregion

    #region permissions
    public void RequestPermissions()
    {
        if (Platform == Platforms.iOS)
        {
            GeolocationPlugin.Call("request_permission");
            // -> OnAuthorizationChanged will be called
            return;
        }

        if (Platform == Platforms.Android)
        {
            OS.RequestPermissions();
            // -> OnGodotPermissionAndroid will be called and will call OnAuthorizationChanged
        }
    }
    #endregion

    #region location
    public void RequestLocation()
    {
        GeolocationPlugin?.Call("request_location");
    }

    public void StartUpdatingLocation()
    {
        GeolocationPlugin?.Call("start_updating_location");
    }

    public void StopUpdatingLocation()
    {
        GeolocationPlugin?.Call("stop_updating_location");
    }
    #endregion

    #region heading
    public void StartUpdatingHeading()
    {
        GeolocationPlugin?.Call("start_updating_heading");
    }

    public void StopUpdatingHeading()
    {
        GeolocationPlugin?.Call("stop_updating_heading");
    }

    #endregion

    #region options

    public void SetDistanceFilter(float meters)
    {
        GeolocationPlugin?.Call("set_distance_filter", meters);
    }

    public void SetDesiredAccuracy(GeolocationDesiredAccuracyConstants accuracy)
    {
        GeolocationPlugin?.Call("set_desired_accuracy", (int)accuracy);
    }

    public void SetUpdateInterval(int seconds)
    {
        if (Platform != Platforms.Android) return; // only supported on Android

        GeolocationPlugin?.Call("set_update_interval", seconds);
    }

    public void SetMaxWaitTime(int seconds)
    {
        if (Platform != Platforms.Android) return; // only supported on Android

        GeolocationPlugin?.Call("set_max_wait_time", seconds);
    }

    private void SetReturnStringCoordinates()
    {
        GeolocationPlugin?.Call("set_return_string_coordinates", true);
    }

    public void SetFailureTimeout(int seconds)
    {
        GeolocationPlugin?.Call("set_failure_timeout", seconds);
    }

    public void SetDebugLogSignal(bool send)
    {
        GeolocationPlugin?.Call("set_debug_log_signal", send);
    }

    public void SetAutoCheckLocationCapability(bool auto)
    {
        GeolocationPlugin?.Call("set_auto_check_location_capability", auto);
    }

    #endregion

    #region status
    public GeolocationAuthorizationStatus AuthorizationStatus
    {
        get => Supported ? (GeolocationAuthorizationStatus)GeolocationPlugin.Call("authorization_status") :
        GeolocationAuthorizationStatus.PERMISSION_STATUS_DENIED;
    }
    public bool AllowsFullAccuracy
    {
        get => Supported ? (bool)GeolocationPlugin.Call("allows_full_accuracy") : false;
    }

    public bool CanRequestPermissions
    {
        get => Supported ? (bool)GeolocationPlugin.Call("can_request_permissions") : false;
    }

    public bool IsUpdatingLocation
    {
        get => Supported ? (bool)GeolocationPlugin.Call("is_updating_location") : false;
    }

    public bool IsUpdatingHeading
    {
        get => Supported ? (bool)GeolocationPlugin.Call("is_updating_heading") : false;
    }

    public bool ShouldShowPermissionRequirementExplanation
    {
        get => Supported ? (bool)GeolocationPlugin.Call("should_show_permission_requirement_explanation") : false;
    }

    public bool ShouldCheckLocationCapability
    {
        get => Supported ? (bool)GeolocationPlugin.Call("should_check_location_capability") : false;
    }

    public void RequestLocationCapability()
    {
        if (Supported) GeolocationPlugin.Call("request_location_capabilty");
    }

    #endregion

    #region platform support

    public bool Supports(string methodName)
    {
        return Supported ? (bool)GeolocationPlugin.Call("supports",methodName) : false;
    }

    #endregion



    public override void _Ready()
    {
        DeterminePlatform();
        if (Platform == Platforms.iOS || Platform == Platforms.Android)
        {
            SetupNativePlugin();
        }
    }

    private void DeterminePlatform()
    {
        Platform = OS.GetName() switch
        {
            "iOS" => Platforms.iOS,
            "Android" => Platforms.Android,
            _ => Platforms.other
        };
    }

    private void SetupNativePlugin()
    {
        var pluginName = "Geolocation";

        if (Platform == Platforms.Android)
        {
            GetTree().Connect("on_request_permissions_result", this, nameof(OnGodotPermissionAndroid));
        }

        if (Engine.HasSingleton(pluginName))
        {
            Supported = true;
            GeolocationPlugin = Engine.GetSingleton(pluginName);
            SetReturnStringCoordinates(); // make sure we get the double precision values as string

            GeolocationPlugin.Connect("error", this, nameof(OnErrorSignal));
            GeolocationPlugin.Connect("log", this, nameof(OnLogSignal));
            GeolocationPlugin.Connect("location_update", this, nameof(OnLocationUpdate));
            GeolocationPlugin.Connect("authorization_changed", this, nameof(OnAuthorizationChanged));
            GeolocationPlugin.Connect("heading_update", this, nameof(OnHeadingUpdate));
            GeolocationPlugin.Connect("location_capability_result", this, nameof(OnLocationCapabilityResult));
        }
    }

    private DateTime? _lastAndroidPermissionSignal;
    // This will be called twice (coarse and fine) and we don' want to get two events, but we want to
    // get a permisson change if the user grants fine later. so we suppress one of the two events 
    private void OnGodotPermissionAndroid(string permissions, bool granted)
    {
        if (_lastAndroidPermissionSignal is null)
        {
            _lastAndroidPermissionSignal = DateTime.UtcNow;
        }
        else
        {
            TimeSpan timeDiff = DateTime.UtcNow - (DateTime)_lastAndroidPermissionSignal;
            _lastAndroidPermissionSignal = null;
            if (timeDiff.TotalMilliseconds < 500) return; // exit early if this was the second call in 500ms
        }

        if (permissions == "android.permission.ACCESS_COARSE_LOCATION" && granted)
        {
            OnAuthorizationChanged((int)GeolocationAuthorizationStatus.PERMISSION_STATUS_ALLOWED);
            return;
        }

        if (permissions == "android.permission.ACCESS_FINE_LOCATION" && granted)
        {
            OnAuthorizationChanged((int)GeolocationAuthorizationStatus.PERMISSION_STATUS_ALLOWED);
            return;
        }

        OnAuthorizationChanged((int)GeolocationAuthorizationStatus.PERMISSION_STATUS_DENIED);
    }

    #region Async Methods

    public Task<GeolocationAuthorizationStatus> RequestPermissionsAsync()
    {
        var permissionsTask = new TaskCompletionSource<GeolocationAuthorizationStatus>();

        EventHandler<GeolocationAuthorizationStatus> permissionStatusChanged = null!;

        permissionStatusChanged = ((object sender, GeolocationAuthorizationStatus authorization) =>
            {
                AuthorizationChanged -= permissionStatusChanged;
                permissionsTask.SetResult(authorization);
            });
        AuthorizationChanged += permissionStatusChanged;
        RequestPermissions();
        return permissionsTask.Task;
    }

    public Task<bool> LocationCapabilityAsync()
    {
        var locationCapabilityTask = new TaskCompletionSource<bool>();

        EventHandler<bool> locationCapabilityReceived = null!;

        locationCapabilityReceived = ((object sender, bool capable) =>
            {
                LocationCapabilityResult -= locationCapabilityReceived;
                locationCapabilityTask.SetResult(capable);
            });
        LocationCapabilityResult += locationCapabilityReceived;
        RequestLocationCapability();
        return locationCapabilityTask.Task;

    }

    // let's you await a Location object or null (if it was not possible to locate/not authorized)
    private async Task<Location?> RequestLocationAsyncInternal(bool startWatch = false)
    {
        var locationRequestTask = new TaskCompletionSource<Location?>();

        EventHandler<GeolocationErrorCodes> errorHappened = null!;
        EventHandler<Location> locationReceived = null!;

        // not supported retrun null immediately 
        if (!Supported)
        {
            OnLogSignal("AUTO unsupported");
            locationRequestTask.SetResult(null);
            //return locationRequestTask.Task; // return early
            return null;
        }

        // helper to remove Location and Error listeners
        void RemoveHandlers()
        {
            LocationUpdate -= locationReceived;
            Error -= errorHappened;
        }

        // 1. authorize
        async Task<Location?> AuthoritzeAndContinue()
        {    
            // don't authorize if already allowed
            if (AuthorizationStatus == GeolocationAuthorizationStatus.PERMISSION_STATUS_ALLOWED)
            {
                return await CheckCapabilityAndContinue();
            }

            OnLogSignal("AUTO authorize");

            var authorizationStatus = await RequestPermissionsAsync();

            if(authorizationStatus != GeolocationAuthorizationStatus.PERMISSION_STATUS_ALLOWED)
            {
                return null;
            }

            return await CheckCapabilityAndContinue();
        }

        // 2. check capability
        async Task<Location?> CheckCapabilityAndContinue()
        {
            // don't check if not necessary
            if(!ShouldCheckLocationCapability) return await RequestLocationAndListen();

            OnLogSignal("AUTO check capability");
            var capable = await LocationCapabilityAsync();
            if(!capable) return null;

            return await RequestLocationAndListen();
        }

        // 1b/2b parallel with AuthoritzeAndContinue or CheckCapabilityAndContinue
        Task<Location?> ListenForErrors()
        {
            errorHappened = ((object sender, GeolocationErrorCodes errorCode) =>
            {
                RemoveHandlers();
                OnLogSignal("AUTO error has occured: " + errorCode.ToString());
                locationRequestTask.SetResult(null);
            });
            Error += errorHappened;

            return locationRequestTask.Task;
        }

        // 3. request location
        Task<Location?> RequestLocationAndListen()
        {
            OnLogSignal("AUTO actually request location");
            locationReceived = ((object sender, Location locationData) =>
            {
                RemoveHandlers();
                locationRequestTask.SetResult(locationData);

            });
            LocationUpdate += locationReceived;

            if (startWatch)
            {
                StartUpdatingLocation();
            }
            else
            {
                RequestLocation();
            }

            return locationRequestTask.Task;
        }

        return await await Task.WhenAny(AuthoritzeAndContinue(), ListenForErrors());
    }

    public Task<Location?> RequestLocationAsync()
    {
        return RequestLocationAsyncInternal(false);
    }

    private void StopUpdatingLocationWhenNothingListens()
    {
        if (LocationUpdate != null) return;

        OnLogSignal("LocationUpdate delegate is null, stop watching!");
        StopUpdatingLocation(); // stop if no watchers
        return;
    }

    private void StopUpdatingHeadingWhenNothingListens()
    {
        if (HeadingUpdate != null) return;

        OnLogSignal("HeadingUpdate delegate is null, stop watching!");
        StopUpdatingHeading();// stop if no watchers
        return;
    }

    public async Task<LocationUpdater?> GetLocationUpdater()
    {
        return await LocationUpdater.CreateAsync(this) ;
    }

    public HeadingUpdater? GetHeadingUpdater()
    {
        return HeadingUpdater.Create(this);
    }

    public class HeadingUpdater
    {
        private Geolocation _geolocation;
        private ReusableAwaiter<Heading?> _headingAwaiter;
        public bool IsUpdating = false;

        public static HeadingUpdater? Create(Geolocation geo)
        {
            var updater = new HeadingUpdater(geo);
            var success = updater.Start();
            return success? updater:null;
        }

        public HeadingUpdater(Geolocation geo)
        {
            _geolocation = geo;
            _headingAwaiter = new ReusableAwaiter<Heading?>();
        }

        public ReusableAwaiter<Heading?> HeadingUpdateAsync()
        {
            return _headingAwaiter.Reset();
        }

        public bool Start()
        {
            if (IsUpdating) return false;
            if (!_geolocation.Supports("start_updating_heading")) return false;

            _geolocation.OnLogSignal("# begin start");
            IsUpdating = true;

            _geolocation.HeadingUpdate += SendUpdate;
            _geolocation.Error += ErrorListener;
            _geolocation.StartUpdatingHeading();
            return true;
        }

        private void SendUpdate(object sender, Heading heading)
        {
            _headingAwaiter.TrySetResult(heading);
        }

        public void Stop()
        {
            if(!IsUpdating) return;

            _geolocation.OnLogSignal("# stop and sending final null");
            IsUpdating = false;
            _geolocation.HeadingUpdate -= SendUpdate;
            _geolocation.Error -= ErrorListener;
            _geolocation.StopUpdatingHeadingWhenNothingListens();

            _headingAwaiter.TrySetResult(null); // send one last null to advance while loop
        }

        // parallel with LocationAwaiter listen for errors
        private void ErrorListener(object sender, GeolocationErrorCodes errorCode)
        {
            _geolocation.OnLogSignal("++ error has occured: " + errorCode.ToString());
            Stop();
        }
    }

    public class LocationUpdater
    {

        private Geolocation _geolocation;
        private ReusableAwaiter<Location?> _locationAwaiter;

        public bool IsUpdating = false;
        public bool HasError = false;
        public GeolocationErrorCodes LastError;

        public static async Task<LocationUpdater?> CreateAsync(Geolocation geo)
        {
            var updater = new LocationUpdater(geo);
            var success = await updater.Start();
            return success? updater:null;
        }

        public LocationUpdater(Geolocation geo)
        {
            _geolocation = geo;
            _locationAwaiter = new ReusableAwaiter<Location?>();
        }

        public ReusableAwaiter<Location?> LocationUpdateAsync()
        {
            return _locationAwaiter.Reset();
        }

        public async Task<bool> Start()
        {
            if (IsUpdating) return false;
            IsUpdating = true;
            HasError = false;

            var firstLocation = await _geolocation.RequestLocationAsyncInternal(true);

            if (firstLocation is null)
            {
                Stop();
                return false;
            }

            _locationAwaiter.TrySetResult(firstLocation);

            _geolocation.LocationUpdate += SendUpdate;
            _geolocation.Error += ErrorListener;
            return true;
        }

        private void SendUpdate(object sender, Location location)
        {
            _locationAwaiter.TrySetResult(location);
        }

        public void Stop()
        {
            if(!IsUpdating) return;
            IsUpdating = false;
            _geolocation.LocationUpdate -= SendUpdate;
            _geolocation.Error -= ErrorListener;
            _geolocation.StopUpdatingLocationWhenNothingListens();
            _locationAwaiter.TrySetResult(null); // send one last null to advance while loop
        }

        // parallel with LocationAwaiter listen for errors
        private void ErrorListener(object sender, GeolocationErrorCodes errorCode)
        {
            HasError = true;
            LastError = errorCode;
            Stop();
        }
    }

    #endregion

    #region Signal handlers

    private void OnAuthorizationChanged(int status)
    {
        var authorization = (GeolocationAuthorizationStatus)status;
        AuthorizationChanged?.Invoke(this, authorization);
    }

    private void OnLocationUpdate(Godot.Collections.Dictionary locationData)
    {
        var location = new Location(locationData);
        LocationUpdate?.Invoke(this, location);
        locationData.Dispose();
    }

    private void OnHeadingUpdate(Godot.Collections.Dictionary headingData)
    {
        var heading = new Heading(headingData);
        HeadingUpdate?.Invoke(this, heading);
        headingData.Dispose();
    }

    private void OnLocationCapabilityResult(bool capable)
    {
        LocationCapabilityResult?.Invoke(this, capable);
    }

    private void OnErrorSignal(int errorCode)
    {
        LastError = (GeolocationErrorCodes)errorCode;
        Error?.Invoke(this, LastError);
    }

    private void OnLogSignal(string message, float code = 0f)
    {
        Log?.Invoke(this, message + " (" + code + ")");
    }

    #endregion
}

public class Heading 
{
    public readonly float MagneticHeading;
    public readonly float TrueHeading;
    public readonly float HeadingAccuracy;
    public readonly int Timestamp;
    public readonly DateTime Time;

    public Heading(Godot.Collections.Dictionary headingDictionary)
    {
        MagneticHeading = (float)headingDictionary["magnetic_heading"];
        TrueHeading = (float)headingDictionary["true_heading"];
        HeadingAccuracy = (float)headingDictionary["heading_accuracy"];
        Timestamp = (int)headingDictionary["timestamp"];
        Time = DateTimeOffset.FromUnixTimeSeconds(Timestamp).LocalDateTime;
    }

    public override string ToString()
    {
        var locationString = new StringBuilder();
        locationString.Append("Magnetic Heading: ");
        locationString.Append(MagneticHeading);
        locationString.Append("\n");

        locationString.Append("True Heading: ");
        locationString.Append(TrueHeading);
        locationString.Append("\n");

        locationString.Append("Heading Accuracy: ");
        locationString.Append(HeadingAccuracy);
        locationString.Append("\n");

        locationString.Append("Timestamp: ");
        locationString.Append(Timestamp);
        locationString.Append(" ");

        locationString.Append(Time);

        return locationString.ToString();
    }

}

public class Location
{
    public readonly double Latitude;
    public readonly double Longitude;
    public readonly float Accuracy;

    public readonly float Altitude;
    public readonly float AltitudeAccuracy;

    public readonly float Course;
    public readonly float CourseAccuracy;

    public readonly float Speed; // in m/s
    public readonly float SpeedAccuracy;

    public readonly int Timestamp;

    public readonly DateTime Time;

    public float SpeedKph
    {
        get => Speed * 3.6f;
    }

    public float SpeedMph
    {
        get => Speed * 2.237f;
    }

    public Location(Godot.Collections.Dictionary locationDictionary)
    {

        Latitude = Double.Parse((string)locationDictionary["latitude_string"]);
        Longitude = Double.Parse((string)locationDictionary["longitude_string"]);

        Accuracy = (float)locationDictionary["accuracy"];

        Altitude = (float)locationDictionary["altitude"];
        AltitudeAccuracy = (float)locationDictionary["altitude_accuracy"];

        Course = (float)locationDictionary["course"];
        CourseAccuracy = (float)locationDictionary["course_accuracy"];
        // heading accuracy?

        Speed = (float)locationDictionary["speed"];
        SpeedAccuracy = (float)locationDictionary["speed_accuracy"];

        Timestamp = (int)locationDictionary["timestamp"];

        Time = DateTimeOffset.FromUnixTimeSeconds(Timestamp).LocalDateTime;
    }

    // dummy location
    public Location()
    {
        Latitude = 51.83;
        Longitude = 7.96;
        Accuracy = 4.44f;

        Altitude = 111.11f;
        AltitudeAccuracy = 1.2f;

        Course = 120.5f;
        CourseAccuracy = 12.3f;

        Speed = 0f;
        SpeedAccuracy = 0f;

        Timestamp = 123456789;

        Time = DateTimeOffset.FromUnixTimeSeconds(Timestamp).LocalDateTime;


    }

    public double DistanceToKilometers(Location otherLocation)
    {
        return CalculateDistance(Latitude, Longitude, otherLocation.Latitude, otherLocation.Longitude);
    }

    public double DistanceToMeters(Location otherLocation)
    {
        return DistanceToKilometers(otherLocation) * 1000;
    }

    private static double CalculateDistance(double lat1, double long1, double lat2, double long2)
    {
        var d2r = (Math.PI / 180.0);
        double dlong = (long2 - long1) * d2r;
        double dlat = (lat2 - lat1) * d2r;
        double a = Math.Pow(Math.Sin(dlat / 2.0), 2) + Math.Cos(lat1 * d2r) * Math.Cos(lat2 * d2r) * Math.Pow(Math.Sin(dlong / 2.0), 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double d = 6367 * c;

        return d;
    }

    public override string ToString()
    {
        var locationString = new StringBuilder();
        locationString.Append("Latitude: ");
        locationString.Append(Latitude);
        locationString.Append("\n");

        locationString.Append("Longitude: ");
        locationString.Append(Longitude);
        locationString.Append("\n");

        locationString.Append("Accuracy: ");
        locationString.Append(Accuracy);
        locationString.Append("\n");

        locationString.Append("Altitude: ");
        locationString.Append(Altitude);
        locationString.Append("m ");

        locationString.Append("(+/- ");
        locationString.Append(AltitudeAccuracy);
        locationString.Append("m)\n");

        locationString.Append("Course: ");
        locationString.Append(Course);
        locationString.Append(" ");

        locationString.Append("(+/-  ");
        locationString.Append(Course);
        locationString.Append(") \n");

        locationString.Append("Speed: ");
        locationString.Append(Speed);
        locationString.Append(" ");

        locationString.Append("(+/- ");
        locationString.Append(SpeedAccuracy);
        locationString.Append("m/s)\n");

        locationString.Append("Timestamp: ");
        locationString.Append(Timestamp);
        locationString.Append(" ");

        locationString.Append(Time);

        return locationString.ToString();

    }
}
