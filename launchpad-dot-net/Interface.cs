using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Midi;
using Midi.Devices;
using Midi.Enums;
using Midi.Messages;

namespace LaunchpadNET
{

    public enum LightingMode { Set, Flash, Pulse, RGB };

    public class Interface
    {
        private Pitch[,] notes = new Pitch[8, 8] {
            { Pitch.A5, Pitch.ASharp5, Pitch.B5, Pitch.C6, Pitch.CSharp6, Pitch.D6, Pitch.DSharp6, Pitch.E6 },
            { Pitch.B4, Pitch.C5, Pitch.CSharp5, Pitch.D5, Pitch.DSharp5, Pitch.E5, Pitch.F5, Pitch.FSharp5 },
            { Pitch.CSharp4, Pitch.D4, Pitch.DSharp4, Pitch.E4, Pitch.F4, Pitch.FSharp4, Pitch.G4, Pitch.GSharp4 },
            { Pitch.DSharp3, Pitch.E3, Pitch.F3, Pitch.FSharp3, Pitch.G3, Pitch.GSharp3, Pitch.A3, Pitch.ASharp3 },
            { Pitch.F2, Pitch.FSharp2, Pitch.G2, Pitch.GSharp2, Pitch.A2, Pitch.ASharp2, Pitch.B2, Pitch.C3 },
            { Pitch.G1, Pitch.GSharp1, Pitch.A1, Pitch.ASharp1, Pitch.B1, Pitch.C2, Pitch.CSharp2, Pitch.D2 },
            { Pitch.A0, Pitch.ASharp0, Pitch.B0, Pitch.C1, Pitch.CSharp1, Pitch.D1, Pitch.DSharp1, Pitch.E1 },
            { Pitch.BNeg1, Pitch.C0, Pitch.CSharp0, Pitch.D0, Pitch.DSharp0, Pitch.E0, Pitch.F0, Pitch.FSharp0 }
        };

        private Pitch[] rightLEDnotes = new Pitch[] {
            Pitch.F6, Pitch.G5, Pitch.A4, Pitch.B3, Pitch.CSharp3, Pitch.DSharp2, Pitch.F1, Pitch.G0
        };

        public InputDevice targetInput;
        public OutputDevice targetOutput;

        public delegate void LaunchpadKeyEventHandler(object source, LaunchpadKeyEventArgs e);

        public delegate void LaunchpadCCKeyEventHandler(object source, LaunchpadCCKeyEventArgs e);

        public delegate void LaunchPadKeyDownHandler(object source, LaunchpadKeyEventArgs e);
        public delegate void LaunchPadKeyUpHandler(object source, LaunchpadKeyEventArgs e);

        /// <summary>
        /// Event Handler when a Launchpad Key is pressed.
        /// </summary>
        public event LaunchpadKeyEventHandler OnLaunchpadKeyPressed;
        public event LaunchpadCCKeyEventHandler OnLaunchpadCCKeyPressed;
        public event LaunchPadKeyDownHandler OnLaunchpadKeyDown;
        public event LaunchPadKeyUpHandler OnLaunchpadKeyUp;

        private byte[] sysexHeader;

        public bool Connected { get; set; }

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
        public void createTextScrollMiniMk3(string text, int speed, bool looping, int velo) {
            //order of the args is diff for this unit
            byte[] sysexStop = { 247 };
            byte operation = 7;
            byte _velocity = (byte)velo;
            byte _speed = (byte)speed;
            byte _loop = Convert.ToByte(looping);
            byte[] _text = { };
            byte[] finalArgs = { operation, _loop, _speed, 0, _velocity };
            List<byte> charList = new List<byte>();
            foreach (char c in text) {
                int unicode = c;
                if (unicode < 128)
                    charList.Add(Convert.ToByte(unicode));
            }
            _text = charList.ToArray();
            byte[] finalBytes = sysexHeader.Concat(finalArgs.Concat(_text.Concat(sysexStop))).ToArray();

            targetOutput.SendSysEx(finalBytes);
        }

        public void createTextScrollMiniMk3RGB(string text, int speed, bool looping, int r, int g, int b) {
            //order of the args is diff for this unit
            byte[] sysexStop = { 247 };
            byte operation = 7;
            byte[] _text = { };
            byte[] finalArgs = { operation, Convert.ToByte(looping), (byte)speed, 1, (byte)r, (byte)g, (byte)b };
            List<byte> charList = new List<byte>();
            foreach (char c in text) {
                int unicode = c;
                if (unicode < 128)
                    charList.Add(Convert.ToByte(unicode));
            }
            _text = charList.ToArray();
            byte[] finalBytes = sysexHeader.Concat(finalArgs.Concat(_text.Concat(sysexStop))).ToArray();

            targetOutput.SendSysEx(finalBytes);
        }

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
            foreach(char c in text)
            {
                int unicode = c;
                if (unicode < 128)
                    charList.Add(Convert.ToByte(unicode));
            }
            _text = charList.ToArray();

            byte[] finalBytes = sysexHeader.Concat(finalArgs.Concat(_text.Concat(sysexStop))).ToArray();

            targetOutput.SendSysEx(finalBytes);
        }

        public void stopLoopingTextScroll() {
            byte[] stop = sysexHeader.Concat(new byte[]{20, 247}).ToArray();
            targetOutput.SendSysEx(stop);
        }

        public void stopLoopingTextScrollMiniMk3() {
            byte[] stop = sysexHeader.Concat(new byte[] { 7, 247 }).ToArray();
            targetOutput.SendSysEx(stop);
        }

        private void sysExAnswer(SysExMessage m)
        {
            byte[] msg = m.Data;
            byte[] stopBytes = sysexHeader.Concat(new byte[]{ 21, 247 }).ToArray();
        }

        private void midiPress(NoteOnMessage msg)
        {
            if (!rightLEDnotes.Contains(msg.Pitch)) {
                if (OnLaunchpadKeyPressed != null) {
                    OnLaunchpadKeyPressed(this, new LaunchpadKeyEventArgs(midiNoteToLed(msg.Pitch)[0], midiNoteToLed(msg.Pitch)[1]));                    
                }
                if (OnLaunchpadKeyUp != null && msg.Velocity == 0) {
                    OnLaunchpadKeyUp(this, new LaunchpadKeyEventArgs(midiNoteToLed(msg.Pitch)[0], midiNoteToLed(msg.Pitch)[1]));
                }
                if (OnLaunchpadKeyDown != null && msg.Velocity == 127) {
                    OnLaunchpadKeyDown(this, new LaunchpadKeyEventArgs(midiNoteToLed(msg.Pitch)[0], midiNoteToLed(msg.Pitch)[1]));
                }
            } else if (OnLaunchpadKeyPressed != null && rightLEDnotes.Contains(msg.Pitch)) {
                OnLaunchpadCCKeyPressed(this, new LaunchpadCCKeyEventArgs(midiNoteToSideLED(msg.Pitch)));
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
            for (int x = 0; x <= 7; x++)
            {
                for (int y = 0; y <= 7; y++)
                {
                    if (notes[x,y] == p)
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
        public Pitch ledToMidiNote(int x, int y)
        {
            return notes[x, y];
        }

        public void clearAllLEDs()
        {
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    setLED(x, y, 0);
                }
            }

            for (int ry = 0; ry < 8; ry++)
            {
                setSideLED(ry, 0);
            }

            for (int tx = 1; tx < 9; tx++)
            {
                try {
                    setTopLEDs(tx, 0);
                } catch {

                }
            }
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
            byte[] data = sysexHeader.Concat(new byte[]{ 10, Convert.ToByte(103+x), Convert.ToByte(velo), 247 }).ToArray();
            targetOutput.SendSysEx(data);
        }

        /// <summary>
        /// Sets a Side LED of the Launchpad.
        /// </summary>
        /// <param name="y">The height of the right Side LED.</param>
        /// <param name="velo">Velocity index.</param>
        public void setSideLED(int y, int velo) {
            targetOutput.SendNoteOn(Channel.Channel1, rightLEDnotes[y], velo);
        }

        public void setSideLEDFlash(int y, int velo) {
            targetOutput.SendNoteOn(Channel.Channel2, rightLEDnotes[y], velo);
        }

        public void setSideLEDPulse(int y, int velo) {
            targetOutput.SendNoteOn(Channel.Channel3, rightLEDnotes[y], velo);
        }

        public void massUpdateLEDs(List<int> xs, List<int> ys, int velo, LightingMode mode = LightingMode.Set, int velo2 = 0, int velo3 = 0) {
            if (xs.Count != ys.Count)
                throw new Exception("count of xs and ys does not match");
            byte[] sendbytes = sysexHeader.Concat(new byte[] { 3 }).ToArray();
            for(int i = 0; i < xs.Count; i++) {
                var note = ledToMidiNote(xs[i], ys[i]);
                if (mode == LightingMode.Flash)
                    sendbytes = sendbytes.Concat(new byte[] { (byte)mode, (byte)note, (byte)velo, (byte)velo2 }).ToArray();
                else if (mode == LightingMode.RGB)
                    sendbytes = sendbytes.Concat(new byte[] { (byte)mode, (byte)note, (byte)velo, (byte)velo2, (byte)velo3 }).ToArray();
                else
                    sendbytes = sendbytes.Concat(new byte[] { (byte)mode, (byte)note, (byte)velo }).ToArray();
            }
            sendbytes = sendbytes.Concat(new byte[] { 247 }).ToArray();
            targetOutput.SendSysEx(sendbytes);
        }

        public void massUpdateLEDsRectangle(int startX, int startY, int endX, int endY, int velo, LightingMode mode = LightingMode.Set, int velo2 = 0, int velo3 = 0) {
            List<int> xs = new List<int>();
            List<int> ys = new List<int>();
            for (int x = startX; x <= endX; x++){
                for (int y = startY; y <= endY; y++) {
                    xs.Add(x);
                    ys.Add(y);
                }
            }
            massUpdateLEDs(xs, ys, velo, mode, velo2, velo3);
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
                targetOutput.SendNoteOn(Channel.Channel1, notes[x, y], velo);
            }
            catch (DeviceException)
            {
                Console.WriteLine("<< LAUNCHPAD.NET >> Midi.DeviceException");
                throw;
            }
        }

        public void setLEDFlash(int x, int y, int velo) {
            try {
                targetOutput.SendNoteOn(Channel.Channel2, notes[x, y], velo);
            } catch (DeviceException) {
                Console.WriteLine("<< LAUNCHPAD.NET >> Midi.DeviceException");
                throw;
            }
        }

        public void setLEDPulse(int x, int y, int velo) {
            try {
                targetOutput.SendNoteOn(Channel.Channel3, notes[x, y], velo);
            } catch (DeviceException) {
                Console.WriteLine("<< LAUNCHPAD.NET >> Midi.DeviceException");
                throw;
            }
        }

        /// <summary>
        /// Returns all connected and installed Launchpads.
        /// </summary>
        /// <returns>Returns LaunchpadDevice array.</returns>
        public LaunchpadDevice[] getConnectedLaunchpads()
        {
            List<LaunchpadDevice> tempDevices = new List<LaunchpadDevice>();

            if (DeviceManager.InputDevices.Where(d => d.Name.ToLower().Contains("launchpad")).Any()) {
                //legacy search.
                foreach (InputDevice id in DeviceManager.InputDevices) {
                    foreach (OutputDevice od in DeviceManager.OutputDevices) {
                        if (id.Name == od.Name) {
                            if (id.Name.ToLower().Contains("launchpad")) {
                                tempDevices.Add(new LaunchpadDevice(id.Name));
                            }
                        }
                    }
                }
            } else {
                string outName = String.Empty;
                string inName = String.Empty;
                foreach (InputDevice id in DeviceManager.InputDevices) {
                    if (id.Name.ToLower().Contains("lpminimk3") && id.Name.ToLower().Contains("midiin")) {
                        inName = id.Name;
                        break;
                    }
                }
                foreach (OutputDevice od in DeviceManager.OutputDevices) {
                    if (od.Name.ToLower().Contains("lpminimk3") && od.Name.ToLower().Contains("midiout")) {
                        outName = od.Name;
                        break;
                    }
                }
                if (!String.IsNullOrWhiteSpace(outName) && !String.IsNullOrWhiteSpace(inName))
                    tempDevices.Add(new LaunchpadDevice(outName, inName));
            }

            return tempDevices.ToArray();
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
            if (device._isLegacy) {
                inName = device._midiName.ToLower();
                outName = inName;
                sysexHeader = new byte[] { 240, 00, 32, 41, 2, 24 };
            } else {
                inName = device._midiIn.ToLower();
                outName = device._midiOut.ToLower(); ;
                sysexHeader = new byte[] { 240, 00, 32, 41, 2, 13 };
            }
            foreach (InputDevice id in DeviceManager.InputDevices)
            {                
                if (id.Name.ToLower() == inName) {
                    targetInput = id;
                    id.Open();
                    targetInput.NoteOn += new NoteOnHandler(midiPress);
                    targetInput.StartReceiving(null);
                }                
            }
            foreach (OutputDevice od in DeviceManager.OutputDevices)
            {
                if (od.Name.ToLower() == outName)
                {
                    targetOutput = od;
                    od.Open();
                }
            }
            Connected = targetInput.IsOpen && targetOutput.IsOpen;
            return Connected;
        }
        /// <summary>
        /// Disconnects a given LaunchpadDevice
        /// </summary>
        /// <param name="device">The Launchpad to disconnect.</param>
        /// <returns>Returns bool if disconnection was successful.</returns>
        public bool disconnect(LaunchpadDevice device)
        {
            if (targetInput.IsOpen && targetOutput.IsOpen)
            {
                targetInput.StopReceiving();
                targetInput.Close();
                targetOutput.Close();
            }
            Connected = !targetInput.IsOpen && !targetOutput.IsOpen;
            return Connected;
        }

        public class LaunchpadDevice {
            public string _midiName;
            //public int _midiDeviceId;
            public string _midiOut;
            public string _midiIn;
            public bool _isLegacy;

            public LaunchpadDevice(string name) {
                _midiName = name;
                _isLegacy = false;
            }

            public LaunchpadDevice(string outName, string inName) {
                _midiOut = outName;
                _midiIn = inName;
                _isLegacy = false;
            }
        }
    }
}
