using GigaCity_Labor3_OOP.Models;
using System.Collections.Generic;
using System;

namespace GigaCity_Labor3_OOP.Models
{
    public class PopulationManager
    {
        public List<Human> Population { get; private set; }
        public int PopulationCount => Population.Count;
        public EducationalInstitution University { get; private set; }

        private readonly Random _random = new Random();

        public PopulationManager()
        {
            Population = new List<Human>();
            // Инициализируем его в конструкторе
            University = new EducationalInstitution();

            // Создаем начальное население
            for (int i = 0; i < 100; i++) // Начнем с 100 человек
            {
                Population.Add(new Human());
            }
        }

        /// <summary>
        /// Выполняет один год симуляции.
        /// </summary>
        public void SimulateYear()
        {
            // 1. Обновляем демографию для каждого человека
            // Используем .ToList(), чтобы безопасно изменять коллекцию Population во время перебора
            foreach (var human in Population.ToList())
            {
                human.Demography(_random, Population);
            }

            // 2. Пытаемся зачислить новых студентов
            foreach (var human in Population)
            {
                // Зачисляем тех, кто начал учиться и еще не зачислен
                if (human.IsAlive && human.IsStudying)
                {
                    University.EnrollStudent(human);
                }
            }
            foreach (var human in Population)
            {
                if (human.IsAlive && !human.IsStudying && !human.IsWorking)
                {
                    University.TryHireGraduate(human);
                }
            }

            
            University.UpdateGraduates();
            University.UpdateEmployees();
        }

        /// <summary>
        /// Добавляет нового человека в население.
        /// </summary>
        public void AddHuman(Human human)
        {
            if (human != null)
            {
                Population.Add(human);
            }
        }
    }
}