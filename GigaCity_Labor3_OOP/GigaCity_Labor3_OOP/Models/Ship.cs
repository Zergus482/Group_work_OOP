using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GigaCity_Labor3_OOP.Models
{
    public class Ship : INotifyPropertyChanged
    {
        private double _x;
        private double _y;
        private bool _isActive;

        public Ship(int fromPortX, int fromPortY, int toPortX, int toPortY)
        {
            FromPortX = fromPortX;
            FromPortY = fromPortY;
            ToPortX = toPortX;
            ToPortY = toPortY;
            
            // Начинаем с позиции первого порта
            X = fromPortX * 15.0 + 7.5; // 15 - размер ячейки, 7.5 - центр
            Y = fromPortY * 15.0 + 7.5;
            
            IsActive = true;
            
            // Вычисляем направление
            double dx = toPortX - fromPortX;
            double dy = toPortY - fromPortY;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            
            if (distance > 0)
            {
                // Скорость корабля немного медленнее самолета
                double speed = 0.8; // пикселей за обновление
                VelocityX = (dx / distance) * speed;
                VelocityY = (dy / distance) * speed;
            }
        }

        public int FromPortX { get; }
        public int FromPortY { get; }
        public int ToPortX { get; }
        public int ToPortY { get; }

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
            double targetX = ToPortX * 15.0 + 7.5;
            double targetY = ToPortY * 15.0 + 7.5;
            
            double distanceToTarget = Math.Sqrt(Math.Pow(X - targetX, 2) + Math.Pow(Y - targetY, 2));
            
            // Увеличиваем радиус достижения цели для более надежного определения прибытия
            if (distanceToTarget < 5.0) // Достигли цели
            {
                IsActive = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }
    }
}

