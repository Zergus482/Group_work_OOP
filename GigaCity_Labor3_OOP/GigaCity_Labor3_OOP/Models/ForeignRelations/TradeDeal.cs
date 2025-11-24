using System;
using System.ComponentModel;

namespace GigaCity_Labor3_OOP.Models.ForeignRelations
{
    public class TradeDeal : INotifyPropertyChanged
    {
        private bool _isActive;

        public string Id { get; set; }
        public Country PartnerCountry { get; set; }
        public ResourceType ImportResource { get; set; }
        public ResourceType ExportResource { get; set; }
        public double ImportQuantity { get; set; }
        public double ExportQuantity { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged(nameof(IsActive));
            }
        }

        public TradeDeal()
        {
            Id = Guid.NewGuid().ToString();
            StartDate = DateTime.Now;
            IsActive = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

