using MicrowaveSupervisorAPI.Server.Interfaces;

namespace MicrowaveSupervisorAPI.Server.Hardware
{
    public class MicrowaveOven : IMicrowaveOvenHW
        {
            private bool _doorOpen;
            private bool _heaterOn;
            private bool _lightOn;

            public MicrowaveOven()
            {
                _doorOpen = false;
                _heaterOn = false;
                _lightOn = false;
            }

            public bool IsHeaterOn => _heaterOn;
            public bool IsLightOn => _lightOn;

            public void TurnOnHeater()
            {
                _heaterOn = true;
                Console.WriteLine("Heater ON");
            }

            public void TurnOffHeater()
            {
                _heaterOn = false;
                Console.WriteLine("Heater OFF");
            }

            public bool DoorOpen
            {
                get => _doorOpen;
                private set
                {
                    if (_doorOpen != value)
                    {
                        _doorOpen = value;
                        DoorOpenChanged?.Invoke(_doorOpen);
                        Console.WriteLine($"Door status changed to: {(_doorOpen ? "Open" : "Closed")}");
                    }
                }
            }

            public event Action<bool>? DoorOpenChanged;
            public event EventHandler? StartButtonPressed;

            /// <summary>
            /// Simulates the user opening the door.
            /// </summary>
            public void SimulateDoorOpen()
            {
                DoorOpen = true;
            }

            /// <summary>
            /// Simulates the user closing the door.
            /// </summary>
            public void SimulateDoorClose()
            {
                DoorOpen = false;
            }

            /// <summary>
            /// Simulates the user pressing the start button.
            /// </summary>
            public void SimulateStartButtonPressed()
            {
                Console.WriteLine("Start button pressed.");
                StartButtonPressed?.Invoke(this, EventArgs.Empty);
            }

            /// <summary>
            /// Turns on the internal light.
            /// </summary>
            public void TurnOnLight()
            {
                _lightOn = true;
                Console.WriteLine("Light ON");
            }

            /// <summary>
            /// Turns off the internal light.
            /// </summary>
            public void TurnOffLight()
            {
                _lightOn = false;
                Console.WriteLine("Light OFF");
            }
        
    }
}
