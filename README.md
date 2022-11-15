# Geolocation Plugin C# API Wrapper

C# Wrapper Class for easier usage of the Godot Geolocation Plugin for Android (<https://github.com/WolfBearGames/Godot-GeolocationPlugin-Android>) and iOS (<https://github.com/WolfBearGames/Godot-GeolocationPlugin-iOS>)

## Usage Example (Example App)

<https://github.com/WolfBearGames/GeolocationTestApp>

## Install Wrapper

1. Copy `Geolocation.cs` and `ReuasableAwaiter.cs` to your project
2. Add `Geolocation.cs` to Project > AutoLoad as "Geolocation"

## Initialization

In your Class define a field for the Wrapper:

```csharp
public Geolocation GeolocationAPI = null!;
```

In the `_Ready` method:

```csharp
GeolocationAPI = GetNode<Geolocation>("/root/Geolocation");

if (GeolocationAPI is null)
    {
        // Wrapper not registered as AutoLoad
        return;
    }

if (GeolocationAPI.Supported)
{
    // Geolocation Plugin is supported

    // Geolocation Settings
    GeolocationAPI.SetDebugLogSignal(true);
    GeolocationAPI.SetFailureTimeout(30);
    //GeolocationAPI.SetAutoCheckLocationCapability(true);
}
else
{
    // Geolocation Plugin not supported
}
```

## API

### Methods

All plugin API methods (see Geolocation Plugin Readme) are supported, but with C# naming (request_permission -> RequestPermission).
Locations will be returned as a Location-Class Object with some additional methods (e.g. DistanceToMeters(Location otherLocation))

#### Convenience Methods

The easiest way to use the Geolocation plugin is to use the convenience methods the C# wrapper provides:

##### Request Permission

```csharp
var authorization = await GeolocationAPI.RequestPermissionsAsync();
```

##### Check Location Capability

Are location services available and enabled?

```csharp
bool capable = await GeolocationAPI.LocationCapabilityAsync();
```

##### Request current Location

```csharp
var aLocation = await GeolocationAPI.RequestLocationAsync();

 if (aLocation != null)
    {
        // use location
    }
    else
    {
        // no location found. get error
        var error = GeolocationAPI.LastError;
    }
```

##### Watch Location

```csharp
// create and initialize updater
var locationUpdater = await GeolocationAPI.GetLocationUpdater();

if (locationUpdater is null)
    {
        var error = GeolocationAPI.LastError;
        return;
    }

Location? location;
while ((location = await locationUpdater.LocationUpdateAsync()) != null)
{
    // use location
    // code will repeat this async loop until you call locationUpdater.Stop() or an error occurs
    //...
}

// we exited the loop ther because of Stop() or an error
if(locationUpdater.HasError)
{   
    // get the error
    var error =  locationUpdater.LastError;
}
```

To stop location updates by calling from somwhere else (you need a reference to `locationUpdater`):

```csharp
locationUpdater.Stop();
```

##### Watch heading (iOS only!)

```csharp
var headingUpdater = GeolocationAPI.GetHeadingUpdater();
if (headingUpdater is null)
{
    Log("## headingUpdater null! don't loop");
    return;
}
Log("## entering new while loop");
Heading? heading;
while ((heading = await headingUpdater.HeadingUpdateAsync()) != null)
{
    LogHeading(heading);
}
```

To stop heading updates by calling from somwhere else (you need a reference to `headingUpdater`):

```csharp
headingUpdater.Stop();
```

## License

Copyright 2022 Andreas Ritter (www.wolfbeargames.de)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
