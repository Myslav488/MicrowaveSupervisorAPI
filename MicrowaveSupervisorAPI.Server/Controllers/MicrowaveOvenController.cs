using Microsoft.AspNetCore.Mvc;
using MicrowaveSupervisorAPI.Server.Interfaces;
using MicrowaveSupervisorAPI.Server.Models;
using System.Timers;

namespace MicrowaveSupervisorAPI.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MicrowaveOvenController : ControllerBase, IDisposable
    {

        private readonly IMicrowaveOvenHW _microwave;
        private bool _isHeaterRunning;
        private int _remainingCookingTimeSeconds;
        private System.Timers.Timer? _cookingTimer;
        private readonly object _lock = new object(); 

        // Internal state for light control
        private bool _isLightOn;

        public MicrowaveOvenController(IMicrowaveOvenHW microwave)
        {
            _microwave = microwave;
            _isHeaterRunning = false;
            _remainingCookingTimeSeconds = 0;
            _isLightOn = false;

            // Subscribe to hardware events
            _microwave.DoorOpenChanged += OnDoorOpenChanged;
            _microwave.StartButtonPressed += OnStartButtonPressed;
        }

        /// <summary>
        /// Handles changes in the door open/closed status.
        /// </summary>
        /// <param name="isDoorOpen">True if the door is open, false if closed.</param>
        private void OnDoorOpenChanged(bool isDoorOpen)
        {
            lock (_lock)
            {
                if (isDoorOpen)
                {
                    // User Story: When I open door Light is on.
                    _isLightOn = true;
                    // User Story: When I open door heater stops if running.
                    if (_isHeaterRunning)
                    {
                        StopHeating();
                    }
                }
                else
                {
                    // User Story: When I close door Light turns off.
                    _isLightOn = false;
                }
                // Notify the hardware  about light changes (if it supports it)
                // In a real system, the controller would directly command the light.
                if (_microwave is Hardware.MicrowaveOven microwave)
                {
                    if (_isLightOn) microwave.TurnOnLight();
                    else microwave.TurnOffLight();
                }
                Console.WriteLine($"[Controller] Door {(_microwave.DoorOpen ? "Opened" : "Closed")}, Light {_isLightOn}");
            }
        }

        /// <summary>
        /// Handles the start button press event.
        /// </summary>
        private void OnStartButtonPressed(object? sender, EventArgs e)
        {
            lock (_lock)
            {
                // User Story: When I press start button when door is open nothing happens.
                if (_microwave.DoorOpen)
                {
                    Console.WriteLine("[Controller] Start button pressed: Door is open, nothing happens.");
                    return;
                }

                // User Story: When I press start button when door is closed, heater runs for 1 minute.
                // User Story: When I press start button when door is closed and already heating, increase remaining time with 1 minute.
                if (!_isHeaterRunning)
                {
                    _remainingCookingTimeSeconds = 60;
                    StartHeating();
                    Console.WriteLine("[Controller] Start button pressed: Starting heating for 1 minute.");
                }
                else
                {
                    _remainingCookingTimeSeconds += 60;
                    Console.WriteLine($"[Controller] Start button pressed: Added 1 minute, total remaining: {_remainingCookingTimeSeconds} seconds.");
                }
            }
        }

        /// <summary>
        /// Starts the heating process and the cooking timer.
        /// </summary>
        private void StartHeating()
        {
            _isHeaterRunning = true;
            _microwave.TurnOnHeater();

            // Initialize or reset the timer
            _cookingTimer?.Dispose();
            _cookingTimer = new System.Timers.Timer(1000);
            _cookingTimer.Elapsed += OnCookingTimerElapsed;
            _cookingTimer.AutoReset = true;
            _cookingTimer.Start();
            Console.WriteLine("[Controller] Heating started.");
        }

        /// <summary>
        /// Stops the heating process and the cooking timer.
        /// </summary>
        private void StopHeating()
        {
            _isHeaterRunning = false;
            _remainingCookingTimeSeconds = 0;
            _microwave.TurnOffHeater();
            _cookingTimer?.Stop();
            _cookingTimer?.Dispose();
            _cookingTimer = null;
            Console.WriteLine("[Controller] Heating stopped.");
        }

        /// <summary>
        /// Handles the cooking timer's elapsed event.
        /// </summary>
        private void OnCookingTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            lock (_lock)
            {
                if (_remainingCookingTimeSeconds > 0)
                {
                    _remainingCookingTimeSeconds--;
                    Console.WriteLine($"[Controller] Remaining time: {_remainingCookingTimeSeconds} seconds.");
                }

                if (_remainingCookingTimeSeconds <= 0 && _isHeaterRunning)
                {
                    Console.WriteLine("[Controller] Cooking time elapsed, stopping heating.");
                    StopHeating();
                }
            }
        }

        /// <summary>
        /// Gets the current status of the microwave oven.
        /// This is an API endpoint for external consumption.
        /// </summary>
        [HttpGet("status")]
        public ActionResult<MicrowaveStatus> GetStatus()
        {
            lock (_lock)
            {
                return Ok(new MicrowaveStatus
                {
                    IsDoorOpen = _microwave.DoorOpen,
                    IsHeaterRunning = _isHeaterRunning,
                    IsLightOn = _isLightOn,
                    RemainingCookingTimeSeconds = _remainingCookingTimeSeconds,
                    Message = "Current Microwave Oven Status"
                });
            }
        }

        /// <summary>
        /// Simulates opening the microwave door.
        /// </summary>
        [HttpPost("openDoor")]
        public ActionResult SimulateOpenDoor()
        {
            if (_microwave is Hardware.MicrowaveOven microwave)
            {
                microwave.SimulateDoorOpen();
                return Ok("Door opened simulation triggered.");
            }
            return BadRequest("Hardware not available for this operation.");
        }

        /// <summary>
        /// Simulates closing the microwave door.
        /// </summary>
        [HttpPost("closeDoor")]
        public ActionResult SimulateCloseDoor()
        {
            if (_microwave is Hardware.MicrowaveOven microwave)
            {
                microwave.SimulateDoorClose();
                return Ok("Door closed simulation triggered.");
            }
            return BadRequest("Hardware not available for this operation.");
        }

        /// <summary>
        /// Simulates pressing the start button.
        /// </summary>
        [HttpPost("pressStart")]
        public ActionResult SimulatePressStartButton()
        {
            if (_microwave is Hardware.MicrowaveOven microwave)
            {
                microwave.SimulateStartButtonPressed();
                return Ok("Start button pressed simulation triggered.");
            }
            return BadRequest("Hardware not available for this operation.");
        }

        public void Dispose()
        {
            // Unsubscribe from events to prevent memory leaks
            _microwave.DoorOpenChanged -= OnDoorOpenChanged;
            _microwave.StartButtonPressed -= OnStartButtonPressed;

            // Dispose the timer
            _cookingTimer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

