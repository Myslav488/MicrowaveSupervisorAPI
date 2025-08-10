using Microsoft.AspNetCore.Mvc;
using MicrowaveSupervisorAPI.Server.Controllers;
using MicrowaveSupervisorAPI.Server.Interfaces;
using MicrowaveSupervisorAPI.Server.Models;
using Moq;

namespace MicrowaveSupervisorAPI.Tests.Controllers
{
    public class MicrowaveOvenControllerTests
    {
        private readonly Mock<IMicrowaveOvenHW> _mockHw;
        private readonly MicrowaveOvenController _controller;

        public MicrowaveOvenControllerTests()
        {
            _mockHw = new Mock<IMicrowaveOvenHW>();
            // Set initial state for DoorOpen on the mock
            _mockHw.SetupGet(hw => hw.DoorOpen).Returns(false);

            _controller = new MicrowaveOvenController(_mockHw.Object);
        }

        // --- User Story: When I open door Light is on. ---
        [Fact]
        public void DoorOpenChanged_WhenOpened_LightTurnsOn()
        {
            // Arrange - Door is initially closed
            _mockHw.SetupGet(hw => hw.DoorOpen).Returns(false); // Ensure door is closed initially
            var initialStatus = (MicrowaveStatus)(_controller.GetStatus()?.Result as OkObjectResult)?.Value;
            Assert.False(initialStatus?.IsLightOn); // Light should be off initially

            // Act - Simulate door opening
            _mockHw.SetupGet(hw => hw.DoorOpen).Returns(true); // Update mock's DoorOpen state
            _mockHw.Raise(hw => hw.DoorOpenChanged += null, true);

            // Assert
            var statusAfterOpen = (MicrowaveStatus)(_controller.GetStatus()?.Result as OkObjectResult)?.Value;
            Assert.True(statusAfterOpen?.IsLightOn);
        }

        // --- User Story: When I close door Light turns off. ---
        [Fact]
        public void DoorOpenChanged_WhenClosed_LightTurnsOff()
        {
            // Arrange - Simulate door being open first, so light is on
            _mockHw.SetupGet(hw => hw.DoorOpen).Returns(true);
            _mockHw.Raise(hw => hw.DoorOpenChanged += null, true);

            var statusBeforeClose = (MicrowaveStatus)(_controller.GetStatus()?.Result as OkObjectResult)?.Value;
            Assert.True(statusBeforeClose?.IsLightOn);

            // Act - Simulate door closing
            _mockHw.SetupGet(hw => hw.DoorOpen).Returns(false);
            _mockHw.Raise(hw => hw.DoorOpenChanged += null, false);

            // Assert
            var statusAfterClose = (MicrowaveStatus)(_controller.GetStatus()?.Result as OkObjectResult)?.Value;
            Assert.False(statusAfterClose?.IsLightOn);
        }

        // --- User Story: When I open door heater stops if running. ---
        [Fact]
        public void DoorOpenChanged_WhenOpenedWhileHeating_HeaterStops()
        {
            // Arrange - Simulate heating first
            _mockHw.SetupGet(hw => hw.DoorOpen).Returns(false); // Door closed
            _mockHw.Raise(hw => hw.StartButtonPressed += null, new EventArgs()); // Press start
            var statusBeforeOpen = (MicrowaveStatus)(_controller.GetStatus()?.Result as OkObjectResult)?.Value;
            Assert.True(statusBeforeOpen?.IsHeaterRunning);
            _mockHw.Verify(hw => hw.TurnOnHeater(), Times.Once());

            // Act - Simulate door opening
            _mockHw.SetupGet(hw => hw.DoorOpen).Returns(true); // Update mock's DoorOpen state
            _mockHw.Raise(hw => hw.DoorOpenChanged += null, true);

            // Assert
            var statusAfterOpen = (MicrowaveStatus)(_controller.GetStatus()?.Result as OkObjectResult)?.Value;
            Assert.False(statusAfterOpen?.IsHeaterRunning);
            _mockHw.Verify(hw => hw.TurnOffHeater(), Times.Once());
            Assert.Equal(0, statusAfterOpen?.RemainingCookingTimeSeconds);
        }

        // --- User Story: When I press start button when door is open nothing happens. ---
        [Fact]
        public void StartButtonPressed_DoorOpen_NothingHappens()
        {
            // Arrange
            _mockHw.SetupGet(hw => hw.DoorOpen).Returns(true); // Door is open
            var initialStatus = (MicrowaveStatus)(_controller.GetStatus()?.Result as OkObjectResult)?.Value;
            Assert.False(initialStatus?.IsHeaterRunning);

            // Act
            _mockHw.Raise(hw => hw.StartButtonPressed += null, new EventArgs());

            // Assert
            var statusAfterPress = (MicrowaveStatus)(_controller.GetStatus()?.Result as OkObjectResult)?.Value;
            Assert.False(statusAfterPress?.IsHeaterRunning);
            _mockHw.Verify(hw => hw.TurnOnHeater(), Times.Never()); // Heater should not turn on
            Assert.Equal(0, statusAfterPress?.RemainingCookingTimeSeconds);
        }

        // --- User Story: When I press start button when door is closed, heater runs for 1 minute. ---
        [Fact]
        public void StartButtonPressed_DoorClosed_StartsHeatingForOneMinute()
        {
            // Arrange
            _mockHw.SetupGet(hw => hw.DoorOpen).Returns(false); // Door is closed
            var initialStatus = (MicrowaveStatus)(_controller.GetStatus()?.Result as OkObjectResult)?.Value;
            Assert.False(initialStatus?.IsHeaterRunning);
            Assert.Equal(0, initialStatus?.RemainingCookingTimeSeconds);

            // Act
            _mockHw.Raise(hw => hw.StartButtonPressed += null, new EventArgs());

            // Assert
            var statusAfterPress = (MicrowaveStatus)(_controller.GetStatus()?.Result as OkObjectResult)?.Value;
            Assert.True(statusAfterPress?.IsHeaterRunning);
            _mockHw.Verify(hw => hw.TurnOnHeater(), Times.Once());
            Assert.Equal(60, statusAfterPress?.RemainingCookingTimeSeconds);
        }

        // --- User Story: When I press start button when door is closed and already heating, increase remaining time with 1 minute. ---
        [Fact]
        public void StartButtonPressed_AlreadyHeating_IncreasesTimeByOneMinute()
        {
            // Arrange - Start heating first for 1 minute
            _mockHw.SetupGet(hw => hw.DoorOpen).Returns(false);
            _mockHw.Raise(hw => hw.StartButtonPressed += null, new EventArgs());
            var statusAfterFirstPress = (MicrowaveStatus)(_controller.GetStatus()?.Result as OkObjectResult)?.Value;
            Assert.True(statusAfterFirstPress?.IsHeaterRunning);
            Assert.Equal(60, statusAfterFirstPress?.RemainingCookingTimeSeconds);

            // Act - Press start again while heating
            _mockHw.Raise(hw => hw.StartButtonPressed += null, new EventArgs());

            // Assert
            var statusAfterSecondPress = (MicrowaveStatus)(_controller.GetStatus()?.Result as OkObjectResult)?.Value;
            Assert.True(statusAfterSecondPress?.IsHeaterRunning); // Should still be heating
            _mockHw.Verify(hw => hw.TurnOnHeater(), Times.Once()); // Heater already on, shouldn't call TurnOnHeater again
            Assert.Equal(120, statusAfterSecondPress?.RemainingCookingTimeSeconds); // Should be 60 + 60 = 120
        }

        // --- Additional Test: Initial state ---
        [Fact]
        public void InitialState_IsCorrect()
        {
            // Act
            var status = (MicrowaveStatus)(_controller.GetStatus()?.Result as OkObjectResult)?.Value;

            // Assert
            Assert.False(status?.IsDoorOpen);
            Assert.False(status?.IsHeaterRunning);
            Assert.False(status?.IsLightOn);
            Assert.Equal(0, status?.RemainingCookingTimeSeconds);
        }

        // --- Additional Test: Light behavior during heating (door closed) ---
        [Fact]
        public void StartHeating_LightStaysOff()
        {
            // Arrange
            _mockHw.SetupGet(hw => hw.DoorOpen).Returns(false);

            // Act
            _mockHw.Raise(hw => hw.StartButtonPressed += null, new EventArgs());

            // Assert
            var status = (MicrowaveStatus)(_controller.GetStatus()?.Result as OkObjectResult)?.Value;
            Assert.False(status?.IsLightOn); // Light should remain off when door is closed and heating
        }

        // --- Additional Test: Heater does not turn on if already heating (internal state check) ---
        [Fact]
        public void StartHeating_AlreadyOn_TurnOnHeaterNotCalledAgain()
        {
            // Arrange
            _mockHw.SetupGet(hw => hw.DoorOpen).Returns(false);
            _mockHw.Raise(hw => hw.StartButtonPressed += null, new EventArgs()); // First press to start heating

            // Reset mock for verifying subsequent calls
            _mockHw.Invocations.Clear();

            // Act
            _mockHw.Raise(hw => hw.StartButtonPressed += null, new EventArgs()); // Second press

            // Assert
            _mockHw.Verify(hw => hw.TurnOnHeater(), Times.Never()); // TurnOnHeater should not be called again
            _mockHw.Verify(hw => hw.TurnOffHeater(), Times.Never()); // TurnOffHeater should not be called
            var status = (MicrowaveStatus)(_controller.GetStatus()?.Result as OkObjectResult)?.Value;
            Assert.True(status?.IsHeaterRunning);
            Assert.Equal(120, status?.RemainingCookingTimeSeconds); // Time should have increased
        }

        // --- Cleanup ---
        public void Dispose()
        {
            _controller.Dispose();
        }
    }
    }