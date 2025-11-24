using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GigaCity_Labor3_OOP.Models
{
    public class Plane : INotifyPropertyChanged
    {
        private double _x;
        private double _y;
        private bool _isActive;

        public Plane(int fromAirportX, int fromAirportY, int toAirportX, int toAirportY)
        {
            FromAirportX = fromAirportX;
            FromAirportY = fromAirportY;
            ToAirportX = toAirportX;
            ToAirportY = toAirportY;
            
            // Начинаем с позиции первого аэропорта
            X = fromAirportX * 15.0 + 7.5; // 15 - размер ячейки, 7.5 - центр
            Y = fromAirportY * 15.0 + 7.5;
            
            IsActive = true;
            
            // Вычисляем направление
            double dx = toAirportX - fromAirportX;
            double dy = toAirportY - fromAirportY;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            if (distance > 0)
            {
                // Самолеты летают намного быстрее машин (3-4 раза быстрее)
                double speed = 4.0; // пикселей за обновление (обновление каждые 30мс) - увеличено с 1.2
                VelocityX = (dx / distance) * speed;
                VelocityY = (dy / distance) * speed;
            }
        }

        public int FromAirportX { get; }
        public int FromAirportY { get; }
        public int ToAirportX { get; }
        public int ToAirportY { get; }

        public double X
        {
            get => _x;
            set
            {
                _x = value;
                OnPropertyChanged();
            }
        }

        public double Y
        {
            get => _y;
            set
            {
                _y = value;
                OnPropertyChanged();
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }

        public double VelocityX { get; }
        public double VelocityY { get; }

        public void Update()
        {
            if (!IsActive) return;

            X += VelocityX;
            Y += VelocityY;

            // Проверяем, достигли ли мы цели
            double targetX = ToAirportX * 15.0 + 7.5;
            double targetY = ToAirportY * 15.0 + 7.5;
            
            double distanceToTarget = Math.Sqrt(Math.Pow(X - targetX, 2) + Math.Pow(Y - targetY, 2));
            
            // Увеличиваем радиус достижения цели для более надежного определения прибытия
            if (distanceToTarget < 5.0) // Достигли цели
            {
                IsActive = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
