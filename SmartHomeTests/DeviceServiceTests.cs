﻿using Energy_Project.Models;
using Energy_Project.Services;
using Energy_Project.Services.Interfaces;
using Moq;

namespace SmartHomeTests
{
    public class DeviceServiceTests
    {
        private const int ExistingDeviceId = 1;
        private const int MissingDeviceId = 99;

        private readonly Mock<IDeviceRepository> _deviceRepo = new();

        private DeviceService CreateService()
        {
            return new DeviceService(_deviceRepo.Object);
        }

        /// <summary>
        /// Перевіряє, що ToggleDevice повертає саме той стан, який передали в метод
        /// (true – увімкнути, false – вимкнути).
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ToggleDevice_ExistingDevice_ReturnsRequestedState(bool turnOn)
        {
            // Arrange
            var device = new Device { Id = ExistingDeviceId, Name = "Lamp", IsOn = !turnOn };
            _deviceRepo.Setup(r => r.GetById(ExistingDeviceId)).Returns(device);
            var service = CreateService();

            // Act
            var result = service.ToggleDevice(ExistingDeviceId, turnOn);

            // Assert
            Assert.Equal(turnOn, result);
        }

        /// <summary>
        /// Перевіряє, що після увімкнення вимкненого пристрою метод повертає true.
        /// </summary>
        [Fact]
        public void ToggleDevice_TurnOn_ReturnsTrue()
        {
            // Arrange
            var device = new Device { Id = ExistingDeviceId, Name = "Lamp", IsOn = false };
            _deviceRepo.Setup(r => r.GetById(ExistingDeviceId)).Returns(device);
            var service = CreateService();

            // Act
            var result = service.ToggleDevice(ExistingDeviceId, true);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Перевіряє, що після вимкнення увімкненого пристрою метод повертає false.
        /// </summary>
        [Fact]
        public void ToggleDevice_TurnOff_ReturnsFalse()
        {
            // Arrange
            var device = new Device { Id = ExistingDeviceId, Name = "Lamp", IsOn = true };
            _deviceRepo.Setup(r => r.GetById(ExistingDeviceId)).Returns(device);
            var service = CreateService();

            // Act
            var result = service.ToggleDevice(ExistingDeviceId, false);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Перевіряє, що для пристрою, якого немає в репозиторії, кидається ArgumentException.
        /// </summary>
        [Fact]
        public void ToggleDevice_DeviceNotFound_ThrowsArgumentException()
        {
            // Arrange
            _deviceRepo.Setup(r => r.GetById(MissingDeviceId)).Returns((Device?)null);
            var service = CreateService();

            // Act
            Action act = () => service.ToggleDevice(MissingDeviceId, true);

            // Assert
            Assert.Throws<ArgumentException>(act);
        }

        /// <summary>
        /// Перевіряє, що для неіснуючого пристрою метод Update репозиторію не викликається жодного разу.
        /// </summary>
        [Fact]
        public void ToggleDevice_DeviceNotFound_NeverCallsUpdate()
        {
            // Arrange
            _deviceRepo.Setup(r => r.GetById(MissingDeviceId)).Returns((Device?)null);
            var service = CreateService();

            // Act
            Record.Exception(() => service.ToggleDevice(MissingDeviceId, true));

            // Assert
            _deviceRepo.Verify(r => r.Update(It.IsAny<Device>()), Times.Never);
        }

        /// <summary>
        /// Перевіряє, що в репозиторій один раз зберігається саме цей пристрій з новим станом (увімкнений).
        /// </summary>
        [Fact]
        public void ToggleDevice_ExistingDevice_UpdatesDeviceWithNewState()
        {
            // Arrange
            var device = new Device { Id = ExistingDeviceId, Name = "Lamp", IsOn = false };
            _deviceRepo.Setup(r => r.GetById(ExistingDeviceId)).Returns(device);
            var service = CreateService();

            // Act
            service.ToggleDevice(ExistingDeviceId, true);

            // Assert
            _deviceRepo.Verify(r => r.Update(It.Is<Device>(d => d.Id == ExistingDeviceId && d.IsOn)), Times.Once);
        }

        /// <summary>
        /// Перевіряє, що для існуючого пристрою ToggleDevice виконується без винятків.
        /// </summary>
        [Fact]
        public void ToggleDevice_ExistingDevice_DoesNotThrow()
        {
            // Arrange
            var device = new Device { Id = ExistingDeviceId, Name = "Lamp", IsOn = false };
            _deviceRepo.Setup(r => r.GetById(ExistingDeviceId)).Returns(device);
            var service = CreateService();

            // Act
            var exception = Record.Exception(() => service.ToggleDevice(ExistingDeviceId, true));

            // Assert
            Assert.Null(exception);
        }

        /// <summary>
        /// Перевіряє, що список активних пристроїв містить увімкнений пристрій.
        /// </summary>
        [Fact]
        public void GetActiveDevices_SomeDevicesOn_ContainsTurnedOnDevice()
        {
            // Arrange
            var lamp = new Device { Id = 1, Name = "Lamp", PowerUsageWatts = 60, IsOn = true };
            var heater = new Device { Id = 2, Name = "Heater", PowerUsageWatts = 2000, IsOn = false };
            _deviceRepo.Setup(r => r.GetAll()).Returns(new List<Device> { lamp, heater });
            var service = CreateService();

            // Act
            var result = service.GetActiveDevices();

            // Assert
            Assert.Contains(lamp, result);
        }

        /// <summary>
        /// Перевіряє, що коли всі пристрої вимкнені, список активних пристроїв порожній.
        /// </summary>
        [Fact]
        public void GetActiveDevices_AllDevicesOff_ReturnsEmpty()
        {
            // Arrange
            var lamp = new Device { Id = 1, Name = "Lamp", PowerUsageWatts = 60, IsOn = false };
            var heater = new Device { Id = 2, Name = "Heater", PowerUsageWatts = 2000, IsOn = false };
            _deviceRepo.Setup(r => r.GetAll()).Returns(new List<Device> { lamp, heater });
            var service = CreateService();

            // Act
            var result = service.GetActiveDevices();

            // Assert
            Assert.Empty(result);
        }
    }
}
