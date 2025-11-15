using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CitySimulation.Models.Resources
{
    public class ResourceStorage
    {
        public ObservableCollection<ResourceItem> Storage { get; private set; }
        public double Capacity { get; set; }

        public ResourceStorage(double capacity = 100000)
        {
            Storage = new ObservableCollection<ResourceItem>();
            Capacity = capacity;
            InitializeStorage();
        }

        private void InitializeStorage()
        {
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                Storage.Add(new ResourceItem { Key = type, Value = 0 });
            }
        }

        public bool AddResource(ResourceType type, double amount)
        {
            var currentTotal = Storage.Sum(x => x.Value);
            if (currentTotal + amount > Capacity) return false;

            var existing = Storage.FirstOrDefault(x => x.Key == type);
            if (existing != null)
            {
                Storage.Remove(existing);
                Storage.Add(new ResourceItem { Key = type, Value = existing.Value + amount });
            }
            return true;
        }

        public double GetTotalStored() => Storage.Sum(x => x.Value);
    }

    public class ResourceItem
    {
        public ResourceType Key { get; set; }
        public double Value { get; set; }

        // Добавляем свойство для русского названия
        public string KeyRussian => Key.ToRussianString();
    }
}