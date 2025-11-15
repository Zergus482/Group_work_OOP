using System;
using System.Collections.Generic;
using TheFinancialSystem;

namespace GigaCity_Labor3_OOP.Models
{
    public class Human
    {
        #region Основа класса Human
        public string _name { get; set; }
        public delegate void EducationFinishedHandler(Human graduate);
        public int _age { get; set; }
        public bool IsStudying { get; private set; }
        
        public bool IsAlive { get; private set; } = true;
        //Учеба
        public string _educationLevel { get; private set; } = "None";
        public int _educationTime { get; private set; }
        public event EducationFinishedHandler OnEducationFinished;
        private static readonly Random _random = new Random();
        // Работа
        public bool IsWorking { get; set; }
        
        private static readonly string[] Names =
        {
            "James", "John", "Michael", "Robert", "David", "William", "Richard", "Joseph", "Thomas", "Charles", "Daniel",
            "Matthew", "Anthony", "Mark", "Donald", "Steven", "Andrew", "Joshua", "Kenneth", "Brian", "George", "Timothy",
            "Ronald", "Edward", "Jason", "Jeffrey", "Jacob", "Nicholas", "Eric", "Jonathan",
        };

        // Возможные продолжительности обучения
        private static readonly int[] EducationDurations = { 12, 16, 18, 20, 22, 24, 26 };

        public Human(string name = null)
        {
            _name = name != null ? name : Names[_random.Next(Names.Length)];
            _age = _random.Next(0,40);
            IsAlive = true;
            _educationLevel = "None";
            IsStudying = false;
            IsWorking = false;
        }
        
        #endregion
        
        public void Employ()
        {
            if (!IsAlive || IsStudying || IsWorking)
            {
                return; 
            }
        
            IsWorking = true;
        }
        public void Demography(Random random, List<Human> population)
        {
            if (!IsAlive) return;
            _age++;
            
            // ОБРАЗОВАНИЕ
            if (_educationLevel == "None" && _age == 6)
            {
                // Начинаем учиться в 6 лет
                StartEducation(random);
            }
            
            if (IsStudying)
            {
                ContinueEducation();
            }
            
            // Смерть
            int deathchance = _age switch
            {
                < 0 => 15,    // детская смертность
                < 5 => 5,    // маленькие дети
                < 15 => 1,   // дети/подростки
                < 30 => 3,   // молодежь
                < 50 => 15,   // взрослые
                < 70 => 65,   // пожилые
                < 85 => 80,  // старики
                _ => 99      // очень старые
            };
            
            if (random.Next(0, 100) < deathchance)
            {
                IsAlive = false;
                return;
            }
            
            // Рождение детей
            if ((random.Next(0, 100) > 85) && (_age >= 18 && _age < 60))
            {
                Human newHuman = new Human();
                population.Add(newHuman);
            }
        }
        public void Dismiss()
        {
            if (IsWorking)
            {
                IsWorking = false;
            }
        }
        
        private void StartEducation(Random random)
        {
            IsStudying = true;
            _educationTime = EducationDurations[random.Next(EducationDurations.Length)];
            _educationLevel = "School";
        }
        
        private void ContinueEducation()
        {
            // Обновляем уровень образования в зависимости от возраста
            string newLevel = GetEducationLevelByAge();
            if (newLevel != _educationLevel)
            {
                _educationLevel = newLevel;
            }
            
            // Закончит обучение? 
            if (_age >= _educationTime)
            {
                IsStudying = false;
                OnEducationFinished?.Invoke(this);
                
            }
        }
        

        
        private string GetEducationLevelByAge()
        {
            return _age switch
            {
                < 12 => "without education",
                < 16 => "School",
                < 18 => "High School",
                < 20 => "Bachelor",
                < 22 => "Bachelor +",
                < 24 => "Master",
                < 26 => "Master +",
                >= 26 => "PhD"
            };
        }
    }
}