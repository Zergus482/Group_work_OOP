using System;
using System.ComponentModel;

namespace GigaCity_Labor3_OOP.Models.EmergencyService
{
    public class Patient : INotifyPropertyChanged
    {
        private double _treatmentProgress;
        private bool _isDischarged;

        public string Id { get; set; }
        public DateTime AdmissionTime { get; set; }
        public double TreatmentDuration { get; set; } = 10.0; // Время лечения в секундах
        public int RoomNumber { get; set; }

        public double TreatmentProgress
        {
            get => _treatmentProgress;
            set
            {
                _treatmentProgress = value;
                OnPropertyChanged(nameof(TreatmentProgress));
                OnPropertyChanged(nameof(IsReadyForDischarge));
            }
        }

        public bool IsReadyForDischarge => TreatmentProgress >= 100.0;

        public bool IsDischarged
        {
            get => _isDischarged;
            set
            {
                _isDischarged = value;
                OnPropertyChanged(nameof(IsDischarged));
            }
        }

        public Patient()
        {
            Id = Guid.NewGuid().ToString();
            AdmissionTime = DateTime.Now;
            TreatmentProgress = 0.0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

