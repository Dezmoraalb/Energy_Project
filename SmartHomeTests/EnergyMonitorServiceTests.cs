﻿using Energy_Project.Models;
using Energy_Project.Services;
using Energy_Project.Services.Interfaces;
using Moq;

namespace SmartHomeTests
{
    public class EnergyMonitorServiceTests
    {
        private const double DailyLimitKwh = 2.0;
        private const string OverloadWord = "Overload";

        private readonly Mock<IDeviceRepository> _deviceRepo = new();
        private readonly Mock<IEnergyPlanRepository> _planRepo = new();
        private readonly Mock<INotificationService> _notify = new();

        private EnergyMonitorService CreateService()
        {
            return new EnergyMonitorService(_deviceRepo.Object, _planRepo.Object, _notify.Object);
        }

        /// <summary>
        /// Перевіряє розрахунок споживання: сума потужностей увімкнених пристроїв у ватах,
        /// поділена на 1000, тобто результат у кВт.
        /// </summary>
        [Theory]
        [InlineData(1000, 500, 1.5)]
        [InlineData(200, 300, 0.5)]
        [InlineData(0, 0, 0)]
        public void CalculateCurrentUsageKwh_ActiveDevices_ReturnsSumInKwh(double firstWatts, double secondWatts, double expectedKwh)
        {
            // Arrange
            _deviceRepo.Setup(r => r.GetAll()).Returns(new List<Device>
            {
                new Device { Id = 1, Name = "TV", PowerUsageWatts = firstWatts, IsOn = true },
                new Device { Id = 2, Name = "Fridge", PowerUsageWatts = secondWatts, IsOn = true }
            });
            var service = CreateService();

            // Act
            var actual = service.CalculateCurrentUsageKwh();

            // Assert
            Assert.Equal(expectedKwh, actual);
        }

        /// <summary>
        /// Перевіряє, що вимкнений пристрій не враховується: результат не дорівнює сумі
        /// потужностей усіх пристроїв (2.1 кВт).
        /// </summary>
        [Fact]
        public void CalculateCurrentUsageKwh_DeviceTurnedOff_IsNotCounted()
        {
            // Arrange
            const double usageOfAllDevicesKwh = 2.1;
            _deviceRepo.Setup(r => r.GetAll()).Returns(new List<Device>
            {
                new Device { Id = 1, Name = "Lamp", PowerUsageWatts = 100, IsOn = true },
                new Device { Id = 2, Name = "Heater", PowerUsageWatts = 2000, IsOn = false }
            });
            var service = CreateService();

            // Act
            var actual = service.CalculateCurrentUsageKwh();

            // Assert
            Assert.NotEqual(usageOfAllDevicesKwh, actual);
        }

        /// <summary>
        /// Перевіряє, що при перевищенні денного ліміту сповіщення надсилається рівно один раз.
        /// </summary>
        [Fact]
        public void CheckForOverload_UsageAboveLimit_SendsAlertExactlyOnce()
        {
            // Arrange
            SetupDevices(3000);
            _planRepo.Setup(p => p.GetCurrentPlan()).Returns(new EnergyPlan { DailyLimitKwh = DailyLimitKwh });
            var service = CreateService();

            // Act
            service.CheckForOverload();

            // Assert
            _notify.Verify(n => n.SendAlert(It.IsAny<string>()), Times.Exactly(1));
        }

        /// <summary>
        /// Перевіряє, що коли споживання не перевищує ліміт, сповіщення не надсилається.
        /// </summary>
        [Fact]
        public void CheckForOverload_UsageBelowLimit_NeverSendsAlert()
        {
            // Arrange
            SetupDevices(100);
            _planRepo.Setup(p => p.GetCurrentPlan()).Returns(new EnergyPlan { DailyLimitKwh = DailyLimitKwh });
            var service = CreateService();

            // Act
            service.CheckForOverload();

            // Assert
            _notify.Verify(n => n.SendAlert(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Перевіряє, що текст сповіщення про перевантаження містить слово "Overload".
        /// </summary>
        [Fact]
        public void CheckForOverload_UsageAboveLimit_AlertMentionsOverload()
        {
            // Arrange
            SetupDevices(3000);
            _planRepo.Setup(p => p.GetCurrentPlan()).Returns(new EnergyPlan { DailyLimitKwh = DailyLimitKwh });
            var service = CreateService();

            // Act
            service.CheckForOverload();

            // Assert
            _notify.Verify(n => n.SendAlert(It.Is<string>(m => m.Contains(OverloadWord))), Times.AtLeastOnce);
        }

        /// <summary>
        /// Перевіряє, що UpdateEnergyLimit зберігає поточний план через репозиторій.
        /// </summary>
        [Fact]
        public void UpdateEnergyLimit_NewLimit_SavesPlan()
        {
            // Arrange
            var plan = new EnergyPlan { DailyLimitKwh = 5 };
            _planRepo.Setup(p => p.GetCurrentPlan()).Returns(plan);
            var service = CreateService();

            // Act
            service.UpdateEnergyLimit(10);

            // Assert
            _planRepo.Verify(p => p.UpdatePlan(plan));
        }

        /// <summary>
        /// Перевіряє, що після UpdateEnergyLimit у плані записано новий денний ліміт.
        /// </summary>
        [Fact]
        public void UpdateEnergyLimit_NewLimit_ChangesDailyLimit()
        {
            // Arrange
            const double newLimit = 10;
            var plan = new EnergyPlan { DailyLimitKwh = 5 };
            _planRepo.Setup(p => p.GetCurrentPlan()).Returns(plan);
            var service = CreateService();

            // Act
            service.UpdateEnergyLimit(newLimit);

            // Assert
            Assert.Equal(newLimit, plan.DailyLimitKwh);
        }

        private void SetupDevices(double heaterWatts)
        {
            _deviceRepo.Setup(r => r.GetAll()).Returns(new List<Device>
            {
                new Device { Id = 1, Name = "Heater", PowerUsageWatts = heaterWatts, IsOn = true }
            });
        }
    }
}
