<a href="https://skyhop.org"><img src="https://skyhop.org/assets/images/skyhop.png" width=200 alt="skyhop logo" /></a>

----

# Skyhop.SpatialMap

This library contains the `SpatialMap` object, which is meant to map arbitrary objects onto a 2 dimensional X/Y grid. The goal is to be able to quickly retrieve elements based on their location on the grid, and this object is optimized exactly for that, and that only.

## Usage

You can clone the code, or [grab it from NuGet](https://www.nuget.org/packages/Skyhop.SpatialMap).

```csharp
 private readonly SpatialMap<PositionUpdate> _map = new SpatialMap<PositionUpdate>(
    q => Math.Cos(Math.PI / 180 * q.Location.Y) * 111 * q.Location.X,
    q => q.Location.Y * 111);
```

This example maps a coordinate system to kilometers from the Greenwich mean line (X) and kilometers from the equator (Y). The two lambda accessors define how to access the X and Y values from the generic type, in this example one of [`PositionUpdate`](https://github.com/skyhop/FlightAnalysis/blob/master/Skyhop.FlightAnalysis/Models/PositionUpdate.cs).

Insertions and removals happen as follows;

```csharp
var positionUpdate = new PositionUpdate();
_map.Add(positionUpdate);
_map.Remove(positionUpdate);
```

When the object is populated nearby points can be queried as follows;

```csharp
var nearbyPositions = _map.Nearby(new PositionUpdate(null, DateTime.MinValue, coordinate.Y, coordinate.X), distance);
```

*Ps. I'll add an method with which values can be queried by arbitrary X/Y coordinates somewhere in the future.*

## Inner workings

This object is based on the `SortedList` as provided by the framework itself. A custom type has been which implements this class, and adds some functionality through the use of reflection to speed things up. Two of these custom `SortedList<T>` types are maintained where `T` is actualy a `List<T>` in order to allow duplicate values for a single key.

## Blog posts

I have written some blog posts about this object before;

- [High performance 2D radius search](https://corstianboerman.com/2020-07-23/high-performance-2d-radius-search.html) - A post about the implementation of this object, which include some benchmarks.
- [Improving the spatial map object](https://corstianboerman.com/2020-10-02/improving-the-spatial-map-object.html) - A blog post about implementing the `SortedList<List<T>>` in order to support duplicate key values.