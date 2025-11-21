using System;
using System.ComponentModel;

namespace GigaCity_Labor3_OOP.Models.ForeignRelations
{
    public class Export : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public Country DestinationCountry { get; set; }
        public ResourceType ResourceType { get; set; }
        public double Quantity { get; set; }
        public decimal Revenue { get; set; }
        public DateTime ExportDate { get; set; }

        public Export()
        {
            Id = Guid.NewGuid().ToString();
            ExportDate = DateTime.Now;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

