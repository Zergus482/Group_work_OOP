using System.ComponentModel;

namespace GigaCity_Labor3_OOP.Models.ForeignRelations
{
    public class Country : INotifyPropertyChanged
    {
        private int _relationsLevel;

        public string Name { get; set; }
        public string Code { get; set; }

        public int RelationsLevel
        {
            get => _relationsLevel;
            set
            {
                _relationsLevel = value;
                OnPropertyChanged(nameof(RelationsLevel));
                OnPropertyChanged(nameof(RelationsStatus));
            }
        }

        public string RelationsStatus
        {
            get
            {
                return RelationsLevel switch
                {
                    >= 80 => "Отличные",
                    >= 60 => "Хорошие",
                    >= 40 => "Нейтральные",
                    >= 20 => "Напряженные",
                    _ => "Враждебные"
                };
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

