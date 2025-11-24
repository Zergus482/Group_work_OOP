using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using GigaCity_Labor3_OOP.Models.Economy;

namespace GigaCity_Labor3_OOP.ViewModels.Economy
{
    public class ResourceStatViewModel : INotifyPropertyChanged
    {
        private readonly Queue<double> _history = new();
        private double _totalStock;
        private double _trend;
        private readonly int _historyLimit;

        public CoreResource Resource { get; }
        public string DisplayName { get; }
        public SolidColorBrush Accent { get; }

        public ResourceStatViewModel(CoreResource resource, string displayName, Color accent, int historyLimit = 40)
        {
            Resource = resource;
            DisplayName = displayName;
            Accent = new SolidColorBrush(accent);
            _historyLimit = historyLimit;
            HistoryPoints = new PointCollection();
        }

        public double TotalStock
        {
            get => _totalStock;
            private set
            {
                if (Math.Abs(_totalStock - value) > 0.001)
                {
                    _totalStock = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalStockText));
                }
            }
        }

        public string TotalStockText => $"{TotalStock:0.0} т";

        public double Trend
        {
            get => _trend;
            private set
            {
                if (Math.Abs(_trend - value) > 0.001)
                {
                    _trend = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TrendText));
                }
            }
        }

        public string TrendText => Trend switch
        {
            > 0.05 => $"▲ {Trend:0.0}/ч",
            < -0.05 => $"▼ {Trend:0.0}/ч",
            _ => "—"
        };

        public PointCollection HistoryPoints { get; }

        public void PushSample(double value)
        {
            _history.Enqueue(value);
            while (_history.Count > _historyLimit)
            {
                _history.Dequeue();
            }

            TotalStock = value;
            if (_history.Count >= 2)
            {
                var arr = _history.ToArray();
                Trend = arr[^1] - arr[Math.Max(0, arr.Length - 3)];
            }

            RebuildPoints();
        }

        private void RebuildPoints()
        {
            HistoryPoints.Clear();
            if (_history.Count == 0)
            {
                return;
            }

            var max = Math.Max(1.0, _history.Max());
            var samples = _history.ToArray();
            for (int i = 0; i < samples.Length; i++)
            {
                double x = 2.0 * i;
                double y = 30 - (samples[i] / max) * 30;
                HistoryPoints.Add(new System.Windows.Point(x, double.IsNaN(y) ? 0 : y));
            }
            OnPropertyChanged(nameof(HistoryPoints));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

