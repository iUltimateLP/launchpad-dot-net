using System;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Core;
using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.Common;

namespace LaunchpadNET
{

    public enum LightingMode { Set, Flash, Pulse, RGB };

    public enum SideLEDs { Zero = 89, One = 79, Two = 69, Three = 59, Four = 49, Five = 39, Six = 29, Seven = 19 }; //top to bottom
    public enum TopLEDs { Up = 91, Down = 92, Left = 93, Right = 94, Session = 95, Drums = 96, Keys = 97, User = 98, Logo = 99 }; //match keys on MiniMk3
    public enum LaunchpadMode { Live = 0, Programmer = 1 };

    public class Interface : IDisposable
    {
        private Action<string> outError;
        private Action<string> outInfo;
        private Action<string> outWarn;
        public bool Evented = false;
        public Interface(Action<string> outError, Action<string> outInfo, Action<string> outWarn)
        {
            this.outError = outError;
            this.outInfo = outInfo;
            this.outWarn = outWarn;
        }
        private Pitch[,] notes = new Pitch[9, 9] {
            { Pitch.G6, Pitch.GSharp6, Pitch.A6, Pitch.ASharp6, Pitch.B6, Pitch.C7, Pitch.CSharp7, Pitch.D7, Pitch.DSharp7 },
            { Pitch.A5, Pitch.ASharp5, Pitch.B5, Pitch.C6, Pitch.CSharp6, Pitch.D6, Pitch.DSharp6, Pitch.E6, Pitch.F6 },
            { Pitch.B4, Pitch.C5, Pitch.CSharp5, Pitch.D5, Pitch.DSharp5, Pitch.E5, Pitch.F5, Pitch.FSharp5, Pitch.G5 },
            { Pitch.CSharp4, Pitch.D4, Pitch.DSharp4, Pitch.E4, Pitch.F4, Pitch.FSharp4, Pitch.G4, Pitch.GSharp4, Pitch.A4 },
            { Pitch.DSharp3, Pitch.E3, Pitch.F3, Pitch.FSharp3, Pitch.G3, Pitch.GSharp3, Pitch.A3, Pitch.ASharp3, Pitch.B3 },
            { Pitch.F2, Pitch.FSharp2, Pitch.G2, Pitch.GSharp2, Pitch.A2, Pitch.ASharp2, Pitch.B2, Pitch.C3, Pitch.CSharp3 },
            { Pitch.G1, Pitch.GSharp1, Pitch.A1, Pitch.ASharp1, Pitch.B1, Pitch.C2, Pitch.CSharp2, Pitch.D2, Pitch.DSharp2 },
            { Pitch.A0, Pitch.ASharp0, Pitch.B0, Pitch.C1, Pitch.CSharp1, Pitch.D1, Pitch.DSharp1, Pitch.E1, Pitch.F1 },
            { Pitch.BNeg1, Pitch.C0, Pitch.CSharp0, Pitch.D0, Pitch.DSharp0, Pitch.E0, Pitch.F0, Pitch.FSharp0, Pitch.G0 }
        };

        private Pitch[] rightLEDnotes = new Pitch[] {
            Pitch.F6, Pitch.G5, Pitch.A4, Pitch.B3, Pitch.CSharp3, Pitch.DSharp2, Pitch.F1, Pitch.G0
        };

        private Pitch[] topLEDNotes = new Pitch[] {
            Pitch.G6, Pitch.GSharp6, Pitch.A6, Pitch.ASharp6, Pitch.B6, Pitch.C7, Pitch.CSharp7, Pitch.D7, Pitch.DSharp7
        };

        public InputDevice targetInput;
        public OutputDevice targetOutput;

        public delegate void LaunchpadKeyEventHandler(object source, LaunchpadKeyEventArgs e);
        public delegate void LaunchpadKeyDownHandler(object source, LaunchpadKeyEventArgs e);
        public delegate void LaunchpadKeyUpHandler(object source, LaunchpadKeyEventArgs e);

        public delegate void LaunchpadCCKeyEventHandler(object source, LaunchpadCCKeyEventArgs e);
        public delegate void LaunchpadCCKeyDownHandler(object source, LaunchpadCCKeyEventArgs e);
        public delegate void LaunchpadCCKeyUpHandler(object source, LaunchpadCCKeyEventArgs e);

        /// <summary>
        /// Event Handler when a Launchpad Key is pressed.
        /// </summary>
        public event LaunchpadKeyEventHandler OnLaunchpadKeyPressed;
        public event LaunchpadCCKeyEventHandler OnLaunchpadCCKeyPressed;
        public event LaunchpadCCKeyDownHandler OnLaunchpadCCKeyDown;
        public event LaunchpadCCKeyUpHandler OnLaunchpadCCKeyUp;
        public event LaunchpadKeyDownHandler OnLaunchpadKeyDown;
        public event LaunchpadKeyUpHandler OnLaunchpadKeyUp;

        public void Dispose()
        {
            OnLaunchpadKeyPressed = null;
            OnLaunchpadCCKeyPressed = null;
            OnLaunchpadCCKeyDown = null;
            OnLaunchpadCCKeyUp = null;
            OnLaunchpadKeyDown = null;
            OnLaunchpadKeyUp = null;
            OnLaunchpadKeyPressed = null;
            OnLaunchpadCCKeyPressed = null;
            OnLaunchpadCCKeyDown = null;
            OnLaunchpadCCKeyUp = null;
            OnLaunchpadKeyDown = null;
            OnLaunchpadKeyUp = null;
        }
        private byte[] sysexHeader;

        public bool Connected { get; set; }
        public bool IsLegacy { get; set; }

        public class LaunchpadCCKeyEventArgs : EventArgs
        {
            private int val;
            public LaunchpadCCKeyEventArgs(int _val)
            {
                val = _val;
            }
            public int GetVal()
            {
                return val;
            }
            public TopLEDs? GetTopLED()
            {
                if (Enum.IsDefined(typeof(TopLEDs), val))
                {
                    foreach (TopLEDs e in Enum.GetValues(typeof(TopLEDs)))
                    {
                        if ((int)e == val)
                            return e;
                    }
                }
                return null;
            }
            public SideLEDs? GetSideLED()
            {
                if (Enum.IsDefined(typeof(SideLEDs), val))
                {
                    foreach (SideLEDs e in Enum.GetValues(typeof(SideLEDs)))
                    {
                        if ((int)e == val)
                            return e;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// EventArgs for pressed Launchpad Key
        /// </summary>
        public class LaunchpadKeyEventArgs : EventArgs
        {
            private int x;
            private int y;
            public LaunchpadKeyEventArgs(int _pX, int _pY)
            {
                x = _pX;
                y = _pY;
            }
            public int GetX()
            {
                return x;
            }
            public int GetY()
            {
                return y;
            }
        }

        /// <summary>
        /// Creates a text scroll.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="speed"></param>
        /// <param name="looping"></param>
        /// <param name="velo"></param>
        /// </summary>
        public void createTextScrollMiniMk3(string text, int speed, bool looping, int velo)
        {
            //order of the args is diff for this unit
            byte[] sysexStop = { 247 };
            byte operation = 7;
            byte _velocity = (byte)velo;
            byte _speed = (byte)speed;
            byte _loop = Convert.ToByte(looping);
            byte[] _text = { };
            byte[] finalArgs = { operation, _loop, _speed, 0, _velocity };
            List<byte> charList = new List<byte>();
            foreach (char c in text)
            {
                int unicode = c;
                if (unicode < 128)
                    charList.Add(Convert.ToByte(unicode));
            }
            _text = charList.ToArray();
            byte[] finalBytes = sysexHeader.Concat(finalArgs.Concat(_text.Concat(sysexStop))).ToArray();

            targetOutput.SendEvent(new NormalSysExEvent(finalBytes));
        }

        /// <summary>
        /// Creates a text scroll in full RGB colour
        /// </summary>
        /// <param name="text"></param>
        /// <param name="speed"></param>
        /// <param name="looping"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// </summary>
        public void createTextScrollMiniMk3RGB(string text, int speed, bool looping, int r, int g, int b)
        {
            //order of the args is diff for this unit
            byte[] sysexStop = { 247 };
            byte operation = 7;
            byte[] _text = { };
            byte[] finalArgs = { operation, Convert.ToByte(looping), (byte)speed, 1, (byte)r, (byte)g, (byte)b };
            List<byte> charList = new List<byte>();
            foreach (char c in text)
            {
                int unicode = c;
                if (unicode < 128)
                    charList.Add(Convert.ToByte(unicode));
            }
            _text = charList.ToArray();
            byte[] finalBytes = sysexHeader.Concat(finalArgs.Concat(_text.Concat(sysexStop))).ToArray();

            targetOutput.SendEvent(new NormalSysExEvent(finalBytes));
        }
        /// <summary>
        /// Creates a text scroll.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="speed"></param>
        /// <param name="looping"></param>
        /// <param name="velo"></param>
        /// </summary>
        public void createTextScroll(string text, int speed, bool looping, int velo)
        {
            byte[] sysexStop = { 247 };
            byte operation = 20;

            byte _velocity = (byte)velo;
            byte _speed = (byte)speed;
            byte _loop = Convert.ToByte(looping);
            byte[] _text = { };

            byte[] finalArgs = { operation, _velocity, _loop, _speed };

            List<byte> charList = new List<byte>();
            foreach (char c in text)
            {
                int unicode = c;
                if (unicode < 128)
                    charList.Add(Convert.ToByte(unicode));
            }
            _text = charList.ToArray();

            byte[] finalBytes = sysexHeader.Concat(finalArgs.Concat(_text.Concat(sysexStop))).ToArray();

            targetOutput.SendEvent(new NormalSysExEvent(finalBytes));
        }

        public void stopLoopingTextScroll()
        {
            byte[] stop = sysexHeader.Concat(new byte[] { 20, 247 }).ToArray();
            targetOutput.SendEvent(new NormalSysExEvent(stop));
        }

        public void stopLoopingTextScrollMiniMk3()
        {
            byte[] stop = sysexHeader.Concat(new byte[] { 7, 247 }).ToArray();
            targetOutput.SendEvent(new NormalSysExEvent(stop));
        }
        public Nullable<LaunchpadMode> ModeAsSet { get; set; }
        /// <summary>
        /// Switches between programmer and live mode.
        /// </summary>
        /// <param name="mode">The mode to set the launchpad to. Can be Programmer or Live mode.</param>
        /// </summary>
        public void SetMode(LaunchpadMode mode)
        {
            ModeAsSet = mode;
            byte[] modeSet = sysexHeader.Concat(new byte[] { 14, (byte)mode, 247 }).ToArray();
            targetOutput.SendEvent(new NormalSysExEvent(modeSet));
        }

        private void sysExAnswer(SysExEvent m)
        {
            byte[] msg = m.Data;
            byte[] stopBytes = sysexHeader.Concat(new byte[] { 21, 247 }).ToArray();
        }

        private void midiPress(object sender, MidiEventReceivedEventArgs msg)
        {
            var evt = msg.Event;
            if (evt.EventType == MidiEventType.ControlChange)
            {
                controlChangePress(sender, msg);
            } else
            {
                if (evt.EventType == MidiEventType.NoteOn)
                {
                    var noteevt = (NoteOnEvent)evt;
                    if (!rightLEDnotes.Contains((Pitch)(int)noteevt.NoteNumber))
                    {                        
                        var coords = midiNoteToLed((Pitch)(int)noteevt.NoteNumber);
                        LaunchpadKeyEventArgs z() => new LaunchpadKeyEventArgs(coords[0], coords[1]);
                        if (OnLaunchpadKeyPressed != null)
                        {
                            OnLaunchpadKeyPressed(this, z());
                        }
                        if (OnLaunchpadKeyUp != null && noteevt.Velocity == 0)
                        {
                            OnLaunchpadKeyUp(this, z());
                        }
                        if (OnLaunchpadKeyDown != null && noteevt.Velocity == 127)
                        {
                            OnLaunchpadKeyDown(this, z());
                        }
                    }
                    else if (OnLaunchpadKeyPressed != null && rightLEDnotes.Contains((Pitch)(int)noteevt.NoteNumber))
                    {
                        OnLaunchpadCCKeyPressed(this, new LaunchpadCCKeyEventArgs(midiNoteToSideLED((Pitch)(int)noteevt.NoteNumber)));
                    }
                }
            }
        }

        private void controlChangePress(object sender, MidiEventReceivedEventArgs msg)
        {

            var ccEvt = (ControlChangeEvent)msg.Event;


            OnLaunchpadCCKeyPressed?.Invoke(this, new LaunchpadCCKeyEventArgs((int)ccEvt.ControlNumber));
            if (OnLaunchpadCCKeyDown != null && ccEvt.ControlValue == 0)
            {
                OnLaunchpadCCKeyDown(this, new LaunchpadCCKeyEventArgs((int)ccEvt.ControlNumber));
            }
            if (OnLaunchpadCCKeyUp != null && ccEvt.ControlValue == 127)
            {
                OnLaunchpadCCKeyUp(this, new LaunchpadCCKeyEventArgs((int)ccEvt.ControlNumber));
            }
            var coords = midiNoteToLed((Pitch)(int)ccEvt.ControlNumber);
            LaunchpadKeyEventArgs z() => new LaunchpadKeyEventArgs(coords[0], coords[1]);
            if (OnLaunchpadKeyPressed != null)
            {
                OnLaunchpadKeyPressed(this, z());
            }
            if (OnLaunchpadKeyUp != null && ccEvt.ControlValue == 0)
            {
                OnLaunchpadKeyUp(this, z());
            }
            if (OnLaunchpadKeyDown != null && ccEvt.ControlValue == 127)
            {
                OnLaunchpadKeyDown(this, z());
            }
        }

        public int midiNoteToSideLED(Pitch p)
        {
            for (int y = 0; y <= 7; y++)
            {
                if (rightLEDnotes[y] == p)
                {
                    return y;
                }
            }
            return 0;
        }

        /// <summary>
        /// Returns the LED coordinates of a MIdi note
        /// </summary>
        /// <param name="p">The Midi Note.</param>
        /// <returns>The X,Y coordinates.</returns>
        public int[] midiNoteToLed(Pitch p)
        {
            for (int x = 0; x <= 8; x++)
            {
                for (int y = 0; y <= 8; y++)
                {
                    if (notes[x, y] == p)
                    {
                        int[] r1 = { x, y };
                        return r1;
                    }
                }
            }
            int[] r2 = { 0, 0 };
            return r2;
        }

        /// <summary>
        /// Returns the equilavent Midi Note to X and Y coordinates.
        /// </summary>
        /// <param name="x">The X coordinate of the LED</param>
        /// <param name="y">The Y coordinate of the LED</param>
        /// <returns>The midi note</returns>
        public Pitch ledToMidiNote(int x, int y) => notes[x, y];

        public void clearAllLEDs()
        {
            massUpdateLEDsRectangle(0, 0, 8, 8, 0);           
        }

        /// <summary>
        /// Fills Top Row LEDs.
        /// </summary>
        /// <param name="startX"></param>
        /// <param name="endX"></param>
        /// <param name="velo"></param>
        public void fillTopLEDs(int startX, int endX, int velo)
        {
            for (int x = 1; x < 9; x++)
            {
                if (x >= startX && x <= endX)
                {
                    setTopLEDs(x, velo);
                }
            }
        }

        /// <summary>
        /// Fills a region of Side LEDs.
        /// </summary>
        /// <param name="startY"></param>
        /// <param name="endY"></param>
        /// <param name="velo"></param>
        public void fillSideLEDs(int startY, int endY, int velo)
        {
            for (int y = 0; y < rightLEDnotes.Length; y++)
            {
                if (y >= startY && y <= endY)
                {
                    setSideLED(y, velo);
                }
            }
        }

        /// <summary>
        /// Creates a rectangular mesh of LEDs.
        /// </summary>
        /// <param name="startX">Start X coordinate</param>
        /// <param name="startY">Start Y coordinate</param>
        /// <param name="endX">End X coordinate</param>
        /// <param name="endY">End Y coordinate</param>
        /// <param name="velo">Painting velocity</param>
        public void fillLEDs(int startX, int startY, int endX, int endY, int velo)
        {
            for (int x = 0; x < notes.Length; x++)
            {
                for (int y = 0; y < notes.Length; y++)
                {
                    if (x >= startX && y >= startY && x <= endX && y <= endY)
                        setLED(x, y, velo);
                }
            }
        }

        /// <summary>
        /// Sets a Top LED of the launchpad
        /// </summary>
        /// <param name="x"></param>
        /// <param name="velo"></param>
        public void setTopLEDs(int x, int velo)
        {
            byte[] data = sysexHeader.Concat(new byte[] { 10, Convert.ToByte(103 + x), Convert.ToByte(velo), 247 }).ToArray();
            targetOutput.SendEvent(new NormalSysExEvent(data));
        }

        public void setTopLED(TopLEDs led, int velo)
        {
            targetOutput.SendEvent(new NoteOnEvent((SevenBitNumber)(int)led, (SevenBitNumber)velo)
            {
                Channel = (FourBitNumber)0
            });
        }

        public void setTopLED(TopLEDs led, int r, int g, int b)
        {
            var retx = GetBytesToSend(r, g, b, (int)led);
            DoASend(retx);
        }
        public void setTopLEDFlash(TopLEDs led, int velo)
        {
            targetOutput.SendEvent(new NoteOnEvent((SevenBitNumber)(int)led, (SevenBitNumber)velo)
            {
                Channel = (FourBitNumber)1
            });
        }
        public void setTopLEDPulse(TopLEDs led, int velo)
        {
            targetOutput.SendEvent(new NoteOnEvent((SevenBitNumber)(int)led, (SevenBitNumber)velo)
            {
                Channel = (FourBitNumber)2
            });
        }

        public void setClock(int bpm)
        {
            if (bpm < 40)
            {
                throw new ArgumentOutOfRangeException("Beats per minute cannot be less than 40.");
            }
            else if (bpm > 240)
            {
                throw new ArgumentOutOfRangeException("Beats per minute cannot be more than 240.");
            }
            var numToSend = 24 * bpm;
            var timeDiff = 60000 / numToSend;
            byte[] data = sysexHeader.Concat(new byte[] { 248, 247 }).ToArray();
            for (int i = 0; i <= 10; i++)
            {
                targetOutput.SendEvent(new NormalSysExEvent(data));
                System.Threading.Thread.Sleep(timeDiff);
            }
        }

        /// <summary>
        /// Sets a Side LED of the Launchpad.
        /// </summary>
        /// <param name="y">The height of the right Side LED.</param>
        /// <param name="velo">Velocity index.</param>
        public void setSideLED(int y, int velo)
        {
            targetOutput.SendEvent(new NoteOnEvent((SevenBitNumber)(int)rightLEDnotes[y], (SevenBitNumber)velo)
            {
                Channel = (FourBitNumber)0
            });
        }

        public void setSideLED(SideLEDs led, int velo)
        {
            targetOutput.SendEvent(new NoteOnEvent((SevenBitNumber)(int)led, (SevenBitNumber)velo)
            {
                Channel = (FourBitNumber)0
            });
        }

        public void setSideLED(SideLEDs led, int r, int g, int b)
        {
            var retx = GetBytesToSend(r, g, b, (int)led);
            DoASend(retx);
        }

        public void setSideLEDFlash(int y, int velo)
        {
            targetOutput.SendEvent(new NoteOnEvent((SevenBitNumber)(int)rightLEDnotes[y], (SevenBitNumber)velo)
            {
                Channel = (FourBitNumber)1
            });
        }
        public void setSideLEDFlash(SideLEDs led, int velo)
        {
            targetOutput.SendEvent(new NoteOnEvent((SevenBitNumber)(int)led, (SevenBitNumber)velo)
            {
                Channel = (FourBitNumber)1
            });
        }

        public void setSideLEDPulse(int y, int velo)
        {
            targetOutput.SendEvent(new NoteOnEvent((SevenBitNumber)(int)rightLEDnotes[y], (SevenBitNumber)velo)
            {
                Channel = (FourBitNumber)2
            });
        }
        public void setSideLEDPulse(SideLEDs led, int velo)
        {
            targetOutput.SendEvent(new NoteOnEvent((SevenBitNumber)(int)led, (SevenBitNumber)velo)
            {
                Channel = (FourBitNumber)2
            });
        }

        public void massUpdateLEDs(IEnumerable<int> xsx, IEnumerable<int> ysx, int red, int green = 0, int blue = 0)
        {
            var xs = xsx.ToList();
            var ys = ysx.ToList();
            if (xs.Count != ys.Count)
                throw new Exception("count of xs and ys does not match");
            List<byte> sendbytes = new List<byte>();
            for (int i = 0; i < xs.Count; i++)
            {
                int x = xs[i];
                int y = ys[i];
                var retx = GetBytesToSend(red, green, blue, x, y);
                sendbytes.AddRange(retx);
            }
            DoASend(sendbytes.ToArray());
        }

        private void DoASend(byte[] bytes)
        {
            var sendbytes = HeaderBytes;
            sendbytes = sendbytes.Concat(bytes).ToArray();
            sendbytes = sendbytes.Concat(new byte[] { 247 }).ToArray();
            SendByte(sendbytes);
        }

        private void SendByte(byte[] sendbytes) => targetOutput.SendEvent(new NormalSysExEvent(sendbytes));

        private byte[] HeaderBytes => sysexHeader.Concat(new byte[] { 3 }).ToArray();

        private byte[] GetBytesToSend(int r, int g, int b, int x, int y)
        {
            var note = GetNoteFromCoords(x, y);
            return (new byte[] { (byte)LightingMode.RGB, (byte)note, (byte)r, (byte)g, (byte)b }).ToArray();
        }
        private byte[] GetBytesToSend(int r, int g, int b, int note)
        {
            return (new byte[] { (byte)LightingMode.RGB, (byte)note, (byte)r, (byte)g, (byte)b }).ToArray();
        }


        private Pitch GetNoteFromCoords(int x, int y)
        {
            return ledToMidiNote(x, y);
        }

        public void massUpdateLEDsRectangle(int startX, int startY, int endX, int endY, int velo, int velo2 = 0, int velo3 = 0)
        {
            List<int> xs = new List<int>();
            List<int> ys = new List<int>();
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    xs.Add(x);
                    ys.Add(y);
                }
            }
            massUpdateLEDs(xs, ys, velo, velo2, velo3);
        }

        /// <summary>
        /// Sets a LED of the Launchpad.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="velo">The velocity.</param>
        public void setLED(int x, int y, int velo)
        {
            try
            {
                targetOutput.SendEvent(new NoteOnEvent((SevenBitNumber)(int)notes[x, y], (SevenBitNumber)velo)
                {
                    Channel = (FourBitNumber)0
                });
            }
            catch (Exception ex)
            {
                outError($"<< LAUNCHPAD.NET >> {nameof(Interface.setLED)} Midi.DeviceException {ex.Message}");
            }
        }

        public void setLED(int x, int y, int r, int g, int b)
        {
            var retx = GetBytesToSend(r, g, b, x, y);
            DoASend(retx);
        }

        public void setLEDFlash(int x, int y, int velo)
        {
            try
            {
                targetOutput.SendEvent(new NoteOnEvent((SevenBitNumber)(int)notes[x, y], (SevenBitNumber)velo)
                {
                    Channel = (FourBitNumber)1
                });
            }
            catch (Exception ex)
            {
                outError($"<< LAUNCHPAD.NET >> {nameof(Interface.setLEDFlash)} Midi.DeviceException {ex.Message}");
            }
        }

        public void setLEDPulse(int x, int y, int velo)
        {
            try
            {
                targetOutput.SendEvent(new NoteOnEvent((SevenBitNumber)(int)notes[x, y], (SevenBitNumber)velo)
                {
                    Channel = (FourBitNumber)2
                });
            }
            catch (Exception ex)
            {
                outError($"<< LAUNCHPAD.NET >> {nameof(Interface.setLEDPulse)} Midi.DeviceException {ex.Message}");
            }
        }

        /// <summary>
        /// Returns all connected and installed Launchpads.
        /// </summary>
        /// <returns>Returns LaunchpadDevice array.</returns>
        public static LaunchpadDevice[] getConnectedLaunchpads(Action<string> outInfo)
        {
            List<LaunchpadDevice> tempDevices = new List<LaunchpadDevice>();

            var a = OutputDevice.GetAll().ToList();
            var b = InputDevice.GetAll().ToList();
            var namesO = OutputDevice.GetAll().Where(x => !x.Name.Contains("Synth")).Where(x => x.Name != "LPMiniMK3 MIDI").Select(x => x.Name).ToArray();
            var namesI = InputDevice.GetAll().Where(x => !x.Name.Contains("Synth")).Where(x => x.Name != "LPMiniMK3 MIDI").Select(x => x.Name).ToArray();

            bool NewMethod()
            {
                var alreadyO = tempDevices.Select(x => x._midiOut).ToArray();
                var alreadyI = tempDevices.Select(x => x._midiIn).ToArray();
                var outputDevices = OutputDevice.GetAll().Where(x => !alreadyO.Contains(x.Name)).ToArray();
                var inputDevices = InputDevice.GetAll().Where(x => !alreadyI.Contains(x.Name)).ToArray();
                foreach (InputDevice id in inputDevices)
                {
                    foreach (OutputDevice od in outputDevices)
                    {
                        if (id.Name == od.Name)
                        {
                            if (id.Name.ToLower().Contains("launchpad"))
                            {
                                tempDevices.Add(new LaunchpadDevice(id.Name));
                            }
                        }
                    }
                }
                string outName = String.Empty;
                string inName = String.Empty;
                foreach (InputDevice id in inputDevices)
                {
                    outInfo(id.Name);
                    var name = id.Name.ToLower();
                    outInfo(name);
                    if (name.Contains("lpminimk3") && name.Contains("midiin"))
                    {
                        outInfo($"{id.Name} ");
                        inName = id.Name;
                        break;
                    }
                }
                foreach (OutputDevice od in outputDevices)
                {
                    var name = od.Name.ToLower();
                    if (name.Contains("lpminimk3") && name.Contains("midiout"))
                    {
                        outName = od.Name;
                        break;
                    }
                }
                if (!String.IsNullOrWhiteSpace(outName) && !String.IsNullOrWhiteSpace(inName))
                {
                    tempDevices.Add(new LaunchpadDevice(outName, inName));
                    return true;
                }
                else
                {
                    return false;
                }
            }
            while (NewMethod()) { }

            return tempDevices.OrderBy(x => x._midiIn).ToArray();
        }

        /// <summary>
        /// Function to connect with a LaunchpadDevice
        /// </summary>
        /// <param name="device">The Launchpad to connect to.</param>
        /// <returns>Returns bool if connection was successful.</returns>
        public bool connect(LaunchpadDevice device)
        {
            string inName = String.Empty;
            string outName = String.Empty;
            if (device._isLegacy)
            {
                inName = device._midiName.ToLower();
                outName = inName;
                sysexHeader = new byte[] { 00, 32, 41, 2, 24 };
                IsLegacy = true;
            }
            else
            {
                inName = device._midiIn.ToLower();
                outName = device._midiOut.ToLower(); ;
                sysexHeader = new byte[] { 00, 32, 41, 2, 13 };
                IsLegacy = false;
            }
            foreach (InputDevice id in InputDevice.GetAll())
            {
                if (id.Name.ToLower() == inName)
                {
                    targetInput = id;
                    targetInput.EventReceived += midiPress;
                    targetInput.StartEventsListening();
                }
            }
            foreach (OutputDevice od in OutputDevice.GetAll())
            {
                if (od.Name.ToLower() == outName)
                {
                    targetOutput = od;
                    od.PrepareForEventsSending();
                }
            }
            Connected = targetInput.IsListeningForEvents && targetOutput.IsEnabled;
            return Connected;
        }
        /// <summary>
        /// Disconnects a given LaunchpadDevice
        /// </summary>
        /// <param name="device">The Launchpad to disconnect.</param>
        /// <returns>Returns bool if disconnection was successful.</returns>
        public bool disconnect(LaunchpadDevice device)
        {
            if (targetInput.IsListeningForEvents && targetOutput.IsEnabled)
            {
                targetInput.StopEventsListening();
                targetInput.Dispose();
                targetOutput.Dispose();
            }
            Connected = !targetInput.IsListeningForEvents && !targetOutput.IsEnabled;
            return Connected;
        }


        public class LaunchpadDevice
        {
            public string Describe() => $"{_midiName} {_midiIn} {_midiOut} {_isLegacy}";
            public string _midiName;
            //public int _midiDeviceId;
            public string _midiOut;
            public string _midiIn;
            public bool _isLegacy;

            public LaunchpadDevice(string name)
            {
                _midiName = name;
                _isLegacy = true;
            }

            public LaunchpadDevice(string outName, string inName)
            {
                _midiOut = outName;
                _midiIn = inName;
                _isLegacy = false;
            }
        }
    }
}