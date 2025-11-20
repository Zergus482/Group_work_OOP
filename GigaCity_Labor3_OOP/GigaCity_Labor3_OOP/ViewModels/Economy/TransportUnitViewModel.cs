using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using GigaCity_Labor3_OOP.Models.Economy;

namespace GigaCity_Labor3_OOP.ViewModels.Economy
{
    public class TransportUnitViewModel : INotifyPropertyChanged
    {
        private double _progress;

        private readonly IReadOnlyList<Point> _pathPoints;
        private readonly double[] _segmentLengths;
        private readonly double _pathLength;

        public Guid Id { get; } = Guid.NewGuid();
        public TransportMode Mode { get; }
        public ResourceFlowViewModel Flow { get; }
        public double Speed { get; }
        public double Payload { get; }

        public TransportUnitViewModel(ResourceFlowViewModel flow, TransportMode mode, double payload)
        {
            Flow = flow;
            Mode = mode;
            Payload = payload;

            _pathPoints = flow.RoadPath != null && flow.RoadPath.Count >= 2
                ? flow.RoadPath
                : new List<Point> { flow.Source.CenterPixel, flow.Target.CenterPixel };

            _segmentLengths = BuildSegmentLengths(_pathPoints);
            _pathLength = Math.Max(ComputeTotalLength(_segmentLengths), 100);

            // Уменьшена скорость для снижения нагрузки
            Speed = mode switch
            {
                TransportMode.Truck => 40 / _pathLength, // Было 80
                TransportMode.Train => 55 / _pathLength, // Было 110
                _ => 35 / _pathLength // Было 70
            };
            _progress = 0;
        }

        public double Progress
        {
            get => _progress;
            private set
            {
                var clamped = Math.Clamp(value, 0, 1.2);
                if (Math.Abs(_progress - clamped) > 0.0001)
                {
                    _progress = clamped;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanvasLeft));
                    OnPropertyChanged(nameof(CanvasTop));
                    OnPropertyChanged(nameof(Rotation));
                }
            }
        }

        public void Advance()
        {
            Progress += Speed;
        }

        public bool HasArrived => Progress >= 1;

        public double CanvasLeft => GetPointAtProgress(Progress).X;

        public double CanvasTop => GetPointAtProgress(Progress).Y;

        public double Rotation
        {
            get
            {
                var point = GetPointAtProgress(Progress);
                var lookAhead = GetPointAtProgress(Math.Min(Progress + 0.02, 1));
                var angle = Math.Atan2(lookAhead.Y - point.Y, lookAhead.X - point.X) * 180 / Math.PI;
                return angle;
            }
        }

        public Brush BodyBrush => Mode switch
        {
            TransportMode.Truck => new SolidColorBrush(Color.FromRgb(230, 140, 70)),
            TransportMode.Train => new SolidColorBrush(Color.FromRgb(90, 160, 220)),
            _ => Brushes.LightGray
        };

        public double CarrierLength => Mode == TransportMode.Truck ? 14 : 24;
        public double CarrierHeight => Mode == TransportMode.Truck ? 6 : 8;

        private static double[] BuildSegmentLengths(IReadOnlyList<Point> path)
        {
            if (path.Count < 2)
            {
                return Array.Empty<double>();
            }

            var lengths = new double[path.Count - 1];
            for (int i = 0; i < lengths.Length; i++)
            {
                var a = path[i];
                var b = path[i + 1];
                lengths[i] = Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
            }
            return lengths;
        }

        private static double ComputeTotalLength(double[] lengths)
        {
            double total = 0;
            foreach (var len in lengths)
            {
                total += len;
            }
            return Math.Max(total, 1);
        }

        private Point GetPointAtProgress(double progress)
        {
            if (_pathPoints.Count == 0)
            {
                return Flow.Source.CenterPixel;
            }

            if (_pathPoints.Count == 1 || _segmentLengths.Length == 0)
            {
                return _pathPoints[^1];
            }

            var targetDistance = Math.Clamp(progress, 0, 1) * _pathLength;
            var remaining = targetDistance;
            for (int i = 0; i < _segmentLengths.Length; i++)
            {
                var segment = _segmentLengths[i];
                var start = _pathPoints[i];
                var end = _pathPoints[i + 1];

                if (segment <= 0)
                {
                    continue;
                }

                if (remaining <= segment)
                {
                    var ratio = remaining / segment;
                    return new Point(
                        start.X + (end.X - start.X) * ratio,
                        start.Y + (end.Y - start.Y) * ratio);
                }

                remaining -= segment;
            }

            return _pathPoints[^1];
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

