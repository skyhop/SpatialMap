using System;
using System.Collections;
using System.Collections.Generic;

namespace Skyhop.SpatialMap
{
    /*
     * The spatial map should be a type easy to use to find nearby data.
     * 
     * Given all data is ordered both based on X and Y axis, it should be
     * fairly simple to retrieve the closest points based on O(log N) performance
     * if run on a single core.
     * 
     * The goals are to;
     * 
     * - Dynamically add data
     * - Dynamically remove data
     * - Find nearby elements based on a maximum distance
     */

    public class SpatialMap<T> : IEnumerable<T>, IEnumerator<T>
    {
        private readonly object _mutationLock = new object();
        private readonly Func<T, double> _xAccessor;
        private readonly Func<T, double> _yAccessor;

        public SpatialMap(
            Func<T, double> xAccessor,
            Func<T, double> yAccessor)
        {
            _xAccessor = xAccessor;
            _yAccessor = yAccessor;
        }

        private CustomSortedList<double, List<T>> _x { get; } = new CustomSortedList<double, List<T>>();
        private CustomSortedList<double, List<T>> _y { get; } = new CustomSortedList<double, List<T>>();

        private int _xIndex = 0;
        private int _subXIndex = -1;
        private bool disposedValue;

        public T Current => _x.GetByIndex(_xIndex)[_subXIndex];
        object IEnumerator.Current => Current;

        public void Add(T element)
        {
            lock (_mutationLock)
            {
                var x = _xAccessor(element);
                var xBucket = _x.IndexOfKey(x);

                if (xBucket >= 0)
                {
                    _x.GetByIndex(xBucket).Add(element);
                }
                else
                {
                    _x.TryAdd(x, new List<T> { element });
                }

                var y = _yAccessor(element);
                var yBucket = _y.IndexOfKey(y);

                if (yBucket >= 0)
                {
                    _y.GetByIndex(yBucket).Add(element);
                }
                else
                {
                    _y.TryAdd(y, new List<T> { element });
                }
            }
        }

        public void Remove(T element)
        {
            if (element == null) return;

            lock (_mutationLock)
            {
                var x = _xAccessor(element);
                var xBucket = _x.IndexOfKey(x);

                if (xBucket >= 0 && _x.GetByIndex(xBucket).Count > 1)
                {
                    _x.GetByIndex(xBucket)
                        .Remove(element);
                }
                else
                {
                    _x.Remove(x);
                }

                var y = _yAccessor(element);
                var yBucket = _y.IndexOfKey(y);

                if (yBucket >= 0 && _y.GetByIndex(yBucket).Count > 1)
                {
                    _y.GetByIndex(yBucket)
                        .Remove(element);
                }
                else
                {
                    _y.Remove(y);
                }
            }
        }

        public T Nearest(T element)
        {
            var x = _xAccessor(element);
            var y = _yAccessor(element);

            T nearest = default;

            var approxX = _x.RoughIndexOfKey(x);
            var approxY = _y.RoughIndexOfKey(y);

            var lowerX = approxX;
            var upperX = approxX;

            var lowerY = approxY;
            var upperY = approxY;

            var hashSet = new HashSet<T>();

            var minDistance = double.MaxValue;

            for (var i = 0;
                ((_xAccessor(_x.GetByIndex(upperX)[0]) - x) < minDistance)
                   || ((x - _xAccessor(_x.GetByIndex(lowerX)[0])) < minDistance);
                i++)
            {
                if (upperX < _x.Count)
                {
                    var list = _x.GetByIndex(upperX);

                    foreach (var el in list)
                    {
                        if (hashSet.Add(el) && Distance(
                            x, y,
                            _xAccessor(el),
                            _yAccessor(el)) < minDistance)
                        {
                            nearest = el;
                            minDistance = Distance(
                            x, y,
                            _xAccessor(el),
                            _yAccessor(el));
                        }
                    }

                    upperX = approxX + i;
                }

                if (lowerX >= 0)
                {
                    var list = _x.GetByIndex(lowerX);

                    foreach (var el in list)
                    {
                        if (hashSet.Add(el) && Distance(
                            x, y,
                            _xAccessor(el),
                            _yAccessor(el)) < minDistance)
                        {
                            nearest = el;
                            minDistance = Distance(
                                x, y,
                                _xAccessor(el),
                                _yAccessor(el));
                        }
                    }

                    lowerX = approxX - i;
                }
            }

            for (var i = 0;
                ((_yAccessor(_y.GetByIndex(upperY)[0]) - y) < minDistance)
                   || ((y - _yAccessor(_y.GetByIndex(lowerY)[0])) < minDistance);
                i++)
            {
                if (upperY < _y.Count)
                {
                    var list = _y.GetByIndex(upperY);

                    foreach (var el in list)
                    {
                        if (hashSet.Add(el) && Distance(
                            x, y,
                            _xAccessor(el),
                            _yAccessor(el)) < minDistance)
                        {
                            nearest = el;
                            minDistance = Distance(
                            x, y,
                            _xAccessor(el),
                            _yAccessor(el));
                        }
                    }

                    upperY = approxY + i;
                }

                if (lowerY >= 0)
                {
                    var list = _y.GetByIndex(lowerY);

                    foreach (var el in list)
                    {
                        if (hashSet.Add(el) && Distance(
                            x, y,
                            _xAccessor(el),
                            _yAccessor(el)) < minDistance)
                        {
                            nearest = el;
                            minDistance = Distance(
                                x, y,
                                _xAccessor(el),
                                _yAccessor(el));
                        }
                    }

                    lowerY = approxY - i;
                }
            }

            return nearest;
        }

        public static double Distance(double x1, double y1, double x2, double y2)
        {
            var x = x2 - x1;
            var y = y2 - y1;

            return Math.Sqrt((x * x) + (y * y));
        }

        public IEnumerable<T> Nearby(T element, double distance)
        {
            var x = _xAccessor(element);
            var y = _yAccessor(element);

            return Nearby(x, y, distance);
        }

        public IEnumerable<T> Nearby(double x, double y, double distance)
        {
            // Compensating the inner distance like this reduces the potential
            // number of potential matches by 10% in an evenly distributed scatter
            var innerDistance = Math.Sqrt(Math.Pow(distance, 2) / 2);

            var lowerXIndex = _x.RoughIndexOfKey(x - innerDistance);
            var upperXIndex = _x.RoughIndexOfKey(x + innerDistance);
            var lowerYIndex = _y.RoughIndexOfKey(y - innerDistance);
            var upperYIndex = _y.RoughIndexOfKey(y + innerDistance);

            var hashSet = new HashSet<T>();

            for (var i = lowerXIndex; i < upperXIndex; i++)
            {
                var list = _x.GetByIndex(i);

                foreach (var el in list)
                {
                    if (hashSet.Add(el)
                        && Distance(
                        x, y,
                        _xAccessor(el),
                        _yAccessor(el)) < distance)
                    {
                        yield return el;
                    }
                }
            }

            for (var i = lowerYIndex; i < upperYIndex; i++)
            {
                var list = _y.GetByIndex(i);

                foreach (var el in list)
                {
                    if (hashSet.Add(el)
                        && Distance(
                            x, y,
                            _xAccessor(el),
                            _yAccessor(el)) < distance)
                    {
                        yield return el;
                    }
                }
            }
        }

        public IEnumerator<T> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;

        public bool MoveNext()
        {
            _subXIndex++;
            if (_x.GetByIndex(_xIndex).Count == _subXIndex)
            {
                _xIndex++;
                _subXIndex = 0;
            }

            if (_x.Count == _xIndex) return false;

            return true;
        }

        public void Reset()
        {
            _xIndex = 0;
            _subXIndex = -1;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
