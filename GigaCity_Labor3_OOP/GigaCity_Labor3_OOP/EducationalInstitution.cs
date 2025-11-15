// EducationalInstitution.cs

using GigaCity_Labor3_OOP.Models;
using System.Collections.Generic;
using System;

namespace GigaCity_Labor3_OOP
{
    public class EducationalInstitution
    {
        private List<Human> _allEmployees = new List<Human>();
        public int TotalEmployeeCapacity { get; set; } = 100;
        
        public bool HireEmployee(Human person)
        {
            // Проверяем условия
            if (_allEmployees.Count >= TotalEmployeeCapacity || 
                !person.IsAlive || 
                person.IsStudying || 
                person.IsWorking)
            {
                return false; // Не нанимаем
            }

            // Нанимаем
            person.Employ();
            _allEmployees.Add(person);
            return true;
        }
        public bool TryHireGraduate(Human graduate)
        {
            return HireEmployee(graduate);
        }
        public void FireEmployee(Human employee)
        {
            if (_allEmployees.Remove(employee))
            {
                employee.Dismiss();
            }
        }
        public void UpdateEmployees()
        {
            _allEmployees.RemoveAll(employee => !employee.IsAlive);
        }

        /// <summary>
        /// Получить статистику по сотрудникам
        /// </summary>
        public int GetEmployeeCount() => _allEmployees.Count;
        public int GetFreeEmployeeSlots() => TotalEmployeeCapacity - _allEmployees.Count;

        public IReadOnlyList<Human> GetAllEmployees() => _allEmployees;
        
        
        
        private static readonly string[] InstitutionalTypes =
        {
            "School", "College", "University"
        };
        
        // Списки студентов для каждого уровня
        private List<Human> SchoolStudentsList = new List<Human>();
        private List<Human> CollegeStudentsList = new List<Human>(); 
        private List<Human> UniversityStudentsList = new List<Human>();

        // Вместимость учебных заведений
        public int SchoolCapacity { get; set; } = 3000;
        public int CollegeCapacity { get; set; } = 2000;
        public int UniversityCapacity { get; set; } = 1000;
        
        /// <summary>
        /// Зачисляет студента в соответствующее учебное заведение.
        /// </summary>
        public void EnrollStudent(Human human)
        {
            // 1. Школа и старшая школа
            if ((human._educationLevel == "School" || human._educationLevel == "High School") && SchoolStudentsList.Count < SchoolCapacity)
            {
                SchoolStudentsList.Add(human);
            }
            // 2. Колледж (Бакалавр и Бакалавр+)
            else if ((human._educationLevel == "Bachelor" || human._educationLevel == "Bachelor +") && CollegeStudentsList.Count < CollegeCapacity)
            {
                CollegeStudentsList.Add(human);
            }
            // 3. Университет (Магистр, Магистр+ и Аспирантура PhD)
            else if ((human._educationLevel == "Master" || human._educationLevel == "Master +" || human._educationLevel == "PhD") && UniversityStudentsList.Count < UniversityCapacity)
            {
                UniversityStudentsList.Add(human);
            }
        }
        
        /// <summary>
        /// Удаляет из списков всех студентов, которые закончили обучение (IsStudying == false).
        /// </summary>
        public void UpdateGraduates()
        {
            SchoolStudentsList.RemoveAll(student => !student.IsStudying);
            CollegeStudentsList.RemoveAll(student => !student.IsStudying);
            UniversityStudentsList.RemoveAll(student => !student.IsStudying);
        }


        // Предоставляем доступ к спискам для чтения статистики
        public IReadOnlyList<Human> GetSchoolStudents() => SchoolStudentsList;
        public IReadOnlyList<Human> GetCollegeStudents() => CollegeStudentsList;
        public IReadOnlyList<Human> GetUniversityStudents() => UniversityStudentsList;
    }
    
}