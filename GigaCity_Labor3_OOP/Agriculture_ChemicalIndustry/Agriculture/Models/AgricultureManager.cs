using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agriculture_ChemicalIndustry.Agriculture.Models
{
    public class AgricultureManager
    {
        private OrganizationManager _organizationManager;
        private List<Farm> _farms;

        public IEnumerable<Farm> Farms => _farms;

        public AgricultureManager(OrganizationManager organizationManager)
        {
            _organizationManager = organizationManager;
            _farms = new List<Farm>();
        }

        public void AddFarm(Farm farm)
        {
            _farms.Add(farm);
            _organizationManager.RegisterOrganization(farm);
        }

        public void ProcessDay()
        {
            foreach (var farm in _farms)
            {
                farm.ProcessProduction();

                // Автоматически выплачиваем зарплаты, если это ферма
                if (farm.Employees.Any() && farm.Budget > 1000)
                {
                    farm.PaySalaries();
                }
            }
        }

        public decimal GetTotalAgriculturalRevenue()
        {
            return _farms.Sum(farm => farm.Revenue);
        }

        public decimal GetTotalAgriculturalCosts()
        {
            return _farms.Sum(farm => farm.Costs);
        }
    }
}
