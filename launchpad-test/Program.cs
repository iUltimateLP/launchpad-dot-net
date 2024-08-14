using static LaunchpadNET.Interface;

var logAction = delegate (string name)
{
    Console.WriteLine(name);
};

var lp = new LaunchpadNET.Interface(logAction, logAction, logAction);

try
{
    //retrieve all launchpads connected to the computer.
    var launchpadList = LaunchpadNET.Interface.getConnectedLaunchpads(logAction);

    if (launchpadList.Any())
    {
        //connect to the first launchpad found.
        lp.connect(launchpadList[0]);
        try
        {
            //prepare launchpaf for our input
            lp.SetMode(LaunchpadNET.LaunchpadMode.Programmer);
            //wipe any active LEDs.
            lp.clearAllLEDs();

            //Create a scrolling text of "Hello World" across the pad. If the launchpad is a MiniMk3, we can utilise full RGB to display the 
            //text in any colour.
            Console.WriteLine("Text loop demo");
            if (lp.IsLegacy)
            {
                lp.createTextScroll("Hello World", 25, true, 125);
            }
            else
            {
                lp.createTextScrollMiniMk3RGB("Hello World", 25, true, 0xFF, 0x00, 0xFB);
            }
            Console.WriteLine("Press enter to continue");
            Console.ReadLine();
            if (lp.IsLegacy)
            {
                lp.stopLoopingTextScroll();
            }
            else
            {
                lp.stopLoopingTextScrollMiniMk3();
            }

            Console.WriteLine("Static LED demo");
            lp.setLED(0, 8, 21);
            lp.setLED(0, 1, 5);
            lp.setLED(0, 0, 74);
            Console.WriteLine("Press enter to continue");
            Console.ReadLine();

            lp.setClock(60);

            Console.WriteLine("Pulsing LED demo");
            lp.setLEDPulse(0, 8, 5);
            lp.setLEDPulse(0, 1, 74);
            lp.setLEDPulse(0, 0, 21);
            Console.WriteLine("Press enter to continue");
            Console.ReadLine();

            Console.WriteLine("Flashing LED demo");
            lp.setLEDFlash(0, 8, 74);
            lp.setLEDFlash(0, 1, 21);
            lp.setLEDFlash(0, 0, 5);
            Console.WriteLine("Press enter to continue");
            Console.ReadLine();

            Console.WriteLine("Clock Change");
            lp.setClock(220);
            Console.WriteLine("Press enter to continue");
            Console.ReadLine();

            Console.WriteLine("RGB Demo");
            lp.setLED(0, 0, 0, 125, 255);
            Console.WriteLine("Press enter to continue");
            Console.ReadLine();

            Console.WriteLine("Clear all LEDs");
            lp.clearAllLEDs();
            Console.WriteLine("Press enter to continue");
            Console.ReadLine();

            Console.WriteLine("Mass update LEDs");
            lp.massUpdateLEDsRectangle(0, 0, 8, 8, 125);
            Console.WriteLine("Press enter to continue");
            Console.ReadLine();

            lp.clearAllLEDs();
            Console.WriteLine("Press a key to activate it");
            lp.OnLaunchpadKeyDown += KeyPressed;
            Console.WriteLine("Press enter to continue");
            Console.ReadLine();
        }
        finally
        {
            //make sure we clean up after we're finished!
            lp.disconnect(launchpadList[0]);
        }
    }
    else
    {
        Console.WriteLine("No launchpads found!");
    }
}
catch (Exception ex)
{
    Console.WriteLine("Failed to retrieve and connect to launchpads with error: " + ex.Message);
}

void KeyPressed(object source, LaunchpadKeyEventArgs e)
{
    logAction($"launchpad button x:{e.GetX()}, y:{e.GetY()} fired");
    lp.setLED(e.GetX(), e.GetY(), 125);
}