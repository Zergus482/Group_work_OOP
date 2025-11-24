using System;
using System.ComponentModel;

namespace GigaCity_Labor3_OOP.Models.ForeignRelations
{
    public class Immigrant : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public Country CountryOfOrigin { get; set; }
        public ImmigrationReason Reason { get; set; }
        public DateTime ArrivalDate { get; set; }

        public Immigrant()
        {
            ArrivalDate = DateTime.Now;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

