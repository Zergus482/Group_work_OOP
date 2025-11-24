using System;
using System.ComponentModel;

namespace GigaCity_Labor3_OOP.Models.ForeignRelations
{
    public class Import : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public Country SourceCountry { get; set; }
        public ResourceType ResourceType { get; set; }
        public double Quantity { get; set; }
        public decimal Cost { get; set; }
        public DateTime ImportDate { get; set; }

        public Import()
        {
            Id = Guid.NewGuid().ToString();
            ImportDate = DateTime.Now;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

